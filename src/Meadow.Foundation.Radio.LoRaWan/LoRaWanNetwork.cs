using System;
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

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);
            try
            {
                var joinResponse = await SendJoinRequest()
                                       .ConfigureAwait(false);
                logger.Debug($"JoinResponse Valid? {joinResponse.IsValid(appKey)}");
                logger.Debug($"Join response: {MicroJson.Serialize(joinResponse)}");
            }
            catch (TimeoutException tex)
            {
                logger.Error(tex, "Join request timed out");
            }
        }

        public void SendMessage() { }

        private async ValueTask<JoinResponse> SendJoinRequest()
        {
            var devNonce = BitConverter.GetBytes(_random.Next(ushort.MinValue, ushort.MaxValue + 1));
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
