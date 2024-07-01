using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Meadow.Foundation.Serialization;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class LoRaWanNetwork(Logger logger, IPlatformOS os, ILoRaRadio radio, byte[] devEui, byte[] appKey, byte[]? appEui = null)
    {
        private static readonly byte[] DefaultAppEui = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        private readonly byte[] _appEui = appEui ?? DefaultAppEui;
        public OtaaSettings? _settings;

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);

#pragma warning disable CS0618 // Type or member is obsolete, stupid trimming
            var settings = new OtaaSettings();
#pragma warning restore CS0618 // Type or member is obsolete
            var settingsFile = Path.Combine(os.FileSystem.DataDirectory, "otaa_settings.json");
            logger.Debug($"Using settings file: {settingsFile}");
            if (File.Exists(settingsFile))
            {
                logger.Debug("Loading settings from file");
                await using var s = File.Open(settingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                using var sr = new StreamReader(s);
                var b = await sr.ReadToEndAsync();
                settings = MicroJson.Deserialize<OtaaSettings>(b);
                logger.Debug("Successfully read settings from file");
            }
            else
            {
            retry:
                try
                {
                    logger.Debug("Activating new device");
                    var devNonce = DeviceNonce.GenerateNewNonce();
                    var joinResponse = await SendJoinRequest(devNonce)
                                           .ConfigureAwait(false);

                    logger.Debug("Device activated successfully");
                    settings =
                        new OtaaSettings(appKey, joinResponse, devNonce);

                    await using var s = File.Open(settingsFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                    await using var sw = new StreamWriter(s);
                    await sw.WriteAsync(MicroJson.Serialize(settings))
                            .ConfigureAwait(false);
                    logger.Debug("Wrote settings to file");
                }
                catch (TimeoutException tex)
                {
                    logger.Error(tex, "Join request timed out");
                    // This is bad, should probably not blindly loop forever.
                    goto retry;
                }
            }
            _settings = settings;
            // This is just an insanity check
            ThrowIfSettingsNull();
        }

        public async ValueTask SendMessage(byte[] payload)
        {
            ThrowIfSettingsNull();
            var finalPayload = EncodeMessage(payload, _settings!.DeviceAddress, _settings.FrameCounter, _settings.AppSKey, _settings.NetworkSKey);
            logger.Debug("Sending message");
            await radio.Send(finalPayload).ConfigureAwait(false);
            await _settings!.IncFrameCounter().ConfigureAwait(false);
            logger.Debug("Finished sending message");
        }

        public static byte[] EncodeMessage(byte[] payload, byte[] deviceAddress, uint frameCounter, byte[] appSKey, byte[] networkSKey)
        {
            //logger.Debug("Building message");
            var macPayload = new byte[13 + payload.Length];
            macPayload[0] = 0x40;                              // MHDR
            Array.Copy(deviceAddress, 0, macPayload, 1, 4);    // DevAddr
            macPayload[5] = 0;                                 // FCtrl
            macPayload[6] = (byte)(frameCounter & 0xFF);       // FCnt (low byte)
            macPayload[7] = (byte)((frameCounter >> 8) & 0xFF);// FCnt (high byte)
            macPayload[8] = 0;                                 // FPort
            Array.Copy(payload, 0, macPayload, 9, payload.Length);

            var encryptedBytes = EncryptPayload(payload[1..],
                                                appSKey,
                                                deviceAddress,
                                                frameCounter,
                                                true);

            Array.Copy(encryptedBytes, 0, macPayload, 9, encryptedBytes.Length);

            //logger.Debug("Calculating MIC");
            var mic = EncryptionTools.ComputeAesCMac(networkSKey, encryptedBytes);

            var finalPayload = new byte[macPayload.Length + 4];
            Array.Copy(macPayload, 0, finalPayload, 0,                 macPayload.Length);
            Array.Copy(mic,        0, finalPayload, macPayload.Length, 4);
            return finalPayload;
        }

        public static byte[] EncryptPayload(byte[] payload, byte[] appSKey, byte[] deviceAddress, uint frameCounter, bool uplink)
        {
            // Initialize AES encryption in ECB mode. CTR mode is not directly supported in .NET but can be emulated using ECB.
            using (Aes aes = new AesManaged())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = appSKey;

                byte[] encryptedPayload = new byte[payload.Length];
                byte[] blockA = new byte[16];
                int blockCounter = 1;

                for (int i = 0; i < payload.Length; i += 16)
                {
                    // Prepare the counter block (A)
                    blockA[0] = 0x01;                             // Reserved bits
                    blockA[5] = uplink ? (byte)0x00 : (byte)0x01; // Direction: 0 for uplink, 1 for downlink
                    Array.Copy(deviceAddress, 0, blockA, 6, 4);   // Device address
                    blockA[10] = (byte)(frameCounter & 0xFF);
                    blockA[11] = (byte)((frameCounter >> 8) & 0xFF);
                    blockA[12] = (byte)((frameCounter >> 16) & 0xFF);
                    blockA[13] = (byte)((frameCounter >> 24) & 0xFF);
                    blockA[14] = (byte)(blockCounter & 0xFF);
                    blockA[15] = (byte)((blockCounter >> 8) & 0xFF);

                    // Encrypt the counter block to get the keystream
                    byte[] keystream = aes.CreateEncryptor().TransformFinalBlock(blockA, 0, blockA.Length);

                    // XOR the payload with the keystream
                    for (int j = 0; j < 16 && (i + j) < payload.Length; j++)
                    {
                        encryptedPayload[i + j] = (byte)(payload[i + j] ^ keystream[j]);
                    }

                    blockCounter++;
                }

                return encryptedPayload;
            }
        }

        private void EncryptPayload(byte[] payload)
        {
            ThrowIfSettingsNull();

            logger.Trace("Encrypting payload, building block");
            var counterBlock = new byte[13 + payload.Length];
            counterBlock[0] = 0x40;
            Array.Copy(_settings!.DeviceAddress, 0, counterBlock, 1, 4);
            counterBlock[5] = 0x00; // This is an uplink, a downlink would be 1
            counterBlock[6] = 0x00; // Low side of frame counter
            counterBlock[7] = BitConverter.GetBytes(_settings.FrameCounter)[0];

            logger.Trace("Starting AES encrypt");
            using Aes aes = new AesManaged();
            aes.Key = _settings.AppSKey;
            aes.Mode = CipherMode.ECB;
            using var encryptor = aes.CreateEncryptor();
            var s = encryptor.TransformFinalBlock(counterBlock, 0, 16);
            for (var i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(payload[i] ^ s[i]);
            }
            logger.Trace("Finished encrypting payload");
        }

        private async ValueTask<JoinAcceptPacket> SendJoinRequest(DeviceNonce devNonce)
        {
            var request = new JoinRequestPacket(_appEui, devEui, appKey, devNonce.Value);
            var message = request.PhyPayload;
            var res = await radio.SendAndReceive(message, TimeSpan.FromMinutes(1));
            logger.Debug(res.MessagePayload.ToHexString());
            return new JoinAcceptPacket(res.MessagePayload);
        }

        private void ThrowIfSettingsNull()
        {
            if (_settings == null)
                throw new InvalidOperationException("Settings cannot be null");
        }
    }
}
