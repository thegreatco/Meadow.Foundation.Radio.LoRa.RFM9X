using Meadow.Units;

using System;
using System.Text;

using ROM = System.ReadOnlyMemory<byte>;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal readonly record struct FieldSizes
    {
        public const int MhdrFieldSize = 1;
        public const int JoinTypeFieldSize = 1;
        public const int JoinEuiFieldSize = 8;
        public const int DevEuiFieldSize = 8;
        public const int DevNonceFieldSize = 2;
        public const int JoinNonceFieldSize = 3;
        public const int RjCount0FieldSize = 2;
        public const int RjCount1FieldSize = 2;
        public const int NetIdFieldSize = 3;
        public const int DevAddrFieldSize = 4;
        public const int DlSettingsFieldSize = 1;
        public const int RxDelayFieldSize = 1;
        public const int CfListFieldSize = 16;
        public const int FhdrDevAddrFieldSize = DevAddrFieldSize;
        public const int FCtrlFieldSize = 1;
        public const int FcntFieldSize = 2;
        public const int FoptsMaxFieldSize = 15;
        public const int FPortFieldSize = 1;
        public const int MacPayloadFieldMaxSize = 242;
        public const int MicFieldSize = 4;
    }

    internal readonly record struct MessageSizes
    {
        public const int JoinRequestMessageSize = FieldSizes.MhdrFieldSize +
                                                  FieldSizes.JoinEuiFieldSize +
                                                  FieldSizes.DevEuiFieldSize +
                                                  FieldSizes.DevNonceFieldSize +
                                                  FieldSizes.MicFieldSize;

        public const int ReJoin0MessageSize = FieldSizes.MhdrFieldSize +
                                              FieldSizes.JoinTypeFieldSize +
                                              FieldSizes.NetIdFieldSize +
                                              FieldSizes.DevEuiFieldSize +
                                              FieldSizes.RjCount0FieldSize +
                                              FieldSizes.MicFieldSize;

        public const int ReJoin1MessageSize = FieldSizes.MhdrFieldSize +
                                              FieldSizes.JoinTypeFieldSize +
                                              FieldSizes.JoinEuiFieldSize +
                                              FieldSizes.DevEuiFieldSize +
                                              FieldSizes.RjCount1FieldSize +
                                              FieldSizes.MicFieldSize;

        public const int JoinAcceptFrameMinimumSize = FieldSizes.MhdrFieldSize +
                                                      FieldSizes.JoinNonceFieldSize +
                                                      FieldSizes.NetIdFieldSize +
                                                      FieldSizes.DevAddrFieldSize +
                                                      FieldSizes.DlSettingsFieldSize +
                                                      FieldSizes.RxDelayFieldSize +
                                                     FieldSizes.MicFieldSize;

        public const int JoinAcceptFrameMaximumSize = FieldSizes.MhdrFieldSize +
                                                      FieldSizes.JoinNonceFieldSize +
                                                      FieldSizes.NetIdFieldSize +
                                                      FieldSizes.DevAddrFieldSize +
                                                      FieldSizes.DlSettingsFieldSize +
                                                      FieldSizes.RxDelayFieldSize +
                                                      FieldSizes.CfListFieldSize +
                                                      FieldSizes.MicFieldSize;

        public const int JoinAcceptMicOffset = FieldSizes.MhdrFieldSize +
                                               FieldSizes.JoinTypeFieldSize +
                                               FieldSizes.JoinEuiFieldSize +
                                               FieldSizes.DevNonceFieldSize;

        public const int FramePayloadOverheadSize = FieldSizes.MhdrFieldSize + FieldSizes.FhdrDevAddrFieldSize + FieldSizes.FcntFieldSize + FieldSizes.FcntFieldSize + FieldSizes.FPortFieldSize + FieldSizes.MicFieldSize;
        public const int FramePayloadMinimumSize = FieldSizes.MhdrFieldSize + FieldSizes.DevAddrFieldSize + FieldSizes.FCtrlFieldSize + FieldSizes.FcntFieldSize + FieldSizes.MicFieldSize;
        public const int FramePayloadMaximumSize = FieldSizes.MhdrFieldSize +
                                                   FieldSizes.DevAddrFieldSize +
                                                   FieldSizes.FCtrlFieldSize +
                                                   FieldSizes.FcntFieldSize +
                                                   FieldSizes.FPortFieldSize +
                                                   FieldSizes.MacPayloadFieldMaxSize +
                                                   FieldSizes.MicFieldSize;
    }

    internal readonly record struct JoinRequestOffsets
    {

    }

    public enum PacketType : byte
    {
        JoinRequest = 0b000,
        JoinResponse = 0b001,
        UnconfirmedDataUp = 0b010,
        UnconfirmedDataDown = 0b011,
        ConfirmedDataUp = 0b100,
        ConfirmedDataDown = 0b101,
        RejoinRequest = 0b110,
        Reserved = 0b111
    }

    public abstract record FrameControl(bool Adr,
                                    bool AdrAckReq,
                                    bool Ack,
                                    bool Rfu,
                                    bool FPending,
                                    byte FOptsLength)
    {
        public bool Adr = Adr;
        public bool Ack = Ack;
        public bool AdrAckReq = AdrAckReq;
        public bool Rfu = Rfu;
        public byte FOptsLength { get; set; } = FOptsLength;
        public bool FPending = FPending;

        public abstract byte Value { get; }
    }

    public record UplinkFrameControl(bool Adr,
                                     bool AdrAckReq,
                                     bool Ack,
                                     bool Rfu,
                                     byte FOptsLength = 0x00)
        : FrameControl(Adr, AdrAckReq, Ack, Rfu, false, FOptsLength)
    {
        public UplinkFrameControl(byte value)
            : this((value & 0b10000000) != 0,
                   (value & 0b01000000) != 0,
                   (value & 0b00100000) != 0,
                   (value & 0b00010000) != 0,
                   (byte)(value & 0x0F))
        {
        }

        public override byte Value => (byte)((Adr ? 0b10000000 : 0) |
                                             (AdrAckReq ? 0b01000000 : 0) |
                                             (Ack ? 0b00100000 : 0) |
                                             (Rfu ? 0b00010000 : 0) |
                                             (FOptsLength & 0x0F));
    }

    public record DownlinkFrameControl(bool Adr,
                                       bool Rfu,
                                       bool Ack,
                                       bool FPending,
                                       byte FOptsLength = 0x00)
        : FrameControl(Adr, false, Ack, Rfu, FPending, FOptsLength)
    {
        public DownlinkFrameControl(byte value)
            : this((value & 0b10000000) != 0,
                   (value & 0b01000000) != 0,
                   (value & 0b00100000) != 0,
                   (value & 0b00010000) != 0,
                   (byte)(value & 0x0F))
        {
        }

        public override byte Value => (byte)((Adr ? 0b10000000 : 0) |
                                             (Rfu ? 0b01000000 : 0) |
                                             (Ack ? 0b00100000 : 0) |
                                             (FPending ? 0b00010000 : 0) |
                                             (FOptsLength & 0x0F));
    }

    public readonly record struct MacHeader(byte Value)
    {
        public MacHeader(PacketType packetType, byte majorVersion)
            : this((byte)(((byte)packetType << 5) | majorVersion))
        {
        }
        public PacketType PacketType { get; } = (PacketType)((Value >> 5) & 0x07);
        public byte MajorVersion { get; } = (byte)(Value & 0x03);
        public byte Value { get; } = Value;
        public int Length { get; } = 1;
    }

    public readonly record struct FrameHeader(DeviceAddress DeviceAddress, FrameControl FrameControl, int FrameCount, ROM FOptions)
    {
        public DeviceAddress DeviceAddress { get; } = DeviceAddress;
        public FrameControl FrameControl { get; } = FrameControl;
        public int FrameCount { get; } = FrameCount;
        public ROM FOptions { get; } = FOptions;
        public ROM Value
        {
            get
            {
                var frameCount = BitConverter.GetBytes(FrameCount).AsSpan(0, 2);
                var bytes = new byte[DeviceAddress.Length + 1 + frameCount.Length + FOptions.Length];
                DeviceAddress.Value.CopyToReverse(bytes);
                bytes[DeviceAddress.Length] = FrameControl.Value;
                frameCount.CopyTo(bytes.AsSpan(DeviceAddress.Length + 1));
                if (FOptions.Length > 0)
                    FOptions.Span.CopyTo(bytes.AsSpan(DeviceAddress.Length + 1 + frameCount.Length));
                return bytes;
            }
        }
    }

    public record struct JoinRequest(AppKey AppKey, JoinEui JoinEui, DevEui DeviceEui, DeviceNonce DeviceNonce)
    {
        #region ranges
        private static int PayloadSize = FieldSizes.MhdrFieldSize + FieldSizes.JoinEuiFieldSize + FieldSizes.DevEuiFieldSize + FieldSizes.DevNonceFieldSize + FieldSizes.MicFieldSize;
        private static Range MicRange = ^4..;
        private static Range MacPayloadRange = FieldSizes.MhdrFieldSize..^4;
        private static Range MacPayloadRangeWithMic = FieldSizes.MhdrFieldSize..;
        private static Range MacHeaderRange = 0..FieldSizes.MhdrFieldSize;
        private static Range JoinEuiRange = MacHeaderRange.End..(MacHeaderRange.End.Value + FieldSizes.JoinEuiFieldSize);
        private static Range DevEuiRange = JoinEuiRange.End..(JoinEuiRange.End.Value + FieldSizes.DevEuiFieldSize);
        private static Range DevNonceRange = DevEuiRange.End..(DevEuiRange.End.Value + FieldSizes.DevNonceFieldSize);
        #endregion
        private byte[] _encryptedMacPayload;
        private AppKey AppKey { get; set; } = AppKey;
        public MacHeader MacHeader { get; private set; } = new MacHeader(0x00);
        public JoinEui JoinEui { get; private set; } = JoinEui;
        public DevEui DeviceEui { get; private set; } = DeviceEui;
        public DeviceNonce DeviceNonce { get; private set; } = DeviceNonce;
        public Mic Mic
        {
            get
            {
                if (AppKey == null)
                    throw new ArgumentNullException(nameof(AppKey));
                return new Mic(EncryptionTools.ComputeAesCMac(AppKey.Value, [0x00])[..4]);
            }
        }

        public byte[] PhyPayload
        {
            get
            {
                var mic = Mic;
                var mac = MacPayload;
                var bytes = new byte[PayloadSize];
                bytes[0] = MacHeader.Value;
                mac.CopyTo(bytes.AsSpan(1));
                Mic.Value.CopyTo(bytes.AsSpan(1 + mac.Length));
                return bytes;
            }
        }

        public byte[] MacPayload
        {
            get
            {
                var bytes = new byte[JoinEui.Length + DeviceEui.Length + DeviceNonce.Length];
                JoinEui.Value.CopyTo(bytes, 0);
                DeviceEui.Value.CopyTo(bytes.AsSpan(JoinEui.Length));
                DeviceNonce.Value.CopyTo(bytes.AsSpan(JoinEui.Length + DeviceEui.Length));
                return bytes;
            }
        }

        // TODO: Add a method to check the MIC

        public static JoinRequest FromPhy(ROM data)
        {
            var j = new JoinRequest()
            {
                MacHeader = new MacHeader(data.Span[0]),
                JoinEui = new JoinEui(data[1..9].ToArray()),
                DeviceEui = new DevEui(data[9..17].ToArray()),
                DeviceNonce = new DeviceNonce(data[17..19].ToArray()),
            };
            var calculatedMic = new Mic(data[MicRange].ToArray());
            if (j.Mic.Value.IsEqual(calculatedMic.Value) == false)
                throw new MicMismatchException(calculatedMic, j.Mic);
            return j;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Join Request");
            sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine();
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {MacHeader.Value.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"                   MIC = {Mic.Value.ToHexString()}");
            sb.AppendLine("");
            sb.AppendLine($"          ( MACPayload = AppEUI[8] | DevEUI[8] | DevNonce[2] )");
            sb.AppendLine($"                AppEUI = {JoinEui.Value.ToHexString()}");
            sb.AppendLine($"                DevEUI = {DeviceEui.Value.ToHexString()}");
            sb.AppendLine($"              DevNonce = {DeviceNonce.Value.ToHexString()}");
            return sb.ToString();
        }
    }

    public record struct JoinAccept(
        AppKey AppKey,
        MacHeader MacHeader,
        JoinNonce JoinNonce,
        NetworkId NetworkId,
        DeviceAddress DeviceAddress,
        DownlinkSettings DownlinkSettings,
        int ReceiveDelay,
        ICFList CFList)
    {
        #region ranges
        private static Range MacPayloadRange = FieldSizes.MhdrFieldSize..^4;
        private static Range MacPayloadRangeWithMic = FieldSizes.MhdrFieldSize..;
        private static Range MacHeaderRange = 0..FieldSizes.MhdrFieldSize;
        private static Range JoinNonceRange = MacHeaderRange.End.Value..(MacHeaderRange.End.Value + FieldSizes.JoinNonceFieldSize);
        private static Range NetIdRange = JoinNonceRange.End.Value..(JoinNonceRange.End.Value + FieldSizes.NetIdFieldSize);
        private static Range DevAddrRange = NetIdRange.End.Value..(NetIdRange.End.Value + FieldSizes.DevAddrFieldSize);
        private static Range DlSettingsRange = DevAddrRange.End.Value..(DevAddrRange.End.Value + FieldSizes.DlSettingsFieldSize);
        private static Range RxDelayRange = DlSettingsRange.End.Value..(DlSettingsRange.End.Value + FieldSizes.RxDelayFieldSize);
        private static Range CFListRange = RxDelayRange.End.Value..(RxDelayRange.End.Value + FieldSizes.CfListFieldSize);
        private static Range MicRange = ^4..;
        #endregion

        private byte[]? _phyPayload;
        private AppKey AppKey { get; set; } = AppKey;
        public MacHeader MacHeader { get; private set; } = MacHeader;
        public JoinNonce JoinNonce { get; private set; } = JoinNonce;
        public NetworkId NetworkId { get; private set; } = NetworkId;
        public DeviceAddress DeviceAddress { get; private set; } = DeviceAddress;
        public DownlinkSettings DownlinkSettings { get; private set; } = DownlinkSettings;
        public int ReceiveDelay { get; private set; } = ReceiveDelay;
        public ICFList? CFList { get; private set; } = CFList;

        public Mic Mic
        {
            get
            {
                if (AppKey == null)
                    throw new ArgumentNullException(nameof(AppKey));
                var macPayload = MacPayload;
                var b = new byte[1 + macPayload.Length];
                b[0] = MacHeader.Value;
                macPayload.CopyTo(b.AsSpan(1));
                return new Mic(EncryptionTools.ComputeAesCMac(AppKey.Value, b)[..4]);
            }
        }

        public byte[] MacPayload
        {
            get
            {
                var b = new byte[
                    JoinNonce.Length +
                    NetworkId.Length +
                    DeviceAddress.Length +
                    DownlinkSettings.Length +
                    1 +
                    (CFList == null ? 0 : CFList.Length)];
                JoinNonce.Value.CopyTo(b.AsSpan(JoinNonceRange.Start.Value - 1));
                NetworkId.Value.CopyTo(b.AsSpan(NetIdRange.Start.Value - 1));
                DeviceAddress.Value.CopyTo(b.AsSpan(DevAddrRange.Start.Value - 1));
                b[DlSettingsRange.Start.Value - 1] = DownlinkSettings.Value;
                b[RxDelayRange.Start.Value - 1] = (byte)ReceiveDelay;
                if (CFList != null)
                {
                    CFList.Value.CopyTo(b.AsSpan(CFListRange.Start.Value - 1));
                }
                return b;
            }
        }

        public byte[] MacPayloadWithMic
        {
            get
            {
                var macPayload = MacPayload;
                var mic = Mic.Value;
                var b = new byte[macPayload.Length + mic.Length];
                macPayload.CopyTo(b, 0);
                mic.CopyTo(b.AsSpan(macPayload.Length));
                return b;
            }
        }

        public byte[] EncryptedMacPayload
        {
            get
            {
                return EncryptionTools.EncryptMessage(AppKey.Value, MacPayloadWithMic);
            }
        }

        public static JoinAccept FromPhy(AppKey appKey, ROM data)
        {
            var decryptedData = EncryptionTools.DecryptMessage(appKey.Value, data[MacPayloadRangeWithMic]);
            var d = new byte[data.Length];
            d[MacHeaderRange.Start] = data.Span[MacHeaderRange.Start];
            decryptedData.CopyTo(d.AsSpan(MacHeaderRange.End));
            var j = new JoinAccept()
            {
                AppKey = appKey,
                _phyPayload = data.ToArray(),
                MacHeader = new MacHeader(d[MacHeaderRange][0]),
                JoinNonce = new JoinNonce(d[JoinNonceRange]),
                NetworkId = new NetworkId(d[NetIdRange]),
                DeviceAddress = new DeviceAddress(d[DevAddrRange]),
                DownlinkSettings = new DownlinkSettings(d[DlSettingsRange][0]),
                ReceiveDelay = d[RxDelayRange][0],
                CFList = d.Length == MessageSizes.JoinAcceptFrameMinimumSize ? null : d[CFListRange.End] == 0x00 ? new CFListType0(d[CFListRange]) : new CFListType1(d[CFListRange])
            };

            var calculatedMic = new Mic(d[MicRange]);
            if (j.Mic.Value.IsEqual(calculatedMic.Value) == false)
                throw new MicMismatchException(calculatedMic, j.Mic);

            return j;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Join Accept" + "");
            sb.AppendLine($"            PHYPayload = {_phyPayload?.ToHexString(false)}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {MacHeader.Value.ToHexString(false)}");
            sb.AppendLine($"  Encrypted MACPayload = {EncryptedMacPayload.ToHexString(false)}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString(false)}");
            sb.AppendLine($"                   MIC = {Mic.Value.ToHexString(false)}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = JoinNonce[3] | NetID[3] | DevAddr[4] | DLSettings[1] | RxDelay[1] | CFList[0|15] )");
            sb.AppendLine($"              AppNonce = {JoinNonce.Value.ToHexString(false)}");
            sb.AppendLine($"                 NetID = {NetworkId.Value.ToHexString(false)}");
            sb.AppendLine($"            DevAddress = {DeviceAddress.Value.ToHexString(false)}");
            sb.AppendLine($"            DLSettings = {DownlinkSettings}");
            sb.AppendLine($"               RxDelay = {ReceiveDelay}");
            sb.AppendLine($"                CFList = {CFList?.Value.ToHexString(false)}");
            sb.AppendLine($"");
            sb.AppendLine(CFList?.ToString());
            sb.AppendLine($"");
            return sb.ToString();
        }
    }

    public record struct UnconfirmedUplinkMessage(
        AppSKey AppSKey,
        NetworkSKey NetworkSKey,
        FrameHeader FrameHeader,
        int FCount,
        byte FPort,
        byte[] FrmPayload)
    {
        #region ranges
        private static int PayloadSize = FieldSizes.MhdrFieldSize + FieldSizes.JoinEuiFieldSize + FieldSizes.DevEuiFieldSize + FieldSizes.DevNonceFieldSize + FieldSizes.MicFieldSize;
        private static Range MicRange = ^4..;
        private static Range MacHeaderRange = 0..FieldSizes.MhdrFieldSize;
        private static Range MacPayloadRange = FieldSizes.MhdrFieldSize..^4;
        private static Range MacPayloadRangeWithMic = FieldSizes.MhdrFieldSize..;
        private static Range JoinEuiRange = MacHeaderRange.End..(MacHeaderRange.End.Value + FieldSizes.JoinEuiFieldSize);
        private static Range DevEuiRange = JoinEuiRange.End..(JoinEuiRange.End.Value + FieldSizes.DevEuiFieldSize);
        private static Range DevNonceRange = DevEuiRange.End..(DevEuiRange.End.Value + FieldSizes.DevNonceFieldSize);
        #endregion
        private AppSKey _appSKey = AppSKey;
        private NetworkSKey _networkSKey = NetworkSKey;
        public MacHeader MacHeader { get; } = new MacHeader(PacketType.UnconfirmedDataUp, 0x00);
        public FrameHeader FrameHeader { get; } = FrameHeader;
        public int FCount { get; } = FCount;
        public byte FPort { get; } = FPort;

        public byte[] MacPayload
        {
            get
            {
                // the 1 below is for the FPort
                var b = new byte[FrameHeader.Value.Length + 1 + FrmPayload.Length];
                FrameHeader.Value.CopyTo(b);
                b[FrameHeader.Value.Length] = FPort;
                FrmPayload.CopyTo(b.AsSpan(FrameHeader.Value.Length + 1));
                return b;
            }
        }

        public byte[] FrmPayload { get; } = EncryptionTools.EncryptMessage(AppSKey.Value, true, FrameHeader.DeviceAddress, FCount, FrmPayload);

        public Mic Mic
        {
            get
            {
                var micIn = new byte[16 + 1 /*mhdr*/ + MacPayload.Length];
                micIn[0] = 0x49;
                micIn[1] = 0x00;
                micIn[2] = 0x00;
                micIn[3] = 0x00;
                micIn[4] = 0x00;
                micIn[5] = 0x00; // Uplink
                FrameHeader.DeviceAddress.Value.CopyTo(micIn, 6);
                BitConverter.GetBytes(FCount).CopyToReverse(micIn.AsSpan(10));
                micIn[14] = 0x00;
                FrmPayload.CopyTo(micIn, 16);
                return new Mic(EncryptionTools.ComputeAesCMac(_networkSKey.Value, micIn)[..4]);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Data");
            //sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {MacHeader.Value.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"    MIC (From Payload) = {Mic.Value.ToHexString()}");
            sb.AppendLine($"      MIC (Calculated) = ");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = FHDR | FPort | FRMPayload )");
            sb.AppendLine($"                  FHDR = {FrameHeader.Value.ToHexString()}");
            sb.AppendLine($"                 FPort = {FPort.ToHexString()}");
            //sb.AppendLine($"   EncryptedFRMPayload = {EncryptedFrmPayload.ToHexString()}");
            sb.AppendLine($"            FRMPayload = {FrmPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( FHDR = DevAddr[4] | FCtrl[1] | FCnt[2] | FOpts[0..15] )");
            sb.AppendLine($"               DevAddr = {FrameHeader.DeviceAddress.Value.ToHexString()} (Big Endian)");
            sb.AppendLine($"                 FCtrl = {FrameHeader.FrameControl.Value.ToHexString()}");
            sb.AppendLine($"                  FCnt = {FrameHeader.FrameCount} (Big Endian)");
            sb.AppendLine($"                 FOpts = {FrameHeader.FOptions.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          Message Type = {this.GetType()}");
            //sb.AppendLine($"             Direction = {(this is UnconfirmedDataUpPacket or ConfirmedDataUpPacket ? "up" : "down")}");
            sb.AppendLine($"                  FCnt = {FrameHeader.FrameCount}");
            sb.AppendLine($"             FCtrl.ACK = {FrameHeader.FrameControl.Ack}");
            sb.AppendLine($"             FCtrl.ADR = {FrameHeader.FrameControl.Adr}");
            sb.AppendLine($"             FCtrl.Rfu = {FrameHeader.FrameControl.Rfu}");
            sb.AppendLine($"        FCtrl.FPending = {FrameHeader.FrameControl.FPending}");
            sb.AppendLine($"     FCtrl.FOptsLength = {FrameHeader.FrameControl.FOptsLength}");
            return sb.ToString();
        }
    }

    public record struct DownlinkSettings(byte Value)
    {
        public readonly byte Rx1DrOffset => (byte)(Value >> 4);
        public readonly byte Rx2DataRate => (byte)(Value & 0x0F);
        public byte Value { get; } = Value;
        public readonly int Length => 1;

        public override string ToString()
        {
            return $"({Rx1DrOffset} | {Rx2DataRate})";
        }
    }

    // TODO: Figure out the CFList
    public interface ICFList
    {
        public CFListType CFListType { get; }
        public byte[] Value { get; }
        public int Length { get; }
    }

    public enum CFListType : byte
    {
        Type0 = 0,
        Type1 = 0x01
    }

    public record struct CFListType0(byte[] Value) : ICFList
    {
        public byte[] Value { get; } = Value;
        public CFListType CFListType { get; } = 0x00;
        public int Length { get; } = Value.Length;
    }

    public record struct CFListType1(byte[] Value) : ICFList
    {
        public byte[] Value { get; } = Value;
        public CFListType CFListType { get; } = CFListType.Type1;
        public int Length { get; } = Value.Length;
    }


    public abstract class Packet()
    {
        protected Packet(ROM data) : this()
        {
            PhyPayload = data;
            Mhdr = data.Span[0];
            MacPayload = data[1..^4];
            MacPayloadWithMic = data[1..];
            Mic = data[^4..];
        }

        public ROM PhyPayload { get; protected set; }
        public byte Mhdr { get; protected set; }
        public ROM MacPayload { get; protected set; }
        public ROM MacPayloadWithMic { get; protected set; }
        public ROM Mic { get; protected set; }

        public byte[]? RejoinType { get; set; }

        public bool VerifyMic(byte[] key)
        {
            return false;
        }

        private protected byte[] CalculateMic(byte[] appKey)
        {
            return EncryptionTools.ComputeAesCMac(appKey, MacPayload.Span)[..4];
        }

        public abstract override string ToString();
    }

    public class JoinRequestPacket : Packet
    {
        public JoinRequestPacket(AppKey appKey, JoinEui appEui, DevEui devEui, DeviceNonce devNonce)
        {
            AppEui = appEui;
            DevEui = devEui;
            DevNonce = devNonce;

            // Now create the MAC payload
            var macPayload = new byte[19];
            macPayload[0] = (byte)PacketType.JoinRequest;
            appEui.Value.CopyTo(macPayload.AsSpan(1));
            devEui.Value.CopyTo(macPayload.AsSpan(9));
            devNonce.Value.CopyTo(macPayload.AsSpan(17));
            MacPayload = macPayload;

            // Calculate the message integrity check
            Console.WriteLine("Calculating MIC");
            var mic = CalculateMic(appKey.Value);
            Mic = mic;

            // Add this to the MAC payload
            var macPayloadWithMic = new byte[MacPayload.Length + 4];
            Array.Copy(macPayload, 0, macPayloadWithMic, 0, macPayload.Length);
            Array.Copy(mic, 0, macPayloadWithMic, macPayload.Length, mic.Length);
            MacPayloadWithMic = macPayloadWithMic;

            // Set the physical payload
            Console.WriteLine("Setting PHYPayload to MACPayloadWithMIC");
            PhyPayload = MacPayloadWithMic;
        }

        public JoinRequestPacket(ROM message)
            : base(message)
        {
            if (message.Length < 23)
            {
                throw new ArgumentException("Invalid Join Request packet length");
            }
            AppEui = new JoinEui(message[1..9].ToArray());
            DevEui = new DevEui(message[9..17].ToArray());
            DevNonce = new DeviceNonce(message[17..19].ToArray());
        }

        public JoinEui AppEui { get; set; }
        public DevEui DevEui { get; set; }
        public DeviceNonce DevNonce { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Join Request");
            sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine();
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {Mhdr.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"                   MIC = {Mic.ToHexString()}");
            sb.AppendLine("");
            sb.AppendLine($"          ( MACPayload = AppEUI[8] | DevEUI[8] | DevNonce[2] )");
            sb.AppendLine($"                AppEUI = {AppEui.Value.ToHexString()}");
            sb.AppendLine($"                DevEUI = {DevEui.Value.ToHexString()}");
            sb.AppendLine($"              DevNonce = {DevNonce.Value.ToHexString()}");
            return sb.ToString();
        }
    }

    public class JoinAcceptPacket : Packet
    {
        public JoinAcceptPacket(AppKey appKey, ROM message)
            : base(message)
        {
            AppKey = appKey;
            if (message.Length < 17)
            {
                throw new ArgumentException("Invalid Join Response packet length");
            }
            var decryptedData = EncryptionTools.DecryptMessage(appKey.Value, message[1..]);
            var d = new byte[1 + decryptedData.Length];
            d[0] = message.Span[0];
            decryptedData.CopyTo(d.AsSpan(1));
            message = d;

            AppNonce = message[1..4];
            NetworkId = message[4..7];
            var devAddress = new byte[4];
            message[7..11].Span.CopyToReverse(devAddress);
            DeviceAddress = devAddress;
            DownlinkSettings = message[11..12];
            ReceiveDelay = message[12..13].Span[0] & 0x15;
            CfList = message.Length == 13 + 16 ? message[13..] : ROM.Empty;
        }

        private AppKey AppKey;
        public ROM AppNonce { get; set; }
        public ROM NetworkId { get; set; }
        public ROM DeviceAddress { get; set; }
        public ROM DownlinkSettings { get; set; }
        public int ReceiveDelay { get; set; }
        public ROM CfList { get; set; }

        // TODO: Remove?
        public byte[]? JoinReqType { get; set; }

        private byte[] CalculateMic(AppKey appKey)
        {
            var b = new byte[1 + AppNonce.Length + NetworkId.Length + DeviceAddress.Length + DownlinkSettings.Length + 1 + CfList.Length];
            b[0] = Mhdr;
            AppNonce.Span.CopyTo(b.AsSpan(1));
            NetworkId.Span.CopyTo(b.AsSpan(4));
            DeviceAddress.Span.CopyTo(b.AsSpan(7));
            DownlinkSettings.Span.CopyTo(b.AsSpan(11));
            b[12] = (byte)ReceiveDelay;
            if (CfList.Length > 0)
            {
                CfList.Span.CopyTo(b.AsSpan(13));
            }
            return EncryptionTools.ComputeAesCMac(appKey.Value, b)[..4];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Join Accept" + "");
            sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {Mhdr.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"                   MIC = {Mic.ToHexString()}");
            sb.AppendLine($"        Calculated MIC = {CalculateMic(AppKey).ToHexString(false)}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = AppNonce[3] | NetID[3] | DevAddr[4] | DLSettings[1] | RxDelay[1] | CFList[0|15] )");
            sb.AppendLine($"              AppNonce = {AppNonce.ToHexString()}");
            sb.AppendLine($"                 NetID = {NetworkId.ToHexString()}");
            sb.AppendLine($"               DevAddr = {DeviceAddress.ToHexString()}");
            sb.AppendLine($"            DLSettings = {DownlinkSettings}");
            sb.AppendLine($"               RxDelay = {ReceiveDelay}");
            sb.AppendLine($"                CFList = {CfList.ToHexString()}");
            sb.AppendLine($"");
            //sb.AppendLine($"DLSettings.RX1DRoffset = " + this.getDLSettingsRxOneDRoffset() + "");
            //sb.AppendLine($"DLSettings.RX2DataRate = " + this.getDLSettingsRxTwoDataRate() + "");
            sb.AppendLine($"           RxDelay.Del = {ReceiveDelay}");
            sb.AppendLine($"");
            return sb.ToString();
        }
    }

    public class RejoinType1RequestPacket(ROM message) : Packet(message)
    {
        public ROM NetworkId { get; set; } = message[2..5];
        public ROM DevEui { get; set; } = message[5..13];
        public ROM ReJoinCount0 { get; set; } = message[13..15];
        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    public class RejoinType2RequestPacket(ROM message) : Packet(message)
    {
        public ROM NetworkId { get; set; } = message[2..10];
        public ROM DevEui { get; set; } = message[10..18];
        public ROM ReJoinCount1 { get; set; } = message[18..20];
        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class DataPacket : Packet
    {
        protected DataPacket(byte mhdr,
                             DeviceAddress deviceAddress,
                             FrameControl frameCtrl,
                             uint fCnt,
                             ROM fOpts,
                             byte fPort,
                             ROM payload,
                             AppSKey? appSKey,
                             NetworkSKey? networkSKey)
        {
            if (fOpts.Length > 15)
            {
                throw new ArgumentException("FOpts must be 15 bytes or less");
            }
            Mhdr = mhdr;
            DeviceAddress = deviceAddress;

            // Make sure the length of fOpts length is included in the FCtrl
            if (FOpts.Length != frameCtrl.FOptsLength)
            {
                frameCtrl.FOptsLength = (byte)FOpts.Length;
            }
            FrameCtrl = frameCtrl;
            FCnt = BitConverter.GetBytes(fCnt)[..2].Reverse();
            FOpts = fOpts;
            FPort = fPort;
            Payload = payload;
            FrmPayload = payload;
            var fhdr = new byte[deviceAddress.Value.Length + 1 /*fctrl length*/ + FCnt.Length + fOpts.Length];
            deviceAddress.Value.CopyToReverse(fhdr);
            // TODO: FCtrl needs to be precisely created because it contains the fOpts length and other stuff
            fhdr[deviceAddress.Length] = FrameCtrl.Value;
            FCnt.Span.CopyToReverse(fhdr.AsSpan(deviceAddress.Length + 1));
            fOpts.Span.CopyTo(fhdr.AsSpan(deviceAddress.Length + 1 + FCnt.Length));
            FHeader = fhdr;
            if (FPort == (byte)0x00)
            {
                if (networkSKey == null)
                {
                    throw new ArgumentNullException(nameof(networkSKey));
                }

                var encryptedMacPayload = EncryptionTools.EncryptMessage(networkSKey.Value.Value, this);
                FrmPayload = encryptedMacPayload;
            }
            else
            {
                if (appSKey == null)
                {
                    throw new ArgumentNullException(nameof(appSKey));
                }
                var encryptedMacPayload = EncryptionTools.EncryptMessage(appSKey.Value.Value, this);
                FrmPayload = encryptedMacPayload;
            }

            var macPayload = new byte[fhdr.Length + 1 /*fPort*/ + FrmPayload.Length];
            FHeader.CopyTo(macPayload);
            macPayload[fhdr.Length] = fPort;
            FrmPayload.Span.CopyTo(macPayload.AsSpan(fhdr.Length + 1));
            MacPayload = macPayload;
            var micIn = new byte[16 + 1 /*mhdr*/ + macPayload.Length];
            micIn[0] = 0x49;
            micIn[1] = 0x00;
            micIn[2] = 0x00;
            micIn[3] = 0x00;
            micIn[4] = 0x00;

            micIn[5] = this is UnconfirmedDataUpPacket or ConfirmedDataUpPacket ? (byte)0x00 : (byte)0x01;
            deviceAddress.Value.CopyToReverse(micIn.AsSpan(6));
            FCnt.Span.CopyToReverse(micIn.AsSpan(10));
            micIn[12] = 0x00;
            micIn[13] = 0x00;
            micIn[14] = 0x00;
            micIn[15] = (byte)(1 + macPayload.Length);
            micIn[16] = mhdr;
            macPayload.CopyTo(micIn.AsSpan(17));

            var mic = EncryptionTools.ComputeAesCMac(networkSKey!.Value.Value, micIn)[..4];
            Mic = mic;
            var phyPayload = new byte[1 + macPayload.Length + mic.Length];
            phyPayload[0] = mhdr;
            macPayload.CopyTo(phyPayload.AsSpan(1));
            mic.CopyTo(phyPayload.AsSpan(1 + macPayload.Length));
            PhyPayload = phyPayload;
            MacPayloadWithMic = PhyPayload[1..];
        }

        protected DataPacket(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
            : base(message)
        {
            Mhdr = message.Span[0];
            var packetType = (PacketType)((Mhdr >> 5) & 0x07);
            DeviceAddress = new DeviceAddress(message[1..5].Reverse());
            FrameCtrl = packetType is PacketType.ConfirmedDataUp or PacketType.UnconfirmedDataUp
                            ? new UplinkFrameControl(message[5..6].Span[0])
                            : new DownlinkFrameControl(message[5..6].Span[0]);
            FCnt = message[6..8].Reverse();
            var fOptsLength = (message[5..6].Span[0] & 0x0f);
            FOpts = message[8..(8 + fOptsLength)];
            var fHdrLength = 7 + fOptsLength;
            FHeader = message[1..(1 + fHdrLength)];

            if (fHdrLength == MacPayload.Length)
            {
                FPort = 0x00;
                FrmPayload = ROM.Empty;
            }
            else
            {
                FPort = message[(8 + fOptsLength)..(9 + fOptsLength)].Span[0];
                FrmPayload = message[(9 + fOptsLength)..^4];
            }

            EncryptedFrmPayload = FrmPayload;

            if (FPort == (byte)0x00)
            {
                if (networkSKey == null)
                {
                    throw new ArgumentNullException(nameof(networkSKey));
                }

                var encryptedMacPayload = EncryptionTools.EncryptMessage(networkSKey.Value.Value, this);
                FrmPayload = encryptedMacPayload;
            }
            else
            {
                if (appSKey == null)
                {
                    throw new ArgumentNullException(nameof(appSKey));
                }
                var encryptedMacPayload = EncryptionTools.EncryptMessage(appSKey.Value.Value, this);
                FrmPayload = encryptedMacPayload;
            }
        }

        public DeviceAddress DeviceAddress { get; protected set; }
        public FrameControl FrameCtrl { get; protected set; }
        public ROM FCnt { get; protected set; }
        public ROM FOpts { get; protected set; }
        public ROM FHeader { get; protected set; }
        public byte FPort { get; protected set; }
        public ROM FrmPayload { get; protected set; }
        public ROM EncryptedFrmPayload { get; protected set; }
        public ROM Payload { get; protected set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Data");
            sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"                  MHDR = {Mhdr.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"    MIC (From Payload) = {Mic.ToHexString()}");
            sb.AppendLine($"      MIC (Calculated) = ");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = FHDR | FPort | FRMPayload )");
            sb.AppendLine($"                  FHDR = {FHeader.ToHexString()}");
            sb.AppendLine($"                 FPort = {FPort.ToHexString()}");
            sb.AppendLine($"   EncryptedFRMPayload = {EncryptedFrmPayload.ToHexString()}");
            sb.AppendLine($"            FRMPayload = {FrmPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( FHDR = DevAddr[4] | FCtrl[1] | FCnt[2] | FOpts[0..15] )");
            sb.AppendLine($"               DevAddr = {DeviceAddress.Value.ToHexString()} (Big Endian)");
            sb.AppendLine($"                 FCtrl = {FrameCtrl.Value.ToHexString()}");
            sb.AppendLine($"                  FCnt = {FCnt.ToHexString()} (Big Endian)");
            sb.AppendLine($"                 FOpts = {FOpts.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          Message Type = {this.GetType()}");
            sb.AppendLine($"             Direction = {(this is UnconfirmedDataUpPacket or ConfirmedDataUpPacket ? "up" : "down")}");
            sb.AppendLine($"                  FCnt = {FCnt.ToHexString()}");
            sb.AppendLine($"             FCtrl.ACK = {FrameCtrl.Ack}");
            sb.AppendLine($"             FCtrl.ADR = {FrameCtrl.Adr}");
            sb.AppendLine($"             FCtrl.Rfu = {FrameCtrl.Rfu}");
            sb.AppendLine($"        FCtrl.FPending = {FrameCtrl.FPending}");
            sb.AppendLine($"     FCtrl.FOptsLength = {FrameCtrl.FOptsLength}");
            return sb.ToString();
        }
    }

    public class UnconfirmedDataUpPacket : DataPacket
    {
        public UnconfirmedDataUpPacket(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
            : base(message, appSKey, networkSKey)
        {
        }

        public UnconfirmedDataUpPacket(DeviceAddress deviceAddress,
                                       FrameControl frameCtrl,
                                       uint fCnt,
                                       ROM fOpts,
                                       byte fPort,
                                       ROM payload,
                                       AppSKey? appSKey,
                                       NetworkSKey? networkSKey)
            : base((byte)PacketType.UnconfirmedDataUp << 5,
                   deviceAddress,
                   frameCtrl,
                   fCnt,
                   fOpts,
                   fPort,
                   payload,
                   appSKey,
                   networkSKey)
        {
        }
    }

    public class UnconfirmedDataDownPacket(ROM message, AppSKey? appSKey,
                                           NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey)
    {
    }

    public class ConfirmedDataUpPacket(ROM message, AppSKey? appSKey,
                                       NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey);

    public class ConfirmedDataDownPacket(ROM message, AppSKey? appSKey,
                                         NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey);

    public class PacketParser(AppKey appKey, AppSKey? appSKey, NetworkSKey? networkSKey)
    {
        public Packet Parse(ROM message)
        {
            var mhdr = message.Span[0];
            var packetType = (PacketType)((mhdr >> 5) & 0x07);
            return packetType switch
            {
                PacketType.JoinRequest => ParseJoinRequest(message),
                PacketType.JoinResponse => ParseJoinAccept(appKey, message),
                PacketType.UnconfirmedDataUp => ParseUnconfirmedDataUp(message, appSKey, networkSKey),
                PacketType.UnconfirmedDataDown => ParseUnconfirmedDataDown(message, appSKey, networkSKey),
                PacketType.ConfirmedDataUp => ParseConfirmedDataUp(message, appSKey, networkSKey),
                PacketType.ConfirmedDataDown => ParseConfirmedDataDown(message, appSKey, networkSKey),
                //PacketType.RejoinRequest => message.Span[1] == 0x01 ? new RejoinType1RequestPacket(message) : new RejoinType2RequestPacket(message),
                _ => throw new ArgumentException("Invalid packet type")
            };
        }

        public static JoinRequestPacket ParseJoinRequest(ROM message)
        {
            return new JoinRequestPacket(message);
        }

        public static JoinAcceptPacket ParseJoinAccept(AppKey appKey, ROM message)
        {
            return new JoinAcceptPacket(appKey, message);
        }

        public static UnconfirmedDataUpPacket ParseUnconfirmedDataUp(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
        {
            return new UnconfirmedDataUpPacket(message, appSKey, networkSKey);
        }

        public static UnconfirmedDataDownPacket ParseUnconfirmedDataDown(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
        {
            return new UnconfirmedDataDownPacket(message, appSKey, networkSKey);
        }

        public static ConfirmedDataUpPacket ParseConfirmedDataUp(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
        {
            return new ConfirmedDataUpPacket(message, appSKey, networkSKey);
        }

        public static ConfirmedDataDownPacket ParseConfirmedDataDown(ROM message, AppSKey? appSKey, NetworkSKey? networkSKey)
        {
            return new ConfirmedDataDownPacket(message, appSKey, networkSKey);
        }
    }
}
