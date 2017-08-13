using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public class BlockingQueue<T>
    {
        private readonly LinkedQueue<T> _queue = new LinkedQueue<T>();
        private bool _stopped;

        public LinkedQueue<T> Queue
        {
            get { return _queue; }
        }

        public bool IsEmpty()
        {
            return _queue.Items.Count == 0;
        }

        public bool Contains(T item)
        {
            return _queue.Items.Contains(item);
        }

        public bool Enqueue(T item)
        {
            if (_stopped)
                return false;
            lock (_queue)
            {
                if (_stopped)
                    return false;
                _queue.Enqueue(item);
                Monitor.Pulse(_queue);
            }
            return true;
        }

        public T Dequeue()
        {
            if (_stopped)
                return default(T);
            lock (_queue)
            {
                if (_stopped)
                    return default(T);
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue);
                    if (_stopped)
                        return default(T);
                }
                return _queue.Dequeue();
            }
        }

        public void Stop()
        {
            if (_stopped)
                return;
            lock (_queue)
            {
                if (_stopped)
                    return;
                _stopped = true;
                Monitor.PulseAll(_queue);
            }
        }

        public void Remove(T item)
        {
            _queue.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _queue.RemoveAt(index);
        }
       
    }

    public class LinkedQueue<T>
    {
        public int Count
        {
            get { return _items.Count; }
        }

        public void Enqueue(T item)
        {
            _items.AddLast(item);
        }

        public T Dequeue()
        {
            if (_items.First == null)
                throw new InvalidOperationException("...");

            var item = _items.First.Value;
            _items.RemoveFirst();

            return item;
        }

        public void Remove(T item)
        {
            _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Remove(_items.Skip(index).First());
        }

        public LinkedList<T> Items
        {
            get { return _items; }
        }

        private LinkedList<T> _items = new LinkedList<T>();
    }
}
