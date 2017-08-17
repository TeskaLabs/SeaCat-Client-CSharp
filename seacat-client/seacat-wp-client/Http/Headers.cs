using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Http
{
    /// <summary>
    /// Wrapper for HTTP headers
    /// </summary>
    public class Headers
    {
        private String[] namesAndValues;

        private Headers(Builder builder)
        {
            this.namesAndValues = builder.namesAndValues.ToArray();
        }

        /** Returns the last value corresponding to the specified field, or null. */
        public String Get(String fieldName)
        {
            if (fieldName == null) return namesAndValues[1];
            else return Get(namesAndValues, fieldName);
        }

        /** Returns the number of field values. */
        public int Size()
        {
            return namesAndValues.Length / 2;
        }

        /** Returns the field at {@code position} or null if that is out of range. */
        public String Name(int index)
        {
            int fieldNameIndex = index * 2;
            if (fieldNameIndex < 0 || fieldNameIndex >= namesAndValues.Length)
            {
                return null;
            }
            return namesAndValues[fieldNameIndex];
        }

        /** Returns the value at {@code index} or null if that is out of range. */
        public String Value(int index)
        {
            int valueIndex = index * 2 + 1;
            if (valueIndex < 0 || valueIndex >= namesAndValues.Length)
            {
                return null;
            }
            return namesAndValues[valueIndex];
        }

        /** Returns an immutable case-insensitive set of header names. */
        public HashSet<string> Names()
        {
            HashSet<string> result = new HashSet<string>(new FieldNameComparator());

            for (int i = 0; i < Size(); i++)
            {
                result.Add(Name(i));
            }
            return result;
        }

        /** Returns an immutable list of the header values for {@code name}. */
        public List<string> Values(string name)
        {
            List<string> result = null;
            for (int i = 0; i < Size(); i++)
            {
                if ((name == null && Name(i) == null) || (name != null && name.ToLower().Equals(Name(i).ToLower())))
                {
                    if (result == null) result = new List<string>(2);
                    result.Add(Value(i));
                }
            }
            return result != null
                ? result.ToList()
                : new List<string>();
        }

        /** @param fieldNames a case-insensitive set of HTTP header field names. */
        // TODO: it is very weird to request a case-insensitive set as a parameter.
        public Headers GetAll(HashSet<string> fieldNames)
        {
            Builder result = new Builder();
            for (int i = 0; i < namesAndValues.Length; i += 2)
            {
                string fieldName = namesAndValues[i];
                if (fieldNames.Contains(fieldName))
                {
                    result.Add(fieldName, namesAndValues[i + 1]);
                }
            }
            return result.Build();
        }

        public Builder NewBuilder()
        {
            Builder result = new Builder();
            result.namesAndValues.AddRange(namesAndValues);
            return result;
        }

        public String ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < Size(); i++)
            {
                result.Append(Name(i)).Append(": ").Append(Value(i)).Append("\n");
            }
            return result.ToString();
        }

        private static string Get(string[] namesAndValues, string fieldName)
        {
            for (int i = namesAndValues.Length - 2; i >= 0; i -= 2)
            {
                if (fieldName.ToLower() == namesAndValues[i].ToLower())
                {
                    return namesAndValues[i + 1];
                }
            }
            return null;
        }

        public class Builder
        {
            public List<String> namesAndValues = new List<String>(20);

            /** Add an header line containing a field name, a literal colon, and a value. */
            public Builder AddLine(String line)
            {
                int index = line.IndexOf(":", 1);
                if (index != -1)
                {
                    return AddLenient(line.Substring(0, index), line.Substring(index + 1));
                }
                else if (line.StartsWith(":"))
                {
                    // Work around empty header names and header names that start with a
                    // colon (created by old broken SPDY versions of the response cache).
                    return AddLenient("", line.Substring(1)); // Empty header name.
                }
                else
                {
                    return AddLenient("", line); // No header name.
                }
            }

            /** Add a field with the specified value. */
            public Builder Add(String fieldName, String value)
            {
                if (fieldName == null) return AddLenient(fieldName, value);
                if (value == null) throw new Exception("value == null");
                if (fieldName.Count() == 0 || fieldName.IndexOf('\0') != -1 || value.IndexOf('\0') != -1)
                {
                    throw new Exception("Unexpected header: " + fieldName + ": " + value);
                }
                return AddLenient(fieldName, value);
            }

            /**
             * Add a field with the specified value without any validation. Only
             * appropriate for headers from the remote peer.
             */
            private Builder AddLenient(String fieldName, String value)
            {
                namesAndValues.Add(fieldName);
                namesAndValues.Add(value.Trim());
                return this;
            }

            public Builder RemoveAll(String fieldName)
            {
                for (int i = 0; i < namesAndValues.Count; i += 2)
                {
                    if (fieldName.ToLower() == namesAndValues[i].ToLower())
                    {
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
            public Builder Set(String fieldName, String value)
            {
                RemoveAll(fieldName);
                Add(fieldName, value);
                return this;
            }

            /** Equivalent to {@code build().get(fieldName)}, but potentially faster. */
            public String Get(String fieldName)
            {
                for (int i = namesAndValues.Count - 2; i >= 0; i -= 2)
                {
                    if (fieldName.ToLower() == namesAndValues[i])
                    {
                        return namesAndValues[i + 1];
                    }
                }
                return null;
            }

            public Headers Build()
            {
                return new Headers(this);
            }
        }
    }

    public class FieldNameComparator : IEqualityComparer<String>
    {

        public int compare(String a, String b)
        {
            if (a == b)
            {
                return 0;
            }
            else if (a == null)
            {
                return -1;
            }
            else if (b == null)
            {
                return 1;
            }
            else
            {
                return String.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool Equals(string x, string y)
        {
            return String.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    };
}
