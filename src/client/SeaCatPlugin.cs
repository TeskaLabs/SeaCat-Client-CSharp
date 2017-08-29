using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace SeaCatCSharpClient {

    public abstract class SeaCatPlugin {

        private static List<SeaCatPlugin> plugins = new List<SeaCatPlugin>();
        private static bool capabilitiesCommited = false;

        public abstract List<Tuple<string, string>> GetCapabilities();

        public static void CommitCapabilities() {
            var deviceInfo = new EasClientDeviceInformation();
            if (capabilitiesCommited) throw new Exception("SeaCat Capabilities are already comitted!");

            List<string> caps = new List<string>();

            foreach (var p in plugins) {
                var pcaps = p.GetCapabilities();
                if (pcaps == null || !pcaps.Any()) continue;

                foreach (var tuple in pcaps) {
                    caps.Add($"{tuple.Item1}\037{tuple.Item2}");
                }
            }

            // Add platform capabilities
            //caps.Add(String.Format("%s\037%s", "plI", deviceInfo.Id)); throws NotImplementedException
            caps.Add($"plm\037{deviceInfo.SystemManufacturer}");
            caps.Add($"plM\037{deviceInfo.SystemSku}");
            caps.Add($"plp\037{deviceInfo.SystemProductName}");

            // Add hardware capabilities
            //caps.Add(String.Format("%s\037%s", "hwb", deviceInfo.SystemFirmwareVersion));
            //caps.Add(String.Format("%s\037%s", "hwd", deviceInfo.SystemHardwareVersion));

            String[] caparr = new String[caps.Count];
            caparr = caps.ToArray<string>();

            int rc = SeaCatClient.Reactor.Bridge.characteristics_store(caparr);
            RC.CheckAndLogError("seacatcc.capabilities_store", rc);
            if (rc == 0) capabilitiesCommited = true;
        }

        public SeaCatPlugin() {
            plugins.Add(this);
        }
    }
}
