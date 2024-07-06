using System;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
    {
        private static readonly JoinEui DefaultAppEui = new([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        private readonly JoinEui _appEui = parameters.AppEui ?? DefaultAppEui;
        public OtaaSettings? Settings;

        private readonly Func<LoRaParameters> _defaultUplinkParameters =
            () => new(parameters.FrequencyManager.GetNextUplinkFrequency(),
                      parameters.FrequencyManager.UplinkBandwidth,
                      CodingRate.Cr45,
                      SpreadingFactor.Sf7,
                      false,
                      true,
                      false);

        private readonly LoRaParameters _defaultDownlinkParameters =
            new(parameters.FrequencyManager.DownlinkBaseFrequency,
                parameters.FrequencyManager.DownlinkBandwidth,
                CodingRate.Cr45,
                SpreadingFactor.Sf7,
                false,
                true,
                true);

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
            logger.Debug("Settings");
            logger.Debug(settings.ToString());
            radio.OnReceived += Radio_OnReceived;
            // This is just an insanity check
            ThrowIfSettingsNull();
        }

        private void Radio_OnReceived(object sender, RadioDataReceived e)
        {
            // TODO: Handle unsolicited downlink messages
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

            await radio.SetLoRaParameters(_defaultUplinkParameters());
            logger.Debug("Sending message");
            await radio.Send(dataPacket.PhyPayload).ConfigureAwait(false);

            await Settings!.IncUplinkFrameCounter().ConfigureAwait(false);

            try
            {
                await radio.SetLoRaParameters(_defaultDownlinkParameters);

                var downlinkData = await radio.Receive(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                var packet = new UnconfirmedDataDownPacket(downlinkData.MessagePayload, Settings.AppSKey, Settings.NetworkSKey);
                if (packet.DeviceAddress != Settings.DeviceAddress)
                {
                    logger.Trace("Received downlink message with incorrect device address");
                    return;
                }
                logger.Debug($"Received message: {packet}");
            }
            catch (TimeoutException)
            {
                logger.Error("No downlink received in window");
            }
            logger.Debug("Finished sending message");
        }

        private async ValueTask<JoinAcceptPacket> SendJoinRequest(DeviceNonce devNonce)
        {
            logger.Info("Sending join-request");
            var request = new JoinRequestPacket(parameters.AppKey, _appEui, parameters.DevEui, devNonce);
            await radio.SetLoRaParameters(_defaultUplinkParameters());
            await radio.Send(request.PhyPayload);
            logger.Debug("join-request sent, waiting for response");
            // TODO: sleep until downlink time
            await Task.Delay(4000).ConfigureAwait(false);
            await radio.SetLoRaParameters(_defaultDownlinkParameters);
            var res = await radio.Receive(TimeSpan.FromSeconds(10));
            logger.Debug($"Join accept received: {Convert.ToBase64String(res.MessagePayload)}");
            return new JoinAcceptPacket(parameters.AppKey, res.MessagePayload);
        }

        private void ThrowIfSettingsNull()
        {
            if (Settings == null)
                throw new InvalidOperationException("Settings cannot be null");
        }
    }
}
