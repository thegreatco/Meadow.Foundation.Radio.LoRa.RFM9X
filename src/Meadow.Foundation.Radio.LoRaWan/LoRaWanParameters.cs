namespace Meadow.Foundation.Radio.LoRaWan
{
    public class LoRaWanParameters(LoRaWanChannelPlan plan, AppKey appKey, DevEui devEui, JoinEui? appEui = null)
    {
        public readonly LoRaWanChannelPlan Plan = plan;
        public readonly AppKey AppKey = appKey;
        public readonly DevEui DevEui = devEui;
        public readonly JoinEui? AppEui = appEui;
        internal readonly LoRaWanFrequencyManager FrequencyManager = new(plan);
    }
}
