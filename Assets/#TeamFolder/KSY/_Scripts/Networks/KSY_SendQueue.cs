using System;
using System.Collections.Generic;

namespace KSY.Networks
{
    public class KSY_SendQueue : IDisposable
    {
        private readonly Queue<KSY_ISendQueueContext> contextQueue;
        private readonly List<KSY_ISendQueueContext> contextFlushBuffer;

        public KSY_SendQueue()
        {
            contextQueue = new Queue<KSY_ISendQueueContext>();
            contextFlushBuffer = new List<KSY_ISendQueueContext>();
        }

        public void Enqueue(KSY_ISendQueueContext context)
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
                KSY_ISendQueueContext sendQueueContext = contextQueue.Dequeue();
                bufferList.Add(sendQueueContext.GetData());
                contextFlushBuffer.Add(sendQueueContext);
            }

            return true;
        }

        public void Clear()
        {
            foreach(KSY_ISendQueueContext item in contextFlushBuffer)
            {
                item?.Dispose();
            }

            contextFlushBuffer.Clear();
        }
        public void Dispose()
        {
            Clear();
            KSY_ISendQueueContext result;
            while(contextQueue.TryDequeue(out result))
            {
                result.Dispose();
            }
        }
    }
}


