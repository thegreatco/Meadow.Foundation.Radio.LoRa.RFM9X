namespace Meadow.Foundation.Radio.LoRaWan
{
    public class LoRaWanParameters(LoRaWanChannel channel, AppKey appKey, DevEui devEui, JoinEui? appEui = null)
    {
        public readonly LoRaWanChannel Channel = channel;
        public readonly AppKey AppKey = appKey;
        public readonly DevEui DevEui = devEui;
        public readonly JoinEui? AppEui = appEui;
        internal readonly LoRaWanFrequencyManager FrequencyManager = new(channel);
    }
}
