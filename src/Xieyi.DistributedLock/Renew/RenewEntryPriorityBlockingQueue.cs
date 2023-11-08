namespace Xieyi.DistributedLock.Renew
{
    public class RenewEntryPriorityBlockingQueue<T> where T : RenewEntry
    {
        private int _capacity;
        private int _size;
        private T[] _heap;

        private readonly object _locker = new object();

        public RenewEntryPriorityBlockingQueue()
        {
            _capacity = 10;
            _size = 0;
            _heap = new T[_capacity + 1];
        }

        public void Offer(T item)
        {
            Monitor.Enter(_locker);
            GrowIfNecessary();

            try
            {
                Insert(item);
                Monitor.Pulse(_locker);
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }

        public T Poll()
        {
            Monitor.Enter(_locker);

            T item;
            try
            {
                var waitTime = Timeout.Infinite;
                while ((item = Peek()) == null || (waitTime = item.TimeToRenew()) > 0)
                {
                    Monitor.Wait(_locker, waitTime);
                    waitTime = Timeout.Infinite;
                }

                item = Delete();
            }
            finally
            {
                Monitor.Exit(_locker);
            }

            return item;
        }

        private void Insert(T item)
        {
            _heap[++_size] = item;
            Swim(_size);
        }

        private T Delete()
        {
            var min = _heap[1];
            if (min != null)
            {
                Swap(1, _size--);
                _heap[_size + 1] = null;
                Sink(1);
            }

            return min;
        }

        private T Peek()
        {
            return _heap[1];
        }

        private void GrowIfNecessary()
        {
            if (_size >= _capacity)
            {
                var newHeap = new T[_capacity * 2 + 1];
                Array.Copy(_heap, 1, newHeap, 1, _size);
                _capacity *= 2;
                _heap = newHeap;
            }
        }

        private void Swim(int k)
        {
            while (k > 1 && LessThan(k, k / 2))
            {
                Swap(k, k / 2);
                k /= 2;
            }
        }

        private void Sink(int k)
        {
            while (2 * k <= _size)
            {
                int j = 2 * k;
                if (j < _size && LessThan(j + 1, j))
                {
                    j++;
                }

                if (!LessThan(j, k))
                {
                    break;
                }

                Swap(k, j);
                k = j;
            }
        }

        private bool LessThan(int i, int j)
        {
            return _heap[i].CompareTo(_heap[j]) < 0;
        }

        private void Swap(int i, int j)
        {
            T temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }
    }
}