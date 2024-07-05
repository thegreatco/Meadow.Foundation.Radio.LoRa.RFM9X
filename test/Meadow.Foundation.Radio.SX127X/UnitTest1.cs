using Meadow.Foundation.Radio.LoRa.SX127X;
using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Hardware;
using Meadow.Logging;
using Meadow.Units;

using Moq;

namespace Meadow.Foundation.Radio.Sx127x.Test
{
    public class Tests
    {
        private record SpiOperation(byte[] WriteBuffer, byte[] ReadBuffer);
        private record TestSpiBus(SpiOperation[] Operations) : ISpiBus
        {
            private int _operationIndex = 0;
            public void Read(IDigitalOutputPort? chipSelect, Span<byte> readBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
            {
                TestContext.WriteLine($"Reading {readBuffer.ToHexString()}");
                readBuffer.CopyTo(Operations[_operationIndex].ReadBuffer);
                _operationIndex++;
            }

            public void Write(IDigitalOutputPort? chipSelect, Span<byte> writeBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
            {
                TestContext.WriteLine($"Writing {writeBuffer.ToHexString()}");
                writeBuffer.CopyTo(Operations[_operationIndex].WriteBuffer);
                _operationIndex++;
            }

            public void Exchange(IDigitalOutputPort? chipSelect, Span<byte> writeBuffer, Span<byte> readBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
            {
                TestContext.WriteLine($"Exchanging {writeBuffer.ToHexString()}");
                writeBuffer.CopyTo(Operations[_operationIndex].WriteBuffer);
                Operations[_operationIndex].ReadBuffer.CopyTo(readBuffer);
                _operationIndex++;
            }

            public Frequency[] SupportedSpeeds { get; } = [];

            public SpiClockConfiguration Configuration { get; } =
                new(new Frequency(1, Frequency.UnitType.Megahertz),
                    SpiClockConfiguration.Mode.Mode0);
        }
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestOutputPower()
        {
            var mockPin = new Mock<IPin>();
            var mockChipSelectPin = new Mock<IPin>();
            var mockChipSelectDigitalOutputPort = new Mock<IDigitalOutputPort>();

            var mockSpi = new TestSpiBus(new []
                                         {
                                             new SpiOperation([0x00, 0x00], [0x00, 0x00]),
                                             new SpiOperation([0x00, 0x00], [0x00, 0x80]),
                                             new SpiOperation([0x00, 0x00], [0x00, 0x00]),
                                         });
            var mockInterrupt = new Mock<IDigitalInterruptPort>();

            var mockDevice = new Mock<IMeadowDevice>();
            mockDevice.Setup(x => x.CreateSpiBus(mockPin.Object, mockPin.Object, mockPin.Object, new Frequency(10, Frequency.UnitType.Megahertz))).Returns(mockSpi);
            mockDevice.Setup(x => x.CreateDigitalOutputPort(mockPin.Object, false, OutputType.PushPull)).Returns(mockChipSelectDigitalOutputPort.Object);
            mockDevice.Setup(x => x.CreateDigitalInterruptPort(mockPin.Object, InterruptMode.EdgeRising)).Returns(mockInterrupt.Object);
            var logger = new Logger();
            var config = new Sx127XLoRaConfiguration([0x00, 0x00, 0x00, 0x00],
                                                mockDevice.Object,
                                                mockSpi,
                                                mockChipSelectPin.Object,
                                                mockPin.Object,
                                                mockPin.Object);

            var radio = new Sx127XLoRaRadio(logger, config);
            radio.OutputPower = 10;
            Assert.That(mockSpi.Operations[0].WriteBuffer, Is.EqualTo(new byte[] { 0x09, 0x00 }));
        }

        [Test]
        public void TestBandwidth()
        {
            var mockPin = new Mock<IPin>();
            var mockChipSelectPin = new Mock<IPin>();
            var mockChipSelectDigitalOutputPort = new Mock<IDigitalOutputPort>();

            var mockSpi = new TestSpiBus(new []
                                         {
                                             new SpiOperation([0x00, 0x00], [0x00, 0b10010010]),
                                         });
            var mockInterrupt = new Mock<IDigitalInterruptPort>();

            var mockDevice = new Mock<IMeadowDevice>();
            mockDevice.Setup(x => x.CreateSpiBus(mockPin.Object, mockPin.Object, mockPin.Object, new Frequency(10, Frequency.UnitType.Megahertz))).Returns(mockSpi);
            mockDevice.Setup(x => x.CreateDigitalOutputPort(mockPin.Object, false, OutputType.PushPull)).Returns(mockChipSelectDigitalOutputPort.Object);
            mockDevice.Setup(x => x.CreateDigitalInterruptPort(mockPin.Object, InterruptMode.EdgeRising)).Returns(mockInterrupt.Object);
            var logger = new Logger();
            var config = new Sx127XLoRaConfiguration([0x00, 0x00, 0x00, 0x00],
                                                     mockDevice.Object,
                                                     mockSpi,
                                                     mockChipSelectPin.Object,
                                                     mockPin.Object,
                                                     mockPin.Object);

            var radio = new Sx127XLoRaRadio(logger, config);
            var bandwidth = radio.Bandwidth;
            Assert.That(bandwidth, Is.EqualTo(Meadow.Foundation.Radio.LoRa.SX127X.Bandwidth.Bw500kHz));
        }

        [Test]
        public void TestOpMode()
        {
            var mockPin = new Mock<IPin>();
            var mockChipSelectPin = new Mock<IPin>();
            var mockChipSelectDigitalOutputPort = new Mock<IDigitalOutputPort>();

            var mockSpi = new TestSpiBus(new []
                                         {
                                             new SpiOperation([0x00, 0x00], [0x00, 0b10000101]),
                                             new SpiOperation([0x00, 0x00], [0x00, 0b10000101]),
                                             new SpiOperation([0x00, 0x00], [0x00, 0x00]),
                                         });
            var mockInterrupt = new Mock<IDigitalInterruptPort>();

            var mockDevice = new Mock<IMeadowDevice>();
            mockDevice.Setup(x => x.CreateSpiBus(mockPin.Object, mockPin.Object, mockPin.Object, new Frequency(10, Frequency.UnitType.Megahertz))).Returns(mockSpi);
            mockDevice.Setup(x => x.CreateDigitalOutputPort(mockPin.Object, false, OutputType.PushPull)).Returns(mockChipSelectDigitalOutputPort.Object);
            mockDevice.Setup(x => x.CreateDigitalInterruptPort(mockPin.Object, InterruptMode.EdgeRising)).Returns(mockInterrupt.Object);
            var logger = new Logger();
            var config = new Sx127XLoRaConfiguration([0x00, 0x00, 0x00, 0x00],
                                                     mockDevice.Object,
                                                     mockSpi,
                                                     mockChipSelectPin.Object,
                                                     mockPin.Object,
                                                     mockPin.Object);

            var radio = new Sx127XLoRaRadio(logger, config);
            var mode = radio.Mode;
            Assert.That(mode, Is.EqualTo(OpMode.ReceiveContinuous));
            radio.Mode = OpMode.Sleep;
            TestContext.WriteLine(mockSpi.Operations[2].WriteBuffer.ToHexString());
            Assert.That(mockSpi.Operations[2].WriteBuffer, Is.EqualTo(new byte[] { 0x80|0x01, 0b10000000 }));
        }
    }
}