﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.MSBuildEditor.Schema;

namespace MonoDevelop.MSBuildEditor.Language
{
	class PropertyValueCollector : IEnumerable<KeyValuePair<string, List<string>>>
	{
		Dictionary<string, List<string>> props = new Dictionary<string, List<string>> ();

		public PropertyValueCollector (bool collectTargetFrameworks)
		{
			if (collectTargetFrameworks) {
				Mark ("TargetFramework");
				Mark ("TargetFrameworks");
				Mark ("_ShortFrameworkVersion");
				Mark ("_ShortFrameworkIdentifier");
				Mark ("TargetFrameworkIdentifier");
				Mark ("TargetFrameworkVersion");
				Mark ("TargetFrameworkProfile");
				Mark ("TargetFrameworkMoniker");
			}
		}

		public IEnumerable<List<string>> Values => props.Values;

		public void Collect (string name, string value)
		{
			if (string.IsNullOrEmpty (value)) {
				return;
			}
			if (props.TryGetValue (name, out List<string> values)) {
				if (values == null) {
					props [name] = values = new List<string> ();
				}
				values.Add (value);
			}
		}

		public void Mark (string name)
		{
			if (!props.ContainsKey (name)) {
				props [name] = null;
			}
		}

		public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator ()
		{
			return props.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return props.GetEnumerator ();
		}

		internal bool TryGetValues (string name, out List<string> values)
		{
			return props.TryGetValue (name, out values) && values != null && values.Count > 0;
		}

		public List<FrameworkReference> GetFrameworks ()
		{
			var list = new List<FrameworkReference> ();
			if (TryGetValues ("TargetFrameworks", out List<string> multiFxList)) {
				foreach (var multiFxStr in multiFxList) {
					if (multiFxStr != null && IsConstExpr (multiFxStr)) {
						var multiFxArr = multiFxStr.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var fxstr in multiFxArr) {
							var fx = FrameworkReference.FromShortName (fxstr);
							if (fx != null) {
								list.Add (fx);
							}
						}
					}
				}
				if (list.Count > 0) {
					return list;
				}
			}
			if (TryGetValues ("TargetFramework", out List<string> fxList)) {
				foreach (var fxstr in fxList) {
					if (IsConstExpr (fxstr)) {
						var fx = FrameworkReference.FromShortName (fxstr);
						if (fx != null) {
							list.Add (fx);
						}
					}
				}
			}

			if (TryGetValues ("TargetFrameworkIdentifier", out List<string> idList) && TryGetValues ("TargetFrameworkVersion", out List<string> versionList)) {
				var id = idList.FirstOrDefault (IsConstExpr);
				var version = versionList.FirstOrDefault (v => {
					if (v [0] == 'v') {
						v = v.Substring (1);
					}
					return IsConstExpr (v) && Version.TryParse (v, out _);
				});

				if (!string.IsNullOrEmpty (version) && !string.IsNullOrEmpty (id)) {
					if (TryGetValues ("TargetFrameworkProfile", out List<string> profileList)) {
						var profile = profileList.FirstOrDefault (IsConstExpr);
						if (profile != null) {
							list.Add (new FrameworkReference (id, version, null, profileList[0]));
							return list;
						}
					}
					list.Add (new FrameworkReference (id, version, null, null));
					return list;
				}
			}

			return list;

			bool IsConstExpr (string p) => p.IndexOf ('$') < 0;
		}

	}
}