using System;

namespace KSY.Networks
{
    public class KSY_ReceiveBuffer
    {
        private byte[] buffer;
        private int readIndex;
        private int writeIndex;

        //남은 여분 버퍼 공간
        public ArraySegment<byte> FreeBuffer => new ArraySegment<byte>(buffer, writeIndex, buffer.Length - writeIndex);
        //쓰고 아직 읽지 않은 공간
        public ArraySegment<byte> UsedBuffer => new ArraySegment<byte>(buffer, readIndex, writeIndex - readIndex);

        public KSY_ReceiveBuffer(int size)
        {
            buffer = new byte[size];
            readIndex = 0;
            writeIndex = 0;
        }

        public void MoveWriteIndex(int count)
        {
            writeIndex = Math.Min(writeIndex + count, buffer.Length);
        }

        public void MoveReadIndex(int count)
        {
            readIndex = Math.Min(readIndex + count, writeIndex);
        }

        public void CleanUp()
        {
            int num = writeIndex - readIndex;
            if (num != 0)
            {
                Array.Copy(buffer, readIndex, buffer, 0, num);
            }

            readIndex = 0;
            writeIndex = num;
        }
    }
}
