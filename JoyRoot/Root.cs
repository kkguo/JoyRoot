using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;

namespace JoyRoot
{
    class RootDevice
    {
        public enum Model {
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

        public Model model;        
        public ulong BTAddress;
        public object BTDevice;

        private Dictionary<string , object> BTcharacteristics = new Dictionary<string, object>();
        private Dictionary<string , object> BTServices = new Dictionary<string, object>();


        public void setCharacteristic(Guid guid, object characteristic)
        {
            BTcharacteristics[guid.ToString()] = characteristic;
        }

        public object getCharacteristic(Guid guid)
        {
            if (BTcharacteristics.ContainsKey(guid.ToString()))
                return BTcharacteristics[guid.ToString()];
            else
                return null;
        }

        public void setService(Guid guid, object service)
        {
            BTServices[guid.ToString()] = service;
        }

        public object getService(Guid guid)
        {
            if (BTServices.ContainsKey(guid.ToString()))
                return BTServices[guid.ToString()];
            else
                return null;
        }

        public string SerialNumber;
        public string HWVersion;
        public string FWVersion;

        public int battery;

        public string Name
        {
            get
            {
                return BTInfo.Name;
            }
        }

        public DeviceInformation BTInfo
        {
            private get;
            set;
        }

        public RootDevice(Model m=Model.RT1) {

        }

        public RootDevice(string sModel)
        {
            if (sModel=="RT0")
            {
                model = Model.RT0;
            } else
            {
                model = Model.RT1;
            }
        }

        public void getCommand(string cmd) {

        }

        public void moveForward() {

        }

        public void moveBackward() {

        }

        public void turnLeft(int angle = 90) {

        }

        public void turnRight(int angle = 90) {

        }

        public void setLed(int type) {
            
        }

        public void resetNavigate() {

        }
        public void navigate(int x, int y) {

        }

    }
}
