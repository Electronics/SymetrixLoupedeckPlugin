namespace Loupedeck.SymetrixPlugin {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class SymetrixInterface {

        private TcpClient client;
        public NetworkStream stream;
        private SymetrixPlugin parent;
        private string lastException;

        private BlockingCollection<WriteQueueItem> writeQueue;
        private Thread readWriteThread;

        private bool runThread = true;

        public SymetrixInterface(SymetrixPlugin parent) {
            this.parent = parent;
            this.writeQueue = new BlockingCollection<WriteQueueItem>();
            readWriteThread = new Thread(readWriteLoop);
            readWriteThread.Start();
        }

        private class WriteQueueItem {
            public string data;
            public AutoResetEvent eventHandle;
            public object returnData;
            public WriteQueueItem(string data) {
                this.data = data;
            }
            public WriteQueueItem(string data, AutoResetEvent eventHandle) {
                this.data = data;
                this.eventHandle = eventHandle;
            }
        }

        public void connect() {
            if (isConnected()) return;
            try {
                this.client = new TcpClient("10.0.1.54", 48631);
                this.stream = this.client.GetStream();
                parent.OnPluginStatusChanged(PluginStatus.Normal, "Connected");

                // clear writeQueue, waking up anything waiting on it still
                while (writeQueue.Count > 0) {
                    var item = writeQueue.Take();
                    item.returnData = null;
                }

            } catch(Exception e) {
                this.lastException = e.ToString();
                parent.OnPluginStatusChanged(PluginStatus.Error, e.ToString());
            }
        }

        public void Dispose() {
            this.runThread = false;
            this.stream.Close();
            this.stream.Dispose();
            this.client.Close();
            this.client.Dispose();
        }

        public bool isConnected() {
            bool isConnected = (this.client?.Connected == true);
            if (!isConnected) {
                parent.OnPluginStatusChanged(PluginStatus.Error, String.IsNullOrEmpty(this.lastException) ? "Connection Failed" : this.lastException);
			}

			return isConnected;
        }

        public int getControl(int controllerNum) {
			var readBlock = new AutoResetEvent(false);
			var item = new WriteQueueItem($"GS {controllerNum}\r", readBlock);
			Debug.WriteLine("getControl> Adding item to queue");
			this.writeQueue.Add(item);
			if (readBlock.WaitOne(500)) {
                Debug.WriteLine($"getControl> Got something in return from thread! {item.returnData}");
                if (item.returnData != null) return (int)item.returnData;
            } else {
                Debug.WriteLine("getControl> Timeout");
                item.data = null; // if the item gets picked up later, it will be skipped
                return -1;
            }
            return -1;
        }

        public bool setControl(int controllerNum, int value) {
			var block = new AutoResetEvent(false);
            var item = new WriteQueueItem($"CSQ {controllerNum} {value}\r", block);
			Debug.WriteLine("setControl> Adding item to queue");
			this.writeQueue.Add(item);
			if (block.WaitOne(500) && (bool)item.returnData) {
				Debug.WriteLine("setControl> Sucessfully set!");
                return true;
			} else {
				Debug.WriteLine("setControl> Timeout or fail to set");
				item.data = null; // if the item gets picked up later, it will be skipped
				return false;
			}
        }

        private bool _write(string d) {
            try {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(d);
                this.stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", data);
                return true;
            } catch (System.IO.IOException) {
                Debug.WriteLine("_write> Failed to write to tcp stream");
            }
            return false;
		}

        private string _read() {
			Byte[] data = new Byte[16];
			try {
				Int32 bytes = stream.Read(data, 0, data.Length);
				string responseData = System.Text.Encoding.ASCII.GetString(data);
				Console.WriteLine($"Received: {responseData}");
				return responseData;
			} catch (System.IO.IOException) {
                Debug.WriteLine("_read> Failed to receive data from tcp stream");
			}
            return null;
		}

        private void readWriteLoop() {
            Debug.WriteLine("Starting read/write thread");
            while (this.runThread) {
                var item = this.writeQueue.Take();
                Debug.WriteLine($"New item for read/write! {item.data}");

                // check connection
                if (!this.isConnected()) {
                    this.connect();
                }

                if (item != null) {
                    // if we have an item, write to the Symetrix and read back the ACK / data
                    this._write(item.data);
                    var retstr = this._read();
                    Debug.WriteLine($"readWriteLoop> got {retstr} back from {item.data}");
                    if (!string.IsNullOrEmpty(retstr)) {
                        // if we got something back
                        // depending on whether on what type of command it was, we need to parse the response differently
                        switch(item.data.Substring(0,2).Trim()) {
                            case "GS":
                                // Get
								try {
									item.returnData = int.Parse(retstr);
								} catch (System.FormatException) {
                                    item.returnData = -1;
								}
                                break;
                            case "CS":
								// Set (+ quick set CSQ)
								if (retstr.Trim('\0').Trim() == "ACK") {
                                    item.returnData = true;
								} else {
                                    item.returnData = false;
								}
                                break;
							default:
                                break; // leave returnData as null
						}
                    } else {
                        Debug.WriteLine($"readWriteLoop> Empty return from Symetrix");
                    }
                    item.eventHandle.Set(); // inform the process that put the item on the queue that a response has been parsed
                } else {
                    Debug.WriteLine("item from writeQueue was NULL???");
                }
            }
            Debug.WriteLine("Exiting read/write thread");
        }

        public float valueToDb(int value, float minValue=-72, float maxValue=12) {
            return minValue + (maxValue - minValue) * (float)value / 65535;
        }
        public int dBtoValue(float db, float minValue=-72, float maxValue=12) {
            return (int)((db-minValue)*65535/(maxValue- minValue));
        }
    }
}
