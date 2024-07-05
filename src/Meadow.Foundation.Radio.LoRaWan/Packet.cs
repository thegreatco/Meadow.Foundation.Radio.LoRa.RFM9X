using System;
using System.Text;

using ROM = System.ReadOnlyMemory<byte>;

namespace Meadow.Foundation.Radio.LoRaWan
{
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

    public class JoinRequestPacket : Packet
    {
        public JoinRequestPacket(AppKey appKey, AppEui appEui, DevEui devEui, DeviceNonce devNonce)
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
            AppEui = new AppEui(message[1..9].ToArray());
            DevEui = new DevEui(message[9..17].ToArray());
            DevNonce = new DeviceNonce(message[17..19].ToArray());
        }

        public AppEui AppEui { get; set; }
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
            if (message.Length < 17)
            {
                throw new ArgumentException("Invalid Join Response packet length");
            }
            var decryptedData = EncryptionTools.DecryptMessage(appKey.Value, message[1..]);
            var d = new byte[1+ decryptedData.Length];
            d[0] = message.Span[0];
            decryptedData.CopyTo(d.AsSpan(1));
            message = d;
            Console.WriteLine($"Decrypted data: {d.ToHexString(false)}");

            AppNonce = message[1..4];
            NetworkId = message[4..7];
            var devAddress = new byte[4];
            message[7..11].Span.CopyToReverse(devAddress);
            Console.WriteLine($"Device Address in JoinAccept: {devAddress.ToHexString(false)}");
            DeviceAddress = devAddress;
            DownlinkSettings = message[11..12];
            ReceiveDelay = message[12..13];
            CfList = message.Length == 13 + 16 ? message[13..] : ROM.Empty;
        }

        public ROM AppNonce { get; set; }
        public ROM NetworkId { get; set; }
        public ROM DeviceAddress { get; set; }
        public ROM DownlinkSettings { get; set; }
        public ROM ReceiveDelay { get; set; }
        public ROM CfList { get; set; }

        // TODO: Remove?
        public byte[]? JoinReqType { get; set; }

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
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = AppNonce[3] | NetID[3] | DevAddr[4] | DLSettings[1] | RxDelay[1] | CFList[0|15] )");
            sb.AppendLine($"              AppNonce = {AppNonce.ToHexString()}");
            sb.AppendLine($"                 NetID = {NetworkId.ToHexString()}");
            sb.AppendLine($"               DevAddr = {DeviceAddress.ToHexString()}");
            sb.AppendLine($"            DLSettings = {DownlinkSettings.ToHexString()}");
            sb.AppendLine($"               RxDelay = {ReceiveDelay.ToHexString()}");
            sb.AppendLine($"                CFList = {CfList.ToHexString()}");
            sb.AppendLine($"");
            //sb.AppendLine($"DLSettings.RX1DRoffset = " + this.getDLSettingsRxOneDRoffset() + "");
            //sb.AppendLine($"DLSettings.RX2DataRate = " + this.getDLSettingsRxTwoDataRate() + "");
            //sb.AppendLine($"           RxDelay.Del = " + this.getRxDelayDel() + "");
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

        public override byte Value => (byte)((Adr         ? 0b10000000 : 0) |
                                             (AdrAckReq   ? 0b01000000 : 0) |
                                             (Ack         ? 0b00100000 : 0) |
                                             (Rfu         ? 0b00010000 : 0) |
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

        public override byte Value => (byte)((Adr         ? 0b10000000 : 0) |
                                             (Rfu         ? 0b01000000 : 0) |
                                             (Ack         ? 0b00100000 : 0) |
                                             (FPending    ? 0b00010000 : 0) |
                                             (FOptsLength & 0x0F));
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
            DeviceAddress = deviceAddress.Value;

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
            micIn[14]= 0x00;
            micIn[15] = (byte)(1+ macPayload.Length);
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
            DeviceAddress = message[1..5];
            FrameCtrl = packetType is PacketType.ConfirmedDataUp or PacketType.UnconfirmedDataUp
                            ? new UplinkFrameControl(message[5..6].Span[0])
                            : new DownlinkFrameControl(message[5..6].Span[0]);
            FCnt = message[6..8];
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

        public ROM DeviceAddress { get; protected set; }
        public FrameControl FrameCtrl { get; protected set; }
        public ROM FCnt { get; protected set; }
        public ROM FOpts { get; protected set; }
        public ROM FHeader { get; protected set; }
        public byte FPort { get; protected set; }
        public ROM FrmPayload { get; protected set; }
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
            sb.AppendLine($"                   MIC = {Mic.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          ( MACPayload = FHDR | FPort | FRMPayload )");
            sb.AppendLine($"                  FHDR = {FHeader.ToHexString()}");
            sb.AppendLine($"                 FPort = {FPort.ToHexString()}");
            sb.AppendLine($"            FRMPayload = {FrmPayload.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"                ( FHDR = DevAddr[4] | FCtrl[1] | FCnt[2] | FOpts[0..15] )");
            sb.AppendLine($"               DevAddr = {DeviceAddress.ToHexString()} (Big Endian)");
            sb.AppendLine($"                 FCtrl = {FrameCtrl.Value.ToHexString()}");
            sb.AppendLine($"                  FCnt = {FCnt.ToHexString()} (Big Endian)");
            sb.AppendLine($"                 FOpts = {FOpts.ToHexString()}");
            sb.AppendLine($"");
            sb.AppendLine($"          Message Type = {this.GetType()}");
            sb.AppendLine($"             Direction = {(this is UnconfirmedDataUpPacket or ConfirmedDataUpPacket ? "up" : "down")}");
            sb.AppendLine($"                  FCnt = {FCnt.ToHexString()}");
            sb.AppendLine($"             FCtrl.ACK = {FrameCtrl.Ack}");
            sb.AppendLine($"             FCtrl.ADR = {FrameCtrl.Adr}");
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

    public class UnconfirmedDataDownPacket(ROM message,AppSKey? appSKey,
                                           NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey);

    public class ConfirmedDataUpPacket(ROM message,AppSKey? appSKey,
                                       NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey);

    public class ConfirmedDataDownPacket(ROM message,AppSKey? appSKey,
                                         NetworkSKey? networkSKey) : DataPacket(message, appSKey, networkSKey);
}
