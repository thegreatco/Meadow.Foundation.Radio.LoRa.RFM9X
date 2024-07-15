using System;
using System.Collections.Generic;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal sealed class LoRaWanFrequencyManager(LoRaWanChannelPlan plan)
    {
        public LoRaWanChannelPlan Plan { get; } = plan;

        private IDictionary<int, LoRaWanChannel1> EnabledUpstreamChannels { get; } = new Dictionary<int, LoRaWanChannel1>(plan.AvailableUpstreamChannels);
        private IDictionary<int, LoRaWanChannel1> EnabledDownstreamChannels { get; } = new Dictionary<int, LoRaWanChannel1>(plan.AvailableDownstreamChannels);

        public LoRaWanChannel1 GetJoinFrequency()
        {
            return Plan.GetJoinChannel();
        }

        public LoRaWanChannel1 GetNextUplinkFrequency()
        {
            throw new NotImplementedException();
        }

        public LoRaWanChannel1 GetDownlinkFrequency()
        {
            return Plan.GetDownstreamChannel();
        }
    }
}
