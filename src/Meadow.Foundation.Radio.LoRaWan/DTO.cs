using System;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal record struct JoinRequest(byte[] JoinEui, byte[] DevEui, byte[] AppKey, byte[] DevNonce)
    {
        public readonly RentedArray<byte> ToMessage()
        {
            byte messageHeader = 0x00;
            var joinRequestMessage = new RentedArray<byte>(23);
            joinRequestMessage.Array[0] = messageHeader; // MHDR join should be 0x00
            Array.Copy(JoinEui, 0, joinRequestMessage.Array, 1, 8);
            Array.Copy(DevEui, 0, joinRequestMessage.Array, 9, 8);
            Array.Copy(DevNonce, 0, joinRequestMessage.Array, 17, 2);

            // Compute MIC here and fill in the last 4 bytes of joinRequestMessage with the MIC value
            var computedMic = EncryptionTools.ComputeAesCMac(AppKey, joinRequestMessage.Array[..19]);

            // Only take the first 4 bytes
            Array.Copy(computedMic, 0, joinRequestMessage.Array, 19, 4);

            return joinRequestMessage;
        }
    }

    internal readonly record struct JoinResponse
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

    internal readonly record struct OtaaSettings
    {
        public OtaaSettings(byte[] appKey, byte[] appNonce, byte[] networkId, byte[] deviceNonce)
        {
            AppKey = appKey;
            AppNonce = appNonce;
            NetworkId = networkId;
            DeviceNonce = deviceNonce;
            NetworkSKey = GenerateNetworkSKey();
            AppSKey = GenerateAppSKey();
        }

        public OtaaSettings(byte[] appKey, byte[] appNonce, byte[] networkId, byte[] deviceNonce, byte[] networkSKey, byte[] appSKey)
        {
            AppKey = appKey;
            AppNonce = appNonce;
            NetworkId = networkId;
            DeviceNonce = deviceNonce;
            NetworkSKey = networkSKey;
        }

        public byte[] AppKey { get; }
        public byte[] AppNonce { get; }
        public byte[] NetworkId { get; }
        public byte[] DeviceNonce { get; }
        public byte[] NetworkSKey { get; }
        public byte[] AppSKey { get; }

        private byte[] GenerateNetworkSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x01;
            Array.Copy(AppNonce, 0, bytes, 1, AppNonce.Length);
            Array.Copy(NetworkId, 0, bytes, 1 + AppNonce.Length, NetworkId.Length);
            Array.Copy(DeviceNonce, 0, bytes, 1 + AppNonce.Length + NetworkId.Length, DeviceNonce.Length);

            return EncryptionTools.EncryptMessage(AppKey, bytes);
        }

        private byte[] GenerateAppSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x02;
            Array.Copy(AppNonce,    0, bytes, 1,                                      AppNonce.Length);
            Array.Copy(NetworkId,   0, bytes, 1 + AppNonce.Length,                    NetworkId.Length);
            Array.Copy(DeviceNonce, 0, bytes, 1 + AppNonce.Length + NetworkId.Length, DeviceNonce.Length);

            return EncryptionTools.EncryptMessage(AppKey, bytes);
        }
    }
}
