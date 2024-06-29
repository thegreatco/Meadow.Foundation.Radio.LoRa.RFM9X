using System;

using Meadow.Units;

using static Meadow.Foundation.Radio.LoRa.RFM9X.LoRaRegisters;

namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public partial class Rfm9X
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

        private void WriteRegister(byte register, byte[] bytes)
        {
            _logger.Debug($"Writing to register {register.ToHexString()} with {bytes.ToHexString()}");
#if CUSTOM_SPI
            var writeBuffer = new byte[bytes.Length + 1];
            writeBuffer[0] = (byte)(0x80 | (byte)register);
            bytes.CopyTo(writeBuffer, 1);
            _config.SpiBus.Write(_chipSelect, writeBuffer);
            _logger.Trace($"Wrote to register {register.ToHexString()} with {writeBuffer.ToHexString()}");
#else
            _comms.WriteRegister((byte)register, bytes);
            _logger.Trace($"Wrote to register {register} with {bytes.ToHexString()}");
#endif

        }

        private byte ReadRegister(Register register)
        {
            _logger.Debug($"Reading register {register}");
#if CUSTOM_SPI
            var writeCommand = 0x7F & (byte)register;
            var writeBuffer = new byte[] { (byte)writeCommand, 0x00 };
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

        private void ReadRegister(Register register, byte[] buffer)
        {
            _logger.Debug($"Reading register {register}");
#if CUSTOM_SPI
            var writeCommand = 0x80 | (byte)register;
            var writeBuffer = new byte[buffer.Length];
            writeBuffer[0] = (byte)writeCommand;
            _comms.ReadRegister((byte)register, buffer);
#else
            _comms.ReadRegister((byte)register, buffer);
#endif
            _logger.Trace($"Read register {((byte)register).ToHexString()} got {buffer.ToHexString()} bytes");
        }

        private void WriteModemConfig1(Frequency bandwidth, ErrorCodingRate codingRate, ImplicitHeaderMode implicitHeaderMode)
        {
            var bw = bandwidth.Kilohertz switch
                            {
                                7.8   => Bandwidth.Bw7_8kHz,
                                10.4  => Bandwidth.Bw10_4kHz,
                                15.6  => Bandwidth.Bw15_6kHz,
                                20.8  => Bandwidth.Bw20_8kHz,
                                31.25 => Bandwidth.Bw31_25kHz,
                                41.7  => Bandwidth.Bw41_7kHz,
                                62.5  => Bandwidth.Bw62_5kHz,
                                125   => Bandwidth.Bw125kHz,
                                250   => Bandwidth.Bw250kHz,
                                500   => Bandwidth.Bw500kHz,
                                _     => throw new ArgumentOutOfRangeException(nameof(bandwidth), "Invalid bandwidth")
                            };
            _logger.Trace($"Setting Bandwidth: {bw}, CodingRate: {codingRate}, ImplicitHeaderMode: {implicitHeaderMode}");
            var value = (byte)bw;
            value |= (byte)codingRate;
            value |= (byte)implicitHeaderMode;
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
    }
}
