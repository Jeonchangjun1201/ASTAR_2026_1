using System;
using System.Buffers;

namespace KSY.Networks
{
    public class ArrayPoolBufferWriter : IBufferWriter<byte>, IDisposable
    {
        public ArraySegment<byte> WrittenSegment => new ArraySegment<byte>(buffer, 0, writtenCount);
        public int WrittenCount => writtenCount;

        private byte[] buffer;

        private const int DEFAULT_INITIAL_CAPACITY = 256;
        
        private readonly ArrayPool<byte> pool;
        private int writtenCount;
        private bool isDisposed;

        public ArrayPoolBufferWriter(int initialCapacity = 256, ArrayPool<byte> pool = null)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException("initialCapacity");
            }

            this.pool = pool ?? ArrayPool<byte>.Shared;
            buffer = this.pool.Rent(initialCapacity);
        }

        #region IBufferWriter
        public void Advance(int count)
        {
            ThrowIfDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            //overflow를 방지해서 우항으로 넘겨서 연산
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
        #endregion

        #region IDisposable
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
        #endregion

        public void Write(ReadOnlySpan<byte> source)
        {
            ThrowIfDisposed();
            if (source.Length > 0)
            {
                EnsureCapacity(source.Length);
                source.CopyTo(buffer.AsSpan(writtenCount));
                writtenCount += source.Length;
            }
        }

        //Ensure : <성공 등을> 확실하게 하다, 보증하다, <지위 등을> 확보하다
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

            int needSize = writtenCount + sizeHint;

            if (needSize > buffer.Length)
            {
                int requestSize = 0;
                for (requestSize = buffer.Length; requestSize < needSize; requestSize *= 2) ;

                byte[] array = buffer;
                buffer = pool.Rent(requestSize);
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