using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace JoyRoot
{
    class BLEManager
    {
        public delegate void BLEManagerAdvertiseEventHandler(ulong address, bool isScanResponse, List<byte[]> content);

        //public delegate void RootBLEManagerEventHandler(RootDevice root);
        public static Dictionary<ulong, RootDevice> deviceList = new Dictionary<ulong, RootDevice>();
        // Static fields for Scan advertising frame
        //public static event RootBLEManagerEventHandler RootDeviceFound;
        public static event BLEManagerAdvertiseEventHandler AdvertiseUpdate;

        private static BluetoothLEAdvertisementWatcher watcher;

        private static List<Guid> ServiceUuidFilter = new List<Guid>();
        public void SetServiceFilter(Guid guid) {
            ServiceUuidFilter.Add(guid);
        }
        private static List<ulong> recorgnizedAddressList;

        public static void Scan() {
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Received += OnAdvertisementReceived;
            watcher.Start();
        }

        private static void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) {            
            bool found = false;
            foreach (var guid in ServiceUuidFilter) {
                if (args.Advertisement.ServiceUuids.Contains(guid)) {
                    found = true;
                    break;
                }
            }
            if (found || (args.IsScanResponse) ){
                List<byte[]> content = new List<byte[]>();
                foreach(var section in args.Advertisement.DataSections) {
                    byte[] tmp = new byte[section.Data.Length];
                    DataReader.FromBuffer(section.Data).ReadBytes(tmp);
                    content.Add(tmp);
                }            
            }
            if (deviceList.ContainsKey(args.BluetoothAddress)) {
                RootDevice root = deviceList[args.BluetoothAddress];
                if (args.IsScanResponse) {
                    // Read Name
                    byte[] tmp = new byte[args.Advertisement.DataSections[0].Data.Length];
                    root.Name = DataReader.FromBuffer(args.Advertisement.DataSections[0].Data).ReadString(args.Advertisement.DataSections[0].Data.Length);
                    // Read state
                    DataReader.FromBuffer(args.Advertisement.DataSections[1].Data).ReadBytes(root.State);
                    //AdvertiseUpdate?.Invoke(root);
                }
            } else if (args.Advertisement.ServiceUuids.Contains(RootDevice.guidRootIdentifierService)) { // found root
                // Read model, 
                byte[] tmp = new byte[5];
                DataReader.FromBuffer(args.Advertisement.DataSections[2].Data).ReadBytes(tmp);
                string model = BitConverter.ToString(tmp, 2);

                RootDevice root = new RootDevice(model);
                root.Address = args.BluetoothAddress;
                deviceList.Add(args.BluetoothAddress, root);
                //RootDeviceFound?.Invoke(root);
            };
        }

        public BLEManager(ulong address) {
            BTAddress = address;
        }

        private BluetoothLEDevice BLEDevice;
        private ulong BTAddress;

        private async Task getBLEDevice() {
            if (BLEDevice == null)
                BLEDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(BTAddress);
        }

        private Dictionary<Guid, GattCharacteristic> BTcharacteristics = new Dictionary<Guid, GattCharacteristic>();
        private Dictionary<Guid, GattDeviceService> BTServices = new Dictionary<Guid, GattDeviceService>();

        public async void Connect() {
            await getBLEDevice();
            ListenToUARTTX();
        }

        public delegate void preDisconnectHandler();
        public preDisconnectHandler preDisconnect;

        public void Disconnect() {
            if (preDisconnect !=null)
                preDisconnect();
            BLEDevice?.Dispose();
            BLEDevice = null;
            GC.Collect();
        }

        #region GATT routine
        private async Task<GattDeviceService> getGattServices(Guid ServiceGuid) {
            await getBLEDevice();
            if (BTServices.ContainsKey(ServiceGuid)) {
                return BTServices[ServiceGuid];
            }
            GattDeviceServicesResult result = await BLEDevice.GetGattServicesForUuidAsync(ServiceGuid);
            if (result.Status == GattCommunicationStatus.Success) {
                BTServices.Add(ServiceGuid, result.Services[0]);
                return result.Services[0];
            } else
                return null;
        }

        private async Task<GattCharacteristic> getGattCharacteristic(Guid ServiceGuid, Guid guid) {
            await getBLEDevice ();
            if (BTcharacteristics.ContainsKey(guid)) {
                return BTcharacteristics[guid];
            }
            GattDeviceService serv = await getGattServices(ServiceGuid);
            if (await serv?.RequestAccessAsync() == DeviceAccessStatus.Allowed) {
                var cresult = await serv.GetCharacteristicsForUuidAsync(guid);
                if (cresult.Status == GattCommunicationStatus.Success) {
                    BTcharacteristics.Add(guid, cresult.Characteristics[0]);
                    return cresult.Characteristics[0];
                }
            }
            return null;
        }

        public async Task<string> readGattCharString(Guid service, Guid guid) {
            var cha = await getGattCharacteristic(service, guid);
            if (cha != null) {
                var gattval = await cha.ReadValueAsync();
                return DataReader.FromBuffer(gattval.Value).ReadString(gattval.Value.Length);
            } else
                return "";
        }

        private async void ListenToUARTTX() {
            var cha = await getGattCharacteristic(RootDevice.guidUARTService, RootDevice.guidTxCharacteristic);
            var status = await cha.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            if (status == GattCommunicationStatus.Success) {
                cha.ValueChanged += UARTTxAvalible;
            }
        }

        public delegate void RootBLEManagerUARTTxEventHandler(RootCommand command);
        public event RootBLEManagerUARTTxEventHandler RootTxCommandRecived;

        private void UARTTxAvalible(GattCharacteristic sender, GattValueChangedEventArgs args) {
            byte[] bytes = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(bytes);
            RootTxCommandRecived?.Invoke(new RootCommand(bytes));
        }
        #endregion

        /// <summary>
        /// Send posted command, not expecting response
        /// </summary>
        /// <param name="cmd"></param>
        public async Task sendCommand(RootCommand cmd) {
            try {
                var rxCh = await getGattCharacteristic(RootDevice.guidUARTService, RootDevice.guidRxCharacteristic);
                if (rxCh != null) {
                    DataWriter dw = new DataWriter();
                    dw.WriteBytes(cmd.pack());
                    await rxCh.WriteValueWithResultAsync(dw.DetachBuffer());
                }
            } catch (ObjectDisposedException e) {
                Disconnect();
            }
        }


    }
}
