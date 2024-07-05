using Meadow.Units;
using System.Linq;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal class LoRaWanFrequencyManager(LoRaWanChannel wanChannel)
    {
        public readonly Frequency UplinkBandwidth = wanChannel.UplinkBandwidth;
        public readonly Frequency DownlinkBandwidth = wanChannel.DownlinkBandwidth;
        public readonly Frequency UplinkBaseFrequency = wanChannel.UplinkBaseFrequency;
        public readonly Frequency DownlinkBaseFrequency = wanChannel.DownlinkBaseFrequency;

        private readonly Frequency[] _uplinkChannels = Enumerable.Range(0, wanChannel.UplinkChannelCount)
                                                                 .Select(i => new Frequency(
                                                                             wanChannel.UplinkBaseFrequency.Hertz
                                                                           + i * wanChannel.UplinkChannelWidth.Hertz))
                                                                 .ToArray();

        private readonly Frequency[] _downlinkChannels = Enumerable.Range(0, wanChannel.DownlinkChannelCount)
                                                                   .Select(i => new Frequency(
                                                                               wanChannel.DownlinkBaseFrequency.Hertz
                                                                             + i * wanChannel.DownlinkChannelWidth.Hertz))
                                                                   .ToArray();

        private int _currentChannel = 0;

        public Frequency GetNextUplinkFrequency()
        {
            if (_currentChannel >= _uplinkChannels.Length)
                _currentChannel = 0;

            return _uplinkChannels[_currentChannel++];
        }
    }
}
