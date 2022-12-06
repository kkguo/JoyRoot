using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
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

        #region static fields
        public static Dictionary<ulong, RootDevice> deviceList = new Dictionary<ulong, RootDevice>();
        
        public static event EventHandler DeviceFound;

        #region constants and GUID
        public static readonly ushort ManufacturerID = 0x600;
        public static readonly byte AdvertisementIdentifierSectionType = 0x6;

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

        #endregion

        public ModelType Model;
        
        #region BT connection
        public ulong Address {
            set;
            get;
        }

        public byte[] State = new byte[4];
        private BluetoothLEDevice _device;
        private bool _connected = false;
        private DateTime lastSeen;

        public event EventHandler AdvertiseUpdate;

        static BluetoothLEAdvertisementWatcher watcher;

        public static void Scan() {
            if (watcher == null)
                watcher = new BluetoothLEAdvertisementWatcher();
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Received += OnAdvertisementReceived;
            watcher.Start();
        }

        public static void StopScan() {
            watcher.Received -= OnAdvertisementReceived;
            watcher.Stop();
        }

        private static void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) {
            RootDevice root;
            if (deviceList.ContainsKey(args.BluetoothAddress)) {
                root = deviceList[args.BluetoothAddress];
                if (args.IsScanResponse) {
#if DEBUG
                    foreach(var section in args.Advertisement.DataSections) {
                        byte[] bytes = new byte[section.Data.Length];
                        DataReader.FromBuffer(section.Data).ReadBytes(bytes);
                        Debug.WriteLine(BitConverter.ToString(bytes));
                    }
#endif
                    // Read Name
                    byte[] tmp = new byte[args.Advertisement.DataSections[0].Data.Length];
                    root.Name = DataReader.FromBuffer(args.Advertisement.DataSections[0].Data).ReadString(args.Advertisement.DataSections[0].Data.Length);
                    // Read state
                    DataReader.FromBuffer(args.Advertisement.DataSections[1].Data).ReadBytes(root.State);
                    root.lastSeen = DateTime.Now;
                    root.AdvertiseUpdate.Invoke(root, new EventArgs());
                }
            } else if (args.Advertisement.ServiceUuids.Contains(guidRootIdentifierService)) { // found root
                // Read model, 
                byte[] tmp = new byte[5];
                DataReader.FromBuffer(args.Advertisement.DataSections[2].Data).ReadBytes(tmp);
                string model = BitConverter.ToString(tmp, 2);

                root = new RootDevice(model);
                root.Address = args.BluetoothAddress;
                deviceList.Add(args.BluetoothAddress, root);
                root.lastSeen = DateTime.Now;
                DeviceFound.Invoke(root, new EventArgs());
            };
        }

        public async Task<BluetoothLEDevice> getBluetoothDevice() {
            if (_device == null)
                _device = await BluetoothLEDevice.FromBluetoothAddressAsync(Address);
            return _device;
        }

        #region GATT routine
        private async Task<GattDeviceService> getGattServices(Guid ServiceGuid)
        {
            if (BTServices.ContainsKey(ServiceGuid))
            {
                return BTServices[ServiceGuid];
            }
            GattDeviceServicesResult result = await _device.GetGattServicesForUuidAsync(ServiceGuid);
            if (result.Status == GattCommunicationStatus.Success)
            {
                BTServices.Add(ServiceGuid, result.Services[0]);
                return result.Services[0];
            }
            else
                return null;
        }

        private async Task<GattCharacteristic> getGattCharacteristic(Guid ServiceGuid, Guid guid)
        {
            if (BTcharacteristics.ContainsKey(guid))
            {
                return BTcharacteristics[guid];
            }
            GattDeviceService serv = await getGattServices(ServiceGuid);
            if (await serv?.RequestAccessAsync() == DeviceAccessStatus.Allowed)
            {
                var cresult = await serv.GetCharacteristicsForUuidAsync(guid);
                if (cresult.Status == GattCommunicationStatus.Success)
                {
                    BTcharacteristics.Add(guid, cresult.Characteristics[0]);
                    return cresult.Characteristics[0];
                }
            }
            return null;
        }

        public async Task<string> readGattCharString(Guid service, Guid guid)
        {
            var cha = await getGattCharacteristic(service, guid);
            if (cha != null)
            {
                var gattval = await cha.ReadValueAsync();
                return DataReader.FromBuffer(gattval.Value).ReadString(gattval.Value.Length);
            }
            else
                return "";
        }

        #endregion

        public async Task<bool> isAvailible() {
            return ((await getBluetoothDevice()) != null);
        }

        private Dictionary<Guid , GattCharacteristic> BTcharacteristics = new Dictionary<Guid, GattCharacteristic>();
        private Dictionary<Guid , GattDeviceService> BTServices = new Dictionary<Guid, GattDeviceService>();

        private BlockingCollection<RootCommand> QCommand;
        private BlockingCollection<RootCommand> QResponse;
        private CancellationTokenSource QCommandCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource QResponseCancellationTokenSource = new CancellationTokenSource();

        // This will trigger root beep
        public async Task Connect() {
            if (await isAvailible()) {
                SerialNumber = await readGattCharString(guidDeviceInfomationService, guidSerialNumberCharacteristic);
                FWVersion = await readGattCharString(guidDeviceInfomationService, guidFirwareVersionCharacteristic);
                HWVersion = await readGattCharString(guidDeviceInfomationService, guidHardwareVersionCharacteristic);                
                QCommand = new BlockingCollection<RootCommand>();
                QResponse = new BlockingCollection<RootCommand>();
                await ListenToRootTX();
                _ = Task.Run(() => sendCommandLoop());
                //disableEvents();
                _connected = true;
            }
        }

        public void Disconnect() {
            _ = sendCommand(RootCommand.DisconnectCmd);
            QCommand.CompleteAdding();
            while(QCommand.IsCompleted == false)
            {
                System.Threading.Thread.Sleep(100);
            }

            //_device.ConnectionStatusChanged -= ConnectionStatusChanged;
            //QResponseCancellationTokenSource.Cancel();
            QResponseCancellationTokenSource?.Dispose();
            //QCommandCancellationTokenSource.Cancel();
            QResponseCancellationTokenSource?.Dispose();
            QResponse?.Dispose();
            QCommand?.Dispose();
            _device?.Dispose();
            _device = null;
            _connected = false;
            //GC.Collect();
        }

        static byte lastPacketID = 255;

        /// <summary>
        /// Command packets sends here
        /// </summary>
        private async void sendCommandLoop()
        {
            try
            {
                while (await isAvailible())
                {
                    RootCommand cmd = QCommand.Take(QCommandCancellationTokenSource.Token);
                    lastPacketID++;
                    cmd.PacketID = lastPacketID;
                    var rxCh = await getGattCharacteristic(guidUARTService, guidRxCharacteristic);
                    if (rxCh != null)
                    {
                        DataWriter dw = new DataWriter();
                        dw.WriteBytes(cmd.pack());
                        await rxCh.WriteValueWithResultAsync(dw.DetachBuffer());
                    }
                }

            }
            catch (ObjectDisposedException e)
            {
                //Disconnect();
            }
        }

        /// <summary>
        /// Recieve packets from Roots
        /// </summary>
        /// <returns></returns>
        private async Task<GattCommunicationStatus> ListenToRootTX()
        {
            var cha = await getGattCharacteristic(guidUARTService, guidTxCharacteristic);
            if (cha.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                var status = await cha.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                if (status == GattCommunicationStatus.Success)
                {
                    cha.ValueChanged += RootTXCommandRecieved;
                }
                return status;
            }
            return GattCommunicationStatus.AccessDenied;
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

        public int Battery {
            get {
                return (int)State[3];
            }
        }

        public string Name;

        public struct RootTouchStatus
        {
            bool FrontLeft;
            bool FrontRight;
            bool RearLeft;
            bool RearRight;
        }

        public RootTouchStatus buttons;

        public struct RootBumperStatus
        {
            bool Left;
            bool Right;
        }

        public RootBumperStatus bumpers;

        public struct RootColorSensorStatus
        {
            System.Drawing.Color[] color;
        }

        RootColorSensorStatus colorSensor;

        public bool CliffSensor;
        public bool LightSensor;

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

        /// <summary>
        /// Send posted command, not expecting response
        /// </summary>
        /// <param name="cmd">command content</param>
        /// <param name="waitForResponse">if set, will return a response, default is false</param>
        public async Task<RootCommand> sendCommand(RootCommand cmd, bool waitForResponse = false) {
            if (await isAvailible()) {
                QCommand.Add(cmd);
                if (waitForResponse)
                {
                    RootCommand response;
                    response = QResponse.Take(QResponseCancellationTokenSource.Token);
                    return response;
                }
            }
            return null;
        }


        public void moveForward() {
            _ = sendCommand(RootCommand.moveForwardCmd);
        }

        public void moveBackward() {
            _ = sendCommand(RootCommand.moveBackwardCmd);
        }

        public void stopMove() {
            _ = sendCommand(RootCommand.stopMoveCmd);
        }

        public void turnLeft(float angle = 0) {
            if (angle == 0) // unlimited
                _ = sendCommand(RootCommand.turnLeftCmd);
            else {
                Int32 anint = (Int32)(-angle*10);
                RootCommand cmd = new RootCommand(RootCommand.RootDeviceCommand.MotorRotate);
                BitConverter.GetBytes(anint).CopyTo(cmd.Payload, 0);
                _ = sendCommand(cmd);
            }
        }

        public void turnRight(float angle = 0) {
            if (angle == 0) // unlimited
                _ = sendCommand(RootCommand.turnRightCmd);
            else {
                Int32 anint = (Int32)(angle * 10);
                RootCommand cmd = new RootCommand(RootCommand.RootDeviceCommand.MotorRotate);
                BitConverter.GetBytes(anint).CopyTo(cmd.Payload, 0);
                _ = sendCommand(cmd);
            }
        }

        public void setLed(System.Drawing.Color color, RootCommand.RootLEDLightState ledstate = RootCommand.RootLEDLightState.On) {
            _ = sendCommand(RootCommand.getSetLEDCmd(color, ledstate));
        }

        public void resetPosition() {
            RootCommand cmd = new RootCommand(RootCommand.RootDeviceCommand.MotorResetPosition);
            _ = sendCommand(cmd);
        }

        public void navigate(UInt16 x, UInt16 y) {
            RootCommand cmd = new RootCommand(RootCommand.RootDeviceCommand.MotorNavigateToPosition);
            BitConverter.GetBytes(x).CopyTo(cmd.Payload, 0);
            BitConverter.GetBytes(y).CopyTo(cmd.Payload, 2);
            _ = sendCommand(cmd);
        }

        public async Task<string> getSerialNumber()
        {
            RootCommand cmd = new RootCommand(RootCommand.RootDeviceCommand.GetSerialNumber);
            RootCommand response = await sendCommand(cmd, true);
            return response.Payload.ToString();
        }

        public void disableEvents()
        {
            var cmd = RootCommand.getDisableEventsCmd();
            _ = sendCommand(cmd);
        }

        #region Response from robot

        #region Events defines
        // event args
        public class RootUARTTxEventArgs : EventArgs
        {
            RootCommand rootCommand;
            public RootUARTTxEventArgs(byte[] bytes) : base() {
                rootCommand = new RootCommand(bytes);
            }
        }

        #region Response Args
        public class RootResponseArgs : RootUARTTxEventArgs
        {
            public RootResponseArgs(byte[] bytes) : base(bytes) {

            }
        }
        public class RootNavigateFinishResponseArgs : RootResponseArgs
        {
            public UInt16 X, Y, Heading;
            public RootNavigateFinishResponseArgs(byte[] bytes) : base(bytes) {
                X = BitConverter.ToUInt16(bytes, 3);
                Y = BitConverter.ToUInt16(bytes, 5);
                Heading = BitConverter.ToUInt16(bytes, 7);
            }
        }
        public class RootDriveArcFinishResponseArgs : RootResponseArgs
        {
            public RootDriveArcFinishResponseArgs(byte[] bytes) : base(bytes) {

            }
        }
        public class RootMakerEraserPositionFinishResponseArgs : RootResponseArgs
        {
            public enum MarkerEraserPositionType:byte
            {
                MarkerUpEraserUp = 0,
                MarkerDownEraserUp = 1,
                MarkerUpEraserDown =2
            }
            public MarkerEraserPositionType position;
            public RootMakerEraserPositionFinishResponseArgs(byte[] bytes) : base(bytes) {
                position = (MarkerEraserPositionType)bytes[3];
            }
        }
        public class RootColorSensorGetDataResponseArgs : RootResponseArgs
        {
            public UInt16[] Data = new UInt16[8];
            public RootColorSensorGetDataResponseArgs(byte[] bytes) : base(bytes) {
                for(int i=0;i<8;i++) {
                    Data[i] = BitConverter.ToUInt16(bytes, i * 2 + 3);
                }
            }
        }
        public class RootPlayNoteFinishResponseArgs : RootUARTTxEventArgs
        {
            public RootPlayNoteFinishResponseArgs(byte[] bytes) : base(bytes) {

            }
        }
        public class RootSayPhraseFinishResponseArgs : RootResponseArgs
        {
            public RootSayPhraseFinishResponseArgs(byte[] bytes) : base(bytes) { }
        }
        public class RootPlaySweepFinishResponseArgs : RootResponseArgs
        {
            public RootPlaySweepFinishResponseArgs(byte[] bytes) : base(bytes) {

            }
        }
        public class RootGetBatteryLevelResponseArgs : RootResponseArgs
        {
            UInt32 timeStamp;
            UInt16 Voltage;
            byte Percent;
            public RootGetBatteryLevelResponseArgs(byte[] bytes) : base(bytes) {
                timeStamp = BitConverter.ToUInt32(bytes, 0);
                Voltage = BitConverter.ToUInt16(bytes, 4);
                Percent = bytes[9];
            }
        }
        public class RootGetVersionResponseArgs : RootResponseArgs
        {
            public byte Board, FWMaj, FWMin, HWMaj, HWMin, BootMaj, BootMin, ProtoMaj, ProtoMin;
            public RootGetVersionResponseArgs(byte[] bytes) : base(bytes) {
                Board = bytes[3];
                FWMaj = bytes[4];
                FWMin = bytes[5];
                HWMaj = bytes[6];
                HWMin = bytes[7];
                BootMaj = bytes[8];
                BootMin = bytes[9];
                ProtoMaj = bytes[10];
                ProtoMin = bytes[11];
            }
        }

        public class RootEventArgs : RootUARTTxEventArgs
        {
            public UInt32 timeStamp;
            public RootEventArgs(byte[] bytes) : base(bytes) {
                timeStamp = BitConverter.ToUInt32(bytes, 3);
            }
        }
        public class RootGetPositionResponseEventArgs : RootEventArgs
        {
            public UInt16 X, Y, Heading;
            public RootGetPositionResponseEventArgs(byte[] bytes):base(bytes) {
                X = BitConverter.ToUInt16(bytes, 7);
                Y = BitConverter.ToUInt16(bytes, 9);
                Heading = BitConverter.ToUInt16(bytes, 11);
            }
        }
        public class RootColorSensorEventArgs : RootEventArgs
        {
            public enum RootColorSensorValue : byte
            {
                White = 0,
                Black = 1,
                Red = 2,
                Green = 3,
                Blue = 4
            }
            public RootColorSensorValue[] color = new RootColorSensorValue[32];
            public RootColorSensorEventArgs(byte[] bytes) : base(bytes) {
                for (int i = 0; i < 16; i++) {
                    color[i * 2] = (RootColorSensorValue)(bytes[i + 3] >> 4);
                    color[i * 2 + 1] = (RootColorSensorValue)(bytes[i + 3] & 0xf);
                }
            }
        }
        public class RootMotorStallEventArgs : RootEventArgs
        {
            public enum MotorType : byte
            {
                Left=0,
                Right=1,
                MarkerEraser=2
            }
            public enum MotorStallCauseType:byte
            {
                NOStall =0,
                OverCurrent=1,
                UnderCurrent=2,
                UnderSpeed = 3,
                SaturatedPID =4,
                TimeOut=5
            }
            public MotorType motor;
            public MotorStallCauseType cause;
            public RootMotorStallEventArgs(byte[] bytes) : base(bytes) {
                motor = (MotorType)bytes[7];
                cause = (MotorStallCauseType)bytes[8];
            }
        }
        public class RootBumperEventArgs: RootEventArgs
        {
            public bool LBumper, RBumper;
            public RootBumperEventArgs(byte[] bytes):base(bytes) {
                LBumper = (bytes[3] & 0x80) == 0;
                RBumper = (bytes[3] & 0x40) == 1;
            }
        }
        public class RootLightSensorEventArgs : RootEventArgs
        {
            public enum RootLightSensorState : byte
            {
                BothEyesDark = 4,
                RIghtEyeBrighter = 5,
                LeftEyeBrighter = 6,
                BothEyesBright = 7
            }
            public RootLightSensorState state;
            public UInt16 leftMillivolts;
            public UInt16 rightMillivolts;
            public RootLightSensorEventArgs(byte[] bytes): base(bytes) {
                state = (RootLightSensorState)bytes[7];
                leftMillivolts = BitConverter.ToUInt16(bytes, 8);
                rightMillivolts = BitConverter.ToUInt16(bytes, 10);
            }
        }
        public class RootBatteryLevelEventArgs : RootEventArgs
        {
            public UInt16 voltage;
            public byte percent;
            public RootBatteryLevelEventArgs(byte[] bytes): base(bytes) {
                voltage = BitConverter.ToUInt16(bytes, 7);
                percent = bytes[9];
            }
        }
        public class RootAccelerometerEventArgs : RootEventArgs
        {
            public Int16 X, Y, Z;
            public RootAccelerometerEventArgs(byte[] bytes): base(bytes) {
                X = BitConverter.ToInt16(bytes, 7);
                Y = BitConverter.ToInt16(bytes, 9);
                Z = BitConverter.ToInt16(bytes, 11);
            }
        }
        public class RootTouchSensorEventArgs : RootEventArgs
        {
            public bool FrontLeft, FrontRight, RearRight, RearLeft;
            public RootTouchSensorEventArgs(byte[] bytes):base(bytes) {
                RearLeft = (bytes[7] & 0x1) != 0;
                RearRight = (bytes[7] & 0x2) != 0;
                FrontRight = (bytes[7] & 0x4) != 0;
                FrontLeft = (bytes[7] & 0x8) != 0;
            }
        }
        public class RootCliffSensorEventArgs : RootEventArgs
        {
            public bool cliff;
            public UInt16 sensorMillivolts, threshold;
            public RootCliffSensorEventArgs(byte[] bytes):base(bytes) {
                cliff = bytes[7] == 1;
                sensorMillivolts = BitConverter.ToUInt16(bytes, 8);
                threshold = BitConverter.ToUInt16(bytes, 10);
            }
        }
        #endregion

        // delegates
        public delegate void RootResponseHandler(RootDevice root, RootResponseArgs e);
        public delegate void RootEventHandler(RootDevice root, RootEventArgs e);
        public event RootEventHandler RootEvent;
        public event RootResponseHandler RootResponse;
        #endregion

        private void RootTXCommandRecieved(GattCharacteristic sender, GattValueChangedEventArgs args) {   
            byte[] bytes = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(bytes);
            var cmd = new RootCommand(bytes);
            if (cmd.isEvent) { // is Event
                switch (cmd.Event) {
                    case RootCommand.RootEventType.BumperEvent:
                        break;
                    case RootCommand.RootEventType.LightEvent:
                        break;
                    case RootCommand.RootEventType.BatteryLevelEvent:
                        break;
                    case RootCommand.RootEventType.TouchSensorEvent:
                        break;
                    case RootCommand.RootEventType.CliffEvent:
                        break;
                }
                RootEvent?.Invoke(this, new RootEventArgs(bytes));
            } else { // is response
                RootResponseArgs responseArgs;
                switch (cmd.DeviceCommand) {
                    case RootCommand.RootDeviceCommand.BatteryGetLevel:
                        responseArgs = new RootGetBatteryLevelResponseArgs(bytes);
                        break;
                    case RootCommand.RootDeviceCommand.GetVersion:
                        responseArgs = new RootGetVersionResponseArgs(bytes);
                        break;
                    //case RootCommand.RootDeviceCommand.MarkerEraserSetPosition:
                    //    break;
                    //case RootCommand.RootDeviceCommand.SoundPlayNote:
                    //    break;
                    default:
                        responseArgs = new RootResponseArgs(bytes);
                        break;
                }

                QResponse.Add(new RootCommand(bytes));                
                if (responseArgs != null)
                    RootResponse?.Invoke(this, responseArgs);
            }           
        }

        #endregion
    }
}
