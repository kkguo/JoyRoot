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
using System.Drawing;
using Windows.Gaming.Input;

namespace JoyRoot
{
    public partial class mainform : Form {
        KeyboardListener KeyboardListener = new KeyboardListener();
        Gamepad joystick;

        public mainform() {
            InitializeComponent();
            
            imageList1.Images.Add("RT1",Properties.Resources.RT1);
            imageList1.Images.Add("RT0",Properties.Resources.RT0);
            var header = new ColumnHeader();
            header.Width = listAvailibleRoot.Width;
            listAvailibleRoot.Columns.Add(header);
            listAvailibleRoot.LargeImageList = imageList1;
            listAvailibleRoot.View = View.LargeIcon;
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
            newitem.ImageKey = root.Model.ToString();
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
            _ = Task.Run(() =>
            {
                while (true)
                {
                    UpdateJoystick(); 
                    System.Threading.Thread.Sleep(100);
                }
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
                RootDevice root = (RootDevice)e.Item.Tag;
                connect(root);
            } else {
                //((RootDevice)e.Item.Tag).disconnect();
            }
        }

        static bool joystickIsInControl=false;
        static int direction = 0;
        private void UpdateJoystick()
        {
            if (Gamepad.Gamepads.Count > 0)
            {
                joystick = Gamepad.Gamepads[0];

                GamepadReading reading = joystick.GetCurrentReading();
                if (reading.Buttons.HasFlag(GamepadButtons.DPadLeft))
                {
                    if (direction != 1)
                    {
                        btnLeft.Invoke((MethodInvoker)delegate { btnLeft_Click(this, new EventArgs()); });
                        joystickIsInControl = true;
                        direction = 1;
                    }
                } else if (reading.Buttons.HasFlag(GamepadButtons.DPadRight))
                {
                    if (direction != 2)
                    {
                        btnRight.Invoke((MethodInvoker)delegate { btnRight_Click(this, new EventArgs()); });
                        joystickIsInControl = true;
                        direction = 2;
                    }
                } else if (reading.Buttons.HasFlag(GamepadButtons.DPadDown))
                {
                    if (direction != 3)
                    {
                        btnDown.Invoke((MethodInvoker)delegate { btnDown_Click(this, new EventArgs()); });
                        joystickIsInControl = true;
                        direction = 3;
                    }
                } else if (reading.Buttons.HasFlag(GamepadButtons.DPadUp))
                {
                    if (direction != 4)
                    {
                        btnUP.Invoke((MethodInvoker)delegate { btnUP_Click(this, new EventArgs()); });
                        joystickIsInControl = true;
                        direction = 4;
                    }
                } else if (joystickIsInControl)
                {
                    btnStop.Invoke((MethodInvoker)delegate { btnStop_Click(this, new EventArgs()); });
                    joystickIsInControl = false;
                    direction = 0;
                }
            }
        }
    }
}
