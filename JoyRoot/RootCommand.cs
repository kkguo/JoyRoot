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
        public enum Devices : byte
        {
            General = 0,
            Motors = 1,
            MarkerNEraser = 2,
            LEDLights = 3,
            ColorSensor = 4,
            Sound = 5,
            IRProximity = 11,
            Bumpers = 12,
            LightSensor = 13,
            Battery = 14,
            Accelerometer = 16,
            TouchSensors = 17,
            DockingSensors = 19,
            CliffSensor = 20,
            Connectivity = 100,
        }
        public enum CommandTypes : UInt16
        {
            GetVersions = 0x0000,
            SetName = 0x0100,
            GetName = 0x0200,
            StopNReset = 0x0300,
            Disconnect = 0x0600,
            EnableEvents = 0x0700,
            DisableEvents = 0x900,
            GetEnabledEvents = 0xB00,
            GetSerialNumber = 0xE00,
            GetSKU = 0xF00,        
            SetMotorSpeed = 0x401,
            SetLeftSpeed = 0x601,
            SetRightSpeed = 0x701,
            DriveDistance = 0x801,
            RotateAngle = 0xC01,
            SetGravityCompensation = 0xD01,
            ResetPosition = 0xF01,
            GetPosition = 0x1001,
            NavigateToPosition=0x1101,
            Dock=0x1301,
            Undock=0x1401,
            DriveArc = 0x1B01,
            MarkerEraserSetPosition=0x0002,
            SetLEDAnimation=0x0203,
            GetColorSensorData=0x0104,
            PlayNote=0x0005,
            StopSound=0x0105,
            SayPhrase=0x0405,
            PlaySweep=0x0505,
            GetIRProximityValue=0x010B,
            GetPackedIRProximityValueNStates=0x020B,
            IRProximitySetEventThresholds=0x030B,
            IRProximityGetEventThresholds = 0x040B,
            LightSensorGetLightValues =0x010D,
            BatteryGetLevel=0x010E,
            AccelerometerGet=0x0110,
            GetDockingValues=0x0113,
            GetIPv4Addresses=0x0164,
            RequestEasyUpdate=0x0264,
        }
        public enum EventTypes : UInt16
        {
            StopProjectEvent=0x0400,
            MotorStallEvent=0x1D01,
            ColorSensorEvent=0x0204,
            BumperEvent=0x000C,
            LightEvent=0x000D,
            BatteryLevelEvent=0x000E,
            TouchSensorEvent=0x0011,
            CliffEvent=0x0014
        }
        #endregion
        
        private UInt16 command;
        public byte PacketID = 0;
        public byte[] Payload = new byte[16];

        public bool isEvent
        {
            get
            {
                return Enum.IsDefined(typeof(EventTypes), command);
            }
        }

        public CommandTypes Command
        {
            get
            {
                return (CommandTypes)command;
            }
        }
        public EventTypes Event {
            get
            {
                return (EventTypes)command;
            }
        }

        public RootCommand(CommandTypes cmd)
        {
            command = (UInt16)cmd;
        }
        /// <summary>
        /// unpack bytes into command, checksum in bytes will be discarded
        /// if input size smaller than 19, it will be padding with 0s.
        /// </summary>
        /// <param name="bytes">command stream</param>
        /// <returns></returns>
        public static RootCommand unpack(byte[] bytes)
        {
            byte[] newbytes = new byte[19]; // avoiding size issue, trim and padding
            for (int i=0; i< bytes.Length && i < 19; i++)
                newbytes[i] = bytes[i];
            RootCommand cmd = new RootCommand((CommandTypes)BitConverter.ToUInt16(newbytes, 0));
            cmd.PacketID = newbytes[2];
            for (int i = 0; i < 16; i++)
                cmd.Payload[i] = newbytes[i + 3];
            return cmd;
        }

        public byte[] pack()
        {
            byte[] newbytes = new byte[20];
            BitConverter.GetBytes(command).CopyTo(newbytes, 0);
            newbytes[2] = PacketID;
            Payload.CopyTo(newbytes, 3);
            // get crc
            byte crc = 0;
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
            //for (int c=0; c< 19; c++)
            //{
            //    for (int i=0; i< 8; i++)
            //    {
            //        byte b = (byte)(crc & 0x80);
            //        if ((newbytes[c] & (0x80>>i)) != 0)
            //        {
            //            b ^= 0x80;
            //        }
            //        crc <<= 1;
            //        if (b != 0)
            //            crc ^= 0x07;
            //    }
            //    crc &= 0xFF;
            //}
            newbytes[19] = crc;
            return newbytes;
        }

        public override string ToString()
        {
            byte[] bytes = pack();
            return Command.ToString() + ":" + BitConverter.ToString(bytes) + " CRC: " + bytes[19];
        }

        #region Device 0 - General
        public enum BoardType : byte
        {
            MainBoard = 0xA5,
            ColorBoard = 0xC6,
        }

        public static RootCommand disableEventsCmd(Devices device)
        {
            RootCommand cmd = new RootCommand(CommandTypes.DisableEvents);
            int byteind = 15 - ((int)device / 8);
            cmd.Payload[byteind] = (byte)(1 << ((int)device % 8));
            return cmd;
        }

        public static RootCommand enableEventsCmd(Devices device)
        {
            RootCommand cmd = new RootCommand(CommandTypes.DisableEvents);
            int byteind = 15 - ((int)device / 8);
            cmd.Payload[byteind] = (byte)(1 << ((int)device % 8));
            return cmd;
        }
        public static RootCommand getVersionsCmd(BoardType board)
        {
            RootCommand command = new RootCommand(CommandTypes.GetVersions);
            command.Payload[0] = (byte)board;
            return command;
        }
        #endregion

        #region Device 1 - Motor
        public static RootCommand setMotorSpeedCmd(Int32 left, Int32 right)
        {
            RootCommand cmd = new RootCommand(CommandTypes.SetMotorSpeed);
            BitConverter.GetBytes(left).Reverse().ToArray().CopyTo(cmd.Payload, 0);
            BitConverter.GetBytes(right).Reverse().ToArray().CopyTo(cmd.Payload, 4);
            return cmd;
        }
        public static RootCommand setLeftMotorSpeedCmd(Int32 left)
        {
            RootCommand cmd = new RootCommand(CommandTypes.SetLeftSpeed);
            BitConverter.GetBytes(left).Reverse().ToArray().CopyTo(cmd.Payload, 0);            
            return cmd;
        }
        public static RootCommand setRightMotorSpeedCmd(Int32 right)
        {
            RootCommand cmd = new RootCommand(CommandTypes.SetMotorSpeed);
            BitConverter.GetBytes(right).Reverse().ToArray().CopyTo(cmd.Payload, 0);
            return cmd;
        }

        public static RootCommand RotateAngleCmd(Int32 angle)
        {            
            RootCommand cmd = new RootCommand(RootCommand.CommandTypes.RotateAngle);
            BitConverter.GetBytes(angle).CopyTo(cmd.Payload, 0);
            return cmd;
        }

        public static RootCommand driveDistanceCmd(Int32 distance)
        {
            RootCommand cmd = new RootCommand(CommandTypes.DriveDistance);
            var bytes = BitConverter.GetBytes(distance);
            bytes.CopyTo(cmd.Payload, 0);
            return cmd;
        }

        public static RootCommand ResetPositionCmd
        {
            get
            {
                return new RootCommand(CommandTypes.ResetPosition);
            }
        }
        #endregion

        #region Device 2- Marker/Eraser
        public static RootCommand markerEraserCmd(bool MarkerUp, bool EraserUp)
        {
            RootCommand cmd = new RootCommand(CommandTypes.MarkerEraserSetPosition);
            cmd.Payload[3] = (byte)(MarkerUp ? (EraserUp ? 3 : 2):1);
            return cmd;
        }

        #endregion

        #region Device 3 - LED Lights
        public enum RootLEDLightState : byte
        {
            Off = 0,
            On = 1,
            Blink = 2,
            Spin = 3
        }

        public static RootCommand setLEDCmd(byte Red, byte Green, byte Blue, RootLEDLightState state)
        {
            RootCommand cmd = new RootCommand(CommandTypes.SetLEDAnimation);
            cmd.Payload[0] = (byte)state;
            cmd.Payload[1] = Red;
            cmd.Payload[2] = Green;
            cmd.Payload[3] = Blue;
            return cmd;
        }

        #endregion

        #region Device 4 - Color Sensor
        public static RootCommand getColorSensorDataCmd(byte sensorBank, byte Lighting, byte Format)
        {
            RootCommand cmd = new RootCommand(CommandTypes.GetColorSensorData);
            cmd.Payload[0] = sensorBank;
            cmd.Payload [1] = Lighting;
            cmd.Payload [2] = Format;
            return cmd;
        }
        #endregion

        #region Device 5 - Sound
        public static RootCommand playSoundCmd(UInt32 frequency, UInt16 duration)
        {
            RootCommand cmd = new RootCommand(CommandTypes.PlayNote);
            var bytes = BitConverter.GetBytes((UInt32)frequency);
            cmd.Payload[3] = bytes[0];
            cmd.Payload[2] = bytes[1];
            cmd.Payload[1] = bytes[2];
            cmd.Payload[0] = bytes[3];
            bytes = BitConverter.GetBytes((UInt16)duration);
            cmd.Payload[4] = bytes[1];
            cmd.Payload[5] = bytes[0];
            return cmd;
        }
        #endregion

        #region Device 11 - IR Proximity

        #endregion

        #region Device 12 - Bumpers

        #endregion

        #region Device 13 - Light Sensors
        #endregion

        #region Device 14 - Battery

        #endregion

        #region Device 16 - Accelerometer

        #endregion

        #region Device 17 - Touch Sensors

        #endregion

        #region Device 19 - Docking Sensors

        #endregion

        #region Device 20 - Cliff Sensor

        #endregion

        #region Device 100 - Connectivity
        #endregion
    }
}
