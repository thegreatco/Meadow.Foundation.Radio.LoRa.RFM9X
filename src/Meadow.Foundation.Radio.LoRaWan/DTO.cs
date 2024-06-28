using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal record struct JoinRequest(byte[] JoinEui, byte[] DevEui, byte[] AppKey, ushort DevNonce)
    {
        public RentedArray<byte> ToMessage()
        {
            var joinRequestMessage = new RentedArray<byte>(23);
            joinRequestMessage.Array[0] = 0x00; // [0]
            Array.Copy(JoinEui, 0, joinRequestMessage.Array, 1, 8);
            Array.Copy(DevEui, 0, joinRequestMessage.Array, 9, 8);
            joinRequestMessage.Array[17] = (byte)(DevNonce & 0xFF);
            joinRequestMessage.Array[18] = (byte)((DevNonce >> 8) & 0xFF);

            // Compute MIC here and fill in the last 4 bytes of joinRequestMessage with the MIC value
            // The ComputeMic method needs to be implemented based on your cryptographic library's capabilities
            //var computedMic = AesCMac.ComputeAesCMac(joinRequestMessage.Array, AppKey);
            var computedMic = AESCMACExample.ComputeAESCMAC(AppKey, joinRequestMessage.Array);
            // Only take the first 4 bytes
            Array.Copy(computedMic, 0, joinRequestMessage.Array, 19, 4);

            return joinRequestMessage;
        }
    }

    internal record struct JoinResponse(byte[] b)
    {
        public byte[] AppNonce { get; } = b[..3];
        public byte[] NetworkIdentifier { get; } = b[3..6];
        public byte[] DeviceAddress { get; } = b[6..10];
        public byte[] DownlinkSettings { get;  } = b[10..11];
        public int ReceiveDelay { get; } = Convert.ToInt32(b[11..12][0]);
        public byte[] ChannelFrequencyList { get; } = b[12..];
    }
}
