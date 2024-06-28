using System;
using System.Buffers;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal class RentedArray<T>(int length) : IDisposable
    {
        public int Length => length;
        private bool _disposed = false;
        private readonly T[] _array = ArrayPool<T>.Shared.Rent(length);
        public T[] Array
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Array));
                return _array;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            ArrayPool<T>.Shared.Return(_array);
        }
    }
}
