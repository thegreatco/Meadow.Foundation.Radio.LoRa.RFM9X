using System;
using System.IO;
using System.Threading.Tasks;
using Meadow.Foundation.Serialization;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public record struct DeviceNonce
    {
        public DeviceNonce(byte[] value)
        {
            Value = value;
        }

        public static DeviceNonce GenerateNewNonce()
        {
            var random = new Random();
            var nonce = new byte[2];
            random.NextBytes(nonce);
            return new DeviceNonce(nonce);
        }

        public byte[] Value { get; set; }
    }

    /// <summary>
    /// Contains the settings for Over The Air Activation (OTAA) for a LoRaWAN device.
    /// This also gets written to disk for persistence through a reboot.
    /// </summary>
    public class OtaaSettings
    {
        private const string FileName = "/meadow0/Data/otaa_settings.json";

        [Obsolete("For JSON only")]
        public OtaaSettings() { }

        public OtaaSettings(byte[] appKey,
                            byte[] appNonce,
                            byte[] networkId,
                            byte[] deviceAddress,
                            byte[] deviceNonce,
                            uint frameCounter)
        {
            AppKey = appKey;
            AppNonce = appNonce;
            NetworkId = networkId;
            DeviceAddress = deviceAddress;
            DeviceNonce = new DeviceNonce(deviceNonce);
            FrameCounter = frameCounter;
            NetworkSKey = GenerateNetworkSKey();
            AppSKey = GenerateAppSKey();
        }

        public OtaaSettings(byte[] appKey,
                            byte[] appNonce,
                            byte[] networkId,
                            byte[] deviceAddress,
                            byte[] deviceNonce,
                            uint frameCounter,
                            byte[] networkSKey,
                            byte[] appSKey)
        {
            AppKey = appKey;
            AppNonce = appNonce;
            NetworkId = networkId;
            DeviceAddress = deviceAddress;
            DeviceNonce = new DeviceNonce(deviceNonce);
            FrameCounter = frameCounter;
            NetworkSKey = networkSKey;
            AppSKey = appSKey;
        }

        public OtaaSettings(byte[] appKey, JoinAcceptPacket joinResponse, DeviceNonce deviceNonce)
        {
            AppKey = appKey;
            AppNonce = joinResponse.AppNonce.ToArray();
            NetworkId = joinResponse.NetworkId.ToArray();
            DeviceAddress = joinResponse.DeviceAddress.ToArray();
            Console.WriteLine($"Device Address: {DeviceAddress.ToHexString(false)}");
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
        public uint FrameCounter { get; set; }
        public byte[] NetworkSKey { get; set; }
        public byte[] AppSKey { get; set; }

        internal async ValueTask IncFrameCounter()
        {
            FrameCounter++;
            var json = MicroJson.Serialize(this);
            await File.WriteAllTextAsync(FileName, json).ConfigureAwait(false);
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
