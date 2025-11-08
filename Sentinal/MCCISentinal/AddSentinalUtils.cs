using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCCISentinal
{
    /// <summary>
    /// Utility class to install the SentinalUtils.jar mod into supported Minecraft clients.
    /// </summary>
    /// <remarks>
    /// SentinalUtils is a Fabric-based mod that requires the Fabric mod loader.
    /// Once installed, it enables scanning and data collection for all players in a world or server.
    /// 
    /// The installer automatically detects and copies the mod to:
    ///   - Feather Client (AppData\Roaming\.feather)
    ///   - Lunar Client (AppData\.lunarclient\profiles\lunar)
    ///   - Standard Minecraft Launcher (.minecraft\mods)
    /// 
    /// Only Fabric versions 1.21 and higher are targeted.
    /// </remarks>
    internal class AddSentinalUtils
    {
        public static void Run()
        {
            string modJarName = "SentinalUtils.jar";
            string modJarSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utils", modJarName);

            if (!File.Exists(modJarSource))
            {
                Console.WriteLine($"Error: {modJarName} not found in current directory!");
                return;
            }

            Console.WriteLine("=== Installing SentinalUtils ===");

            try { InstallFeather(modJarName, modJarSource); } catch (Exception ex) { Console.WriteLine($"[Feather Error] {ex.Message}"); }
            try { InstallLunar(modJarName, modJarSource); } catch (Exception ex) { Console.WriteLine($"[Lunar Error] {ex.Message}"); }
            try { InstallMinecraftLauncher(modJarName, modJarSource); } catch (Exception ex) { Console.WriteLine($"[Minecraft Error] {ex.Message}"); }

            Console.WriteLine("\n=== Setup complete! ===");
        }

        // ---------------- FEATHER ----------------
        private static void InstallFeather(string modJarName, string modJarSource)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string featherJsonPath = Path.Combine(userFolder, @"AppData\Roaming\.feather\mods\feather-mods.json");
            string userModsPath = Path.Combine(userFolder, @"AppData\Roaming\.feather\user-mods");

            if (!File.Exists(featherJsonPath))
            {
                Console.WriteLine("[Feather] feather-mods.json not found. Skipping...");
                return;
            }

            string jsonText = File.ReadAllText(featherJsonPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
            if (data == null || !data.ContainsKey("customAddedMods"))
            {
                Console.WriteLine("[Feather] customAddedMods not found in JSON!");
                return;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var customModsJson = data["customAddedMods"].ToString();
            var customMods = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(customModsJson);

            foreach (var version in customMods.Keys)
            {
                if (!version.StartsWith("1.21"))
                    continue;

                string versionFolder = Path.Combine(userModsPath, version);
                Directory.CreateDirectory(versionFolder);

                string destJarPath = Path.Combine(versionFolder, modJarName);
                if (!File.Exists(destJarPath))
                {
                    File.Copy(modJarSource, destJarPath, true);
                    Console.WriteLine($"[Feather] Copied {modJarName} to {versionFolder}");
                }

                if (!customMods[version].Contains(modJarName))
                {
                    customMods[version].Add(modJarName);
                    Console.WriteLine($"[Feather] Added {modJarName} to customAddedMods[{version}]");
                }
            }

            data["customAddedMods"] = customMods;
            string updatedJson = JsonSerializer.Serialize(data, options);
            File.WriteAllText(featherJsonPath, updatedJson);

            Console.WriteLine("[Feather] Setup complete for 1.21+ versions.");
        }

        // ---------------- LUNAR ----------------
        private static void InstallLunar(string modJarName, string modJarSource)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string lunarPath = Path.Combine(userFolder, @".lunarclient\profiles\lunar");

            if (!Directory.Exists(lunarPath))
            {
                Console.WriteLine("[Lunar] Path not found. Skipping...");
                return;
            }

            string[] versionFolders = Directory.GetDirectories(lunarPath);

            foreach (string versionDir in versionFolders)
            {
                string versionName = Path.GetFileName(versionDir);
                if (!IsVersionAtLeast(versionName, "1.21"))
                    continue;

                string modsPath = Path.Combine(versionDir, "mods");
                if (!Directory.Exists(modsPath))
                    continue;

                string[] fabricFolders = Directory.GetDirectories(modsPath, "fabric-*");
                foreach (string fabricFolder in fabricFolders)
                {
                    string fabricVersion = Path.GetFileName(fabricFolder);
                    if (!IsVersionAtLeast(fabricVersion.Replace("fabric-", ""), "1.21"))
                        continue;

                    string destJarPath = Path.Combine(fabricFolder, modJarName);
                    Directory.CreateDirectory(fabricFolder);

                    if (!File.Exists(destJarPath))
                    {
                        File.Copy(modJarSource, destJarPath, true);
                        Console.WriteLine($"[Lunar] Copied {modJarName} to {fabricFolder}");
                    }
                    else
                    {
                        Console.WriteLine($"[Lunar] {modJarName} already exists in {fabricFolder}");
                    }
                }
            }

            Console.WriteLine("[Lunar] Setup complete for fabric 1.21+ versions.");
        }

        // ---------------- MINECRAFT ----------------
        private static void InstallMinecraftLauncher(string modJarName, string modJarSource)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string mcPath = Path.Combine(userFolder, @"AppData\Roaming\.minecraft");
            string modsPath = Path.Combine(mcPath, "mods");

            if (!Directory.Exists(mcPath))
            {
                Console.WriteLine("[Minecraft] .minecraft folder not found. Skipping...");
                return;
            }

            Directory.CreateDirectory(modsPath);
            string destJarPath = Path.Combine(modsPath, modJarName);

            if (!File.Exists(destJarPath))
            {
                File.Copy(modJarSource, destJarPath, true);
                Console.WriteLine($"[Minecraft] Copied {modJarName} to {modsPath}");
            }
            else
            {
                Console.WriteLine($"[Minecraft] {modJarName} already exists in mods folder.");
            }

            Console.WriteLine("[Minecraft] Setup complete.");
        }

        // ---------------- UTILITY ----------------
        private static bool IsVersionAtLeast(string version, string minVersion)
        {
            try
            {
                string[] vParts = version.Split('.');
                string[] minParts = minVersion.Split('.');

                for (int i = 0; i < Math.Min(vParts.Length, minParts.Length); i++)
                {
                    if (!int.TryParse(vParts[i], out int v) || !int.TryParse(minParts[i], out int m))
                        return false;

                    if (v > m) return true;
                    if (v < m) return false;
                }
                return true;
            }
            catch { return false; }
        }
    }
}