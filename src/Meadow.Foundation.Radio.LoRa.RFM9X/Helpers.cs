using System;

using Meadow.Hardware;
using Meadow.Units;

namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public record Rfm9XConfiguration(
        byte[] DeviceAddress,
        LoRaChannels Channels,
        IMeadowDevice Device,
        ISpiBus SpiBus,
        IPin ChipSelectPin,
        IPin ResetPin,
        IPin Dio0,
        IPin? Dio1 = null,
        IPin? Dio2 = null,
        IPin? Dio3 = null,
        IPin? Dio4 = null,
        IPin? Dio5 = null)
    {
        // TODO: Add all the other possible settings
        public byte[] DeviceAddress { get; } = DeviceAddress ?? throw new ArgumentNullException(nameof(DeviceAddress));
        public Frequency SpiFrequency { get; } = new(1, Units.Frequency.UnitType.Kilohertz);
        public LoRaChannels Channels { get; } = Channels ?? throw new ArgumentNullException(nameof(Channels));
        public IMeadowDevice Device { get; } = Device ?? throw new ArgumentNullException(nameof(Device));
        public ISpiBus SpiBus { get; } = SpiBus ?? throw new ArgumentNullException(nameof(SpiBus));
        public IPin ChipSelectPin { get; } = ChipSelectPin ?? throw new ArgumentNullException(nameof(ChipSelectPin));
        public IPin ResetPin { get; } = ResetPin ?? throw new ArgumentNullException(nameof(ResetPin));
        public IPin Dio0 { get; } = Dio0 ?? throw new ArgumentNullException(nameof(Dio0));
        public IPin? Dio1 { get; } = Dio1;
        public IPin? Dio2 { get; } = Dio2;
        public IPin? Dio3 { get; } = Dio3;
        public IPin? Dio4 { get; } = Dio4;
        public IPin? Dio5 { get; } = Dio5;
    }

    public static class Helpers
    {
        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        public static string ToHexString(this byte @byte)
        {
            return ToHexString([@byte]);
        }
    }
}
