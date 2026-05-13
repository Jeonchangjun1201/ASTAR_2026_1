using System;
using System.Collections.Generic;

namespace KSY.Networks
{
    public class SendQueue : IDisposable
    {
        private readonly Queue<ISendQueueContext> contextQueue;
        private readonly List<ISendQueueContext> contextFlushBuffer;

        public SendQueue()
        {
            contextQueue = new Queue<ISendQueueContext>();
            contextFlushBuffer = new List<ISendQueueContext>();
        }

        public void Enqueue(ISendQueueContext context)
        {
            contextQueue.Enqueue(context);
        }

        public bool TryFlush(out List<ArraySegment<byte>> bufferList)
        {
            bufferList = null;
            if(contextFlushBuffer.Count > 0)
            {
                return false;   
            }

            if(contextQueue.Count <= 0)
            {
                return false;
            }

            bufferList = new List<ArraySegment<byte>>();
            while(contextQueue.Count > 0)
            {
                ISendQueueContext sendQueueContext = contextQueue.Dequeue();
                bufferList.Add(sendQueueContext.GetData());
                contextFlushBuffer.Add(sendQueueContext);
            }

            return true;
        }

        public void Clear()
        {
            foreach(ISendQueueContext item in contextFlushBuffer)
            {
                item?.Dispose();
            }

            contextFlushBuffer.Clear();
        }
        public void Dispose()
        {
            Clear();
            ISendQueueContext result;
            while(contextQueue.TryDequeue(out result))
            {
                result.Dispose();
            }
        }
    }
}


