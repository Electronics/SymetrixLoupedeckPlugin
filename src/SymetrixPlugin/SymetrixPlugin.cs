namespace Loupedeck.SymetrixPlugin
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class SymetrixPlugin : Plugin
    {
        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        public static SymetrixInterface symetrixInterface;

        public SymetrixPlugin() {
            symetrixInterface = new SymetrixInterface(this);
			//this.PluginPreferences.Add(new SymetrixIPPreference(PluginPreferenceType.None, "Something"));
		}

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
            String symetrixIP = String.Empty;
            if (!TryGetPluginSetting("symetrixIP", out symetrixIP)) {
                
            }
            symetrixInterface.connect();
        }

        

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
            symetrixInterface.Dispose();
        }
    }
}
