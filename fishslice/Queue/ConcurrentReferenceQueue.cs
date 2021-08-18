using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace fishslice.Queue
{
    public class ConcurrentReferenceQueue<T> : ConcurrentQueue<StrongBox<T>>
    {
        public bool TryDequeue(out T item)
        {
            if (TryDequeue(out StrongBox<T> wrappedItem))
            {
                item = wrappedItem.Value;
                wrappedItem.Value = default;
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }

        public void Enqueue(T item)
        {
            var wrappedItem = new StrongBox<T>(item);
            Enqueue(wrappedItem);
        }
    }
}
