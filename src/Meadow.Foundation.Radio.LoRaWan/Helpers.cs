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
        public static string ToBase64(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static string ToBase64(this Span<byte> bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static string ToBase64(this ReadOnlySpan<byte> bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static string ToBase64(this Memory<byte> bytes)
        {
            return Convert.ToBase64String(bytes.Span);
        }

        public static string ToBase64(this ReadOnlyMemory<byte> bytes)
        {
            return Convert.ToBase64String(bytes.Span);
        }

        public static string ToHexString(this byte[] bytes, bool prefix = true)
        {
            return prefix
                       ? $"0x{BitConverter.ToString(bytes).Replace("-", ", 0x")}"
                       : $"{BitConverter.ToString(bytes).Replace("-", "")}";
        }

        public static string ToHexString(this byte @byte, bool prefix = true)
        {
            return ToHexString([@byte]);
        }

        public static string ToHexString(this ReadOnlyMemory<byte> memory, bool prefix = false)
        {
            return memory.Span.ToHexString(prefix);
        }

        public static string ToHexString(this ReadOnlySpan<byte> span, bool prefix = false)
        {
            if (span.Length == 0)
                return string.Empty;
            var chars = new char[span.Length * 2 + (prefix ? 3 : 0)];
            var offset = 0;
            foreach (var b in span)
            {
                var hex = b.ToString("X2");
                chars[offset] = hex[0];
                chars[offset + 1] = hex[1];
                if (prefix)
                {
                    chars[offset + 2] = ',';
                    chars[offset + 3] = ' ';
                    offset += 4;
                }
                else
                {
                    offset += 2;
                }
            }

            return new string(chars);
        }

        public static string ToHexString(this Memory<byte> memory, bool prefix = false)
        {
            return memory.Span.ToHexString(prefix);
        }

        public static string ToHexString(this Span<byte> span, bool prefix = false)
        {
            if (span.Length == 0)
                return string.Empty;
            var chars = new char[span.Length * 2 + (prefix ? 3 : 0)];
            var offset = 0;
            foreach (var b in span)
            {
                var hex = b.ToString("X2");
                chars[offset] = hex[0];
                chars[offset + 1] = hex[1];
                if (prefix)
                {
                    chars[offset + 2] = ',';
                    chars[offset + 3] = ' ';
                    offset += 4;
                }
                else
                {
                    offset += 2;
                }
            }

            return new string(chars);
        }

        public static void CopyToReverse<T>(this ReadOnlySpan<T> src, T[] dest)
        {
            if (dest.Length < src.Length)
                throw new ArgumentException("Destination array is too small");
            for (var i = 0; i < src.Length; i++)
            {
                dest[i] = src[src.Length - i - 1];
            }
        }

        public static void CopyToReverse<T>(this ReadOnlySpan<T> src, Span<T> dest)
        {
            if (dest.Length < src.Length)
                throw new ArgumentException("Destination array is too small");
            for (var i = 0; i < src.Length; i++)
            {
                dest[i] = src[src.Length - i - 1];
            }
        }

        public static void CopyToReverse<T>(this T[] src, T[] dest)
        {
            if (dest.Length < src.Length)
                throw new ArgumentException("Destination array is too small");
            for (var i = 0; i < src.Length; i++)
            {
                dest[i] = src[src.Length - i - 1];
            }
        }

        public static void CopyToReverse<T>(this T[] src, Span<T> dest)
        {
            if (dest.Length < src.Length)
                throw new ArgumentException("Destination array is too small");
            for (var i = 0; i < src.Length; i++)
            {
                dest[i] = src[src.Length - i - 1];
            }
        }

        public static void CopyToReverse<T>(this T[] src, T[] dest, int destOffset)
        {
            if (destOffset + dest.Length < src.Length)
                throw new ArgumentException("Destination array is too small");
            for (var i = 0; i < src.Length; i++)
            {
                dest[destOffset + i] = src[src.Length - i - 1];
            }
        }

        public static T[] Reverse<T>(this T[] array)
        {
            var newArray = new T[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                newArray[i] = array[array.Length - i - 1];
            }

            return newArray;
        }

        public static T[] Reverse<T>(this Span<T> array)
        {
            var newArray = new T[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                newArray[i] = array[array.Length - i - 1];
            }

            return newArray;
        }

        public static T[] Reverse<T>(this ReadOnlySpan<T> array)
        {
            var newArray = new T[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                newArray[i] = array[array.Length - i - 1];
            }

            return newArray;
        }

        public static T[] Reverse<T>(this ReadOnlyMemory<T> array)
        {
            return array.Span.Reverse();
        }

        public static T[] Reverse<T>(this Memory<T> array)
        {
            return array.Span.Reverse();
        }
    }
}
