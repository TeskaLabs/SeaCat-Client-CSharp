using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    /// <summary>
    /// Blocking queue that uses linked list 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T> {
        
        // inner queue
        protected readonly Queue<T> queue;
        protected bool stopped;

        public BlockingQueue() {
            queue = new LinkedQueue<T>();
        }

        public BlockingQueue(Queue<T> queue) {
            this.queue = queue;
        }

        public Queue<T> Queue {
            get { return queue; }
        }

        public bool IsEmpty() {
            return queue.Items.Count == 0;
        }

        public bool Contains(T item) {
            return queue.Items.Contains(item);
        }


        public virtual bool Enqueue(T item) {
            if (stopped)
                return false;
            lock (queue) {
                if (stopped)
                    return false;
                queue.Enqueue(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public virtual T Dequeue() {
            if (stopped)
                return default(T);
            lock (queue) {
                if (stopped)
                    return default(T);
                while (queue.Count == 0) {
                    Monitor.Wait(queue);

                    if (stopped)
                        return default(T);
                }
                return queue.Dequeue();
            }
        }

        public virtual T Dequeue(int timeoutMillis, out bool success) {
            success = true;
            if (stopped)
                return default(T);
            lock (queue) {
                if (stopped)
                    return default(T);
                while (queue.Count == 0) {
                    success = Monitor.Wait(queue, timeoutMillis);

                    if (!success || stopped)
                        return default(T);
                }
                return queue.Dequeue();
            }
        }

        public void Stop() {
            if (stopped)
                return;
            lock (queue) {
                if (stopped)
                    return;
                stopped = true;
                Monitor.PulseAll(queue);
            }
        }
        

        public virtual void Remove(T item) {
            queue.Remove(item);
        }

        public virtual void RemoveAt(int index) {
            queue.RemoveAt(index);
        }

    }

    /// <summary>
    /// Interface for non-blocking queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface Queue<T> {
        int Count { get; }
        void Enqueue(T item);
        T Dequeue();
        void Remove(T item);
        void RemoveAt(int index);
        ICollection<T> Items { get; }
    }

    /// <summary>
    /// Queue implementation that uses linked list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedQueue<T> : Queue<T> {

        private LinkedList<T> items = new LinkedList<T>();

        public int Count {
            get { return items.Count; }
        }

        public void Enqueue(T item) {
            items.AddLast(item);
        }

        public T Dequeue() {
            if (items.First == null)
                throw new InvalidOperationException("...");

            var item = items.First.Value;
            items.RemoveFirst();

            return item;
        }

        public void Remove(T item) {
            items.Remove(item);
        }

        public void RemoveAt(int index) {
            Remove(items.Skip(index).First());
        }

        public ICollection<T> Items {
            get { return items; }
        }
    }

    /// <summary>
    /// Queue implementation that uses list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListQueue<T> : Queue<T> {

        private List<T> items = new List<T>();

        public int Count {
            get { return items.Count; }
        }

        public void Enqueue(T item) {
            items.Add(item);
        }

        public T Dequeue() {
            if (items.First() == null)
                throw new InvalidOperationException("...");

            var item = items.First();
            items.RemoveAt(0);

            return item;
        }

        public void Remove(T item) {
            items.Remove(item);
        }

        public void RemoveAt(int index) {
            Remove(items.Skip(index).First());
        }

        public ICollection<T> Items {
            get { return items; }
        }
    }
}
