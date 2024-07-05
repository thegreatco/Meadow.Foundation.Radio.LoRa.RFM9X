using Meadow.Units;

using static Meadow.Units.Frequency.UnitType;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public class LoRaWanChannel
    {
        public readonly Frequency UplinkBaseFrequency;
        public readonly Frequency UplinkChannelWidth;
        public readonly Frequency UplinkBandwidth;
        public readonly int UplinkChannelCount;
        public readonly Frequency DownlinkBaseFrequency;
        public readonly Frequency DownlinkChannelWidth;
        public readonly Frequency DownlinkBandwidth;
        public readonly int DownlinkChannelCount;

        private LoRaWanChannel(Frequency uplinkBaseFrequency,
                             Frequency uplinkChannelWidth,
                             Frequency uplinkSignalBandwidth,
                             int uplinkChannelCount,
                             Frequency downlinkBaseFrequency,
                             Frequency downlinkChannelWidth,
                             Frequency downlinkSignalBandwidth,
                             int downlinkChannelCount)
        {
            UplinkBaseFrequency = uplinkBaseFrequency;
            UplinkChannelWidth = uplinkChannelWidth;
            UplinkChannelCount = uplinkChannelCount;
            UplinkBandwidth = uplinkSignalBandwidth;

            DownlinkBaseFrequency = downlinkBaseFrequency;
            DownlinkChannelWidth = downlinkChannelWidth;
            DownlinkChannelCount = downlinkChannelCount;
            DownlinkBandwidth = downlinkSignalBandwidth;
        }

        public static Frequency Bandwidth7_8kHz = new(7.8, Kilohertz);
        public static Frequency Bandwidth10_4kHz = new(10.4, Kilohertz);
        public static Frequency Bandwidth15_6kHz = new(15.6, Kilohertz);
        public static Frequency Bandwidth20_8kHz = new(20.8, Kilohertz);
        public static Frequency Bandwidth31_25kHz = new(31.25, Kilohertz);
        public static Frequency Bandwidth41_7kHz = new(41.7, Kilohertz);
        public static Frequency Bandwidth62_5kHz = new(62.5, Kilohertz);
        public static Frequency Bandwidth125kHz = new(125, Kilohertz);
        public static Frequency Bandwidth250kHz = new(250, Kilohertz);
        public static Frequency Bandwidth500kHz = new(500, Kilohertz);

        public static Frequency ChannelWidth200kHz = new(200, Kilohertz);
        public static Frequency ChannelWidth600kHz = new(600, Kilohertz);


        public static LoRaWanChannel Us915Fsb1 = new(new Frequency(902.3, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb2 = new(new Frequency(903.9, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb3 = new(new Frequency(905.5, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb4 = new(new Frequency(907.1, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb5 = new(new Frequency(908.7, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb6 = new(new Frequency(910.3, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb7 = new(new Frequency(911.9, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaWanChannel Us915Fsb8 = new(new Frequency(913.5, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);
    }
}
