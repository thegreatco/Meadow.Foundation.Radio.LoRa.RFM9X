using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public record struct DeviceNonce(byte[] Value)
    {
        public readonly int Length => Value.Length;
        public static DeviceNonce GenerateNewNonce()
        {
            var filePath = "/meadow0/Data/nonce.bin";
            if (File.Exists(filePath))
            {
                var bytes = File.ReadAllBytes(filePath);
                var s = BitConverter.ToInt16(bytes);
                s++;
                bytes = BitConverter.GetBytes(s);
                File.WriteAllBytes(filePath, bytes);
                return new DeviceNonce(bytes);
            }
            else
            {
                var rand = new Random();
                short s = (short)rand.Next(0, 255);
                var bytes = BitConverter.GetBytes(s);
                File.WriteAllBytes(filePath, bytes);
                return new DeviceNonce(bytes);
            }
        }
    }

    public abstract class ByteValue(byte[] Value)
    {
        public readonly byte[] Value = Value;
        public readonly int Length = Value.Length;
        public override string ToString()
        {
            return Value.ToHexString();
        }
        public override bool Equals(object? obj)
        {
            if (obj is ByteValue other)
            {
                return Value.SequenceEqual(other.Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Compute hash code from the contents of the byte array
                foreach (byte element in Value)
                {
                    hash = hash * 31 + element.GetHashCode();
                }
                return hash;
            }
        }
    }

    public class Mic(byte[] Value) : ByteValue(Value);
    public class DevEui(byte[] Value) : ByteValue(Value);
    public class JoinEui(byte[] Value) : ByteValue(Value);
    public class AppKey(byte[] Value) : ByteValue(Value);
    public class JoinNonce(byte[] Value) : ByteValue(Value);
    public class NetworkId(byte[] Value) : ByteValue(Value);
    public class DeviceAddress(byte[] Value) : ByteValue(Value);
    public class NetworkSKey(byte[] Value) : ByteValue(Value);
    public class AppSKey(byte[] Value) : ByteValue(Value);

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

        public OtaaSettings(AppKey appKey, JoinAccept joinAccept, DeviceNonce deviceNonce)
        {
            AppKey = appKey;
            AppNonce = joinAccept.JoinNonce;
            NetworkId = joinAccept.NetworkId;
            DeviceAddress = joinAccept.DeviceAddress;
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

        internal void IncUplinkFrameCounter()
        {
            UplinkFrameCounter++;
            using var s = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            s.Seek(28, SeekOrigin.Begin);
            s.Write(BitConverter.GetBytes(UplinkFrameCounter)[..2]);
            s.Flush();
        }

        internal void IncDownlinkFrameCounter()
        {
            DownlinkFrameCounter++;
            using var s = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            s.Seek(30, SeekOrigin.Begin);
            s.Write(BitConverter.GetBytes(UplinkFrameCounter)[..2]);
            s.Flush();
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

        public static async ValueTask<OtaaSettings?> LoadSettings(string? fileName = null)
        {
            fileName ??= FileName;

            if (!File.Exists(fileName))
            {
                Console.WriteLine("Settings file not found.");
                return null;
            }

            await using var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytes = new byte[64];
            _ = await s.ReadAsync(bytes).ConfigureAwait(false);
            return new OtaaSettings(bytes);
        }

        public async ValueTask SaveSettings(string? fileName = null)
        {
            fileName ??= FileName;

            await using var s = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
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
