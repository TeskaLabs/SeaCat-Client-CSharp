using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils {

    /// <summary>
    /// Simple un-synchronized counter object
    /// </summary>
    public class IntegerCounter {

        private int counter;

        public IntegerCounter(int start) {
            this.counter = start;
        }

        public override string ToString() {
            return "[Counter counter=" + counter + "]";
        }

        public void Set(int value) {
            counter = value;
        }

        public int Get() {
            return counter;
        }

        public int GetAndAdd(int increment) {
            int ret = counter;
            counter += increment;
            return ret;
        }

    }
}
