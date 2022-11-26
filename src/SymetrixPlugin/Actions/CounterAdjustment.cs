namespace Loupedeck.SymetrixPlugin
{
    using System;
    using System.Threading;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class CounterAdjustment : PluginDynamicAdjustment
    {
        // This variable holds the current value of the counter.
        private int mainVolume;
        private DateTime lastUpdated;

        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public CounterAdjustment() : base(displayName: "Volume", description: "Sets symetrix volume", groupName: "Adjustments", hasReset: true) {
            
            this.MakeProfileAction("text;Test test Test:");
            if (!SymetrixPlugin.symetrixInterface.isConnected()) {
                SymetrixPlugin.symetrixInterface.connect();
            } else {
                updateValues();
            }
        }

        private void updateValues() {
            var value = SymetrixPlugin.symetrixInterface.getControl(60);
            if (value > 0) this.mainVolume = value;
        }

        // This method is called when the dial associated to the plugin is rotated.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            // update the local copy of the values if we haven't changed in a while (don't do it every update otherwise it will slow down)
            var now = DateTime.Now;
            if (now>lastUpdated + new TimeSpan(0,0,2)) {
                updateValues();
            }
			lastUpdated = now; // reset every time it's moved
			this.mainVolume += diff*50; // Increase or decrease the counter by the number of ticks.
            if (this.mainVolume < 0) this.mainVolume = 0;
            if (this.mainVolume > 65535) this.mainVolume = 65535;

            if (SymetrixPlugin.symetrixInterface.setControl(60, this.mainVolume)) {
                this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            } else {
                this.mainVolume-= diff*50; // undo the change - it didn't actually apply
            }
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter)
        {
            this.mainVolume = SymetrixPlugin.symetrixInterface.dBtoValue(-30);
			SymetrixPlugin.symetrixInterface.setControl(60, this.mainVolume);
			this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override String GetAdjustmentValue(String actionParameter) {
            //updateValues();
            return $"{SymetrixPlugin.symetrixInterface.valueToDb(this.mainVolume):F1}";
        }
    }
}
