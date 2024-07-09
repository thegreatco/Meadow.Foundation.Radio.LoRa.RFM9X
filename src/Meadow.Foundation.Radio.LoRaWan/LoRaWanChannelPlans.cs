using Meadow.Units;

using System;
using System.Collections.Generic;
using System.Text;

using static Meadow.Units.Frequency.UnitType;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal enum DataRate
    {
        DR0 = 0,
        DR1 = 1,
        DR2 = 2,
        DR3 = 3,
        DR4 = 4,
        DR5 = 5,
        DR6 = 6,
        DR7 = 7,
        DR8 = 8,
        DR9 = 9,
        DR10 = 10,
        DR11 = 11,
        DR12 = 12,
        DR13 = 13,
        DR14 = 14,
        DR15 = 15,
    };

    internal enum DutyCycle
    {
        None,
        ListenBeforeTalk,
        LessThanOnePercent,
        LessThanTenPercent,
    }

    internal record class JoinRequestDataRateRange(DataRate Min, DataRate Max);
    internal record class DataRateRange(DataRate Min, DataRate Max);
    internal record class ChannelRange(int Min, int Max);
    internal record class ChannelMask(int Index, ChannelRange ChannelRange);
    internal record class DwellTimeLimitation(TimeSpan MaxDwellTime, ChannelRange ChannelRange);

    internal abstract class LoRaWanChannelPlan(Frequency MinimumFrequency,
                                              Frequency MaximumFrequency,
                                              Frequency[] JoinFrequencies,
                                              JoinRequestDataRateRange JoinRequestDataRate,
                                              CFListType SupportedCFListType,
                                              DataRateRange MandatoryDataRate,
                                              DataRateRange? OptionalDataRate,
                                              ChannelRange NumberOfChannels,
                                              object ChannelMaskControl,
                                              ChannelRange DefaultChannels,
                                              TimeSpan DefaultRx1DrOffset,
                                              TimeSpan[] AllowedRx1DrOffsets,
                                              DutyCycle DutyCycle,
                                              DwellTimeLimitation[]? DwellTimeLimitations,
                                              bool TxParamSetupReqSupport,
                                              int MaxEIRPTxPower0,
                                              DataRate DefaultRx2DataRate,
                                              Frequency DefaultRx2Frequency,
                                              Frequency ClassBDefaultBeaconFrequency,
                                              Frequency ClassBDefaultDownlinkPingSlotFrequency)
    {
        public readonly Frequency MinimumFrequency = MinimumFrequency;
        public readonly Frequency MaximumFrequency = MaximumFrequency;
        public readonly Frequency[] JoinFrequencies = JoinFrequencies;
        public readonly JoinRequestDataRateRange JoinRequestDataRate = JoinRequestDataRate;
        public readonly CFListType SupportedCFListType = SupportedCFListType;
        public readonly DataRateRange MandatoryDataRate = MandatoryDataRate;
        public readonly DataRateRange? OptionalDataRate = OptionalDataRate;
        public readonly ChannelRange NumberOfChannels = NumberOfChannels;
        // TODO: Figure out how to model this
        public readonly object ChannelMaskControl = ChannelMaskControl;

        public readonly ChannelRange DefaultChannels = DefaultChannels;

        // 0 seems weird, am I doing this right?
        public readonly TimeSpan DefaultRx1DrOffset = DefaultRx1DrOffset;
        public readonly TimeSpan[] AllowedRx1DrOffsets = AllowedRx1DrOffsets;
        public readonly DutyCycle DutyCycle = DutyCycle;
        public readonly DwellTimeLimitation[]? DwellTimeLimitations = DwellTimeLimitations;
        public readonly bool TxParamSetupReqSupport = TxParamSetupReqSupport;

        /// <summary>
        /// The Max EIRP TX Power in dBm
        /// </summary>
        public readonly int MaxEIRPTxPower0 = MaxEIRPTxPower0;

        public readonly DataRate DefaultRx2DataRate = DefaultRx2DataRate;
        public readonly Frequency DefaultRx2Frequency = DefaultRx2Frequency;
        public readonly Frequency ClassBDefaultBeaconFrequency = ClassBDefaultBeaconFrequency;
        public readonly Frequency ClassBDefaultDownlinkPingSlotFrequency = ClassBDefaultDownlinkPingSlotFrequency;
    }

    internal class KR920() : LoRaWanChannelPlan(new Frequency(920.9, Megahertz),
                                              new Frequency(923.3, Megahertz),
                                              [new Frequency(922.1, Megahertz), new Frequency(922.3, Megahertz), new Frequency(922.5, Megahertz)],
                                              new(DataRate.DR0, DataRate.DR5),
                                              CFListType.Type0,
                                              new(DataRate.DR0, DataRate.DR5),
                                              null,
                                              new(24, 80),
                                              new object(),
                                              new(0, 2),
                                              TimeSpan.FromSeconds(0),
                                              [TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5)],
                                              DutyCycle.ListenBeforeTalk,
                                              null,
                                              false,
                                              14,
                                              DataRate.DR0,
                                              new Frequency(921.9, Megahertz),
                                              new Frequency(923.1, Megahertz),
                                              new Frequency(923.1, Megahertz));
}
