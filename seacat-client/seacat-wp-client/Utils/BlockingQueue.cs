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
        protected readonly Queue<T> queue;
        protected bool stopped;

        public BlockingQueue()
        {
            queue = new LinkedQueue<T>();
        }

        public BlockingQueue(Queue<T> queue)
        {
            this.queue = queue;
        }

        public Queue<T> Queue
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
        
        public virtual bool Enqueue(T item)
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

        public virtual T Dequeue()
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

        public virtual void Remove(T item)
        {
            queue.Remove(item);
        }

        public virtual void RemoveAt(int index)
        {
            queue.RemoveAt(index);
        }
       
    }

    public interface Queue<T>
    {
        int Count { get; }
        void Enqueue(T item);
        T Dequeue();
        void Remove(T item);
        void RemoveAt(int index);
        ICollection<T> Items { get; }
    }

    public class LinkedQueue<T> : Queue<T>
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

        public ICollection<T> Items
        {
            get { return _items; }
        }

        private LinkedList<T> _items = new LinkedList<T>();
    }

    public class ListQueue<T> : Queue<T>
    {
        public int Count
        {
            get { return _items.Count; }
        }

        public void Enqueue(T item)
        {
            _items.Add(item);
        }

        public T Dequeue()
        {
            if (_items.First() == null)
                throw new InvalidOperationException("...");

            var item = _items.First();
            _items.RemoveAt(0);

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

        public ICollection<T> Items
        {
            get { return _items; }
        }

        private List<T> _items = new List<T>();
    }
}
