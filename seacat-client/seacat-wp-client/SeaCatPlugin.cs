using seacat_wp_client.Core;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace seacat_wp_client
{
    public abstract class SeaCatPlugin
    {
        private static List<SeaCatPlugin> plugins = new List<SeaCatPlugin>();
        private static bool capabilitiesCommited = false;

        public static void CommitCapabilities()
        {
            var deviceInfo = new EasClientDeviceInformation();
            if (capabilitiesCommited) throw new Exception("SeaCat Capabilities are already comitted!");

            List<string> caps = new List<string>();

            foreach (var p in plugins)
            {
                var pcaps = p.GetCapabilities();
                if (pcaps == null || !pcaps.Any()) continue;

                foreach (var tuple in pcaps)
                {
                    caps.Add(String.Format("%s\037%s", tuple.Item1, tuple.Item2));
                }
            }

            // Add platform capabilities
            caps.Add(String.Format("%s\037%s", "plI", deviceInfo.Id));
            caps.Add(String.Format("%s\037%s", "plm", deviceInfo.SystemManufacturer));
            caps.Add(String.Format("%s\037%s", "plM", deviceInfo.SystemSku));
            caps.Add(String.Format("%s\037%s", "plp", deviceInfo.SystemProductName));

            // Add hardware capabilities
            caps.Add(String.Format("%s\037%s", "hwb", deviceInfo.SystemFirmwareVersion));
            caps.Add(String.Format("%s\037%s", "hwd", deviceInfo.SystemHardwareVersion));

            caps.Add(null);

            String[] caparr = new String[caps.Count];

            caparr = caps.ToArray<string>();

            int rc = Reactor.Instance.Bridge.capabilities_store(caparr);
            RC.CheckAndLogError("seacatcc.capabilities_store", rc);
            if (rc == 0) capabilitiesCommited = true;
        }

        ///

        public SeaCatPlugin()
        {
            plugins.Add(this);
        }

        public abstract List<Tuple<string, string>> GetCapabilities();

    }
}
