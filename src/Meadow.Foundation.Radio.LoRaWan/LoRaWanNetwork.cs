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
        private OtaaSettings _settings;

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);
            OtaaSettings settings;
            var settingsFile = Path.Combine(os.FileSystem.DataDirectory, "otaa_settings.json");
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
        }

        public async ValueTask SendMessage(byte[] payload)
        {
            logger.Debug("Building message");
            var payloadLength = 13 + payload.Length;
            var finalPayload = new byte[payloadLength + 4];
            finalPayload[0] = 0x40;                                 // MHDR
            Array.Copy(_settings.DeviceAddress, 0, finalPayload, 1, 4);
            finalPayload[5] = 0;                                    // FCtrl
            finalPayload[6] = (byte)(_settings.FrameCounter& 0xFF); // FCnt (low byte)
            finalPayload[7] = 0;                                    // FCnt (high byte)
            finalPayload[8] = 1;                                    // FPort
            Array.Copy(payload, 0, finalPayload, 9, payload.Length);

            EncryptPayload(finalPayload, payloadLength);

            logger.Debug("Calculating MIC");
            var mic = EncryptionTools.ComputeAesCMac(_settings.NetworkSKey, finalPayload[..payloadLength]);

            Array.Copy(mic, 0, finalPayload, payloadLength, 4);

            logger.Debug("Sending message");
            await radio.Send(finalPayload).ConfigureAwait(false);
            logger.Debug("Finished sending message");
        }

        private async ValueTask<JoinResponse> SendJoinRequest(DeviceNonce devNonce)
        {
            var request = new JoinRequest(_appEui, devEui, appKey, devNonce);
            var message = request.ToMessage();
            var res = await radio.SendAndReceive(message, TimeSpan.FromMinutes(1));
            logger.Debug(res.MessagePayload.ToHexString());
            return new JoinResponse(appKey, res.MessagePayload);
        }

        private void EncryptPayload(byte[] payload, int payloadLength)
        {
            logger.Trace("Encrypting payload, building block");
            var blockA = new byte[13 + payloadLength];
            blockA[0] = 0x40;
            Array.Copy(_settings.DeviceAddress, 0, blockA, 1, 4);
            blockA[5] = 0x00; // This is an uplink, a downlink would be 1
            blockA[6] = 0x00; // Low side of frame counter
            blockA[7] = BitConverter.GetBytes(_settings.FrameCounter)[0];

            logger.Trace("Starting AES encrypt");
            using Aes aes = new AesManaged();
            aes.Key = _settings.AppSKey;
            using var encryptor = aes.CreateEncryptor();
            var s = encryptor.TransformFinalBlock(blockA, 0, 16);
            for (var i = 0; i < payloadLength; i++)
            {
                payload[i] = (byte)(payload[i] ^ s[i]);
            }
            logger.Trace("Finished encrypting payload");
        }
    }
}
