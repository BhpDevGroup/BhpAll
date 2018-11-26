using System.Collections.Concurrent;

namespace Bhp
{
    public class WalletTxQueue
    {
        ConcurrentQueue<WalletTx> queue;

        public WalletTxQueue()
        {
            queue = new ConcurrentQueue<WalletTx>();
        }

        public void Push(WalletTx wtx)
        {
            queue.Enqueue(wtx);
        }

        public bool Pop(out WalletTx wtx)
        {
            return queue.TryDequeue(out wtx);
        }

        public bool IsEmpty()
        {
            return queue.IsEmpty;
        }
    }
}
