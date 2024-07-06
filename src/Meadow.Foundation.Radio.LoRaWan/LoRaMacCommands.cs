using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal abstract class MacCommand(byte cid)
    {
        public byte CID { get; } = cid;
    }
    internal class LinkCheckReq() : MacCommand(0x02);
    internal class LinkCheckAns(ReadOnlyMemory<byte> data) : MacCommand(0x02)
    {
        public static int Length { get; } = 2;
        public byte Margin { get; } = data.Span[0];
        public byte GwCnt { get; } = data.Span[1];
    }
    internal class LinkADRReq(ReadOnlyMemory<byte> data) : MacCommand(0x03)
    {
        public static int Length { get; } = 4;
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
    }
    internal class LinkADRAns() : MacCommand(0x03);
    internal class DutyCycleReq() : MacCommand(0x04);
    internal class DutyCycleAns() : MacCommand(0x04);
    internal class RXParamSetupReq() : MacCommand(0x05);
    internal class RXParamSetupAns() : MacCommand(0x05);
    internal class DevStatusReq() : MacCommand(0x06);
    internal class DevStatusAns() : MacCommand(0x06);
    internal class NewChannelReq() : MacCommand(0x07);
    internal class NewChannelAns() : MacCommand(0x07);
    internal class RXTimingSetupReq() : MacCommand(0x08);
    internal class RXTimingSetupAns() : MacCommand(0x08);
    internal class TxParamSetupReq() : MacCommand(0x09);
    internal class TxParamSetupAns() : MacCommand(0x09);
    internal class DlChannelReq() : MacCommand(0x0A);
    internal class DlChannelAns() : MacCommand(0x0A);
    internal class DeviceTimeReq() : MacCommand(0x0D);
    internal class DeviceTimeAns() : MacCommand(0x0D);

    internal class MacCommandFactory
    {
        public ICollection<MacCommand> Create(ReadOnlyMemory<byte> data)
        {
            var commands = new List<MacCommand>();
            var d = data;
            while (d.Length > 0)
            {
                switch (data.Span[0])
                {
                    case 0x02:
                        commands.Add(new LinkCheckAns(data[1..LinkCheckAns.Length]));
                        d = d[LinkCheckAns.Length..];
                        break;
                    case 0x03:
                        commands.Add(new LinkADRReq(data[1..LinkADRReq.Length]));
                        d = d[LinkADRReq.Length..];
                        break;
                    //case 0x04:
                    //    return new DutyCycleReq();
                    //case 0x05:
                    //    return new RXParamSetupReq();
                    //case 0x06:
                    //    return new DevStatusReq();
                    //case 0x07:
                    //    return new NewChannelReq();
                    //case 0x08:
                    //    return new RXTimingSetupReq();
                    //case 0x09:
                    //    return new TxParamSetupReq();
                    //case 0x0A:
                    //    return new DlChannelReq();
                    //case 0x0D:
                    //    return new DeviceTimeReq();
                    default:
                        throw new NotImplementedException();
                }
            }
            return commands;
        }
    }
}
