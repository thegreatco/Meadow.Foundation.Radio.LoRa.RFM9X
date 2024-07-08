using Meadow.Units;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

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

    public readonly record struct FrameHeader(DeviceAddress DeviceAddress, FrameControl FrameControl, int FrameCount, IReadOnlyList<MacCommand> MacCommands)
    {
        public FrameHeader(DeviceAddress DeviceAddress, FrameControl FrameControl, int FrameCount, ROM? FOptions)
            : this(DeviceAddress, FrameControl, FrameCount, FOptions == null ? Array.Empty<MacCommand>() : MacCommandFactory.Create(FOptions.Value))
        {
        }
        public DeviceAddress DeviceAddress { get; } = DeviceAddress;
        public FrameControl FrameControl { get; } = FrameControl;
        public int FrameCount { get; } = FrameCount;
        // TODO: Parse the FOptions
        public ROM FOptions
        {
            get
            {
                if (MacCommands.Count == 0)
                    return ROM.Empty;

                var length = MacCommands.Sum(x => x.Length);
                if (length > 15)
                    throw new InvalidOperationException("FOptions cannot exceed 15 bytes");
                var b = new byte[length];
                var offset = 0;
                foreach (var command in MacCommands)
                {
                    command.Value.CopyTo(b.AsSpan(offset));
                    offset += command.Length;
                }
                return b;
            }
        }
        public IReadOnlyList<MacCommand> MacCommands { get; } = MacCommands;
        public ROM Value
        {
            get
            {
                var frameCount = BitConverter.GetBytes(FrameCount).AsSpan(0, 2);
                var bytes = new byte[DeviceAddress.Length + 1 + frameCount.Length + FOptions.Length];
                DeviceAddress.Value.CopyToReverse(bytes);
                bytes[DeviceAddress.Length] = FrameControl.Value;
                frameCount.CopyTo(bytes.AsSpan(DeviceAddress.Length + 1));
                var fOptions = FOptions;
                if (fOptions.Length > 0)
                    fOptions.Span.CopyTo(bytes.AsSpan(DeviceAddress.Length + 1 + frameCount.Length));
                return bytes;
            }
        }
    }

    public abstract class LoRaMessage();

    public class JoinRequest(AppKey AppKey, JoinEui JoinEui, DevEui DeviceEui, DeviceNonce DeviceNonce) : LoRaMessage
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

    public class JoinAccept(
        AppKey AppKey,
        MacHeader MacHeader,
        JoinNonce JoinNonce,
        NetworkId NetworkId,
        DeviceAddress DeviceAddress,
        DownlinkSettings DownlinkSettings,
        int ReceiveDelay,
        ICFList? CFList) : LoRaMessage
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
            var macHeader = new MacHeader(d[MacHeaderRange][0]);
            var joinNonce = new JoinNonce(d[JoinNonceRange]);
            var networkId = new NetworkId(d[NetIdRange]);
            var deviceAddress = new DeviceAddress(d[DevAddrRange]);
            var downlinkSettings = new DownlinkSettings(d[DlSettingsRange][0]);
            var receiveDelay = d[RxDelayRange][0];
            ICFList? cfList = d.Length == MessageSizes.JoinAcceptFrameMinimumSize ? null : d[CFListRange.End] == 0x00 ? new CFListType0(d[CFListRange]) : new CFListType1(d[CFListRange]);
            var j = new JoinAccept(appKey, macHeader, joinNonce, networkId, deviceAddress, downlinkSettings, receiveDelay, cfList)
            {
                _phyPayload = data.ToArray(),
            };

            var messageMic = new Mic(d[MicRange]);
            if (j.Mic.Value.IsEqual(messageMic.Value) == false)
                throw new MicMismatchException(messageMic, j.Mic);

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

    public class DataMessage(
        AppSKey AppSKey,
        NetworkSKey NetworkSKey,
        MacHeader MacHeader,
        FrameHeader FrameHeader,
        int FCount,
        byte? FPort,
        byte[]? FrmPayload) : LoRaMessage
    {
        #region ranges
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
        public MacHeader MacHeader { get; private set; } = MacHeader;
        public FrameHeader FrameHeader { get; private set; } = FrameHeader;
        public int FCount { get; private set; } = FCount;
        public byte? FPort { get; private set; } = FPort;

        public byte[] MacPayload
        {
            get
            {
                var frameHeader = FrameHeader;
                var frmPayload = FrmPayload;
                byte[] b;
                if (FPort.HasValue)
                {
                    b = new byte[frameHeader.Value.Length + 1 + frmPayload.Length];
                }
                else
                {
                    b = new byte[frameHeader.Value.Length + frmPayload.Length];
                }
                var offset = 0;
                // the 1 below is for the FPort
                frameHeader.Value.CopyTo(b);
                offset += frameHeader.Value.Length;
                if (FPort.HasValue)
                {
                    b[offset] = FPort.Value;
                    offset += 1;
                }
                if (frmPayload.Length > 0)
                {
                    frmPayload.CopyTo(b.AsSpan(offset));
                }
                return b;
            }
        }

        public byte[] FrmPayload { get; private set; } = FrmPayload == null ? Array.Empty<byte>() : EncryptionTools.EncryptMessage(AppSKey.Value, true, FrameHeader.DeviceAddress, FCount, FrmPayload);

        public Mic Mic
        {
            get
            {
                // Copy this locally since each call is an allocation
                var macPayload = MacPayload;
                var micIn = new byte[16 + 1 /*mhdr*/ + MacPayload.Length];
                micIn[0] = 0x49;
                micIn[1] = 0x00;
                micIn[2] = 0x00;
                micIn[3] = 0x00;
                micIn[4] = 0x00;
                micIn[5] = MacHeader.PacketType is PacketType.ConfirmedDataUp or PacketType.UnconfirmedDataUp ? (byte)0x00 : (byte)0x01; // Uplink
                FrameHeader.DeviceAddress.Value.CopyToReverse(micIn, 6);
                BitConverter.GetBytes(FCount).CopyTo(micIn.AsSpan(10));
                micIn[14] = 0x00;
                micIn[15] = (byte)(MacHeader.Length + macPayload.Length);
                micIn[16] = MacHeader.Value;
                macPayload.CopyTo(micIn, 17);
                return new Mic(EncryptionTools.ComputeAesCMac(_networkSKey.Value, micIn)[..4]);
            }
        }

        public byte[] PhyPayload
        {
            get
            {
                var mic = Mic;
                var mac = MacPayload;
                var bytes = new byte[1 + mac.Length + mic.Length];
                bytes[0] = MacHeader.Value;
                mac.CopyTo(bytes.AsSpan(1));
                Mic.Value.CopyTo(bytes.AsSpan(1 + mac.Length));
                return bytes;
            }
        }

        public static DataMessage FromPhy(AppSKey appSKey, NetworkSKey networkSKey, ROM data)
        {
            var macHeader = new MacHeader(data.Span[0]);
            var mic = data[MicRange];
            var macPayload = data[MacPayloadRange];
            var macOffset = 0;
            var deviceAddress = new DeviceAddress(macPayload[..4].Reverse());
            macOffset += 4;

            FrameControl frameControl = macHeader.PacketType switch
            {
                PacketType.UnconfirmedDataUp => new UplinkFrameControl(macPayload.Span[macOffset]),
                PacketType.UnconfirmedDataDown => new DownlinkFrameControl(macPayload.Span[macOffset]),
                PacketType.ConfirmedDataUp => new UplinkFrameControl(macPayload.Span[macOffset]),
                PacketType.ConfirmedDataDown => new DownlinkFrameControl(macPayload.Span[macOffset]),
                _ => throw new ArgumentOutOfRangeException(nameof(macHeader.PacketType))
            };
            macOffset += 1;
            var fCnt = BitConverter.ToUInt16(macPayload[macOffset..(macOffset + 2)].ToArray());
            macOffset += 2;

            byte[] fOptions = Array.Empty<byte>();
            byte? fPort = null;
            if (frameControl.FOptsLength > 0)
            {
                fOptions = macPayload[macOffset..(macOffset + frameControl.FOptsLength)].ToArray();
                macOffset += frameControl.FOptsLength;
            }
            else
            {
                fPort = macPayload[(macOffset)..(macOffset + 1)].ToArray()[0];
                macOffset += 1;
            }
            var frameHeader = new FrameHeader(deviceAddress, frameControl, fCnt, fOptions);
            var encryptedFrmPayload = macPayload[(macOffset)..].ToArray();
            byte[] frmPayload;
            bool isUplink = macHeader.PacketType is PacketType.ConfirmedDataUp or PacketType.UnconfirmedDataUp;
            if (fPort == 0)
            {
                frmPayload = EncryptionTools.EncryptMessage(networkSKey.Value, isUplink, deviceAddress, fCnt, encryptedFrmPayload);
            }
            else
            {
                frmPayload = EncryptionTools.EncryptMessage(appSKey.Value, isUplink, deviceAddress, fCnt, encryptedFrmPayload);
            }
            var j = new DataMessage(appSKey, networkSKey, macHeader, frameHeader, fCnt, fPort, frmPayload);

            var messageMic = new Mic(data[MicRange].ToArray());

            return j;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"          Message Type = Data");
            sb.AppendLine($"            PHYPayload = {PhyPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( PHYPayload = MHDR[1] | MACPayload[..] | MIC[4] )");
            sb.AppendLine($"        Message Header = {MacHeader.Value.ToHexString()}");
            sb.AppendLine($"            MACPayload = {MacPayload.ToHexString()}");
            sb.AppendLine($"      MIC (Calculated) = {Mic.Value.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = FHDR | FPort | FRMPayload )");
            sb.AppendLine($"           FrameHeader = {FrameHeader.Value.ToHexString()}");
            sb.AppendLine($"             FramePort = {FPort?.ToHexString() ?? "<empty>"}");
            sb.AppendLine($"            FRMPayload = {FrmPayload.ToHexString() ?? "<empty>"}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( FHDR = DevAddr[4] | FCtrl[1] | FCnt[2] | FOpts[0..15] )");
            sb.AppendLine($"            DevAddress = {FrameHeader.DeviceAddress.Value.ToHexString()} (Big Endian)");
            sb.AppendLine($"          FrameControl = {FrameHeader.FrameControl.Value.ToHexString()}");
            sb.AppendLine($"            FrameCount = {FrameHeader.FrameCount} (Big Endian)");
            sb.AppendLine($"          FrameOptions = {FrameHeader.FOptions.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          Message Type = {GetType()}");
            sb.AppendLine($"             Direction = {(MacHeader.PacketType is PacketType.ConfirmedDataUp or PacketType.UnconfirmedDataUp ? "up" : "down")}");
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

    public class PacketFactory(OtaaSettings settings)
    {
        public LoRaMessage Parse(ROM data)
        {
            var macHeader = new MacHeader(data.Span[0]);
            return macHeader.PacketType switch
            {
                PacketType.JoinResponse => ParseJoinAccept(data),
                PacketType.UnconfirmedDataUp => ParseUnconfirmedDataUp(data),
                PacketType.UnconfirmedDataDown => ParseUnconfirmedDataDown(data),
                PacketType.ConfirmedDataUp => ParseConfirmedDataUp(data),
                PacketType.ConfirmedDataDown => ParseConfirmedDataDown(data),
                //PacketType.RejoinRequest => message.Span[1] == 0x01 ? new RejoinType1RequestPacket(message) : new RejoinType2RequestPacket(message),
                _ => throw new ArgumentException("Invalid packet type")
            };
        }

        public JoinAccept ParseJoinAccept(ROM data)
        {
            return JoinAccept.FromPhy(settings.AppKey, data);
        }

        public DataMessage ParseUnconfirmedDataUp(ROM data)
        {
            var message = DataMessage.FromPhy(settings.AppSKey, settings.NetworkSKey, data);
            settings.IncDownlinkFrameCounter();
            return message;
        }

        public DataMessage ParseUnconfirmedDataDown(ROM data)
        {
            var message = DataMessage.FromPhy(settings.AppSKey, settings.NetworkSKey, data);
            settings.IncDownlinkFrameCounter();
            return message;
        }

        public DataMessage ParseConfirmedDataUp(ROM data)
        {
            var message = DataMessage.FromPhy(settings.AppSKey, settings.NetworkSKey, data);
            settings.IncDownlinkFrameCounter();
            return message;
        }

        public DataMessage ParseConfirmedDataDown(ROM data)
        {
            var message = DataMessage.FromPhy(settings.AppSKey, settings.NetworkSKey, data);
            settings.IncDownlinkFrameCounter();
            return message;
        }

        public DataMessage CreateLinkCheckRequestMessage()
        {
            MacCommand[] macCommands = [new LinkCheckReq()];
            var macHeader = new MacHeader(PacketType.UnconfirmedDataUp, 0x00);
            var frameControl = new UplinkFrameControl(false, false, false, false, (byte)macCommands.Sum(x => x.Length));
            var frameHeader = new FrameHeader(settings.DeviceAddress, frameControl, settings.UplinkFrameCounter, macCommands);
            var message = new DataMessage(settings.AppSKey, settings.NetworkSKey, macHeader, frameHeader, settings.UplinkFrameCounter, null, null);
            return message;
        }

        public DataMessage CreateAdaptiveDataRateAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateDutyCycleAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateRxParamSetupAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateDevStatusAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateNewChannelAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateRxTimingSetupAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateTxParamSetupAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateDlChannelAnswer()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateDeviceTimeRequest()
        {
            throw new NotImplementedException();
        }

        public DataMessage CreateUnconfirmedDataUpMessage(byte[] payload)
        {
            var macHeader = new MacHeader(PacketType.UnconfirmedDataUp, 0x00);
            var frameControl = new UplinkFrameControl(false, false, false, false, 0);
            var frameHeader = new FrameHeader(settings.DeviceAddress, frameControl, settings.UplinkFrameCounter, Array.Empty<MacCommand>());
            var message = new DataMessage(settings.AppSKey, settings.NetworkSKey, macHeader, frameHeader, settings.UplinkFrameCounter, 1, payload);
            return message;
        }
    }
}
