using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiveSentinal
{
    internal class ClientInfo
    {
        /// <summary>
        /// Gets the currently logged-in Xbox gamertag from the registry.
        /// </summary>
        public string GetXboxGamertag()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\XboxLive"))
                {
                    if (key != null)
                        return key.GetValue("Gamertag")?.ToString();
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }
        /// <summary>
        /// Gets the version of Minecraft Windows Edition if it's running.
        /// </summary>
        public string GetMinecraftVersion()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("Minecraft.Windows");
                if (procs.Length == 0) return "(Minecraft process not found)";

                string exePath = procs[0].MainModule.FileName;
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.ProductVersion;
            }
            catch (Exception ex)
            {
                return $"Error retrieving version: {ex.Message}";
            }
        }
    }
}

