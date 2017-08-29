using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Http {
    /// <summary>
    /// Wrapper for HTTP headers
    /// </summary>
    public class Headers {
        private string[] namesAndValues;

        private Headers(Builder builder) {
            this.namesAndValues = builder.namesAndValues.ToArray();
        }

        public string this[string key] => Get(namesAndValues, key);
        public int Size => namesAndValues.Length / 2;


        /// <summary>
        /// Returns the field name at index or null if that is out of range
        /// </summary>
        /// <returns></returns>
        public string Name(int index) {
            int fieldNameIndex = index * 2;
            if (fieldNameIndex < 0 || fieldNameIndex >= namesAndValues.Length) {
                return null;
            }
            return namesAndValues[fieldNameIndex];
        }

        /// <summary>
        /// Returns the field value at index or null if that is out of range
        /// </summary>
        /// <returns></returns>
        public string Value(int index) {
            int valueIndex = index * 2 + 1;
            if (valueIndex < 0 || valueIndex >= namesAndValues.Length) {
                return null;
            }
            return namesAndValues[valueIndex];
        }

        /// <summary>
        /// Returns an immutable case-insensitive set of header names
        /// </summary>
        /// <returns></returns>
        public HashSet<string> Names() {
            HashSet<string> result = new HashSet<string>(new FieldNameComparator());

            for (int i = 0; i < Size; i++) {
                result.Add(Name(i));
            }
            return result;
        }

        /// <summary>
        /// Returns an immutable case-insensitive set of header values
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<string> Values(string name) {
            List<string> result = null;
            for (int i = 0; i < Size; i++) {
                if ((name == null && Name(i) == null) || (name != null && name.ToLower().Equals(Name(i).ToLower()))) {
                    if (result == null) result = new List<string>(2);
                    result.Add(Value(i));
                }
            }
            return result != null ? result.ToList() : new List<string>();
        }


        public override string ToString() {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < Size; i++) {
                result.Append(Name(i)).Append(": ").Append(Value(i)).Append("\n");
            }
            return result.ToString();
        }

        private static string Get(string[] namesAndValues, string fieldName) {
            for (int i = namesAndValues.Length - 2; i >= 0; i -= 2) {
                if (fieldName.ToLower() == namesAndValues[i].ToLower()) {
                    return namesAndValues[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Builder for HTTP header collection
        /// </summary>
        public class Builder {
            public List<string> namesAndValues = new List<string>(20);

            /// <summary>
            /// Add an header line containing a field name, a literal colon, and a value
            /// </summary>
            /// <param name="line">line to add</param>
            /// <returns></returns>
            public Builder AddLine(string line) {
                int index = line.IndexOf(":", 1);
                if (index != -1) {
                    return AddLenient(line.Substring(0, index), line.Substring(index + 1));
                } else if (line.StartsWith(":")) {
                    // Work around empty header names and header names that start with a
                    // colon (created by old broken SPDY versions of the response cache).
                    return AddLenient("", line.Substring(1)); // Empty header name.
                } else {
                    return AddLenient("", line); // No header name.
                }
            }

            /// <summary>
            /// Add a field with the specified value
            /// </summary>
            /// <param name="fieldName">field name</param>
            /// <param name="value">field value</param>
            /// <returns></returns>
            public Builder Add(string fieldName, string value) {
                if (fieldName == null) return AddLenient(fieldName, value);
                if (value == null) throw new Exception("value == null");
                if (fieldName.Count() == 0 || fieldName.IndexOf('\0') != -1 || value.IndexOf('\0') != -1) {
                    throw new Exception("Unexpected header: " + fieldName + ": " + value);
                }
                return AddLenient(fieldName, value);
            }
            
            /// <summary>
            /// Add a field with the specified value without any validation. Only
            /// appropriate for headers from the remote peer.
            /// </summary>
            /// <param name="fieldName">field name</param>
            /// <param name="value">field value</param>
            /// <returns></returns>
            private Builder AddLenient(string fieldName, string value) {
                namesAndValues.Add(fieldName);
                namesAndValues.Add(value.Trim());
                return this;
            }

            public Builder RemoveAll(string fieldName) {
                for (int i = 0; i < namesAndValues.Count; i += 2) {
                    if (fieldName.ToLower() == namesAndValues[i].ToLower()) {
                        namesAndValues.RemoveAt(i); // field name
                        namesAndValues.RemoveAt(i); // value
                    }
                }
                return this;
            }

            /**
             * Set a field with the specified value. If the field is not found, it is
             * added. If the field is found, the existing values are replaced.
             */
            public Builder Set(string fieldName, string value) {
                RemoveAll(fieldName);
                Add(fieldName, value);
                return this;
            }
            
            public string Get(string fieldName) {
                for (int i = namesAndValues.Count - 2; i >= 0; i -= 2) {
                    if (fieldName.ToLower() == namesAndValues[i]) {
                        return namesAndValues[i + 1];
                    }
                }
                return null;
            }

            public Headers Build() {
                return new Headers(this);
            }
        }
    }

    public class FieldNameComparator : IEqualityComparer<string> {

        public int compare(string a, string b) {
            if (a == b) {
                return 0;
            } else if (a == null) {
                return -1;
            } else if (b == null) {
                return 1;
            } else {
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool Equals(string x, string y) {
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public int GetHashCode(string obj) {
            return obj.GetHashCode();
        }
    };
}
