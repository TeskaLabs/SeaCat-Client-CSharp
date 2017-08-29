using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    /// <summary>
    /// Flag for Seacat logging
    /// </summary>
    public class LogFlag {

        public static LogFlag DEBUG_GENERIC = new LogFlag(0x0000000000000001);

        public LogFlag(long v) {
            this.Value = v;
        }

        public long Value { get; set; }

        public void AddMask(long value) {
            Value |= value;
        }

        public bool ContainsMask(long val) => (val & Value) == val;
    }
}
