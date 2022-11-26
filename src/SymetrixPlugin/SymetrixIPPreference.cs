using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loupedeck.SymetrixPlugin {
	internal class SymetrixIPPreference : PluginPreference{

		public new string Value;

		public new string Name = "Symetrix IP";
		public SymetrixIPPreference(PluginPreferenceType type, string name) : base(type, name) {
			// I DON'T KNOW WHAT I'M DOING
			this.Value = "10.0.0.0.";
			this.DisplayName = name;
			this.IsRequired= true;
		}
	}
}
