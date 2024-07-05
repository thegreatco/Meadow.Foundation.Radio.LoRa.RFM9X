using System;
using System.Threading.Tasks;
using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
    {
        private static readonly AppEui DefaultAppEui = new([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        private readonly AppEui _appEui = parameters.AppEui ?? DefaultAppEui;
        public OtaaSettings? Settings;

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);

            var settings = await OtaaSettings.LoadSettings();
            if (settings == null)
            {
            retry:
                try
                {
                    logger.Debug("Activating new device");
                    var devNonce = DeviceNonce.GenerateNewNonce();
                    var joinResponse = await SendJoinRequest(devNonce)
                                           .ConfigureAwait(false);

                    logger.Debug("Device activated successfully");
                    settings = new OtaaSettings(parameters.AppKey, joinResponse, devNonce);
                    await settings.SaveSettings().ConfigureAwait(false);
                    logger.Debug("Wrote settings to file");
                }
                catch (TimeoutException tex)
                {
                    logger.Error(tex, "Join request timed out");
                    // This is bad, should probably not blindly loop forever.
                    goto retry;
                }
            }
            Settings = settings;
            Console.WriteLine(settings);
            // This is just an insanity check
            ThrowIfSettingsNull();
        }

        public async ValueTask SendMessage(byte[] payload)
        {
            ThrowIfSettingsNull();
            var dataPacket = new UnconfirmedDataUpPacket(Settings!.DeviceAddress,
                                                     new UplinkFrameControl(false, false, false, false),
                                                     Settings.UplinkFrameCounter,
                                                     ReadOnlyMemory<byte>.Empty,
                                                     0x01,
                                                     payload,
                                                     Settings.AppSKey,
                                                     Settings.NetworkSKey);
            logger.Debug("Sending message");
            logger.Trace($"PHYPayload: {Convert.ToBase64String(dataPacket.PhyPayload.Span)}");
            await radio.Send(dataPacket.PhyPayload).ConfigureAwait(false);
            await Settings!.IncUplinkFrameCounter().ConfigureAwait(false);
            logger.Debug("Finished sending message");
        }

        private async ValueTask<JoinAcceptPacket> SendJoinRequest(DeviceNonce devNonce)
        {
            var request = new JoinRequestPacket(parameters.AppKey, _appEui, parameters.DevEui, devNonce);
            var res = await radio.SendAndReceive(request.PhyPayload, TimeSpan.FromMinutes(1));
            logger.Debug(Convert.ToBase64String(res.MessagePayload));
            return new JoinAcceptPacket(parameters.AppKey, res.MessagePayload);
        }

        private void ThrowIfSettingsNull()
        {
            if (Settings == null)
                throw new InvalidOperationException("Settings cannot be null");
        }
    }
}
