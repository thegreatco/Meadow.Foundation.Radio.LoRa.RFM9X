namespace Meadow.Foundation.Radio.LoRaWan
{
    internal sealed class LoRaWanFrequencyManager
    {
        private int _currentUpstreamChannel = 0;

        public LoRaWanFrequencyManager(LoRaWanChannelPlan plan)
        {
            Plan = plan;
            EnabledUpstreamChannels = new LoRaWanChannel[plan.AvailableUpstreamChannels.Count];
            foreach (var channel in plan.AvailableUpstreamChannels)
            {
                EnabledUpstreamChannels[channel.Key] = channel.Value;
            }
            EnabledDownstreamChannels = new LoRaWanChannel[plan.AvailableDownstreamChannels.Count];
            foreach (var channel in plan.AvailableDownstreamChannels)
            {
                EnabledDownstreamChannels[channel.Key] = channel.Value;
            }
        }

        public LoRaWanChannelPlan Plan { get; }

        public LoRaWanChannel?[] EnabledUpstreamChannels { get; }
        public LoRaWanChannel?[] EnabledDownstreamChannels { get; }

        public void SetChannelState(int channel, bool enabled)
        {
            if (EnabledUpstreamChannels[channel] != null && !enabled)
            {
                EnabledUpstreamChannels[channel] = null;
            }

            if (EnabledUpstreamChannels[channel] == null && enabled)
            {
                EnabledUpstreamChannels[channel] = Plan.AvailableUpstreamChannels[channel];
            }
        }

        public bool GetChannelState(int channel)
        {
            return EnabledUpstreamChannels[channel] != null;
        }

        public LoRaWanChannel GetJoinFrequency()
        {
            return Plan.GetJoinChannel();
        }

        public LoRaWanChannel GetNextUplinkFrequency()
        {
            LoRaWanChannel? channel;
            try
            {
                int startChannel = _currentUpstreamChannel;
                do
                {
                    channel = EnabledUpstreamChannels[_currentUpstreamChannel];
                    if (channel != null)
                        return channel;
                    _currentUpstreamChannel = (_currentUpstreamChannel + 1) % EnabledUpstreamChannels.Length;
                } while (_currentUpstreamChannel != startChannel);
            }
            finally
            {
                _currentUpstreamChannel = (_currentUpstreamChannel + 1) % EnabledUpstreamChannels.Length;
            }

            throw new NoAvailableChannelsException();
        }

        public LoRaWanChannel GetDownlinkFrequency()
        {
            return Plan.GetDownstreamChannel();
        }
    }
}
