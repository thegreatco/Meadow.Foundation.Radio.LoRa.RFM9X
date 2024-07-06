using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public record struct DeviceNonce(byte[] Value)
    {
        public readonly int Length => Value.Length;
        public static DeviceNonce GenerateNewNonce()
        {
            var random = new Random();
            var nonce = new byte[2];
            random.NextBytes(nonce);
            return new DeviceNonce(nonce);
        }
    }


    public record struct Mic(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }

    public record struct DevEui(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct JoinEui(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct AppKey(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct JoinNonce(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct NetworkId(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct DeviceAddress(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct NetworkSKey(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }
    public record struct AppSKey(byte[] Value)
    {
        public readonly int Length => Value.Length;
    }

    /// <summary>
    /// Contains the settings for Over The Air Activation (OTAA) for a LoRaWAN device.
    /// This also gets written to disk for persistence through a reboot.
    /// </summary>
    public class OtaaSettings
    {
        private const string FileName = "/meadow0/Data/otaa_settings.bin";

        public OtaaSettings(ReadOnlyMemory<byte> bytes)
        {
            AppKey = new AppKey(bytes[..16].ToArray());
            AppNonce = new JoinNonce(bytes[16..19].ToArray());
            NetworkId = new NetworkId(bytes[19..21].ToArray());
            DeviceNonce = new DeviceNonce(bytes[22..24]
                                               .ToArray());
            DeviceAddress = new DeviceAddress(bytes[24..28].ToArray());
            UplinkFrameCounter = BitConverter.ToUInt16(bytes[28..30].ToArray());
            DownlinkFrameCounter = BitConverter.ToUInt16(bytes[30..32].ToArray());
            NetworkSKey = new NetworkSKey(bytes[32..48].ToArray());
            AppSKey = new AppSKey(bytes[48..64].ToArray());
        }

        public OtaaSettings(AppKey appKey,
                            JoinNonce appNonce,
                            NetworkId networkId,
                            DeviceAddress deviceAddress,
                            DeviceNonce deviceNonce,
                            ushort uplinkFrameCounter,
                            ushort downlinkFrameCounter,
                            NetworkSKey networkSKey,
                            AppSKey appSKey)
        {
            AppKey = appKey;
            AppNonce = appNonce;
            NetworkId = networkId;
            DeviceAddress = deviceAddress;
            DeviceNonce = deviceNonce;
            UplinkFrameCounter = uplinkFrameCounter;
            DownlinkFrameCounter = downlinkFrameCounter;
            NetworkSKey = networkSKey;
            AppSKey = appSKey;
        }

        public OtaaSettings(AppKey appKey, JoinAcceptPacket joinResponse, DeviceNonce deviceNonce)
        {
            AppKey = appKey;
            AppNonce = new JoinNonce(joinResponse.AppNonce.ToArray());
            NetworkId = new NetworkId(joinResponse.NetworkId.ToArray());
            DeviceAddress = new DeviceAddress(joinResponse.DeviceAddress.ToArray());
            Console.WriteLine($"Device Address: {DeviceAddress.Value.ToHexString(false)}");
            DeviceNonce = deviceNonce;
            UplinkFrameCounter = 0;
            DownlinkFrameCounter = 0;
            NetworkSKey = GenerateNetworkSKey();
            AppSKey = GenerateAppSKey();
        }

        public AppKey AppKey { get; set; }
        public JoinNonce AppNonce { get; set; }
        public NetworkId NetworkId { get; set; }
        public DeviceNonce DeviceNonce { get; set; }
        public DeviceAddress DeviceAddress { get; set; }
        public ushort UplinkFrameCounter { get; set; }
        public ushort DownlinkFrameCounter { get; set; }
        public NetworkSKey NetworkSKey { get; set; }
        public AppSKey AppSKey { get; set; }

        internal async ValueTask IncUplinkFrameCounter()
        {
            UplinkFrameCounter++;
            await using var s = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            s.Seek(28, SeekOrigin.Begin);
            await s.WriteAsync(BitConverter.GetBytes(UplinkFrameCounter)[..2]);
            await s.FlushAsync();
        }

        internal async ValueTask IncDownlinkFrameCounter()
        {
            DownlinkFrameCounter++;
            await using var s = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            s.Seek(30, SeekOrigin.Begin);
            await s.WriteAsync(BitConverter.GetBytes(UplinkFrameCounter)[..2]);
            await s.FlushAsync();
        }

        private NetworkSKey GenerateNetworkSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x01;
            Array.Copy(AppNonce.Value, 0, bytes, 1, AppNonce.Value.Length);
            Array.Copy(NetworkId.Value, 0, bytes, 1 + AppNonce.Value.Length, NetworkId.Value.Length);
            Array.Copy(DeviceNonce.Value, 0, bytes, 1 + AppNonce.Value.Length + NetworkId.Value.Length, DeviceNonce.Value.Length);

            return new NetworkSKey(EncryptionTools.EncryptMessage(AppKey.Value, bytes));
        }

        private AppSKey GenerateAppSKey()
        {
            var bytes = new byte[16];
            bytes[0] = 0x02;
            Array.Copy(AppNonce.Value, 0, bytes, 1, AppNonce.Value.Length);
            Array.Copy(NetworkId.Value, 0, bytes, 1 + AppNonce.Value.Length, NetworkId.Value.Length);
            Array.Copy(DeviceNonce.Value, 0, bytes, 1 + AppNonce.Value.Length + NetworkId.Value.Length, DeviceNonce.Value.Length);

            return new AppSKey(EncryptionTools.EncryptMessage(AppKey.Value, bytes));
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[64];
            AppKey.Value.CopyTo(bytes, 0);
            AppNonce.Value.CopyTo(bytes, 16);
            NetworkId.Value.CopyTo(bytes, 19);
            DeviceNonce.Value.CopyTo(bytes, 22);
            DeviceAddress.Value.CopyTo(bytes, 24);
            BitConverter.GetBytes(UplinkFrameCounter).CopyTo(bytes, 28);
            BitConverter.GetBytes(DownlinkFrameCounter).CopyTo(bytes, 30);
            NetworkSKey.Value.CopyTo(bytes, 32);
            AppSKey.Value.CopyTo(bytes, 48);
            return bytes;
        }

        public static async ValueTask<OtaaSettings?> LoadSettings()
        {
            if (!File.Exists(FileName))
            {
                Console.WriteLine("Settings file not found.");
                return null;
            }

            await using var s = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytes = new byte[64];
            _ = await s.ReadAsync(bytes).ConfigureAwait(false);
            return new OtaaSettings(bytes);
        }

        public async ValueTask SaveSettings()
        {
            await using var s = File.Open(FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            await s.WriteAsync(ToBytes());
            await s.FlushAsync();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"AppKey:             {AppKey.Value.ToHexString(false)}");
            sb.AppendLine($"AppNonce:           {AppNonce.Value.ToHexString(false)}");
            sb.AppendLine($"NetworkId:          {NetworkId.Value.ToHexString(false)}");
            sb.AppendLine($"DeviceNonce:        {DeviceNonce.Value.ToHexString(false)}");
            sb.AppendLine($"DeviceAddress:      {DeviceAddress.Value.ToHexString(false)}");
            sb.AppendLine($"UplinkFrameCount:   {UplinkFrameCounter}");
            sb.AppendLine($"DownlinkFrameCount: {DownlinkFrameCounter}");
            sb.AppendLine($"NetworkSKey:        {NetworkSKey.Value.ToHexString(false)}");
            sb.AppendLine($"AppSKey:            {AppSKey.Value.ToHexString(false)}");
            return sb.ToString();
        }
    }
}
