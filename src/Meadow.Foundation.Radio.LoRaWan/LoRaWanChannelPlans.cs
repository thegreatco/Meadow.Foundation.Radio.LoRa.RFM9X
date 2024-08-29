using Meadow.Foundation.Radio.LoRa;
using Meadow.Units;

using System;
using System.Collections.Generic;
using System.Linq;

using static Meadow.Units.Frequency.UnitType;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public enum DataRate
    {
        DR0 = 0,
        DR1 = 1,
        DR2 = 2,
        DR3 = 3,
        DR4 = 4,
        DR5 = 5,
        DR6 = 6,
        DR7 = 7,
        // Downlink only
        DR8 = 8,
        DR9 = 9,
        DR10 = 10,
        DR11 = 11,
        DR12 = 12,
        DR13 = 13,
        DR14 = 14,
        DR15 = 15,
    };

    public enum DutyCycle
    {
        None,
        ListenBeforeTalk,
        LessThanOnePercent,
        LessThanTenPercent,
    }

    public record class FrequencyRange(Frequency Min, Frequency Max);
    public record class JoinRequestDataRateRange(DataRate Min, DataRate Max);
    public record class DataRateRange(DataRate Min, DataRate Max);
    public record class ChannelRange(int Min, int Max);
    public record class ChannelMask(int Index, ChannelRange ChannelRange);
    public record class DwellTimeLimitation(TimeSpan MaxDwellTime, ChannelRange ChannelRange);
    public record class TimeSpanRange(TimeSpan Min, TimeSpan Max);
    public record class LoRaWanChannelSettings(Frequency Frequency, Frequency Bandwidth, SpreadingFactor SpreadingFactor, DataRate DataRate);
    public class LoRaWanChannel(int channelNumber, Frequency frequency, Frequency bandwidth, bool enabled)
    {
        public int ChannelNumber { get; init; } = channelNumber;
        public Frequency Frequency { get; init; } = frequency;
        public Frequency Bandwidth { get; init; } = bandwidth;
        public bool Enabled { get; set; } = enabled;

        public override string ToString() => $"Channel {ChannelNumber}: {Frequency.Megahertz} {Bandwidth.Kilohertz} {(Enabled ? "Enabled" : "Disabled")}";
    }

    public sealed class US915ChannelPlan : LoRaWanChannelPlan
    {
        public US915ChannelPlan()
        {
            CodingRate = CodingRate.Cr45;
            DownstreamDataRateByRx1OffsetAndUpstreamDataRate = new Dictionary<DataRate, IReadOnlyDictionary<int, DataRate>>
            {
                {DataRate.DR0, new Dictionary<int, DataRate> {
                    {0, DataRate.DR10},
                    {1, DataRate.DR9},
                    {2, DataRate.DR8},
                    {3, DataRate.DR8}}},
                {DataRate.DR1, new Dictionary<int, DataRate> {
                    {0, DataRate.DR11},
                    {1, DataRate.DR10},
                    {2, DataRate.DR9},
                    {3, DataRate.DR8}}},
                {DataRate.DR2, new Dictionary<int, DataRate> {
                    {0, DataRate.DR12},
                    {1, DataRate.DR11},
                    {2, DataRate.DR10},
                    {3, DataRate.DR9}}},
                {DataRate.DR3, new Dictionary<int, DataRate> {
                    {0, DataRate.DR13},
                    {1, DataRate.DR12},
                    {2, DataRate.DR11},
                    {3, DataRate.DR10}}},
                {DataRate.DR4, new Dictionary<int, DataRate> {
                    {0, DataRate.DR13},
                    {1, DataRate.DR13},
                    {2, DataRate.DR12},
                    {3, DataRate.DR11}}},
                {DataRate.DR5, new Dictionary<int, DataRate> {
                    {0, DataRate.DR10},
                    {1, DataRate.DR9},
                    {2, DataRate.DR8},
                    {3, DataRate.DR8}}},
                {DataRate.DR6, new Dictionary<int, DataRate> {
                    {0, DataRate.DR11},
                    {1, DataRate.DR10},
                    {2, DataRate.DR9},
                    {3, DataRate.DR8}}},
            };
            SpreadingFactorByDataRate = new Dictionary<DataRate, SpreadingFactor>
            {
                {DataRate.DR0, SpreadingFactor.Sf10},
                {DataRate.DR1, SpreadingFactor.Sf9},
                {DataRate.DR2, SpreadingFactor.Sf8},
                {DataRate.DR3, SpreadingFactor.Sf7},
                {DataRate.DR4, SpreadingFactor.Sf8},
                {DataRate.DR8, SpreadingFactor.Sf12},
                {DataRate.DR9, SpreadingFactor.Sf11},
                {DataRate.DR10, SpreadingFactor.Sf10},
                {DataRate.DR11, SpreadingFactor.Sf9},
                {DataRate.DR12, SpreadingFactor.Sf8},
                {DataRate.DR13, SpreadingFactor.Sf7},
            };
            BandwidthByDataRate = new Dictionary<DataRate, Bandwidth>
            {
                { DataRate.DR0, new Bandwidth(125, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR1, new Bandwidth(125, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR2, new Bandwidth(125, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR3, new Bandwidth(125, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR4, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR5, new Bandwidth(1523, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR6, new Bandwidth(1523, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR8, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR9, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR10, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR11, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR12, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) },
                { DataRate.DR13, new Bandwidth(500, Bandwidth.UnitType.Kilohertz) }
            };

            var joinChannels = new List<LoRaWanChannelSettings>(72);
            foreach (var channel in Enumerable.Range(0, 64))
            {
                joinChannels.Add(new LoRaWanChannelSettings(new((double)(902.3M + (0.2M * channel)), Megahertz), new(125, Kilohertz), SpreadingFactor.Sf10, DataRate.DR0));
            }
            foreach (var channel in Enumerable.Range(0, 8))
            {
                joinChannels.Add(new LoRaWanChannelSettings(new((double)(903M + (1.6M * channel)), Megahertz), new(500, Kilohertz), SpreadingFactor.Sf8, DataRate.DR4));
            }
            JoinRequestChannels = joinChannels.ToArray();

            var upstreamChannels = new Dictionary<int, LoRaWanChannel>();
            foreach (var channel in Enumerable.Range(0, 64))
            {
                upstreamChannels.Add(channel, new LoRaWanChannel(channel, new((double)(902.3M + (0.2M * channel)), Megahertz), new(125, Kilohertz), true));
            }
            foreach (var channel in Enumerable.Range(0, 8))
            {
                upstreamChannels.Add(channel + 64, new LoRaWanChannel(channel + 64, new((double)(903M + (1.6M * channel)), Megahertz), new(500, Kilohertz), true));
            }
            AvailableUpstreamChannels = upstreamChannels;

            var downstreamChannels = new Dictionary<int, LoRaWanChannel>();
            foreach (var channel in Enumerable.Range(0, 8))
            {
                downstreamChannels.Add(channel, new LoRaWanChannel(channel, new((double)(923.3M + (0.6M * channel)), Megahertz), new(500, Kilohertz), true));
            }
            AvailableDownstreamChannels = downstreamChannels;

            _joinChannelTracker = new JoinChannelTracker(72);
        }

        public override void SetUpstreamDataRate(DataRate dataRate, bool supportRepeater)
        {
            UpstreamSpreadingFactor = dataRate switch
            {
                DataRate.DR0 => SpreadingFactor.Sf10,
                DataRate.DR1 => SpreadingFactor.Sf9,
                DataRate.DR2 => SpreadingFactor.Sf8,
                DataRate.DR3 => SpreadingFactor.Sf7,
                DataRate.DR4 => SpreadingFactor.Sf8,
                DataRate.DR5 => throw new NotSupportedException("This data rate is not supported"),
                DataRate.DR6 => throw new NotSupportedException("This data rate is not supported"),
                DataRate.DR7 => throw new NotSupportedException("This data rate is reserved for future use"),
                _ => throw new NotSupportedException("Cannot specify downlink data rate"),
            };
            MaxMacPayloadSize = dataRate switch
            {
                DataRate.DR0 => 19,
                DataRate.DR1 => 61,
                DataRate.DR2 => 133,
                DataRate.DR3 => 230,
                DataRate.DR4 => 230,
                DataRate.DR5 => 58,
                DataRate.DR6 => 133,
                DataRate.DR7 => throw new NotSupportedException("This data rate is reserved for future use"),
                _ => throw new NotSupportedException("Cannot specify downlink data rate"),
            };
            UpstreamDataRate = dataRate;
        }
        public override LoRaWanChannelSettings[] JoinRequestChannels { get; }
        protected override IReadOnlyDictionary<DataRate, IReadOnlyDictionary<int, DataRate>> DownstreamDataRateByRx1OffsetAndUpstreamDataRate { get; }
        protected override IReadOnlyDictionary<DataRate, SpreadingFactor> SpreadingFactorByDataRate { get; }
        protected override IReadOnlyDictionary<DataRate, Bandwidth> BandwidthByDataRate { get; }
        internal override IReadOnlyDictionary<int, LoRaWanChannel> AvailableUpstreamChannels { get; }
        internal override IReadOnlyDictionary<int, LoRaWanChannel> AvailableDownstreamChannels { get; }

        private JoinChannelTracker _joinChannelTracker;

        internal override LoRaWanChannel GetJoinChannel()
        {
            return AvailableUpstreamChannels[_joinChannelTracker.GetNextChannel()];
        }

        internal override LoRaWanChannel GetDownstreamChannel()
        {
            return AvailableDownstreamChannels[_joinChannelTracker.CurrentChannel % 8];
        }

        internal override SpreadingFactor GetJoinSpreadingFactor(DataRate dataRate)
        {
            return dataRate switch
            {
                DataRate.DR0 => SpreadingFactor.Sf10,
                DataRate.DR1 => SpreadingFactor.Sf9,
                DataRate.DR2 => SpreadingFactor.Sf8,
                DataRate.DR3 => SpreadingFactor.Sf7,
                DataRate.DR4 => SpreadingFactor.Sf8,
                _ => throw new NotSupportedException("This data rate is not supported"),
            };
        }

        private class JoinChannelTracker(int channelCount)
        {
            public int CurrentChannel = 0;
            private Random _random = new();
            private int _lastOctet = 0;
            private IDictionary<int, bool[]> _channelsInEachOctet = Enumerable.Range(0, channelCount / 8).ToDictionary(x => x, x => new bool[8]);
            public int GetNextChannel()
            {
                try
                {
                    if (_lastOctet == channelCount / 8)
                        _lastOctet = 0;
                    int selectedChannel;
                    do
                    {
                        // If all the channels in this octet have been used, reset it.
                        if (_channelsInEachOctet[_lastOctet].All(x => x))
                        {
                            _channelsInEachOctet[_lastOctet] = new bool[8];
                        }
                        selectedChannel = _random.Next(0, 8);
                    } while (_channelsInEachOctet[_lastOctet][selectedChannel]);
                    _channelsInEachOctet[_lastOctet][selectedChannel] = true;
                    CurrentChannel = selectedChannel + (_lastOctet * 8);
                    return CurrentChannel;
                }
                finally
                {
                    _lastOctet++;
                }
            }
        }
    }

    public abstract class LoRaWanChannelPlan
    {
        protected int MaxMacPayloadSize { get; set; }
        public int MaxPayloadSize => MaxMacPayloadSize - 8;
        public int Rx1DROffset { get; protected set; }
        public CodingRate CodingRate { get; protected set; }
        public TimeSpan ReceiveDelay1 { get; protected set; } = TimeSpan.FromSeconds(1);
        public TimeSpan ReceiveDelay2 { get; protected set; } = TimeSpan.FromSeconds(2);
        public TimeSpan JoinAcceptDelay1 { get; } = TimeSpan.FromSeconds(5);
        public TimeSpan JoinAcceptDelay2 { get; } = TimeSpan.FromSeconds(6);

        public abstract void SetUpstreamDataRate(DataRate dataRate, bool supportRepeater);
        public abstract LoRaWanChannelSettings[] JoinRequestChannels { get; }
        protected abstract IReadOnlyDictionary<DataRate, IReadOnlyDictionary<int, DataRate>> DownstreamDataRateByRx1OffsetAndUpstreamDataRate { get; }
        protected abstract IReadOnlyDictionary<DataRate, SpreadingFactor> SpreadingFactorByDataRate { get; }
        protected abstract IReadOnlyDictionary<DataRate, Bandwidth> BandwidthByDataRate { get; }
        internal abstract IReadOnlyDictionary<int, LoRaWanChannel> AvailableUpstreamChannels { get; }
        internal abstract IReadOnlyDictionary<int, LoRaWanChannel> AvailableDownstreamChannels { get; }
        internal DataRate UpstreamDataRate { get; set; }
        internal SpreadingFactor UpstreamSpreadingFactor { get; set; }
        internal abstract LoRaWanChannel GetJoinChannel();
        internal abstract SpreadingFactor GetJoinSpreadingFactor(DataRate dataRate);
        internal abstract LoRaWanChannel GetDownstreamChannel();
        internal int GetChannelNumber(LoRaWanChannel channel) => AvailableUpstreamChannels.Single(x => x.Value == channel).Key;
    }
}
