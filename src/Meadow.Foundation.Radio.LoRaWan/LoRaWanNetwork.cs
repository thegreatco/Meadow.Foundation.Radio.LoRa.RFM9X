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
            OtaaSettings settings = new OtaaSettings();
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
                    settings = new OtaaSettings(appKey, joinResponse, devNonce);

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
            Console.WriteLine($"Device Address: {settings.DeviceAddress.ToHexString(false)}");
            _settings = settings;
            // This is just an insanity check
            ThrowIfSettingsNull();
        }

        public async ValueTask SendMessage(byte[] payload)
        {
            ThrowIfSettingsNull();
            var dataPacket = new UnconfirmedDataUpPacket(_settings!.DeviceAddress,
                                                     new UplinkFrameControl(false, false, false, false),
                                                     BitConverter.GetBytes(_settings.FrameCounter),
                                                     ReadOnlyMemory<byte>.Empty,
                                                     0x01,
                                                     payload,
                                                     _settings.NetworkSKey,
                                                     _settings.AppSKey);
            logger.Debug("Sending message");
            logger.Trace($"PHYPayload: {Convert.ToBase64String(dataPacket.PhyPayload.Span)}");
            await radio.Send(dataPacket.PhyPayload).ConfigureAwait(false);
            await _settings!.IncFrameCounter().ConfigureAwait(false);
            logger.Debug("Finished sending message");
        }

        private async ValueTask<JoinAcceptPacket> SendJoinRequest(DeviceNonce devNonce)
        {
            var request = new JoinRequestPacket(appKey, _appEui, devEui, devNonce.Value);
            var message = request.PhyPayload;
            var res = await radio.SendAndReceive(message, TimeSpan.FromMinutes(1));
            logger.Debug(res.MessagePayload.ToHexString());
            return (JoinAcceptPacket)Packet.DecodePacket(appKey, res.MessagePayload);
        }

        private void ThrowIfSettingsNull()
        {
            if (_settings == null)
                throw new InvalidOperationException("Settings cannot be null");
        }
    }
}
