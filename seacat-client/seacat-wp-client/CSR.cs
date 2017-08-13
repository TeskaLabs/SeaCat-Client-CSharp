using seacat_wp_client.Core;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client
{
    public class CSR
    {
        private Dictionary<String, String> paramMap = new Dictionary<String, String>();


        public CSR()
        {
        }


        public void Set(String name, String value)
        {
            paramMap.Add(name, value);
        }

        public String Get(String name)
        {
            return paramMap[name];
        }

        ///

        public String[] ToStringArray()
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

        ///

        public String GetCountry()
        {
            return paramMap["C"];
        }

        public void SetCountry(String country)
        {
            paramMap.Add("C", country);
        }


        public String GetState()
        {
            return paramMap["ST"];
        }

        public void SetState(String state)
        {
            paramMap.Add("ST", state);
        }


        public String GetLocality()
        {
            return paramMap["L"];
        }

        public void SetLocality(String locality)
        {
            paramMap.Add("L", locality);
        }


        public String GetOrganization()
        {
            return paramMap["O"];
        }

        public void SetOrganization(String organization)
        {
            paramMap.Add("O", organization);
        }


        public String GetOrganizationUnit()
        {
            return paramMap["OU"];
        }

        public void SetOrganizationUnit(String organization_unit)
        {
            paramMap.Add("OU", organization_unit);
        }


        public String GetCommonName()
        {
            return paramMap["CN"];
        }

        public void SetCommonName(String common_name)
        {
            paramMap.Add("CN", common_name);
        }


        public String GetSurname()
        {
            return paramMap["SN"];
        }

        public void SetSurname(String surname)
        {
            paramMap.Add("SN", surname);
        }


        public String GetGivenName()
        {
            return paramMap["GN"];
        }

        public void SetGivenName(String given_name)
        {
            paramMap.Add("GN", given_name);
        }


        public String GetEmailAddress()
        {
            return paramMap["emailAddress"];
        }

        public void SetEmailAddress(String emailAddress)
        {
            paramMap.Add("emailAddress", emailAddress);
        }



        public String GetUniqueIdentifier()
        {
            return paramMap["UID"];
        }

        public void SetUniqueIdentifier(String uniqueIdentifier)
        {
            paramMap.Add("UID", uniqueIdentifier);
        }


        ///

        public void SetData(String data)
        {
            this.Set("description", data);
        }

        /*
        public void SetJsonData(JSONObject jsonData)
        {
            this.setData(jsonData.toString());
        }*/

        ///

        public void Submit()
        {
            int rc = Reactor.Instance.Bridge.csrgen_worker(this.ToStringArray());
            RC.CheckAndThrowIOException("seacatcc.csrgen_worker", rc);
        }

        ///

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

                    System.Diagnostics.Debug.WriteLine(string.Format("Exception in CSR.createDefault: %s", e.Message));
                }
            });
        }

    }
}
