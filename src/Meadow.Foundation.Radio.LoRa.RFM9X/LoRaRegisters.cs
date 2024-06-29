using System;
using System.Collections.Generic;
using System.Linq;

using Meadow.Units;
using Meadow.Utilities;

using static Meadow.Units.Frequency.UnitType;

/*
 * The register maps and definitions can be found in the datasheet https://www.hoperf.com/uploads/RFM96W-V2.0_1695351477.pdf
 */
namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public class LoRaRegisters
    {
        internal enum Register : byte
        {
            /// <summary>
            /// FIFO read/write access
            /// </summary>
            Fifo = 0x00,

            /// <summary>
            /// Operating mode & LoRa/FSK selection
            /// </summary>
            OpMode = 0x01,

            /// <summary>
            /// RF Carrier Frequency, most significant bits
            /// </summary>
            FrfMsb = 0x06,

            /// <summary>
            /// RF Carrier Frequency, intermediate bits
            /// </summary>
            FrfMid = 0x07,

            /// <summary>
            /// RF Carrier Frequency, least significant bits
            /// </summary>
            FrfLsb = 0x08,

            /// <summary>
            /// PA selection and output power control
            /// </summary>
            PaConfig = 0x09,

            /// <summary>
            /// Control of PA ramp time, low phase noise PLL
            /// </summary>
            PaRamp = 0x0A,

            /// <summary>
            /// Over current protection control
            /// </summary>
            Ocp = 0x0B,

            /// <summary>
            /// LNA settings
            /// </summary>
            Lna = 0x0C,

            /// <summary>
            /// FIFO SPI pointer
            /// </summary>
            FifoAddressPointer = 0x0D,

            /// <summary>
            /// Start Tx data
            /// </summary>
            FifoTransmitBaseAddress = 0x0E,

            /// <summary>
            /// Start Rx data
            /// </summary>
            FifoReceiveBaseAddress = 0x0F,

            /// <summary>
            /// Start address of last packet received
            /// </summary>
            FifoReceiveCurrentAddress = 0x10,

            /// <summary>
            /// Option IRQ flag mask
            /// </summary>
            InterruptFlagsMask = 0x11,

            /// <summary>
            /// IRQ flags
            /// </summary>
            InterruptFlags = 0x12,

            /// <summary>
            /// Number of received bytes
            /// </summary>
            NumberOfReceivedBytes = 0x13,

            /// <summary>
            /// Number of valid headers received, most significant bits
            /// </summary>
            ReceiveHeaderCountMsb = 0x14,

            /// <summary>
            /// Number of valid headers received, least significant bits
            /// </summary>
            ReceiveHeaderCountLsb = 0x15,

            /// <summary>
            /// Number of valid packets received, most significant bits
            /// </summary>
            ReceivePacketCountMsb = 0x16,

            /// <summary>
            /// Number of valid packets received, least significant bits
            /// </summary>
            ReceivePacketCountLsb = 0x17,

            /// <summary>
            /// Live LoRa modem status
            /// </summary>
            ModemStatus = 0x18,

            /// <summary>
            /// Estimation of last SNR packet
            /// </summary>
            LastPacketSnr = 0x19,

            /// <summary>
            /// RSSI of last packet
            /// </summary>
            LastPacketRssi = 0x1A,

            /// <summary>
            /// Current RSSI
            /// </summary>
            Rssi = 0x1B,

            /// <summary>
            /// FHSS start channel
            /// </summary>
            HopChannel = 0x1C,

            /// <summary>
            /// Modem PHY config 1
            /// </summary>
            ModemConfig1 = 0x1D,

            /// <summary>
            /// Modem PHY config 2
            /// </summary>
            ModemConfig2 = 0x1E,

            /// <summary>
            /// Receive timeout
            /// </summary>
            ReceiveTimeoutLsb = 0x1F,

            /// <summary>
            /// Preamble size, most significant bits
            /// </summary>
            PreambleMsb = 0x20,

            /// <summary>
            /// Preamble size, least significant bits
            /// </summary>
            PreambleLsb = 0x21,

            /// <summary>
            /// LoRa payload length
            /// </summary>
            PayloadLength = 0x22,

            /// <summary>
            /// LoRa max payload length
            /// </summary>
            MaxPayloadLength = 0x23,

            /// <summary>
            /// FHSS hop period
            /// </summary>
            HopPeriod = 0x24,

            /// <summary>
            /// Address of last byte written in FIFO
            /// </summary>
            FifoRxByteAddress = 0x25,

            /// <summary>
            /// Modem PHY config 3
            /// </summary>
            ModemConfig3 = 0x26,

            /// <summary>
            /// Estimated frequency error, most significant bits
            /// </summary>
            FeiMsb = 0x28,

            /// <summary>
            /// Estimated frequency error, middle bits
            /// </summary>
            FeiMid = 0x29,

            /// <summary>
            /// Estimated frequency error, least significant bits
            /// </summary>
            FeiLsb = 0x2A,

            /// <summary>
            /// Wideband RSSI measurement
            /// </summary>
            RssiWideband = 0x2C,

            /// <summary>
            /// Optimize receiver
            /// </summary>
            IfFreq1 = 0x2F,

            /// <summary>
            /// Optimize receiver
            /// </summary>
            IfFreq2 = 0x30,

            /// <summary>
            /// LoRa detection optimize for SF6
            /// </summary>
            DetectOptimize = 0x31,

            /// <summary>
            /// Invert LoRa I and Q signals
            /// </summary>
            InvertIq = 0x33,

            /// <summary>
            /// Sensitivity optimization for 500kHz bandwidth
            /// </summary>
            HighBwOptimize1 = 0x36,

            /// <summary>
            /// LoRa detection threshold for SF6
            /// </summary>
            DetectionThreshold = 0x37,

            /// <summary>
            /// LoRa sync word
            /// </summary>
            SyncWord = 0x39,

            /// <summary>
            /// Sensitivity optimization for 500kHz bandwidth
            /// </summary>
            HighBwOptimize2 = 0x3A,

            /// <summary>
            /// Optimize for inverted IQ
            /// </summary>
            InvertIq2 = 0x3B,

            /// <summary>
            /// Mapping of pins DIO0 to DIO3
            /// </summary>
            DioMapping1 = 0x40,

            /// <summary>
            /// Mapping of pins for DIO4 and DIO5, ClkOutFrequency
            /// </summary>
            DioMapping2 = 0x41,

            /// <summary>
            /// Semtech ID relating the silicon revision
            /// </summary>
            Version = 0x42,

            /// <summary>
            /// TCXO or XTAL input settings
            /// </summary>
            Tcxo = 0x4B,

            /// <summary>
            /// Higher power setting sof the PA
            /// </summary>
            PaDac = 0x4D,

            /// <summary>
            /// Stored temperature during the former IQ calibration
            /// </summary>
            FormerTemp = 0x5B,

            /// <summary>
            /// Adjustment of the AGC thresholds
            /// </summary>
            AgcRef = 0x61,

            /// <summary>
            /// Adjustment of the AGC thresholds
            /// </summary>
            AgcThresh1 = 0x62,

            /// <summary>
            /// Adjustment of the AGC thresholds
            /// </summary>
            AgcThresh2 = 0x63,

            /// <summary>
            /// Adjustment of the AGC thresholds
            /// </summary>
            AgcThresh3 = 0x64,

            /// <summary>
            /// Control of the PLL bandwidth
            /// </summary>
            Pll = 0x70,
        }

        public struct RegOpMode(byte b)
        {
            public bool LongRangeMode { get; set; } = (b & 0b10000000) == 128;
            public ModulationType Modulation { get; set; } = (ModulationType)((b & 0b01100000) >> 5);
            public bool LowFrequencyModeOn { get; set; } = (b & 0b00001000) == 8;
            public OpMode Mode { get; set; } = (OpMode)((b & 0b00000111));

            public enum OpMode : byte
            {
                Sleep = 0b000,
                StandBy = 0b001,
                FrequencySynthesisTx = 0b010,
                Transmit = 0b011,
                FrequencySynthesisRx = 0b100,
                ReceiveContinuous = 0b101,
                ReceiveSingle = 0b110,
                ChannelActivityDetection = 0b111,
            }

            public enum ModulationType : byte
            {
                FSK = 0b00,
                OOK = 0b01,
            }

            public static implicit operator RegOpMode(byte b)
            {
                return new RegOpMode(b);
            }

            public static implicit operator byte(RegOpMode opModeRegister)
            {
                byte b = 0;
                if (opModeRegister.LongRangeMode)
                    b |= 0b10000000;

                b |= (byte)opModeRegister.Modulation;
                if (opModeRegister.LowFrequencyModeOn)
                    b |= 0b00001000;

                b |= (byte)opModeRegister.Mode;
                return b;
            }
        }

        public struct RegPaConfig
        {
            public RegPaConfig(byte b)
            {
                PaSelect = BitHelpers.GetBitValue(b, 7);
                var maxPower = Convert.ToInt32((b & 0b01110000) >> 4);
                MaxPower = 10.8 + (0.6 * maxPower);
                var outputPower = Convert.ToInt32((b & 0b00001111));
                OutputPower = PaSelect
                                  ? OutputPower = 17 - (15 - outputPower)
                                  : OutputPower = MaxPower - (15 - outputPower);
            }

            public bool PaSelect { get; set; }
            public double MaxPower { get; set; }
            public double OutputPower { get; set; }

            // TODO: Add implicit conversion operators?
        }

        public struct RegPaRamp(byte b)
        {
            public ModulationShaping Modulation { get; set; } = (ModulationShaping)((b & 0b01100000) >> 5);
            public PaRamp Ramp { get; set; } = (PaRamp)(b & 0b00001111);

            public enum ModulationShaping : byte
            {
                NoShaping = 0b00,
                GaussianFilterBT1 = 0b01,
                GaussianFilterBT0_5 = 0b10,
                GaussianFilterBT0_3 = 0b11
            }

            public enum PaRamp : byte
            {
                Ms3_4 = 0b0000,
                Ms2 = 0b0001,
                Ms1 = 0b0010,
                Us500 = 0b0011,
                Us250 = 0b0100,
                Us125 = 0b0101,
                Us100 = 0b0110,
                Us62 = 0b0111,
                Us50 = 0b1000,
                Us40 = 0b1001,
                Us31 = 0b1010,
                Us25 = 0b1011,
                Us20 = 0b1100,
                Us15 = 0b1101,
                Us12 = 0b1110,
                Us10 = 0b1111
            }
        }

        public struct RegOcp();

        public struct RegLna();

        public struct RegRxConfig();

        public struct RegRssiConfig();

        public struct RegRxBw();

        public struct RegAfcBw();

        public struct RegOokPeak();

        public struct RegOokAvg();

        public struct RegAfcFei();

        public struct RegPreambleDetect();

        public struct RegOsc();

        public struct RegSyncConfig();

        [Flags]
        internal enum InterruptFlagsMask : byte
        {
            ReceiveTimeout = 0b10000000,
            ReceiveDone = 0b01000000,
            PayLoadCrcError = 0b00100000,
            ValidHeader = 0b00010000,
            TransmitDone = 0b00001000,
            CadDone = 0b00000100,
            FhssChangeChannel = 0b00000010,
            CadDetected = 0b00000001,
        }

        [Flags]
        internal enum InterruptFlags : byte
        {
            ReceiveTimeout = 0b10000000,
            ReceiveDone = 0b01000000,
            PayloadCrcError = 0b00100000,
            ValidHeader = 0b00010000,
            TransmitDone = 0b00001000,
            CadDone = 0b00000100,
            FhssChangeChannel = 0b00000010,
            CadDetected = 0b00000001,
            ClearAll = 0b11111111,
        }

        [Flags]
        public enum DioMapping1
        {
            Dio0RxDone = 0b00000000,
            Dio0TxDone = 0b01000000,
            Dio0CadDone = 0b10000000,
            Dio1RxTimeout = 0b00000000,
            Dio1FhssChangeChannel = 0b00010000,
            Dio1CadDetected = 0b00100000,
        }

        [Flags]
        public enum DioMapping2
        {
            Dio3CadDone = 0b00000000,
            Dio3ValidHeader = 0b01000000,
            Dio3PayloadCrcError = 0b10000000,
            Dio4CadDetected = 0b00000000,
            Dio4PllLock = 0b00010000,
            Dio5ModeReady = 0b00000000,
            Dio5ClkOut = 0b00100000,
        }

        public enum Bandwidth : byte
        {
            Bw7_8kHz = 0b00000000,
            Bw10_4kHz = 0b00010000,
            Bw15_6kHz = 0b00100000,
            Bw20_8kHz = 0b00110000,
            Bw31_25kHz = 0b01000000,
            Bw41_7kHz = 0b01010000,
            Bw62_5kHz = 0b01100000,
            Bw125kHz = 0b01110000,
            Bw250kHz = 0b10000000,
            Bw500kHz = 0b10010000
        }

        public Bandwidth GetBandwidth(Frequency bandwidth)
        {
            return bandwidth.Hertz switch
            {
                7.8 => Bandwidth.Bw7_8kHz,
                10.4 => Bandwidth.Bw10_4kHz,
                15.6 => Bandwidth.Bw15_6kHz,
                20.8 => Bandwidth.Bw20_8kHz,
                31.25 => Bandwidth.Bw31_25kHz,
                41.7 => Bandwidth.Bw41_7kHz,
                62.5 => Bandwidth.Bw62_5kHz,
                125 => Bandwidth.Bw125kHz,
                250 => Bandwidth.Bw250kHz,
                500 => Bandwidth.Bw500kHz,
                _ => throw new ArgumentOutOfRangeException(nameof(bandwidth), "Invalid bandwidth")
            };
        }

        public enum ErrorCodingRate : byte
        {
            ECR4_5 = 0b00000010,
            ECR4_6 = 0b00000100,
            ECR4_7 = 0b00000110,
            ECR4_8 = 0b00001000
        }

        public enum ImplicitHeaderMode : byte
        {
            Off = 0b00000000,
            On = 0b00000001
        }

        public enum SpreadingFactor : byte
        {
            SF6 = 0b01100000,
            SF7 = 0b01110000,
            SF8 = 0b10000000,
            SF9 = 0b10010000,
            SF10 = 0b10100000,
            SF11 = 0b10110000,
            SF12 = 0b11000000
        }

        public enum PayloadCrcMode : byte
        {
            Off = 0b00000000,
            On = 0b00000100
        }
    }

    public class LoRaChannels
    {
        public readonly Frequency UplinkBaseFrequency;
        public readonly Frequency UplinkChannelWidth;
        public readonly Frequency UplinkBandwidth;
        public readonly int UplinkChannelCount;
        public readonly Frequency DownlinkBaseFrequency;
        public readonly Frequency DownlinkChannelWidth;
        public readonly Frequency DownlinkBandwidth;
        public readonly int DownlinkChannelCount;

        private LoRaChannels(Frequency uplinkBaseFrequency,
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

        public static Frequency Bandwidth7_8kHz = new Frequency(7.8, Kilohertz);
        public static Frequency Bandwidth10_4kHz = new Frequency(10.4, Kilohertz);
        public static Frequency Bandwidth15_6kHz = new Frequency(15.6, Kilohertz);
        public static Frequency Bandwidth20_8kHz = new Frequency(20.8, Kilohertz);
        public static Frequency Bandwidth31_25kHz = new Frequency(31.25, Kilohertz);
        public static Frequency Bandwidth41_7kHz = new Frequency(41.7, Kilohertz);
        public static Frequency Bandwidth62_5kHz = new Frequency(62.5, Kilohertz);
        public static Frequency Bandwidth125kHz = new Frequency(125, Kilohertz);
        public static Frequency Bandwidth250kHz = new Frequency(250, Kilohertz);
        public static Frequency Bandwidth500kHz = new Frequency(500, Kilohertz);

        public static Frequency ChannelWidth200kHz = new Frequency(200, Kilohertz);
        public static Frequency ChannelWidth600kHz = new Frequency(600, Kilohertz);


        public static LoRaChannels Us915Fsb1 = new(new Frequency(902.3, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb2 = new(new Frequency(903.9, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb3 = new(new Frequency(905.5, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb4 = new(new Frequency(907.1, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb5 = new(new Frequency(908.7, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb6 = new(new Frequency(910.3, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb7 = new(new Frequency(911.9, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);

        public static LoRaChannels Us915Fsb8 = new(new Frequency(913.5, Megahertz),
                                                   ChannelWidth200kHz,
                                                   Bandwidth125kHz,
                                                   8,
                                                   new Frequency(923.3, Megahertz),
                                                   ChannelWidth600kHz,
                                                   Bandwidth500kHz,
                                                   8);
    }

    internal class LoRaFrequencyManager(LoRaChannels channels)
    {
        public readonly Frequency UplinkBandwidth = channels.UplinkBandwidth;
        public readonly Frequency DownlinkBandwidth = channels.DownlinkBandwidth;
        public readonly Frequency UplinkBaseFrequency = channels.UplinkBaseFrequency;
        public readonly Frequency DownlinkBaseFrequency = channels.DownlinkBaseFrequency;

        private readonly Frequency[] _uplinkChannels = Enumerable.Range(0, channels.UplinkChannelCount)
                                                                 .Select(i => new Frequency(
                                                                             channels.UplinkBaseFrequency.Hertz
                                                                           + i * channels.UplinkChannelWidth.Hertz))
                                                                 .ToArray();

        private readonly Frequency[] _downlinkChannels = Enumerable.Range(0, channels.DownlinkChannelCount)
                                                                   .Select(i => new Frequency(
                                                                               channels.DownlinkBaseFrequency.Hertz
                                                                             + i * channels.DownlinkChannelWidth.Hertz))
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
