using System;
using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Units;
using static Meadow.Foundation.Radio.LoRa.RFM9X.LoRaRegisters;

namespace Meadow.Foundation.Radio.Sx127X
{
    public partial class Sx127X
    {
        private void WriteRegister(Register register, byte value)
        {
            WriteRegister(register, [value]);
        }

        private void WriteRegister(byte register, byte value)
        {
            WriteRegister(register, [value]);
        }

        private void WriteRegister(Register register, byte[] bytes)
        {
            WriteRegister((byte)register, bytes);
        }

        private void WriteRegister(Register register, ReadOnlyMemory<byte> bytes)
        {
            WriteRegister((byte)register, bytes.Span);
        }

        private void WriteRegister(byte register, ReadOnlySpan<byte> bytes)
        {
#if CUSTOM_SPI
            Span<byte> writeBuffer = new byte[bytes.Length + 1];
            writeBuffer[0] = (byte)(0x80 | register);
            bytes.CopyTo(writeBuffer[1..]);
            _config.SpiBus.Write(_chipSelect, writeBuffer);
            _logger.Trace($"Wrote to register {register.ToHexString()} with {writeBuffer.ToHexString()}");
#else
            _comms.WriteRegister((byte)register, bytes);
            _logger.Trace($"Wrote to register {register} with {bytes.ToHexString()}");
#endif
        }

        private void WriteRegister(byte register, byte[] bytes)
        {
#if CUSTOM_SPI
            Span<byte> writeBuffer = new byte[bytes.Length + 1];
            writeBuffer[0] = (byte)(0x80 | register);
            bytes.CopyTo(writeBuffer[1..]);
            _config.SpiBus.Write(_chipSelect, writeBuffer);
            _logger.Trace($"Wrote to register {register.ToHexString()} with {writeBuffer.ToHexString()}");
#else
            _comms.WriteRegister((byte)register, bytes);
            _logger.Trace($"Wrote to register {register} with {bytes.ToHexString()}");
#endif
        }

        private byte ReadRegister(Register register)
        {
#if CUSTOM_SPI
            var command = 0x7F & (byte)register;
            var writeBuffer = new byte[] { (byte)command, 0x00 };
            var readBuffer = new byte[2];
            _config.SpiBus.Exchange(_chipSelect, writeBuffer, readBuffer);
            _logger.Trace($"Read register {((byte)register).ToHexString()} got {readBuffer.ToHexString()}");
            var value = readBuffer[1];
#else
            var value = _comms.ReadRegister((byte)register);
            _logger.Trace($"Read register {((byte)register).ToHexString()} got {value.ToHexString()}");

#endif
            return value;
        }

        private byte[] ReadRegister(Register register, int length)
        {
#if CUSTOM_SPI
            var command = 0x7F & (byte)register;
            var writeBuffer = new byte[length + 1];
            writeBuffer[0] = (byte)command;
            var buffer = new byte[length + 1];
            _config.SpiBus.Exchange(_chipSelect, writeBuffer, buffer);
#else
            _comms.ReadRegister((byte)register, buffer);
#endif
            _logger.Trace($"Read register {((byte)register).ToHexString()} got {buffer.ToHexString()} bytes");
            return buffer[1..];
        }

        private void ReadRegister(Register register, byte[] buffer)
        {
#if CUSTOM_SPI
            var command = 0x7F & (byte)register;
            var writeBuffer = new byte[buffer.Length];
            writeBuffer[0] = (byte)command;
            _config.SpiBus.Exchange(_chipSelect, writeBuffer, buffer);
#else
            _comms.ReadRegister((byte)register, buffer);
#endif
            _logger.Trace($"Read register {((byte)register).ToHexString()} got {buffer.ToHexString()} bytes");
        }

        private void WriteModemConfig1(Frequency bandwidth, ErrorCodingRate codingRate, ImplicitHeaderMode implicitHeaderMode)
        {
            var bw = bandwidth.Kilohertz switch
                            {
                                7.8   => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw7_8kHz,
                                10.4  => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw10_4kHz,
                                15.6  => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw15_6kHz,
                                20.8  => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw20_8kHz,
                                31.25 => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw31_25kHz,
                                41.7  => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw41_7kHz,
                                62.5  => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw62_5kHz,
                                125   => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw125kHz,
                                250   => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw250kHz,
                                500   => LoRa.RFM9X.LoRaRegisters.Bandwidth.Bw500kHz,
                                _     => throw new ArgumentOutOfRangeException(nameof(bandwidth), $"Invalid bandwidth {bandwidth.Kilohertz}")
                            };
            var value = (byte)bw;
            value |= (byte)codingRate;
            value |= (byte)implicitHeaderMode;
            _logger.Trace($"Setting Bandwidth: {bw}, CodingRate: {codingRate}, ImplicitHeaderMode: {implicitHeaderMode}");
            WriteRegister(Register.ModemConfig1, value);
        }

        public void WriteModemConfig2(SpreadingFactor spreadingFactor, PayloadCrcMode payloadCrcMode)
        {
            _logger.Trace($"Setting SpreadingFactor: {spreadingFactor}, PayloadCrcMode: {payloadCrcMode}");
            // We have to get the current value of the register because the last 2 bits are part of the timeout
            var value = ReadRegister(Register.ModemConfig2);
            value &= 0x03; // Clear everything but the last 2 bits

            // Set the spreading factor
            value |= (byte)spreadingFactor;
            // Set the payload crc mode
            value |= (byte)payloadCrcMode;
            WriteRegister(Register.ModemConfig2, value);
        }

        public void WriteModemConfig3()
        {
            // First 4 are unused, LowDataRateOptimize is bit 3, AgcAutoOn is bit 2, bits 1 and 2 are reserved
            var value = ReadRegister(Register.ModemConfig3);
            // turn off LowDataRateOptimize
            value &= 0b11110011;
            value |= 0b00000100;
            WriteRegister(Register.ModemConfig3, value);
        }

        private void SetFrequency(Frequency frequency)
        {
            _logger.Trace($"Setting frequency to {frequency.Hertz}");
            var registerFrequency = Convert.ToInt64(((uint)frequency.Hertz) / (32000000.0 / 524288.0));
            var bytes = BitConverter.GetBytes(registerFrequency);
            WriteRegister(Register.FrfMsb, bytes[2]);
            WriteRegister(Register.FrfMid, bytes[1]);
            WriteRegister(Register.FrfLsb, bytes[0]);
        }

        private Frequency GetFrequency()
        {
            var msb = ReadRegister(Register.FrfMsb);
            var mid = ReadRegister(Register.FrfMid);
            var lsb = ReadRegister(Register.FrfLsb);
            var frequency = ((msb << 16) | (mid << 8) | lsb) * (32000000.0 / 524288.0);
            return new Frequency(frequency);
        }

        public void SetMode(RegOpMode.OpMode opMode)
        {
            var mode = new RegOpMode()
                       {
                           LongRangeMode = true,
                           LowFrequencyModeOn = _frequency.Hertz < RfMidBandThreshold,
                           Mode = opMode
                       };
            WriteRegister(Register.OpMode, mode);
        }
    }
}
