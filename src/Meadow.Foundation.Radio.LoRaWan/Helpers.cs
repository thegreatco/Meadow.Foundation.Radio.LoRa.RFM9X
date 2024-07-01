using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal class RentedArray<T> : IDisposable, IEnumerable<T>, IEnumerable
    {
        private readonly object _syncRoot = new();
        private int _disposed;
        private readonly T[] _array;

        public RentedArray(int length)
        {
            Length = length;
            _array = ArrayPool<T>.Shared.Rent(length);
        }

        public int Length { get; }

        public T this[int index]
        {
            get
            {
                ThrowIfDisposed();
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                lock (_syncRoot)
                {
                    return _array[index];
                }
            }
            set
            {
                ThrowIfDisposed();
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                lock (_syncRoot)
                {
                    _array[index] = value;
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                ArrayPool<T>.Shared.Return(_array);
            }
        }

        public T[] ToArray(int? length = null)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                var array = new T[length ?? Length];
                Array.Copy(_array, array, Length);
                return array;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                _array.CopyTo(array, arrayIndex);
            }
        }

        public void CopyFrom(T[] array, int arrayIndex, int destinationIndex, int length)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                Array.Copy(array, arrayIndex, _array, destinationIndex, length);
            }
        }

        // This may be really inefficient, but it's the only way to implement IEnumerable<T> without exposing the array
        // and protecting the caller from another thread calling dispose and returning the array.
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Length; i++)
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    yield return _array[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException(nameof(RentedArray<T>));
        }
    }

    public static class Helpers
    {
        public static string ToHexString(this byte[] bytes)
        {
            return $"0x{BitConverter.ToString(bytes).Replace("-", ", 0x")}";
        }

        public static string ToHexString(this byte @byte)
        {
            return ToHexString([@byte]);
        }
    }
}
