using System;
using System.Collections.Generic;

using ROM = System.ReadOnlyMemory<byte>;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class MacCommand(byte cid, int length)
    {
        public byte CID { get; } = cid;
        public int Length { get; } = length;
        public abstract byte[] Value { get; }
    }
    public class LinkCheckReq() : MacCommand(0x02, 0)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = [0x02];
    }
    public class LinkCheckAns(ROM data) : MacCommand(0x02, Length)
    {
        public static new readonly int Length = 3;
        public byte Margin { get; } = data.Span[1];
        public byte GwCnt { get; } = data.Span[2];
        public override byte[] Value { get; } = data.ToArray();
    }
    public class LinkADRReq(ROM data) : MacCommand(0x03, Length)
    {
        public static new readonly int Length = 5;
        public int DataRate { get; } = data.Span[0] & 0x0F;
        public int TxPower { get; } = (data.Span[0] & 0xF0) >> 4;
        public bool Channel1 { get; } = (data.Span[1] & 0x01) == 0x01;
        public bool Channel2 { get; } = (data.Span[1] & 0x02) == 0x02;
        public bool Channel3 { get; } = (data.Span[1] & 0x04) == 0x04;
        public bool Channel4 { get; } = (data.Span[1] & 0x08) == 0x08;
        public bool Channel5 { get; } = (data.Span[1] & 0x10) == 0x10;
        public bool Channel6 { get; } = (data.Span[1] & 0x20) == 0x20;
        public bool Channel7 { get; } = (data.Span[1] & 0x40) == 0x40;
        public bool Channel8 { get; } = (data.Span[1] & 0x80) == 0x80;
        public bool Channel9 { get; } = (data.Span[2] & 0x01) == 0x01;
        public bool Channel10 { get; } = (data.Span[2] & 0x02) == 0x02;
        public bool Channel11 { get; } = (data.Span[2] & 0x04) == 0x04;
        public bool Channel12 { get; } = (data.Span[2] & 0x08) == 0x08;
        public bool Channel13 { get; } = (data.Span[2] & 0x10) == 0x10;
        public bool Channel14 { get; } = (data.Span[2] & 0x20) == 0x20;
        public bool Channel15 { get; } = (data.Span[2] & 0x40) == 0x40;
        public bool Channel16 { get; } = (data.Span[2] & 0x80) == 0x80;
        public byte Redundancy { get; } = data.Span[3];
        public override byte[] Value { get; } = data.ToArray();
    }
    public class LinkADRAns(ROM data) : MacCommand(0x03, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DutyCycleReq(ROM data) : MacCommand(0x04, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DutyCycleAns(ROM data) : MacCommand(0x04, Length)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class RXParamSetupReq(ROM data) : MacCommand(0x05, Length)
    {
        public static new readonly int Length = 5;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class RXParamSetupAns(ROM data) : MacCommand(0x05, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DevStatusReq(ROM data) : MacCommand(0x06, Length)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DevStatusAns(ROM data) : MacCommand(0x06, Length)
    {
        public DevStatusAns(byte battery, byte radioStatus) : this(new byte[] { 0x06, battery, radioStatus })
        { }
        public static new readonly int Length = 3;
        public override byte[] Value { get; } = data.ToArray();
        /// <summary>
        /// 0 - The end device is connected to an external power source
        /// 1..254 - The battery level, 1 is minimum and 254 is maximum battery level
        /// 255 - The end-device was not able to measure the battery level
        /// </summary>
        public byte Battery { get; } = data.Span[1];

        /// <summary>
        /// 7:6 - RFU
        /// 5:0 - SNR
        /// </summary>
        public byte RadioStatus { get; } = data.Span[2];
    }
    public class NewChannelReq(ROM data) : MacCommand(0x07, Length)
    {
        public static new readonly int Length = 6;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class NewChannelAns(ROM data) : MacCommand(0x07, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class RXTimingSetupReq(ROM data) : MacCommand(0x08, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class RXTimingSetupAns(ROM data) : MacCommand(0x08, Length)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class TxParamSetupReq(ROM data) : MacCommand(0x09, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class TxParamSetupAns() : MacCommand(0x09, Length)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = [0x09];
    }
    public class DlChannelReq(ROM data) : MacCommand(0x0A, Length)
    {
        public static new readonly int Length = 5;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DlChannelAns(ROM data) : MacCommand(0x0A, Length)
    {
        public static new readonly int Length = 2;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DeviceTimeReq(ROM data) : MacCommand(0x0D, Length)
    {
        public static new readonly int Length = 1;
        public override byte[] Value { get; } = data.ToArray();
    }
    public class DeviceTimeAns(ROM data) : MacCommand(0x0D, Length)
    {
        public static new readonly int Length = 6;
        public override byte[] Value { get; } = data.ToArray();
    }

    public static class MacCommandFactory
    {
        public static IReadOnlyList<MacCommand> Create(bool isUplink, ROM data)
        {
            var commands = new List<MacCommand>();
            var d = data;
            while (d.Length > 0)
            {
                switch (data.Span[0])
                {
                    case 0x02:
                        if (isUplink)
                        {
                            commands.Add(new LinkCheckReq());
                            d = d[LinkCheckReq.Length..];
                        }
                        else
                        {
                            commands.Add(new LinkCheckAns(data[..LinkCheckAns.Length]));
                            d = d[LinkCheckAns.Length..];
                        }
                        break;
                    case 0x03:
                        if (isUplink)
                        {
                            commands.Add(new LinkADRAns(data[..LinkADRAns.Length]));
                            d = d[LinkADRAns.Length..];
                        }
                        else
                        {
                            commands.Add(new LinkADRReq(data[..LinkADRReq.Length]));
                            d = d[LinkADRReq.Length..];
                        }
                        break;
                    case 0x04:
                        if (isUplink)
                        {
                            commands.Add(new DutyCycleAns(data[..DutyCycleAns.Length]));
                            d = d[DutyCycleAns.Length..];
                        }
                        else
                        {
                            commands.Add(new DutyCycleReq(data[..DutyCycleReq.Length]));
                            d = d[DutyCycleReq.Length..];
                        }
                        break;
                    case 0x05:
                        if (isUplink)
                        {
                            commands.Add(new RXParamSetupAns(data[..RXParamSetupAns.Length]));
                            d = d[RXParamSetupAns.Length..];
                        }
                        else
                        {
                            commands.Add(new RXParamSetupReq(data[..RXParamSetupReq.Length]));
                            d = d[RXParamSetupReq.Length..];
                        }
                        break;
                    case 0x06:
                        if (isUplink)
                        {
                            commands.Add(new DevStatusAns(data[..DevStatusAns.Length]));
                            d = d[DevStatusAns.Length..];
                        }
                        else
                        {
                            commands.Add(new DevStatusReq(data[..DevStatusReq.Length]));
                            d = d[DevStatusReq.Length..];
                        }
                        break;
                    case 0x07:
                        if (isUplink)
                        {
                            commands.Add(new NewChannelAns(data[..NewChannelAns.Length]));
                            d = d[NewChannelAns.Length..];
                        }
                        else
                        {
                            commands.Add(new NewChannelReq(data[..NewChannelReq.Length]));
                            d = d[NewChannelReq.Length..];
                        }
                        break;
                    case 0x08:
                        if (isUplink)
                        {
                            commands.Add(new RXTimingSetupAns(data[..RXTimingSetupAns.Length]));
                            d = d[RXTimingSetupAns.Length..];
                        }
                        else
                        {
                            commands.Add(new RXTimingSetupReq(data[..RXTimingSetupReq.Length]));
                            d = d[RXTimingSetupReq.Length..];
                        }
                        break;
                    case 0x09:
                        if (isUplink)
                        {
                            commands.Add(new TxParamSetupAns());
                            d = d[TxParamSetupAns.Length..];
                        }
                        else
                        {
                            commands.Add(new TxParamSetupReq(data[..TxParamSetupReq.Length]));
                            d = d[TxParamSetupReq.Length..];
                        }
                        break;
                    case 0x0A:
                        if (isUplink)
                        {
                            commands.Add(new DlChannelAns(data[..DlChannelAns.Length]));
                            d = d[DlChannelAns.Length..];
                        }
                        else
                        {
                            commands.Add(new DlChannelReq(data[..DlChannelReq.Length]));
                            d = d[DlChannelReq.Length..];
                        }
                        break;
                    case 0x0D:
                        if (isUplink)
                        {
                            commands.Add(new DeviceTimeReq(data[..DeviceTimeReq.Length]));
                            d = d[DeviceTimeReq.Length..];
                        }
                        else
                        {
                            commands.Add(new DeviceTimeAns(data[..DeviceTimeReq.Length]));
                            d = d[DeviceTimeReq.Length..];
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown MAC Command type");
                }
            }
            return commands;
        }
    }
}
