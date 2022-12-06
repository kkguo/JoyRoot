using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace JoyRoot
{
    /// <summary>
    /// https://github.com/iRobotEducation/root-robot-ble-protocol
    /// 
    /// </summary>
    /// 
    public class RootCommand
    {
        #region enum
        public enum DeviceCode : byte
        {
            General = 0,
            Motors = 1,
            MarkerNEraser = 2,
            LEDLights = 3,
            ColorSensor = 4,
            Sound = 5,
            Bumpers = 12,
            LightSensor = 13,
            Battery = 14,
            Accelerometer = 16,
            TouchSensors = 17,
            CliffSensor = 20
        }
        public enum RootDeviceCommand : UInt16
        {
            GetVersion = 0x0000,
            SetName = 0x0100,
            GetName = 0x0200,
            StopNReset = 0x0300,
            Disconnect = 0x0400,
            EnableEvents = 0x0500,
            DisableEvents = 0x600,
            GetEnabledEvents = 0x700,
            GetSerialNumber = 0x800,
            GetSKU = 0x900,        
            MotorSetSpeed = 0x401,
            MotorSetLeftSpeed = 0x601,
            MotorSetRightSpeed = 0x701,
            MotorDriveDistance = 0x801,
            MotorRotate = 0xC01,
            MotorSetGravityCompensation = 0xD01,
            MotorResetPosition = 0xF01,
            MotorGetPosition = 0x1001,
            MotorNavigateToPosition=0x1101,
            MotorDriveArc = 0x1B01,
            MarkerEraserSetPosition=0x0002,
            LEDSetAnimation=0x0203,
            ColorSensorGetData=0x0104,
            SoundPlayNote=0x0005,
            SoundStopSound=0x0105,
            SoundSayPhrase=0x0405,
            SoundPlaySweep=0x0505,
            LightSensorGetValue=0x010D,
            BatteryGetLevel=0x010E,
            AccelerometerGet=0x0110
        }
        public enum RootEventType : UInt16
        {
            MotorStallEvent=0x1D01,
            ColorSensorEvent=0x0204,
            BumperEvent=0x000C,
            LightEvent=0x000D,
            BatteryLevelEvent=0x000E,
            TouchSensorEvent=0x0011,
            CliffEvent=0x0014
        }
        public enum PacketIDType : byte
        {
            Inc,
            Req,
            Evt
        }
        #endregion

        private DeviceCode Device;
        private byte commandCode;
        private UInt16 _devicecommand {
            get {
                return (UInt16)((commandCode << 8) + (byte)Device);
            }
        }

        public RootDeviceCommand DeviceCommand {
            get {                
                return (RootDeviceCommand)_devicecommand;
            }
        }
        public RootEventType Event {
            get {
                return (RootEventType)_devicecommand;
            }
        }

        public bool isEvent {
            get {                
                return Enum.IsDefined(typeof(RootEventType), _devicecommand);
            }
        }

        private PacketIDType IDType = PacketIDType.Inc;
        public byte PacketID = 0;
        public byte[] Payload = new byte[16];

        protected byte checksum
        {
            get
            {
                byte[] newbytes = new byte[19];
                byte crc = 0;
                newbytes[0] = (byte)Device;
                newbytes[1] = commandCode;
                newbytes[2] = PacketID;
                Payload.CopyTo(newbytes, 3);

                for (int i = 0; i < 19; i++)
                {
                    crc ^= newbytes[i];
                    for (int k = 0; k < 8; k++)
                        if ((crc & 0x80) > 0)
                        {
                            crc = (byte)((crc << 1) ^ 0x07);
                        }
                        else
                        {
                            crc = (byte)(crc << 1);
                        }
                }
                crc &= 0xff;
                return crc;
            }
        }

        public byte[] pack()
        {
            byte[] newbytes = new byte[20];
            newbytes[0] = (byte)Device;
            newbytes[1] = commandCode;
            newbytes[2] = PacketID;
            Payload.CopyTo(newbytes, 3);
            newbytes[19] = checksum;
            return newbytes;
        }

        public override string ToString()
        {
            return BitConverter.ToString(pack());
        }

        private RootCommand() { }
        public RootCommand(RootDeviceCommand cmd) {
            byte[] bytes=BitConverter.GetBytes((UInt16)cmd);
            Device = (DeviceCode)bytes[0];
            commandCode = bytes[1];
        }
        public RootCommand(byte[] bytes)
        {
            Device = (DeviceCode)bytes[0];
            commandCode = bytes[1];
            PacketID = bytes[2];
            Payload = bytes.Skip(3).Take(16).ToArray();
        }

        #region Motor Command
        public static RootCommand getMoveCommand(Int32 left, Int32 right)
        {
            RootCommand cmd = new RootCommand(RootDeviceCommand.MotorSetSpeed);
            BitConverter.GetBytes(left).Reverse().ToArray().CopyTo(cmd.Payload, 0);
            BitConverter.GetBytes(right).Reverse().ToArray().CopyTo(cmd.Payload, 4);
            return cmd;
        }

        public static RootCommand stopMoveCmd
        {
            get
            {
                return getMoveCommand(0, 0);
            }
        }

        public static RootCommand moveForwardCmd
        {
            get
            {
                return getMoveCommand(100, 100);
            }
        }

        public static RootCommand moveBackwardCmd
        {
            get
            {
                return getMoveCommand(-100, -100);
            }
        }

        public static RootCommand turnLeftCmd
        {
            get
            {
                return getMoveCommand(-100, 100);
            }
        }

        public static RootCommand turnRightCmd
        {
            get
            {
                return getMoveCommand(100, -100);
            }
        }

        public static RootCommand DisconnectCmd
        {
            get
            {
                return new RootCommand(RootDeviceCommand.Disconnect);
            }
        }

        public static RootCommand moveDistanceCmd(Int32 distance)
        {
            RootCommand cmd = new RootCommand(RootDeviceCommand.MotorDriveDistance);
            var bytes = BitConverter.GetBytes(distance);
            bytes.CopyTo(cmd.Payload, 0);
            return cmd;
        }
        #endregion

        public static RootCommand getMarkerEraserCmd(bool MarkerUp, bool EraserUp)
        {
            RootCommand cmd = new RootCommand(RootDeviceCommand.MarkerEraserSetPosition);
            cmd.Payload[3] = (byte)(MarkerUp ? (EraserUp ? 3 : 2):1);
            return cmd;
        }

        public enum RootLEDLightState : byte
        {
            Off = 0,
            On = 1,
            Blink = 2,
            Spin = 3
        }

        public static RootCommand getSetLEDCmd(Color color, RootLEDLightState state = RootLEDLightState.On)
        {
            RootCommand cmd = new RootCommand();
            cmd.Device = DeviceCode.LEDLights;
            cmd.commandCode = 0x2;
            cmd.Payload[0] = (byte)state;
            cmd.Payload[1] = color.R;
            cmd.Payload[2] = color.G;
            cmd.Payload[3] = color.B;
            return cmd;
        }

        public static RootCommand ResetPositionCmd {
            get {
                return new RootCommand(RootDeviceCommand.MotorResetPosition);
            }
        }

        public static RootCommand getDisableEventsCmd()
        {
            RootCommand cmd = new RootCommand(RootDeviceCommand.DisableEvents);
            cmd.Payload[15] = 255;
            cmd.Payload[14] = 255;
            return cmd;
        }
    }
}
