using System;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace JoyRoot
{
    class RootDevice
    {
        public enum ModelType {
            RT0=0,
            RT1=1,
            UNKNOWN=2
        };

        // Advertisement 
        public static readonly ushort ManufacturerID = 0x600;
        public static readonly byte AdvertisementIdentifierSectionType = 0x6;

        #region GUID
        // GATT Services
        public static readonly Guid guidRootIdentifierService         = Guid.Parse("48c5d828-ac2a-442d-97a3-0c9822b04979");
        public static readonly Guid guidDeviceInfomationService       = Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb");
        public static readonly Guid guidUARTService                   = Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");

        // Characteristics
        public static readonly Guid guidSerialNumberCharacteristic    = Guid.Parse("00002a25-0000-1000-8000-00805f9b34fb");
        public static readonly Guid guidFirwareVersionCharacteristic  = Guid.Parse("00002a26-0000-1000-8000-00805f9b34fb");
        public static readonly Guid guidHardwareVersionCharacteristic = Guid.Parse("00002a27-0000-1000-8000-00805f9b34fb");
        public static readonly Guid guidManufacturerCharacteristic    = Guid.Parse("00002a29-0000-1000-8000-00805f9b34fb");
        public static readonly Guid guidRobotStateCharacteristic      = Guid.Parse("00008bb6-0000-1000-8000-00805f9b34fb");        
        public static readonly Guid guidRxCharacteristic              = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        public static readonly Guid guidTxCharacteristic              = Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
        #endregion

        public ModelType Model;

        #region BT connection
        public ulong Address {
            private set;
            get;
        }
        private BluetoothLEDevice _device;
        public BluetoothLEDevice Bluetooth {
            set {
                _device = value;
                _device.ConnectionStatusChanged += ConnectionStatusChanged;
            }
            get {
                return _device;
            }
        }

        public async Task<bool> isAvailible() {
            if (Bluetooth == null) {
                Debug.WriteLine("seeing root missing, tring to reconnect");
                return await query(Address);
            } else {                
                return true;
            }
        }

        public bool isConnected {
            get {                
                return Bluetooth?.ConnectionStatus == BluetoothConnectionStatus.Connected;
            }
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args) {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected) {
                Connected?.Invoke(this, new EventArgs());
            } else if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected) {
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        private Dictionary<Guid , GattCharacteristic> BTcharacteristics = new Dictionary<Guid, GattCharacteristic>();
        private Dictionary<Guid , GattDeviceService> BTServices = new Dictionary<Guid, GattDeviceService>();

        public async Task<bool> query(ulong address) {
            Address = address;
            Bluetooth = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            return (Bluetooth != null);
        }

        public async void connect() {
            SerialNumber = await readGattCharString(guidDeviceInfomationService, guidSerialNumberCharacteristic);
            FWVersion = await readGattCharString(guidDeviceInfomationService, guidFirwareVersionCharacteristic);
            HWVersion = await readGattCharString(guidDeviceInfomationService, guidHardwareVersionCharacteristic);
        }

        public void disconnect() {            
            _device.ConnectionStatusChanged -= ConnectionStatusChanged;
            _device?.Dispose();
            _device = null;
            GC.Collect();
        }

        #endregion

        #region Root General info
        public string SerialNumber {
            get;
            private set;
        }
        public string HWVersion { 
            get; 
            private set; 
        }
        public string FWVersion {
            get;
            private set;
        }

        public int battery;

        public string Name
        {
            get
            {
                Debug.WriteLine("getting name");
                if (Bluetooth== null) {
                    Debug.WriteLine("BT is null");
                } else {
                    Debug.WriteLine("Got name this time:" + Bluetooth.DeviceInformation.Name);
                }
                return Bluetooth?.DeviceInformation.Name;
            }
        }

        #endregion

        public RootDevice(ModelType m=ModelType.RT1) {

        }

        public RootDevice(string sModel)
        {
            if (sModel=="RT0")
            {
                Model = ModelType.RT0;
            } else
            {
                Model = ModelType.RT1;
            }
        }

        // Commands
        public async void sendCmd(RootCommand cmd) {
            if (await isAvailible()) {
                var rxCh = await getGattCharacteristic(guidUARTService, guidRxCharacteristic);
                if (rxCh != null) {
                    DataWriter dw = new DataWriter();
                    dw.WriteBytes(cmd.pack());
                    await rxCh.WriteValueWithResultAsync(dw.DetachBuffer());
                }
            }
        }

        public void moveForward() {
            sendCmd(RootCommand.moveForwardCmd);
        }

        public void moveBackward() {
            sendCmd(RootCommand.moveBackwardCmd);
        }

        public void stopMove() {
            sendCmd(RootCommand.stopMoveCmd);
        }

        public void turnLeft(int angle = -1) {
            if (angle == -1) // unlimited
                sendCmd(RootCommand.turnLeftCmd);
        }

        public void turnRight(int angle = -1) {
            if (angle == -1) // unlimited
                sendCmd(RootCommand.turnRightCmd);
        }

        public void setLed(System.Drawing.Color color, RootCommand.LEDState ledstate = RootCommand.LEDState.On) {
            sendCmd(RootCommand.getSetLEDCmd(color.R, color.G, color.B, ledstate));
        }

        public void resetNavigate() {

        }
        public void navigate(int x, int y) {

        }

        private async Task<GattDeviceService> getGattServices(Guid ServiceGuid) {
            if (BTServices.ContainsKey(ServiceGuid)) {
                return BTServices[ServiceGuid];
            }
            GattDeviceServicesResult result = await _device.GetGattServicesForUuidAsync(ServiceGuid);
            if (result.Status == GattCommunicationStatus.Success) {
                BTServices.Add(ServiceGuid, result.Services[0]);
                return result.Services[0];
            }  else
                return null;
        }

        private async Task<GattCharacteristic> getGattCharacteristic(Guid guid) {
            if (BTcharacteristics.ContainsKey(guid)) {
                return BTcharacteristics[guid];
            }
            var result = await _device.GetGattServicesAsync();
            foreach(var serv in result.Services) {
                var cresult = await serv.GetCharacteristicsForUuidAsync(guid);
                if (cresult.Status == GattCommunicationStatus.Success) {
                    BTcharacteristics.Add(guid, cresult.Characteristics[0]);
                    return cresult.Characteristics[0];
                }
            }
            return null;
        }

        private async Task<GattCharacteristic> getGattCharacteristic(Guid ServiceGuid, Guid guid) {
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

        private async void subscribeUartTx() {
            var cha = await getGattCharacteristic(guidUARTService, guidTxCharacteristic);
            var status = await cha.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            if (status == GattCommunicationStatus.Success) {
                cha.ValueChanged += UARTTx_Recieved;
            }
        }

        #region Response from robot
        public class RootEventArgs : EventArgs
        {
            public byte[] packet;
            public RootEventArgs(byte[] bytes) : base() {
                packet = bytes;
            }
        }

        public delegate void RootEventHandler(RootDevice root, RootEventArgs e);

        public event RootEventHandler ResponseRecieved;
        public event RootEventHandler StatusChanged;

        public event RootEventHandler MoveFinished;
        public event RootEventHandler BumperEvent;
        public event RootEventHandler MotorStallEvent;

        private void UARTTx_Recieved(GattCharacteristic sender, GattValueChangedEventArgs args) {                
            byte[] bytes = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(bytes);
            var cmd = new RootCommand(bytes);            
            switch (bytes[0]) { // Dev
                case 1:
                    switch(bytes[1]) { // cmd
                        case 8:
                        case 12:
                        case 17:
                        case 27:
                            MoveFinished.Invoke(this, new RootEventArgs(bytes));
                            break;
                        case 16:
                            ResponseRecieved.Invoke(this, new RootEventArgs(bytes));
                            break;
                        case 29:
                            MotorStallEvent.Invoke(this, new RootEventArgs(bytes));
                            break;
                    }                    
                    break;
                case 2:
                    MoveFinished(this, new RootEventArgs(bytes));
                    break;
                case 12: // Bumper
                    BumperEvent.Invoke(this, new RootEventArgs(bytes));
                    break;
                default:
                    ResponseRecieved.Invoke(this, new RootEventArgs(bytes));
                    break;            
            }
        }

        #endregion
    }
}
