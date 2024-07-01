using System;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public record struct DeviceNonce(byte[] Value)
    {
        public static DeviceNonce GenerateNewNonce()
        {
            var random = new Random();
            var nonce = new byte[2];
            random.NextBytes(nonce);
            return new DeviceNonce(nonce);
        }
    }

    public record struct EncryptedMessage(byte[] Value);

    public record struct JoinRequest(byte[] JoinEui, byte[] DevEui, byte[] AppKey, DeviceNonce DevNonce)
    {
        public readonly byte[] ToMessage()
        {
            byte messageHeader = 0x00;
            var joinRequestMessage = new byte[23];
            joinRequestMessage[0] = messageHeader; // MHDR join should be 0x00
            Array.Copy(JoinEui, 0, joinRequestMessage, 1, 8);
            Array.Copy(DevEui, 0, joinRequestMessage, 9, 8);
            Array.Copy(DevNonce.Value, 0, joinRequestMessage, 17, 2);

            // Compute MIC here and fill in the last 4 bytes of joinRequestMessage with the MIC value
            var computedMic = EncryptionTools.ComputeAesCMac(AppKey, joinRequestMessage[..19]);

            // Only take the first 4 bytes
            Array.Copy(computedMic, 0, joinRequestMessage, 19, 4);

            return joinRequestMessage;
        }
    }

    public readonly record struct JoinResponse
    {
        public JoinResponse(byte[] appKey, byte[] message)
        {
            RawMessage = new byte[17];
            RawMessage[0] = message[0];
            var decryptedMessage = EncryptionTools.DecryptMessage(appKey, message[1..]);
            Array.Copy(decryptedMessage, 0, RawMessage, 1, 16);
            JoinNonce = RawMessage[1..4];
            NetworkId = RawMessage[4..7];
            DeviceAddress = RawMessage[7..11];
            DownlinkSettings = RawMessage[11..12];
            ReceiveDelay = Convert.ToInt32(RawMessage[12..13][0]);
            if (RawMessage.Length > 17)
            {
                ChannelFrequencyList = RawMessage[13..];
            }
            Mic = RawMessage[^4..];
        }
        public byte[] RawMessage { get; }
        public byte[] JoinNonce { get; }
        public byte[] NetworkId { get; }
        public byte[] DeviceAddress { get; }
        public byte[] DownlinkSettings { get; }
        public int ReceiveDelay { get; }
        public byte[] Mic { get; }
        public byte[]? ChannelFrequencyList { get; }

        public bool IsValid(byte[] appKey)
        {
            var computedMic = EncryptionTools.ComputeAesCMac(appKey, RawMessage[..^4]);
            var valid = true;
            for (var i = 0; i < 4; i++)
            {
                if (Mic[i] != computedMic[i])
                {
                    valid = false;
                }
            }
            return valid;
        }
    }

    /// <summary>
    /// Contains the settings for Over The Air Activation (OTAA) for a LoRaWAN device.
    /// This also gets written to disk for persistence through a reboot.
    /// </summary>
    public record struct OtaaSettings
    {
        public OtaaSettings(byte[] appKey, JoinResponse joinResponse, DeviceNonce deviceNonce)
        {
            AppKey = appKey;
            AppNonce = joinResponse.JoinNonce;
            NetworkId = joinResponse.NetworkId;
            DeviceAddress = joinResponse.DeviceAddress;
            DeviceNonce = deviceNonce;
            FrameCounter = 0;
            NetworkSKey = GenerateNetworkSKey();
            AppSKey = GenerateAppSKey();
        }

        public byte[] AppKey { get; set; }
        public byte[] AppNonce { get; set; }
        public byte[] NetworkId { get; set; }
        public DeviceNonce DeviceNonce { get; set; }
        public byte[] DeviceAddress { get; set; }
        public int FrameCounter { get; private set; }
        public byte[] NetworkSKey { get; set; }
        public byte[] AppSKey { get; set; }

        public void IncFrameCounter()
        {
            FrameCounter++;
        }

        private byte[] GenerateNetworkSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x01;
            Array.Copy(AppNonce, 0, bytes, 1, AppNonce.Length);
            Array.Copy(NetworkId, 0, bytes, 1 + AppNonce.Length, NetworkId.Length);
            Array.Copy(DeviceNonce.Value, 0, bytes, 1 + AppNonce.Length + NetworkId.Length, DeviceNonce.Value.Length);

            return EncryptionTools.EncryptMessage(AppKey, bytes);
        }

        private byte[] GenerateAppSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x02;
            Array.Copy(AppNonce,    0, bytes, 1,                                      AppNonce.Length);
            Array.Copy(NetworkId,   0, bytes, 1 + AppNonce.Length,                    NetworkId.Length);
            Array.Copy(DeviceNonce.Value, 0, bytes, 1 + AppNonce.Length + NetworkId.Length, DeviceNonce.Value.Length);

            return EncryptionTools.EncryptMessage(AppKey, bytes);
        }
    }
}
