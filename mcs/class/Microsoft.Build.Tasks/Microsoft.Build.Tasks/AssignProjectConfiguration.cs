//
// AssignProjectConfiguration.cs
//
// Author:
//   Marek Sieradzki (marek.sieradzki@gmail.com)
//   Ankit Jain (jankit@novell.com)
//
// (C) 2006 Marek Sieradzki
// Copyright 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Tasks {
	public class AssignProjectConfiguration : ResolveProjectBase {
	
		ITaskItem[]	assignedProjects;
		string		solutionConfigurationContents;
		ITaskItem[]	unassignedProjects;
	
		public AssignProjectConfiguration ()
		{
		}
		
		[MonoTODO]
		public override bool Execute ()
		{
			if (String.IsNullOrEmpty (solutionConfigurationContents))
				return true;

			XmlReader xr = null;
			Dictionary<Guid, string> guidToConfigPlatform = null;
			try {
				xr = XmlReader.Create (new StringReader (solutionConfigurationContents));
				guidToConfigPlatform = new Dictionary<Guid, string> ();

				xr.Read ();
				while (!xr.EOF) {
					xr.Read ();
					if (xr.NodeType != XmlNodeType.Element)
						continue;

					string guid_str = xr.GetAttribute ("Project");
					string config_str = xr.ReadString ();

					Guid guid;
					if (!String.IsNullOrEmpty (guid_str) && !String.IsNullOrEmpty (config_str) &&
						TryParseGuid (guid_str, out guid))
						guidToConfigPlatform [guid] = config_str;
				}
			} catch (XmlException xe) {
				Log.LogError ("XmlException while parsing SolutionConfigurationContents: {0}",
						xe.ToString ());

				return false;
			} finally {
				((IDisposable)xr).Dispose ();
			}

			List<ITaskItem> tempAssignedProjects = new List<ITaskItem> ();
			List<ITaskItem> tempUnassignedProjects = new List<ITaskItem> ();
			foreach (ITaskItem item in ProjectReferences) {
				string config;

				string guid_str = item.GetMetadata ("Project");

				Guid guid = Guid.Empty;
				if (!string.IsNullOrEmpty(guid_str) && !TryParseGuid (guid_str, out guid)) {
					Log.LogError ("Project reference '{0}' has invalid or missing guid for metadata 'Project'.",
							item.ItemSpec);
					return false;
				}

				if (guid != Guid.Empty && guidToConfigPlatform.TryGetValue (guid, out config)) {
					string [] parts = config.Split (new char [] {'|'}, 2);

					ITaskItem new_item = new TaskItem (item);

					new_item.SetMetadata ("SetConfiguration", "Configuration=" + parts [0]);
					new_item.SetMetadata ("SetPlatform", "Platform=" +
							((parts.Length > 1) ? parts [1] : String.Empty));

					tempAssignedProjects.Add (new_item);
				} else {
					Log.LogWarning ("Project reference '{0}' could not be resolved.",
							item.ItemSpec);
					tempUnassignedProjects.Add (item);
				}
			}

			assignedProjects = tempAssignedProjects.ToArray ();
			unassignedProjects = tempUnassignedProjects.ToArray ();

			return true;
		}

		bool TryParseGuid (string guid_str, out Guid guid)
		{
			guid = Guid.Empty;
			try {
				guid = new Guid (guid_str);
			} catch (ArgumentNullException) {
				return false;
			} catch (FormatException) {
				return false;
			} catch (OverflowException) {
				return false;
			}

			return true;
		}


		[Output]
		public ITaskItem[] AssignedProjects {
			get { return assignedProjects; }
			set { assignedProjects = value; }
		}
		
		public string SolutionConfigurationContents {
			get { return solutionConfigurationContents; }
			set { solutionConfigurationContents = value; }
		}
		
		[Output]
		public ITaskItem[] UnassignedProjects {
			get { return unassignedProjects; }
			set { unassignedProjects = value; }
		}
	}
}

