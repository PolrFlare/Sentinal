using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HiveSentinal
{
    /// <summary>
    /// This tool scans Minecraft Windows Edition memory to find the list of players in a level(world).
    /// Since there is no external pointer to the player list, heuristic scans are used.
    /// The scan starts with the gamertag, then checks memory 0xC0 apart for other possible usernames.
    /// Player count pointer is used to avoid reading past the last player.
    /// Another heuristic: 40 bytes after each username is a 24-byte pattern with the last two bytes as 00 00, helping confirm the exact entry.
    /// As far as I know, this works process works for both 1.21.100 and 1.21.94, so I am assuming it'll work for other versions.
    /// </summary>
    class MinecraftWorldReader
    {
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_QUERY_INFORMATION = 0x0400;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, int size, out int bytesRead);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);
        static List<string> PlayerNames = new List<string>();

        static List<long> foundAddresses = new List<long>();

        // On version 1.21.94 scanning for the address takes 4x as long compared to 1.21.100
        // May just be my system, we'll see
        public static async Task ScanWorldAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            string targetString = GetXboxGamertag();
            if (string.IsNullOrEmpty(targetString))
                return;

            Process[] processes = Process.GetProcessesByName("Minecraft.Windows");
            if (processes.Length == 0)
                return;

            Process proc = processes[0];
            IntPtr handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, proc.Id);
            IntPtr moduleBase = proc.MainModule.BaseAddress;

            string version = GetMinecraftVersion(proc);

            long initialPlayerCountOffset = 0x8722DC8; // default
            if (!string.IsNullOrEmpty(version))
            {
                if (version.Contains("1.21.100"))
                    initialPlayerCountOffset = 0x901DEE8;
                else if (version.Contains("1.21.94"))
                    initialPlayerCountOffset = 0x8722DC8;
                else if (version.Contains("1.21.101"))
                    initialPlayerCountOffset = 0x901CEF8;
                else if (version.Contains("1.21.111"))
                    initialPlayerCountOffset = 0x95EC238;
                else if (version.Contains("1.21.113"))
                    initialPlayerCountOffset = 0x95EC228;
                else if (version.Contains("1.21.114"))
                    initialPlayerCountOffset = 0x95EC248;
            }

            int initialPlayerCount = ReadInt32AtOffset(handle, moduleBase, initialPlayerCountOffset);
            if (initialPlayerCount <= 1)
            {
                CloseHandle(handle);
                return;
            }

            var regions = new List<MEMORY_BASIC_INFORMATION>();
            IntPtr address = IntPtr.Zero;

            // only valid memory regions
            while (true)
            {
                if (VirtualQueryEx(handle, address, out MEMORY_BASIC_INFORMATION memInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    break;

                const uint MEM_COMMIT = 0x1000;
                const uint PAGE_READWRITE = 0x04;
                const uint PAGE_READONLY = 0x02;

                if ((memInfo.State & MEM_COMMIT) != 0 &&
                    (memInfo.Type == 0x20000 || memInfo.Type == 0x40000) &&
                    ((memInfo.Protect & PAGE_READWRITE) != 0 || (memInfo.Protect & PAGE_READONLY) != 0))
                {
                    regions.Add(memInfo);
                }

                address = new IntPtr(memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64());
            }

            int totalMatches = 0;
            object totalLock = new object();

            // we scan asynchronously
            var tasks = regions.Select(region => Task.Run(async () =>
            {
                long regionBase = region.BaseAddress.ToInt64();
                long regionSize = region.RegionSize.ToInt64();
                const int chunkSize = 0x10000; // 64KB chunks
                byte[] buffer = new byte[chunkSize];

                for (long offset = 0; offset < regionSize; offset += chunkSize)
                {
                    int bytesToRead = (int)Math.Min(chunkSize, regionSize - offset);
                    if (ReadProcessMemory(handle, new IntPtr(regionBase + offset), buffer, bytesToRead, out int bytesRead) && bytesRead > 0)
                    {
                        int matches = ScanBuffer(buffer, Encoding.ASCII.GetBytes(targetString), new IntPtr(regionBase + offset), handle);
                        matches += ScanBuffer(buffer, Encoding.Unicode.GetBytes(targetString), new IntPtr(regionBase + offset), handle);

                        if (matches > 0)
                        {
                            lock (totalLock)
                                totalMatches += matches;
                        }
                    }
                    await Task.Yield(); // let other tasks breathe
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            if (foundAddresses.Count > 0)
            {
                long lowestAddr = foundAddresses.Min();
                int playerCount = ReadInt32AtOffset(handle, moduleBase, initialPlayerCountOffset);
                if (playerCount > 1)
                    ReadPlayerListNames(handle, lowestAddr, playerCount);
            }

            sw.Stop();
            CloseHandle(handle);
        }

        static int ScanBuffer(byte[] buffer, byte[] targetBytes, IntPtr baseAddress, IntPtr handle)
        {
            int matches = 0;
            int step = 16; // only check addresses divisible by 0x10 (another pattern i saw)

            // Use a sliding window to scan
            for (int i = 0; i < buffer.Length - targetBytes.Length; i += step)
            {
                bool found = true;
                for (int j = 0; j < targetBytes.Length; j++)
                {
                    if (buffer[i + j] != targetBytes[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (!found) continue;

                int nextIndex = i + targetBytes.Length;
                bool validNext8 = true;
                for (int k = 0; k < 16; k++)
                {
                    int checkIndex = nextIndex + k;
                    if (checkIndex >= buffer.Length || !(buffer[checkIndex] == 0x00 || buffer[checkIndex] <= 0x20))
                    {
                        validNext8 = false;
                        break;
                    }
                }
                if (!validNext8) continue;

                long foundAddr = baseAddress.ToInt64() + i;
                string nextName = ReadPossibleUsername(handle, foundAddr + 0xC0);
                int placeholderCount = CountPlaceholders(handle, foundAddr + targetBytes.Length, 0xC0 - targetBytes.Length);

                if (IsLikelyUsername(nextName) && placeholderCount >= 120)
                {
                    long extraAddr = foundAddr + 0x28;
                    byte[] extraData = new byte[24];
                    if (ReadProcessMemory(handle, (IntPtr)extraAddr, extraData, extraData.Length, out int bytesReadExtra))
                    {
                        AnalyzeAndDisplayEntry(handle, foundAddr, nextName, placeholderCount, extraData, bytesReadExtra, ref matches);
                    }
                }
            }

            return matches;
        }
        static void ReadPlayerListNames(IntPtr handle, long baseAddr, int playerCount)
        {
            int entrySize = 0xC0;
            Console.WriteLine($"\nReading playerlist from 0x{baseAddr:X} ...\n");

            for (int i = 1; i < playerCount; i++)
            {
                long addr = baseAddr + (entrySize * i);
                string name = ReadPossibleUsername(handle, addr);
                if (!string.IsNullOrEmpty(name))
                    PlayerNames.Add(name);
            }
        }

        static int CountPlaceholders(IntPtr handle, long startAddr, int length)
        {
            byte[] buffer = new byte[length];
            if (ReadProcessMemory(handle, (IntPtr)startAddr, buffer, buffer.Length, out int bytesRead))
            {
                int count = 0;
                foreach (byte b in buffer) if (b == 0x00 || b <= 0x20) count++;
                return count;
            }
            return 0;
        }

        static string ReadPossibleUsername(IntPtr handle, long addr)
        {
            byte[] buffer = new byte[32];
            if (ReadProcessMemory(handle, (IntPtr)addr, buffer, buffer.Length, out int bytesRead))
                return ExtractPrintable(Encoding.ASCII.GetString(buffer));
            return string.Empty;
        }

        static string ExtractPrintable(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ') sb.Append(c);
                else break;
            }
            return sb.ToString().TrimEnd('\0');
        }

        static bool IsLikelyUsername(string s)
        {
            if (s.Length < 3 || s.Length > 16) return false;
            if (s.Contains("254")) return false;
            foreach (char c in s) if (!(char.IsLetterOrDigit(c) || c == ' ')) return false;
            if (s.StartsWith(" ") || s.EndsWith(" ") || s.Contains("  ")) return false;
            return true;
        }

        static void AnalyzeAndDisplayEntry(IntPtr handle, long foundAddr, string nextName, int placeholderCount, byte[] extraData, int bytesReadExtra, ref int matches)
        {
            int printableCount = 0, nonPrintableCount = 0, extendedCount = 0, skippedCount = 0;
            StringBuilder asciiBuilder = new StringBuilder();

            foreach (byte b in extraData)
            {
                if (b == 0x00) { skippedCount++; asciiBuilder.Append('.'); continue; }
                if (b >= 0x20 && b <= 0x7E) { asciiBuilder.Append((char)b); printableCount++; }
                else if (b >= 0x80) { asciiBuilder.Append('.'); extendedCount++; }
                else { asciiBuilder.Append('.'); nonPrintableCount++; }
            }

            if (skippedCount == 2)
            {
                string hexBytes = BitConverter.ToString(extraData).Replace("-", " ");
                //Console.WriteLine($"Playerlist entry found at 0x{foundAddr:X}");
                //Console.WriteLine($"    → Next username @ +0xC0: {nextName}");
                //Console.WriteLine($"    → Placeholder bytes between: {placeholderCount}");
                //Console.WriteLine($"    → +0x28 (next 24 bytes): {hexBytes}");
                //Console.WriteLine($"    → ASCII Output: {asciiBuilder}");
                //Console.WriteLine($"    → Printable: {printableCount}, Non-Printable: {nonPrintableCount}, Extended: {extendedCount}, Skipped(0x00): {skippedCount}\n");

                matches++;
                foundAddresses.Add(foundAddr);
            }
        }

        static int ReadInt32AtOffset(IntPtr handle, IntPtr moduleBase, long offset)
        {
            long address = moduleBase.ToInt64() + offset;
            byte[] buffer = new byte[4];

            if (ReadProcessMemory(handle, (IntPtr)address, buffer, buffer.Length, out int bytesRead) && bytesRead == 4)
                return BitConverter.ToInt32(buffer, 0);
            else { Console.WriteLine($"Failed to read int at 0x{address:X}"); return -1; }
        }

        static string GetXboxGamertag()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\XboxLive"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("Gamertag");
                        if (value != null) return value.ToString();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error reading registry: " + ex.Message); }
            return null;
        }

        static string GetMinecraftVersion(Process proc)
        {
            try
            {
                string exePath = proc.MainModule.FileName;
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.ProductVersion;
            }
            catch (Exception ex) { Console.WriteLine($"Error reading version: {ex.Message}"); return null; }
        }
        public async Task<List<string>> GetPlayerListAsync()
        {
            PlayerNames.Clear();
            await ScanWorldAsync();
            return new List<string>(PlayerNames);
        }

        private static MinecraftWorldReader _instance;
        public static MinecraftWorldReader Instance
        {
            get
            {
                if (_instance == null) _instance = new MinecraftWorldReader();
                return _instance;
            }
        }
    }
}

