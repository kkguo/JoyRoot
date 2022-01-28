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
        KeyboardListener KeyboardListener = new KeyboardListener();

        public mainform() {
            InitializeComponent();
            var header = new ColumnHeader();
            header.Width = listAvailibleRoot.Width;
            listAvailibleRoot.Columns.Add(header);
            //listAvailibleRoot.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void mainform_Load(object sender, EventArgs e) {
            KeyboardListener.KeyDown += new RawKeyEventHandler(Kebyaord_KeyDown);
            KeyboardListener.KeyUp += new RawKeyEventHandler(Keyboard_KeyUp);
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            RootDevice.DeviceFound += RootDevice_DeviceFound;
            RootDevice.Scan();
        }

        private void RootDevice_DeviceFound(object sender, EventArgs e) {
            RootDevice root = (RootDevice)sender;
            root.AdvertiseUpdate += RootDevice_AdvertiseUpdate;
            var newitem = new ListViewItem(" (" + root.Model.ToString() + ")");
            newitem.Tag = root;
            listAvailibleRoot.Invoke((MethodInvoker)delegate
            {
                listAvailibleRoot.Items.Add(newitem);
            });
        }

        private void RootDevice_AdvertiseUpdate(object o, EventArgs e) {
            listAvailibleRoot.Invoke((MethodInvoker)delegate
            {
                RootDevice root = (RootDevice)o;
                foreach (ListViewItem item in listAvailibleRoot.Items) {
                    if (item.Tag == root) {
                        item.Text = root.Name + " (" + root.Model.ToString() + ") " + root.Battery + "%";
                    }
                }
            });
        }

        private async void connect(RootDevice root)
        {
            await root.Connect();
            label1.Invoke((MethodInvoker)delegate {
                label1.Text = root.SerialNumber;
            });

            btnUP.Enabled = true;
            btnDown.Enabled = true;
            btnLeft.Enabled = true;
            btnRight.Enabled = true;
            btnStop.Enabled = true;
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
            foreach (var root in RootDevice.deviceList.Values)
            {
                root.Disconnect();
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
