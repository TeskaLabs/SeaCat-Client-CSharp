using seacat_wp_client.Core;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace seacat_wp_client
{
    public class CSR
    {
        private Dictionary<string, string> paramMap = new Dictionary<string, string>();

        public CSR()
        {

        }
        
        public void Set(string name, string value) => paramMap.Add(name, value);
        
        public string Get(string name) => paramMap[name];

        public string[] ToStringArray()
        {
            int cnt = paramMap.Count;
            String[] arr = new String[cnt * 2];

            int pos = 0;
            foreach (var key in paramMap.Keys)
            {
                arr[pos++] = key;
                arr[pos++] = paramMap[key];
            }

            return arr;
        }

        public string GetCountry() => paramMap["C"];

        public void SetCountry(string country) => paramMap.Add("C", country);

        public string GetState() => paramMap["ST"];

        public void SetState(string state) => paramMap.Add("ST", state);

        public string GetLocality() => paramMap["L"];

        public void SetLocality(string locality) => paramMap.Add("L", locality);

        public string GetOrganization() => paramMap["O"];

        public void SetOrganization(string organization) => paramMap.Add("O", organization);

        public string GetOrganizationUnit() => paramMap["OU"];

        public void SetOrganizationUnit(string organization_unit) => paramMap.Add("OU", organization_unit);

        public string GetCommonName() => paramMap["CN"];

        public void SetCommonName(string common_name) => paramMap.Add("CN", common_name);

        public string GetSurname() => paramMap["SN"];

        public void SetSurname(string surname) => paramMap.Add("SN", surname);

        public string GetGivenName() => paramMap["GN"];

        public void SetGivenName(string given_name) => paramMap.Add("GN", given_name);

        public string GetEmailAddress() => paramMap["emailAddress"];

        public void SetEmailAddress(string emailAddress) => paramMap.Add("emailAddress", emailAddress);

        public string GetUniqueIdentifier() => paramMap["UID"];

        public void SetUniqueIdentifier(string uniqueIdentifier) => paramMap.Add("UID", uniqueIdentifier);

        public void SetData(string data) => this.Set("description", data);

        public void Submit()
        {
            int rc = SeaCatClient.GetReactor().Bridge.csrgen_worker(this.ToStringArray());
            RC.CheckAndThrowIOException("seacatcc.csrgen_worker", rc);
        }

        public static Task CreateDefault()
        {
            return new Task(() =>
            {
                CSR csr = new CSR();

                try
                {
                    csr.Submit();
                }
                catch (IOException e)
                {
                    Logger.Error("CSR", $"Exception in CSR.createDefault: {e.Message}");
                }
            });
        }
    }
}
