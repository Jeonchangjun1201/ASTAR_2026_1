using System;
using System.Buffers;

namespace KSY.Networks
{
    public class ArrayPoolBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private const int DEFAULT_INITIAL_CAPACITY = 256;
        private readonly ArrayPool<byte> pool;
        private byte[] buffer;
        private int writtenCount;
        private bool isDisposed;
        public int WrittenCount => writtenCount;
        public ArraySegment<byte> WrittenSegment => new ArraySegment<byte>(buffer, 0, writtenCount);

        public ArrayPoolBufferWriter(int initialCapacity = 256, ArrayPool<byte> pool = null)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException("initialCapacity");
            }

            this.pool = pool ?? ArrayPool<byte>.Shared;
            buffer = this.pool.Rent(initialCapacity);
        }

        public void Advance(int count)
        {
            ThrowIfDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (WrittenCount > buffer.Length - count)
            {
                throw new InvalidOperationException("Cannot advance past the end of the current buffer.");
            }

            writtenCount += count;
        }
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            ThrowIfDisposed();
            EnsureCapacity(sizeHint);
            return buffer.AsMemory(writtenCount);
        }
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            ThrowIfDisposed();
            EnsureCapacity(sizeHint);
            return buffer.AsSpan(writtenCount);
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (buffer != null)
                {
                    pool.Return(buffer);
                    buffer = null;
                }

                writtenCount = 0;
            }
        }
        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException("sizeHint");
            }

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            int num = writtenCount + sizeHint;
            if (num > buffer.Length)
            {
                int num2;
                for (num2 = buffer.Length; num2 < num; num2 *= 2)
                {
                }

                byte[] array = buffer;
                buffer = pool.Rent(num2);
                array.AsSpan(0, writtenCount).CopyTo(buffer);
                pool.Return(array);
            }
        }
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("ArrayPoolBufferWriter");
            }
        }
    }
}