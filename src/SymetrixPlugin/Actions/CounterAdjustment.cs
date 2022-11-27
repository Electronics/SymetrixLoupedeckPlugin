namespace Loupedeck.SymetrixPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class CounterAdjustment : PluginDynamicAdjustment
    {
        // This variable holds the current values of the controls (so we don't have to constantly look up the current values).
        private Dictionary<int,int> values = new Dictionary<int, int>();
        private DateTime lastUpdated;

        public const int VALUE_INCREMENT = 50;

        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public CounterAdjustment() : base(displayName: "Volume", description: "Sets symetrix volume", groupName: "Adjustments", hasReset: true) {
            this.MakeProfileAction("text"); // this has to be "text" to add a parameter, but you then can't change the name of it at all?...
            //this.AddParameter("HELLO", "Control Number", "text");
            //TODO: add default value parameter
            if (!SymetrixPlugin.symetrixInterface.isConnected()) {
                SymetrixPlugin.symetrixInterface.connect();
            }
        }

        private void updateValue(int controllerNumber) {
			var value = SymetrixPlugin.symetrixInterface.getControl(controllerNumber);
			if (value > 0) this.values[controllerNumber] = value;
		}
        private void updateValues() {
            foreach (var kvp in values) {
                var value = SymetrixPlugin.symetrixInterface.getControl(kvp.Key);
                if (value > 0) this.values[kvp.Key] = value;
            }
        }
        private void checkInitValue(int controlNumber) {
            int dummy;
            if (this.values.TryGetValue(controlNumber, out dummy)) {
                return;
            } else {
                this.values[controlNumber] = -1; // give it at least something to start with
                updateValue(controlNumber);
            }
        }
        private int checkInitValue(string s) {
			int controlNumber;
			try {
				controlNumber = int.Parse(s);
			} catch (FormatException) {
				Debug.WriteLine($"Invalid control number: {s}");
                return -1;
			}
			checkInitValue(controlNumber);
			return controlNumber;
		}

        // This method is called when the dial associated to the plugin is rotated.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff) {
            int controlNumber = checkInitValue(actionParameter);
            if (controlNumber < 0) return;

			// update the local copy of the values if we haven't changed in a while (don't do it every update otherwise it will slow down)
			var now = DateTime.Now;
            if (now>lastUpdated + new TimeSpan(0,0,2)) {
                updateValue(controlNumber);
            }
			lastUpdated = now; // reset every time it's moved
            this.values[controlNumber] += diff * VALUE_INCREMENT; // Increase or decrease the counter by the number of ticks.
            if (this.values[controlNumber] < 0) this.values[controlNumber] = 0;
            if (this.values[controlNumber] > 65535) this.values[controlNumber] = 65535;

            if (SymetrixPlugin.symetrixInterface.setControl(controlNumber, this.values[controlNumber])) {
                this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            } else {
                this.values[controlNumber] -= diff * VALUE_INCREMENT; // undo the change - it didn't actually apply
            }
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter) {
			int controlNumber = checkInitValue(actionParameter);
			if (controlNumber < 0) return;

			this.values[controlNumber] = SymetrixPlugin.symetrixInterface.dBtoValue(-30);
			SymetrixPlugin.symetrixInterface.setControl(60, this.values[controlNumber]);
			this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override String GetAdjustmentValue(String actionParameter) {
			int controlNumber = checkInitValue(actionParameter);
			if (controlNumber < 0) return "";

			//updateValues();
			return $"{SymetrixPlugin.symetrixInterface.valueToDb(this.values[controlNumber]):F1} dB";
        }
    }
}
