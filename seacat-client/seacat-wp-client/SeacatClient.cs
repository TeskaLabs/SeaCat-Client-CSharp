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
        private CoreAPI coreAPI;
        private SeacatBridge seacatBridge;

        public void Init()
        {
            coreAPI = new CoreAPI();
            seacatBridge = new SeacatBridge();

            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            Package package = Package.Current;

            seacatBridge.init((ISeacatCoreAPI)coreAPI, package.Id.Name, "dev", "WM8",
                local.Path + "\\core"); // subdir must be specified since the core api adds a suffix to it
        }
    }
}
