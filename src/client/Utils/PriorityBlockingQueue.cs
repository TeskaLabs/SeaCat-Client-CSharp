using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    public class PriorityBlockingQueue<TValue> : BlockingQueue<TValue> {

        private readonly IComparer<TValue> priorityComparer;

        public PriorityBlockingQueue(IComparer<TValue> comparer) : base(new ListQueue<TValue>()) {
            if (comparer == null) {
                throw new ArgumentNullException();
            }

            priorityComparer = comparer;
        }



        /// <summary>
        /// Add an element to the priority queue.
        /// </summary>
        /// <param name="priority">Priority of the element</param>
        /// <param name="value"></param>
        public override bool Enqueue(TValue value) {
            var result = base.Enqueue(value);
            BubbleUp();
            return result;
        }

        private List<TValue> Items {
            get {
                return queue.Items as List<TValue>;
            }
        }

        /// <summary>
        /// Pop the minimal element of the queue. Will fail at runtime if queue is empty.
        /// </summary>
        /// <returns>The minmal element</returns>
        public override TValue Dequeue() {
            if (!Items.Any()) {
                return default(TValue);
            }

            var items = Items;
            var ret = items[0];
            items[0] = items[queue.Count - 1];
            items.RemoveAt(items.Count - 1);
            BubbleDown();
            return ret;
        }

        /// <summary>
        /// Removes the first element that equals the value from the queue
        /// </summary>
        public override void Remove(TValue value) {
            var items = Items;
            int idx = items.IndexOf(value);
            if (idx == -1) {
                Logger.Error("PBQ", "Unknown value! Can't remove from queue");
            }

            items[idx] = items[queue.Count - 1];
            items.RemoveAt(queue.Count - 1);
            BubbleDown();
        }

        public override void RemoveAt(int index) {
            var items = Items;
            items[index] = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            BubbleDown();

        }

        /// <summary>
        /// Bubble up the last element in the queue until it's in the correct spot.
        /// </summary>
        private void BubbleUp() {
            var items = Items;
            int node = queue.Count - 1;
            while (node > 0) {
                int parent = (node - 1) >> 1;
                if (priorityComparer.Compare(items[parent], items[node]) < 0) {
                    break; // we're in the right order, so we're done
                }
                var tmp = items[parent];
                items[parent] = items[node];
                items[node] = tmp;
                node = parent;
            }
        }

        /// <summary>
        /// Bubble down the first element until it's in the correct spot.
        /// </summary>
        private void BubbleDown() {
            var items = Items;
            int node = 0;
            while (true) {
                // Find smallest child
                int child0 = (node << 1) + 1;
                int child1 = (node << 1) + 2;
                int smallest;
                if (child0 < queue.Count && child1 < queue.Count) {
                    smallest = priorityComparer.Compare(items[child0], items[child1]) < 0 ? child0 : child1;
                } else if (child0 < queue.Count) {
                    smallest = child0;
                } else if (child1 < queue.Count) {
                    smallest = child1;
                } else {
                    break; // 'node' is a leaf, since both children are outside the array
                }

                if (priorityComparer.Compare(items[node], items[smallest]) < 0) {
                    break; // we're in the right order, so we're done.
                }

                var tmp = items[node];
                items[node] = items[smallest];
                items[smallest] = tmp;
                node = smallest;
            }
        }
    }
}
