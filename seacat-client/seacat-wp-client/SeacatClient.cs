using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using seacat_core_bridge;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Reflection;

namespace seacat_wp_client
{

    public class SeacatClient
    {

        private static SeacatClient _instance;

        private SeacatClient()
        {

        }

        public static SeacatClient Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SeacatClient();
                }

                return _instance;
            }
        }

        public SeacatBridge Bridge
        {
            get;private set;
        }
        
        private CoreAPI coreAPI;
  
        public void Init()
        {
            coreAPI = new CoreAPI();
            Bridge = new SeacatBridge();

            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            Package package = Package.Current;

            Bridge.init((ISeacatCoreAPI)coreAPI, package.Id.Name, "dev", "WM8",
                local.Path + "\\core"); // subdir must be specified since the core api adds a suffix to it
        }
    }
}
