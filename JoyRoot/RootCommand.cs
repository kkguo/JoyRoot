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
    public class RootCommand
    {
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
        public enum GeneralCommand : byte
        {
            GetVersion = 0,
            SetName = 1,
            GetName = 2,
            StopNReset = 3,
            Disconnect = 4,
            EnableEvents = 5,
            DisableEvents = 6,
            GetEnabledEvents = 7,
            GetSerialNumber = 8,
            GetSKU = 9
        }
        public enum MotorsCommand : byte
        {
            SetSpeed = 4,
            SetLeftSpeed = 6,
            SetRightSpeed = 7,
            DriveDistance = 8,
            Rotate = 12,
            SetGravityCompensation = 13,
            ResetPosition = 15,
            GetPosition = 16,
            DriveArc = 27
        }
        public enum PacketIDType : byte
        {
            Inc,
            Req,
            Evt
        }
        public enum MarkerCommand : byte
        {
            SetPosition = 0,
        }

        protected DeviceCode Device;
        protected byte Command;

        private static byte currCmdID = 0;
        protected PacketIDType IDType = PacketIDType.Inc;
        private byte idSet;
        protected byte PacketID
        {
            get
            {
                if (IDType == PacketIDType.Inc)
                {
                    //return currCmdID++;
                    return 0;
                } else
                {
                    return idSet;
                }
            }
            set
            {
                if (value == 0)
                {
                    currCmdID = 0;
                } else
                {
                    idSet = value;
                }
            }
        }
        protected byte[] Payload = new byte[16];

        protected byte checksum
        {
            get
            {
                byte[] newbytes = new byte[19];
                byte crc = 0;
                newbytes[0] = (byte)Device;
                newbytes[1] = Command;
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
            newbytes[1] = Command;
            newbytes[2] = PacketID;
            Payload.CopyTo(newbytes, 3);
            newbytes[19] = checksum;
            return newbytes;
        }

        public override string ToString()
        {
            return BitConverter.ToString(pack());
        }

        public RootCommand() { }

        public RootCommand(byte[] bytes)
        {
            Device = (DeviceCode)bytes[0];
            Command = bytes[1];
            PacketID = bytes[2];
            Payload = bytes.Skip(3).Take(16).ToArray();
        }

        #region Motor Command
        public static RootCommand MoveCommand(Int32 left, Int32 right)
        {
            RootCommand cmd = new RootCommand();
            cmd.Device = DeviceCode.Motors;
            cmd.Command = (byte)MotorsCommand.SetSpeed;
            BitConverter.GetBytes(left).Reverse().ToArray().CopyTo(cmd.Payload, 0);
            BitConverter.GetBytes(right).Reverse().ToArray().CopyTo(cmd.Payload, 4);
            return cmd;
        }

        public static RootCommand stopMoveCmd
        {
            get
            {
                return MoveCommand(0, 0);
            }
        }

        public static RootCommand moveForwardCmd
        {
            get
            {
                return MoveCommand(100, 100);
            }
        }

        public static RootCommand moveBackwardCmd
        {
            get
            {
                return MoveCommand(-100, -100);
            }
        }

        public static RootCommand turnLeftCmd
        {
            get
            {
                return MoveCommand(-100, 100);
            }
        }

        public static RootCommand turnRightCmd
        {
            get
            {
                return MoveCommand(100, -100);
            }
        }

        public static RootCommand moveDistanceCmd(Int32 distance)
        {
            RootCommand cmd = new RootCommand();
            cmd.Device = DeviceCode.Motors;
            cmd.Command = (byte)MotorsCommand.DriveDistance;
            var bytes = BitConverter.GetBytes(distance);
            bytes.CopyTo(cmd.Payload, 0);
            return cmd;
        }
        #endregion

        public static RootCommand MoveMarkerCmd()
        {
            RootCommand cmd = new RootCommand();
            cmd.Device = DeviceCode.MarkerNEraser;
            cmd.Command = (byte)MarkerCommand.SetPosition;
            
            return cmd;
        }

        public enum LEDState : byte
        {
            Off = 0,
            On = 1,
            Blink = 2,
            Spin = 3
        }

        public static RootCommand getSetLEDCmd(byte R, byte G, byte B, LEDState state = LEDState.On)
        {
            RootCommand cmd = new RootCommand();
            cmd.Device = DeviceCode.LEDLights;
            cmd.Command = 0x2;
            cmd.Payload[0] = (byte)state;
            cmd.Payload[1] = R;
            cmd.Payload[2] = G;
            cmd.Payload[3] = B;
            return cmd;
        }

    }
}
