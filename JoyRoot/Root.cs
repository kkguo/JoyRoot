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
using System.Text.RegularExpressions;

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

        #region Root General info
        public string SerialNumber
        {
            get;
            private set;
        }
        public string HWVersion
        {
            get;
            private set;
        }
        public string FWVersion
        {
            get;
            private set;
        }

        #endregion

        #region States
        public int BatteryPercentage
        {
            get;                            
            private set;
        }
        public int BatteryVoltage { get; private set; }
        

        public string Name;

        [Flags]
        public enum BumperState : byte
        {
            Right = 0x40,
            Left = 0x80,
        }

        public enum EyeState : byte
        {
            BothDark = 4,
            RightBrighter = 5,
            LeftBrighter = 6,
            BothBright = 7,
        }

        public BumperState Bumpers { get; private set; }

        public EyeState eyeState { get; private set; }
        public UInt16 EyeAmbientLevelLeft { get; private set; }
        public UInt16 EyeAmbientLevelRight { get; private set; }
        
        [Flags]
        public enum TouchSensorState:byte
        {
            FrontLeft = 0x8,
            FrontRight = 0x4,
            RearLeft = 0x2,
            RearRight = 0x1,
        }

        public TouchSensorState TouchSensor;

        public bool Cliff { get; private set; }
        public UInt16 CliffSensorValue { get; private set; }
        public UInt16 CliffSensorThreshold { get; private set; }

        public enum ColorSensorColor : byte
        {
            White = 0,
            Black = 1,
            Red = 2,
            Green = 3,
            Blue = 4,
        }

        public ColorSensorColor[] ColorSensorColors
        {
            get; private set;
        }

        public enum StalledMotor :byte
        {
            Left = 0,
            Right = 1,
            MarkerEraser = 2,
        }
        public enum StalledMotorCause : byte
        {
            NOStall = 0,
            OverCurrent = 1,
            UnderCurrent = 2,
            UnderSpeed = 3,
            SaturatedPID = 4,
            TimeOut = 5
        }
        public StalledMotor stalledMotor { get; private set; }
        public StalledMotorCause stalledMotorCause { get; private set; }        

        #endregion

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
                    root.BatteryPercentage = (int)root.State[3];
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
        private Dictionary<RootCommand.RootDeviceCommand, BlockingCollection<RootCommand>> DicResponse;

        // This will trigger root beep
        public async Task Connect() {
            if (await isAvailible()) {
                SerialNumber = await readGattCharString(guidDeviceInfomationService, guidSerialNumberCharacteristic);
                FWVersion = await readGattCharString(guidDeviceInfomationService, guidFirwareVersionCharacteristic);
                HWVersion = await readGattCharString(guidDeviceInfomationService, guidHardwareVersionCharacteristic);                
                QCommand = new BlockingCollection<RootCommand>();
                DicResponse = new Dictionary<RootCommand.RootDeviceCommand, BlockingCollection<RootCommand>>();
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
            //QResponse?.Dispose();
            QCommand?.Dispose();
            //_device.ConnectionStatusChanged -= ConnectionStatusChanged;
            //QResponseCancellationTokenSource.Cancel();
            //QResponseCancellationTokenSource?.Dispose();
            //QCommandCancellationTokenSource.Cancel();
            //QResponseCancellationTokenSource?.Dispose();            
            _device?.Dispose();
            _device = null;
            _connected = false;
            //GC.Collect();
        }

        private byte lastSentPacketID = 255;

        /// <summary>
        /// Command packets sends here
        /// </summary>
        private async void sendCommandLoop()
        {
            try
            {
                while (await isAvailible())
                {
                    RootCommand cmd = QCommand.Take();
                    lastSentPacketID++;
                    cmd.PacketID = lastSentPacketID;
                    var rxCh = await getGattCharacteristic(guidUARTService, guidRxCharacteristic);
                    if (rxCh != null)
                    {
                        DataWriter dw = new DataWriter();
                        dw.WriteBytes(cmd.pack());
                        await rxCh.WriteValueAsync(dw.DetachBuffer());
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

        #region Response and Events defines
        // event args
        public class RootUARTTxEventArgs : EventArgs
        {
            public RootCommand rootCommand;
            public RootUARTTxEventArgs(RootCommand cmd) : base()
            {
                rootCommand = cmd;
            }
        }

        public class RootEventArgs : RootUARTTxEventArgs
        {
            public UInt32 timeStamp;
            public RootEventArgs(RootCommand cmd) : base(cmd)
            {
                timeStamp = BitConverter.ToUInt32(cmd.Payload, 3);
            }
        }

        public class RootResponseArgs : RootUARTTxEventArgs
        {
            public RootResponseArgs(RootCommand cmd) : base(cmd) { }
        }


        // delegates
        public delegate void RootResponseHandler(RootDevice root, RootResponseArgs e);
        public delegate void RootEventHandler(RootDevice root, RootEventArgs e);
        public event RootEventHandler RootEvent;
        public event RootResponseHandler RootResponse;

        #endregion
        private void RootTXCommandRecieved(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            lastSeen = DateTime.Now;
            byte[] bytes = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(bytes);
            var cmd = new RootCommand(bytes);
            if (cmd.isEvent)
            { // is Event, update device states
                switch (cmd.Event)
                {
                    case RootCommand.RootEventType.BumperEvent:
                        Bumpers = (BumperState)cmd.Payload[4];
                        break;
                    case RootCommand.RootEventType.LightEvent:
                        eyeState = (EyeState)cmd.Payload[4];
                        EyeAmbientLevelLeft = BitConverter.ToUInt16(cmd.Payload, 5);
                        EyeAmbientLevelRight = BitConverter.ToUInt16(cmd.Payload, 7);
                        break;
                    case RootCommand.RootEventType.BatteryLevelEvent:
                        BatteryVoltage = BitConverter.ToUInt16(cmd.Payload, 4); 
                        BatteryPercentage = cmd.Payload[6];                        
                        break;
                    case RootCommand.RootEventType.TouchSensorEvent:
                        TouchSensor = (TouchSensorState)cmd.Payload[4];
                        break;
                    case RootCommand.RootEventType.CliffEvent:
                        Cliff = (cmd.Payload[4] != 0);
                        CliffSensorValue = BitConverter.ToUInt16(cmd.Payload, 5);
                        CliffSensorThreshold = BitConverter.ToUInt16(cmd.Payload, 7);
                        break;
                    case RootCommand.RootEventType.ColorSensorEvent:
                        ColorSensorColors = new ColorSensorColor[32];
                        for(int i= 0; i<16;i++)
                        {
                            byte v = (byte)(cmd.Payload[i] & 0xF);
                            ColorSensorColors[i * 2 + 1] = (v > 4) ? ColorSensorColor.White : (ColorSensorColor)v;
                            v = (byte)(cmd.Payload[i] >> 4);
                            ColorSensorColors[i * 2]     = (v > 4) ? ColorSensorColor.White :(ColorSensorColor)v;
                        }
                        break;
                    case RootCommand.RootEventType.MotorStallEvent:
                        stalledMotor = (StalledMotor) cmd.Payload[4];
                        stalledMotorCause = (StalledMotorCause)cmd.Payload[5];
                        break;
                }
                RootEvent?.Invoke(this, new RootEventArgs(cmd));
            }
            else
            { // is response, rise event and send back
                DicResponse[cmd.DeviceCommand].Add(cmd);
                RootResponse?.Invoke(this, new RootResponseArgs(cmd));
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

        /// <summary>
        /// Send command
        /// </summary>
        /// <param name="cmd">command content</param>
        /// <param name="waitForResponse">if set, will wait and return the response, default is false</param>
        /// <param name="timeout">response timeout</param>
        public async Task<RootCommand> sendCommand(RootCommand cmd, bool waitForResponse = false, uint timeout = 1000) {
            if (await isAvailible()) {
                if (waitForResponse && !DicResponse.ContainsKey(cmd.DeviceCommand))
                    DicResponse[cmd.DeviceCommand] = new BlockingCollection<RootCommand>(1); // make sure queue is created before response recieved.
                QCommand.Add(cmd);
                if (waitForResponse)
                {
                    RootCommand response;
                    BlockingCollection<RootCommand> QResponse = DicResponse[cmd.DeviceCommand];
                    if (QResponse.TryTake(out response, TimeSpan.FromMilliseconds(timeout))) // try 1s timeout
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
            _ = sendCommand(RootCommand.setLEDCmd(color, ledstate));
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
            var cmd = RootCommand.disableEventsCmd();
            _ = sendCommand(cmd);
        }

        /// <summary>
        /// Convert Note into frequency and send to root
        /// </summary>
        /// <param name="note">note name, pitch and octave, such as "C4" "Db2"</param>
        /// <param name="duration"> default is 500ms (half second)</param>
        public void playNote(string note, UInt16 duration=500)
        {
            double base_freq;
            UInt32 frequency;
            string pitch;
            uint octave;
            if (note.Length > 3)
                note = note.ToUpper().Substring(0, 3) ;// in case note string is longer than 3
            var match = Regex.Match(note, "([A-G][#b]?)([0-9]?)");
            if (match.Success)
            {
                pitch = match.Groups[1].Value;
                if (match.Groups[2].Value == "")
                    octave = 4; // default                
                else
                    octave = uint.Parse(match.Groups[2].Value);

                base_freq = get_pitch_base_frequency(pitch);
                if (base_freq > 0)
                {
                    frequency = (uint)Math.Floor(base_freq * (2 ^ octave));
                    var cmd = RootCommand.playSoundCmd(frequency, duration);
                    _ = sendCommand(cmd, true); // always wait for note finish
                }
            }
        }

        private double get_pitch_base_frequency(string pitch)
        {
            //frequence picked from https://pages.mtu.edu/~suits/notefreqs.html
            if (pitch == "C") return 16.35;
            else if (pitch == "C#" || pitch == "Db") return 17.32;
            else if (pitch == "D") return 18.35;
            else if (pitch == "D#" || pitch == "Eb") return 19.45;
            else if (pitch == "E") return 20.60;
            else if (pitch == "F") return 21.83;
            else if (pitch == "F#" || pitch == "Gb") return 23.12;
            else if (pitch == "G") return 24.50;
            else if (pitch == "G#" || pitch == "Gb") return 25.96;
            else if (pitch == "A") return 27.50;
            else if (pitch == "A#" || pitch == "Bb") return 29.14;
            else if (pitch == "B") return 30.87;
            else return 0;
        }

    }
}
