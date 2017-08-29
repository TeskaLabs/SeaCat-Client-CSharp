using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SeaCatCSharpClient {

    /// <summary>
    /// Helper for CSR structure
    /// </summary>
    public class CSR {

        private Dictionary<string, string> paramMap = new Dictionary<string, string>();

        public CSR() {

        }

        public string Country
        {
            get { return paramMap["C"]; }
            set { paramMap.Add("C", value);}
        }

        public string State
        {
            get { return paramMap["ST"]; }
            set { paramMap.Add("ST", value);}
        }

        public string Locality
        {
            get { return paramMap["L"]; }
            set { paramMap.Add("L", value);}
        }

        public string Organization
        {
            get { return paramMap["O"]; }
            set { paramMap.Add("O", value);}
        }

        public string OrganizationUnit
        {
            get { return paramMap["OU"]; }
            set { paramMap.Add("OU", value);}
        }

        public string CommonName
        {
            get { return paramMap["CN"]; }
            set { paramMap.Add("CN", value);}
        }

        public string Surname
        {
            get { return paramMap["SN"]; }
            set { paramMap.Add("SN",value);}
        }

        public string GivenName
        {
            get { return paramMap["GN"]; }
            set { paramMap.Add("GN",value);}
        }

        public string EmailAddress
        {
            get { return paramMap["emailAddress"]; }
            set { paramMap.Add("emailAddress",value);}
        }

        public string UniqueIdentifier
        {
            get { return paramMap["UID"]; }
            set { paramMap.Add("UID",value);}
        }

        public void Set(string name, string value) => paramMap.Add(name, value);

        public string Get(string name) => paramMap[name];

        public string[] ToStringArray() {
            int cnt = paramMap.Count;
            String[] arr = new String[cnt * 2];

            int pos = 0;
            foreach (var key in paramMap.Keys) {
                arr[pos++] = key;
                arr[pos++] = paramMap[key];
            }

            return arr;
        }

        public void SetData(string data) => this.Set("description", data);

        public void Submit() {
            int rc = SeaCatClient.Reactor.Bridge.csrgen_worker(this.ToStringArray());
            RC.CheckAndThrowIOException("seacatcc.csrgen_worker", rc);
        }

        public static Task CreateDefault() {
            return TaskHelper.CreateTask("CSR", () => {
                CSR csr = new CSR();

                try {
                    csr.Submit();
                } catch (IOException e) {
                    Logger.Error("CSR", $"Exception in CSR.createDefault: {e.Message}");
                }
            });
        }
    }
}
