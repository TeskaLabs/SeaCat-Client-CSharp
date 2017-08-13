using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    /// <summary>
    /// Blocking queue that uses linked list 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T>
    {
        private readonly LinkedQueue<T> queue = new LinkedQueue<T>();
        private bool stopped;

        public LinkedQueue<T> Queue
        {
            get { return queue; }
        }

        public bool IsEmpty()
        {
            return queue.Items.Count == 0;
        }

        public bool Contains(T item)
        {
            return queue.Items.Contains(item);
        }

        public bool Enqueue(T item)
        {
            if (stopped)
                return false;
            lock (queue)
            {
                if (stopped)
                    return false;
                queue.Enqueue(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public T Dequeue()
        {
            if (stopped)
                return default(T);
            lock (queue)
            {
                if (stopped)
                    return default(T);
                while (queue.Count == 0)
                {
                    Monitor.Wait(queue);
                    if (stopped)
                        return default(T);
                }
                return queue.Dequeue();
            }
        }

        public void Stop()
        {
            if (stopped)
                return;
            lock (queue)
            {
                if (stopped)
                    return;
                stopped = true;
                Monitor.PulseAll(queue);
            }
        }

        public void Remove(T item)
        {
            queue.Remove(item);
        }

        public void RemoveAt(int index)
        {
            queue.RemoveAt(index);
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
