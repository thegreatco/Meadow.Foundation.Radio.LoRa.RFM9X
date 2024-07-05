using System;
using Meadow.Hardware;
using Meadow.Units;

namespace Meadow.Foundation.Radio.Sx127X
{
    public record Sx172XConfiguration(
        byte[] DeviceAddress,
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
}
