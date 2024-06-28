using System;

using static Meadow.Foundation.Radio.LoRa.RFM9X.LoRaRegisters;

namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public partial class Rfm9X
    {
        private void WriteRegister(Register register, byte value)
        {
            WriteRegister(register, [value]);
        }

        private void WriteRegister(Register register, byte[] bytes)
        {
            _logger.Debug($"Writing to register {register} with {bytes.ToHexString()}");
#if CUSTOM_SPI
            var writeBuffer = new byte[bytes.Length + 1];
            writeBuffer[0] = (byte)(0x80 | (byte)register);
            bytes.CopyTo(writeBuffer, 1);
            _config.SpiBus.Write(_chipSelect, writeBuffer);
            _logger.Trace($"Wrote to register {register} with {writeBuffer.ToHexString()}");
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
    }
}
