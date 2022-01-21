using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace JoyRoot
{
    public partial class mainform : Form {
        private List<RootDevice> rootList = new List<RootDevice>();
        private List<ulong> addressList = new List<ulong>();

        public mainform()
        {
            InitializeComponent();
        }

        private void mainform_Load(object sender, EventArgs e) {

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

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            if (!addressList.Contains(eventArgs.BluetoothAddress))
            {
                try
                {
                    if (eventArgs.Advertisement.ServiceUuids.Contains(RootDevice.guidRootIdentifierService))
                    {
                        var mdata = eventArgs.Advertisement.GetManufacturerDataByCompanyId(RootDevice.ManufacturerID);
                        if (mdata.Count == 1)
                        {
                            byte[] tmp = new byte[mdata[0].Data.Length];
                            string deviceType = DataReader.FromBuffer(mdata[0].Data).ReadString(3);
                            RootDevice root = new RootDevice(deviceType);
                            root.BTAddress = eventArgs.BluetoothAddress;
                            addressList.Add(root.BTAddress);
                            rootList.Add(root);

                            BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(root.BTAddress);
                            root.BTDevice = device;
                            root.BTInfo = device.DeviceInformation;                            

                            listBox1.Invoke((MethodInvoker)delegate
                            {
                                listBox1.Items.Add(root.Name + " " + root.model.ToString() + " " + root.BTAddress);
                            });
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        private async void connect(RootDevice root)
        {
            string content = await readDeviceInfo(root, RootDevice.guidManufacturerCharacteristic);
            root.SerialNumber = await readDeviceInfo(root, RootDevice.guidSerialNumberCharacteristic);
            root.HWVersion = await readDeviceInfo(root, RootDevice.guidHardwareVersionCharacteristic);
            root.FWVersion = await readDeviceInfo(root, RootDevice.guidFirwareVersionCharacteristic);

            label1.Invoke((MethodInvoker)delegate {
                label1.Text = content;
            });

        }

        private async void subscribeTX(RootDevice root)
        {
            var ser = await getRootService(root, RootDevice.guidUARTService);
            var cha = await getRootCharacteristic(root, RootDevice.guidUARTService, RootDevice.guidTxCharacteristic);
            var status = await cha.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            if (status == GattCommunicationStatus.Success)
            {
                cha.ValueChanged += Characteristic_ValueChanged;
            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {

        }

        private async Task<GattDeviceService> getRootService(RootDevice root, Guid guid)
        {
            if (root.getService(guid)==null)
            {
                BluetoothLEDevice device = (BluetoothLEDevice)root.BTDevice;
                if (root.getService(guid) == null)
                {
                    GattDeviceServicesResult result = await device.GetGattServicesForUuidAsync(guid);
                    if (result.Status == GattCommunicationStatus.Success)
                        root.setService(guid, result.Services[0]);
                }
            }
            return (GattDeviceService)root.getService(guid);
        }

        private async Task<GattCharacteristic> getRootCharacteristic(RootDevice root, Guid service, Guid characteristic)
        {
            if (root.getCharacteristic(characteristic) == null)
            {
                var serv = await getRootService(root, service);
                if (await serv.RequestAccessAsync() == DeviceAccessStatus.Allowed)
                {
                    var cresult = await serv.GetCharacteristicsForUuidAsync(characteristic);
                    if (cresult.Status == GattCommunicationStatus.Success)
                    {
                        root.setCharacteristic(characteristic, cresult.Characteristics[0]);
                    }
                }
                else
                {
                    throw new Exception("Device access is not allowed.");
                }
            } 
            return (GattCharacteristic)root.getCharacteristic(characteristic);
        }

        private async Task<string> readDeviceInfo(RootDevice root, Guid characteristic)
        {
            GattCharacteristic rootch =
                await getRootCharacteristic(root, RootDevice.guidDeviceInfomationService, characteristic);

            if (rootch != null)
            {
                var gattval = await rootch.ReadValueAsync();
                return DataReader.FromBuffer(gattval.Value).ReadString(gattval.Value.Length);
            } else
                return string.Empty;
        }

        private async void writeRXCommand(RootDevice root, RootCommand cmd)
        {
            GattCharacteristic rxCh = 
                await getRootCharacteristic(root, RootDevice.guidUARTService, RootDevice.guidRxCharacteristic);
            if (rxCh != null)
            {
                DataWriter dw = new DataWriter();
                dw.WriteBytes(cmd.pack());
                await rxCh.WriteValueWithResultAsync(dw.DetachBuffer());
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                connect(rootList[index]);
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                writeRXCommand(rootList[index], RootCommand.moveBackwardCmd);
            }
            label1.Text = RootCommand.moveBackwardCmd.ToString();
        }

        private void btnUP_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                writeRXCommand(rootList[index], RootCommand.moveForwardCmd);
            }
            label1.Text = RootCommand.moveForwardCmd.ToString();
            //writeCommand(rootList[0], RootCommand.moveForwardCmd);
        }

        private void btnLeft_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                writeRXCommand(rootList[index], RootCommand.turnLeftCmd);
            }
            label1.Text = RootCommand.turnLeftCmd.ToString();
        }

        private void btnRight_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                writeRXCommand(rootList[index], RootCommand.turnRightCmd);
            }
            label1.Text = RootCommand.turnRightCmd.ToString();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                int index = listBox1.SelectedIndex;
                if (rootList[index] != null)
                {
                    var color = colorDialog1.Color;
                    writeRXCommand(rootList[index], RootCommand.getSetLEDCmd(color.R,color.G,color.B));
                    pictureBox1.BackColor = color;
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (rootList[index] != null)
            {
                writeRXCommand(rootList[index], RootCommand.stopMoveCmd);
            }
        }

        private void mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var root in rootList)
            {
                disconnect(root);
            }
        }

        private void disconnect(RootDevice root)
        {
            ((BluetoothLEDevice)root.BTDevice)?.Dispose();
            root.BTDevice = null;
            GC.Collect();
        }
    }
}
