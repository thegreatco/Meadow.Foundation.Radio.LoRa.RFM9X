using System;
using System.IO;
using System.Threading.Tasks;

using Meadow.Foundation.Serialization;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, byte[] devEui, byte[] appKey, byte[]? appEui = null)
    {
        private readonly Random _random = new();
        private static readonly byte[] DefaultAppEui = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        private readonly byte[] _appEui = appEui ?? DefaultAppEui;
        private OTAASettings _settings;

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);
            OTAASettings settings;
            if (File.Exists("otaa_settings.json"))
            {
                var json = await File.ReadAllTextAsync("otaa_settings.json").ConfigureAwait(false);
                settings = MicroJson.Deserialize<OTAASettings>(json);
            }
            else
            {
            retry:
                try
                {
                    var devNonce = BitConverter.GetBytes(_random.Next(ushort.MinValue, ushort.MaxValue + 1));
                    var joinResponse = await SendJoinRequest(devNonce)
                                           .ConfigureAwait(false);

                    settings =
                        new OTAASettings(appKey, joinResponse.JoinNonce, joinResponse.NetworkId, devNonce);

                    await File.WriteAllTextAsync("otaa_settings.json", MicroJson.Serialize(settings)).ConfigureAwait(false);

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

        public void SendMessage(byte[] message)
        {
            var payload = new byte[13 + message.Length];
            payload[0] = 0x40;
            Array.Copy(_settings.DeviceAddress, 0, payload, 1, 4);
            payload[5] = 0x00; // This is an uplink, a downlink would be 1
            payload[6] = 0x00; // Low side of frame counter
            payload[7] = BitConverter.GetBytes(_settings.FrameCounter)[0];
        }

        private async ValueTask<JoinResponse> SendJoinRequest(byte[] devNonce)
        {
            var request = new JoinRequest(_appEui, devEui, appKey, devNonce);
            using var message = request.ToMessage();
            //while (true)
            //{
            //    logger.Info($"Sending join message {BitConverter.ToString(message.Array[..message.Length]).Replace("-", " ")}");
            //    await radio.Send(message.Array[..message.Length]).ConfigureAwait(false);
            //    await Task.Delay(1000).ConfigureAwait(false);
            //}
            var res = await radio.SendAndReceive(message.Array[..message.Length], TimeSpan.FromMinutes(1));
            logger.Debug(res.MessagePayload.ToHexString());
            return new JoinResponse(appKey, res.MessagePayload);
        }
    }
}
