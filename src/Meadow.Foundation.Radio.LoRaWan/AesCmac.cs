using System.Security.Cryptography;
using System;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal class AesCMac
    {
        private const int BlockSize = 16; // AES block size in bytes

        public static byte[] ComputeAesCMac(byte[] key, byte[] message)
        {
            // Generate subkeys K1 and K2
            var subKeys = GenerateSubKeys(key);
            var K1 = subKeys.Item1;
            var K2 = subKeys.Item2;

            // Pad the message if necessary
            var paddedMessage = PadMessage(message);

            // Determine which subkey to use
            var lastBlock = new byte[BlockSize];
            var numberOfBlocks = paddedMessage.Length / BlockSize;

            if (message.Length % BlockSize == 0)
            {
                // XOR last block with K1
                Array.Copy(paddedMessage, (numberOfBlocks - 1) * BlockSize, lastBlock, 0, BlockSize);
                lastBlock = Xor(lastBlock, K1);
            }
            else
            {
                // XOR last block with K2
                Array.Copy(paddedMessage, (numberOfBlocks - 1) * BlockSize, lastBlock, 0, BlockSize);
                lastBlock = Xor(lastBlock, K2);
            }

            // Initialize the AES encryption
            using var aes = new AesManaged { Key = key, Mode = CipherMode.ECB, Padding = PaddingMode.None };
            var cmac = new byte[BlockSize];
            using var encryptor = aes.CreateEncryptor();
            var block = new byte[BlockSize];
            for (var i = 0; i < numberOfBlocks - 1; i++)
            {
                Array.Copy(paddedMessage, i * BlockSize, block, 0, BlockSize);
                cmac = encryptor.TransformFinalBlock(Xor(block, cmac), 0, BlockSize);
            }

            cmac = encryptor.TransformFinalBlock(Xor(lastBlock, cmac), 0, BlockSize);

            return cmac;
        }

        private static Tuple<byte[], byte[]> GenerateSubKeys(byte[] key)
        {
            using var aes = new AesManaged { Key = key, Mode = CipherMode.ECB, Padding = PaddingMode.None };
            var zeroBlock = new byte[BlockSize];
            byte[] lBlock;

            using (var encryptor = aes.CreateEncryptor())
            {
                lBlock = encryptor.TransformFinalBlock(zeroBlock, 0, BlockSize);
            }

            var K1 = LeftShift(lBlock);
            if ((lBlock[0] & 0x80) != 0)
            {
                K1[^1] ^= 0x87;
            }

            var K2 = LeftShift(K1);
            if ((K1[0] & 0x80) != 0)
            {
                K2[^1] ^= 0x87;
            }

            return new Tuple<byte[], byte[]>(K1, K2);
        }

        private static byte[] LeftShift(byte[] input)
        {
            var output = new byte[input.Length];
            byte overflow = 0;

            for (var i = input.Length - 1; i >= 0; i--)
            {
                output[i] = (byte)(input[i] << 1);
                output[i] |= overflow;
                overflow = (byte)((input[i] & 0x80) >> 7);
            }

            return output;
        }

        private static byte[] Xor(byte[] a, byte[] b)
        {
            var result = new byte[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }

            return result;
        }

        private static byte[] PadMessage(byte[] message)
        {
            var remainder = message.Length % BlockSize;
            if (remainder == 0)
            {
                return message;
            }

            var paddedMessage = new byte[message.Length + (BlockSize - remainder)];
            Array.Copy(message, paddedMessage, message.Length);
            paddedMessage[message.Length] = 0x80; // padding with 0x80 followed by zeros

            return paddedMessage;
        }
    }

}
