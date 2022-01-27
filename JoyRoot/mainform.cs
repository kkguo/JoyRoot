using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace JoyRoot
{
    public partial class mainform : Form {
        private Dictionary<ulong, RootDevice> rootList = new Dictionary<ulong, RootDevice>();

        KeyboardListener KeyboardListener = new KeyboardListener();

        public mainform()
        {
            InitializeComponent();
        }

        private void mainform_Load(object sender, EventArgs e) {
            KeyboardListener.KeyDown += new RawKeyEventHandler(Kebyaord_KeyDown);
            KeyboardListener.KeyUp += new RawKeyEventHandler(Keyboard_KeyUp);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            scanAdvertisement();
        }


        private void scanAdvertisement()
        {
            BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += OnAdvertisementReceived;
            watcher.Start();
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs) {
            RootDevice root;
            if (rootList.ContainsKey(eventArgs.BluetoothAddress)) { // this device has been scanned
                root = rootList[eventArgs.BluetoothAddress];                
                if (await root.isAvailible()) { //still availible 
                    return;
                } else { // lost connect
                    Debug.WriteLine("Seeing root disapear");
                    Root_Disconnected(root, new EventArgs());
                }
            } else if (eventArgs.Advertisement.ServiceUuids.Contains(RootDevice.guidRootIdentifierService)) {  // new device found                        
                var mdata = eventArgs.Advertisement.GetManufacturerDataByCompanyId(RootDevice.ManufacturerID);
                if (mdata.Count == 1) {
                    byte[] tmp = new byte[mdata[0].Data.Length];
                    string deviceType = DataReader.FromBuffer(mdata[0].Data).ReadString(3);
                    root = new RootDevice(deviceType);
                    await root.query(eventArgs.BluetoothAddress);
                    root.Disconnected += Root_Disconnected;
                    rootList.Add(eventArgs.BluetoothAddress, root);
                    var newitem = new ListViewItem(root.Name + " (" + root.Model.ToString() + ")");
                    Debug.WriteLine("found root:" + root.Name);
                    newitem.Tag = root;
                    listAvailibleRoot.Invoke((MethodInvoker)delegate
                    {
                        listAvailibleRoot.Items.Add(newitem);
                    });
                }

            }
        }

        private void Root_Disconnected(object sender, EventArgs e) {
            rootList.Remove(((RootDevice)sender).Address);
        }

        private async void connect(RootDevice root)
        {
            root.connect();
            label1.Invoke((MethodInvoker)delegate {
                label1.Text = root.SerialNumber;
            });

            btnUP.Enabled = true;
            btnDown.Enabled = true;
            btnLeft.Enabled = true;
            btnRight.Enabled = true;
            btnStop.Enabled = true;
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {

        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listAvailibleRoot.Items) {
                if (item.Checked) {
                    RootDevice root = (RootDevice)item.Tag;
                    root.moveBackward();
                }
            }
        }

        private void btnUP_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listAvailibleRoot.Items) {
                if (item.Checked) {
                    RootDevice root = (RootDevice)item.Tag;
                    root.moveForward();
                }
            }
        }

        private void btnLeft_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listAvailibleRoot.Items) {
                if (item.Checked) {
                    RootDevice root = (RootDevice)item.Tag;
                    root.turnLeft();
                }
            }
        }

        private void btnRight_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listAvailibleRoot.Items) {
                if (item.Checked) {
                    RootDevice root = (RootDevice)item.Tag;
                    root.turnRight(); 
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in listAvailibleRoot.Items) {
                    if (item.Checked) {
                        var color = colorDialog1.Color;
                        RootDevice root = (RootDevice)item.Tag;
                        root.setLed(color);                        
                        pictureBox1.BackColor = color;
                    }
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listAvailibleRoot.Items) {
                if (item.Checked) {
                    RootDevice root = (RootDevice)item.Tag;
                    root.stopMove();
                }
            }
        }

        private void mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var root in rootList.Values)
            {
                root.disconnect();
            }
        }

        private void Kebyaord_KeyDown(object sender, RawKeyEventArgs args) {
            if (args.Key == System.Windows.Input.Key.Up) {
                btnUP_Click(sender, new EventArgs());
            } else if (args.Key == System.Windows.Input.Key.Down) {
                btnDown_Click(sender, new EventArgs());
            } else if (args.Key == System.Windows.Input.Key.Left) {
                btnLeft_Click(sender, new EventArgs());
            } else if (args.Key == System.Windows.Input.Key.Right) {
                btnRight_Click(sender, new EventArgs());
            }
        }

        private void Keyboard_KeyUp(object sender, RawKeyEventArgs args) {
            if (args.Key == System.Windows.Input.Key.Up ||
                args.Key == System.Windows.Input.Key.Down ||
                args.Key == System.Windows.Input.Key.Left ||
                args.Key == System.Windows.Input.Key.Right) {
                btnStop_Click(sender, new EventArgs());
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void listAvailibleRoot_ItemChecked(object sender, ItemCheckedEventArgs e) {
            if (e.Item.Checked) {
                connect(((RootDevice)e.Item.Tag));
            } else {
                //((RootDevice)e.Item.Tag).disconnect();
            }
        }
    }
}
