using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoyRoot
{
    /// <summary>
    /// https://github.com/iRobotEducation/root-robot-ble-protocol
    /// 
    /// </summary>
    /// 

    struct BlePacket
    {
        byte Device;
        byte Command;
        byte PacketID;

        byte[] Payload;
        byte checksum;

        public byte crc8(byte[] data)
        {
            return 0;
        }

        public byte[] pack()
        {
            return new byte[18];
        }
    }

    class Root
    {
        public enum Model {
            rt0=0,
            rt1=1
        };

        public Model model;

        static string RootIdentifierService = "48c5d828-ac2a-442d-97a3-0c9822b04979";
        static string DeviceInfomationService = "0000180a-0000-1000-8000-00805f9b34fb";
        static string SerialNumberCharacteristic = "00002a25-0000-1000-8000-00805f9b34fb";
        static string FirwareVersionCharacteristic = "00002a26-0000-1000-8000-00805f9b34fb";
        static string HardwareVersionCharacteristic = "00002a27-0000-1000-8000-00805f9b34fb";
        static string ManufacturerCharacteristic = "00002a29-0000-1000-8000-00805f9b34fb";
        static string RobotStateCharacteristic = "00008bb6-0000-1000-8000-00805f9b34fb";
        static string UARTService = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        static string RxCharacteristic = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        static string TxCharacteristic = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        public void sendCommand(string cmd) {

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
