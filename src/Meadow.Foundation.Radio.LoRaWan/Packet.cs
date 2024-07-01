﻿using System;
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

        public static Packet DecodePacket(byte[] appKey, ROM data)
        {
            var packetType = (PacketType)((data[..1].Span[0] & 0b11100000) >> 5);
            switch (packetType)
            {
                case PacketType.JoinRequest:
                    if (data.Length < 23)
                    {
                        throw new ArgumentException("Invalid Join Request packet length");
                    }
                    return new JoinRequestPacket(data);
                case PacketType.JoinResponse:
                    if (data.Length < 17)
                    {
                        throw new ArgumentException("Invalid Join Response packet length");
                    }
                    // TODO: Avoid this allocation
                    var decryptedData = EncryptionTools.DecryptMessage(appKey, data[1..]);
                    var d = new byte[1+ decryptedData.Length];
                    d[0] = data.Span[0];
                    decryptedData.CopyTo(d.AsSpan(1));
                    return new JoinAcceptPacket(d);
                case PacketType.RejoinRequest:
                    throw new NotImplementedException("rejoin packets are not implemented");
                    return new RejoinType1RequestPacket(data);
                case PacketType.UnconfirmedDataUp:
                    return new UnconfirmedDataUpPacket(data);
                case PacketType.UnconfirmedDataDown:
                    return new UnconfirmedDataDownPacket(data);
                case PacketType.ConfirmedDataUp:
                    return new ConfirmedDataUpPacket(data);
                case PacketType.ConfirmedDataDown:
                    return new ConfirmedDataDownPacket(data);
                default:
                    throw new NotImplementedException();
            }
        }

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
        public JoinRequestPacket(byte[] appKey, ROM appEui, ROM devEui, ROM devNonce)
        {
            AppEui = appEui;
            DevEui = devEui;
            DevNonce = devNonce;

            // Now create the MAC payload
            var macPayload = new byte[19];
            macPayload[0] = (byte)PacketType.JoinRequest;
            appEui.Span.CopyTo(macPayload.AsSpan(1));
            devEui.Span.CopyTo(macPayload.AsSpan(9));
            devNonce.Span.CopyTo(macPayload.AsSpan(17));
            MacPayload = macPayload;

            // Calculate the message integrity check
            var mic = CalculateMic(appKey);
            Mic = mic;

            // Add this to the MAC payload
            var macPayloadWithMic = new byte[MacPayload.Length + 4];
            Array.Copy(macPayload, 0, macPayloadWithMic, 0, macPayload.Length);
            Array.Copy(mic, 0, macPayloadWithMic, macPayload.Length, mic.Length);
            MacPayloadWithMic = macPayloadWithMic;

            // Set the physical payload
            PhyPayload = MacPayloadWithMic;
        }

        public JoinRequestPacket(ROM message)
            : base(message)
        {
            AppEui = message[1..9];
            DevEui = message[9..17];
            DevNonce = message[17..19];
        }

        public ROM AppEui { get; set; }
        public ROM DevEui { get; set; }
        public ROM DevNonce { get; set; }

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
            sb.AppendLine($"                AppEUI = {AppEui.ToHexString()}");
            sb.AppendLine($"                DevEUI = {DevEui.ToHexString()}");
            sb.AppendLine($"              DevNonce = {DevNonce.ToHexString()}");
            return sb.ToString();
        }
    }

    public class JoinAcceptPacket(ROM message) : Packet(message)
    {
        public ROM AppNonce { get; set; } = message[1..4];
        public ROM NetworkId { get; set; } = message[4..7];
        public ROM DeviceAddress { get; set; } = message[7..11];
        public ROM DownlinkSettings { get; set; } = message[11..12];
        public ROM ReceiveDelay { get; set; } = message[12..13];
        public ROM CfList { get; set; } = message.Length == 13 + 16 ? message[13..] : ROM.Empty;

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
                             ROM deviceAddress,
                             FrameControl frameCtrl,
                             ROM fCnt,
                             ROM fOpts,
                             byte fPort,
                             ROM payload,
                             ROM? networkSKey,
                             ROM? appSKey)
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
            FCnt = fCnt;
            FOpts = fOpts;
            FPort = fPort;
            FrmPayload = payload;
            var fhdr = new byte[deviceAddress.Length + 1 /*fctrl length*/ + fCnt.Length + fOpts.Length];
            deviceAddress.Span.CopyToReverse(fhdr);
            // TODO: FCtrl needs to be precisely created because it contains the fOpts length and other stuff
            fhdr[deviceAddress.Length] = FrameCtrl.Value;
            fCnt.Span.CopyToReverse(fhdr.AsSpan(deviceAddress.Length + 1));
            fOpts.Span.CopyTo(fhdr.AsSpan(deviceAddress.Length + 1 + fCnt.Length));
            FHeader = fhdr;
            if (FPort == (byte)0x00)
            {
                if (networkSKey == null)
                {
                    throw new ArgumentNullException(nameof(networkSKey));
                }

                var encryptedMacPayload = EncryptionTools.EncryptMessage(networkSKey.Value.ToArray(), this);
                FrmPayload = encryptedMacPayload;
            }
            else
            {
                if (appSKey == null)
                {
                    throw new ArgumentNullException(nameof(appSKey));
                }
                var encryptedMacPayload = EncryptionTools.EncryptMessage(appSKey.Value.ToArray(), this);
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
            deviceAddress.Span.CopyToReverse(micIn.AsSpan(6));
            fCnt.Span.CopyToReverse(micIn.AsSpan(10));
            micIn[12] = 0x00;
            micIn[13] = 0x00;
            micIn[14]= 0x00;
            micIn[15] = (byte)(1+ macPayload.Length);
            micIn[16] = mhdr;
            macPayload.CopyTo(micIn.AsSpan(17));

            var mic = EncryptionTools.ComputeAesCMac(networkSKey.Value.Span, micIn)[..4];
            Mic = mic;
            var phyPayload = new byte[1 + macPayload.Length + mic.Length];
            phyPayload[0] = mhdr;
            macPayload.CopyTo(phyPayload.AsSpan(1));
            mic.CopyTo(phyPayload.AsSpan(1 + macPayload.Length));
            PhyPayload = phyPayload;
            MacPayloadWithMic = PhyPayload[1..];
        }

        protected DataPacket(ROM message)
            : base(message)
        {
            DeviceAddress = message[1..5];
            //FrameCtrl = new FrameControl(message[5..6].Span[0]);
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
        }

        public ROM DeviceAddress { get; protected set; }
        public FrameControl FrameCtrl { get; protected set; }
        public ROM FCnt { get; protected set; }
        public ROM FOpts { get; protected set; }
        public ROM FHeader { get; protected set; }
        public byte FPort { get; protected set; }
        public ROM FrmPayload { get; protected set; }

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
        public UnconfirmedDataUpPacket(ROM message)
            : base(message)
        {
        }

        public UnconfirmedDataUpPacket(ROM deviceAddress,
                                       FrameControl frameCtrl,
                                       ROM fCnt,
                                       ROM fOpts,
                                       byte fPort,
                                       ROM payload,
                                       ROM? networkSKey,
                                       ROM? appSKey)
            : base((byte)PacketType.UnconfirmedDataUp << 5,
                   deviceAddress,
                   frameCtrl,
                   fCnt,
                   fOpts,
                   fPort,
                   payload,
                   networkSKey,
                   appSKey)
        {
        }
    }

    public class UnconfirmedDataDownPacket(ROM message) : DataPacket(message);

    public class ConfirmedDataUpPacket(ROM message) : DataPacket(message);

    public class ConfirmedDataDownPacket(ROM message) : DataPacket(message);
}