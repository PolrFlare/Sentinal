using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text.Json;

namespace HiveSentinal
{
    public partial class MainWindow : SourceChord.FluentWPF.AcrylicWindow
    {
        private ClientInfo clientInfo = new ClientInfo();
        private DispatcherTimer checkMCProcess;
        private bool isScanning = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            checkMinecraftProcess();
        }

        private async void RenderHivePlayerData()
        {
            if (isScanning) return;
            isScanning = true;
            try
            {
                List<string> players = null;
                try
                {
                    players = await Task.Run(async () =>
                    {
                        var scanner = new MinecraftWorldReader();
                        return await scanner.GetPlayerListAsync();
                    });
                }
                catch (Exception ex)
                {
                    return;
                }

                if (players == null || players.Count == 0)
                {
                    //Console.WriteLine("No players found in world scan.");
                    return;
                }

                players = players.Where(p => !string.IsNullOrWhiteSpace(p) && p != "(unreadable)").ToList();
                if (players.Count <= 1)
                {
                    return;
                }

                //Console.WriteLine($"Found {players.Count} readable players. Fetching Hive stats...\n");

                appUI.CoreWebView2?.ExecuteScriptAsync("clearPlayerTable();");

                using (var client = new HttpClient())
                {
                    bool clearedSkeletons = false;

                    foreach (var player in players)
                    {
                        string url = $"https://api.playhive.com/v0/game/all/all/{player}";
                        try
                        {
                            string response = await client.GetStringAsync(url);
                            var json = JsonSerializer.Deserialize<JsonElement>(response);

                            if (!json.TryGetProperty("main", out JsonElement main)) continue;
                            if (!json.TryGetProperty("sky", out JsonElement sky)) continue;

                            if (!clearedSkeletons)
                            {
                                appUI.CoreWebView2?.ExecuteScriptAsync("revertScanButton();");
                                clearedSkeletons = true;
                            }

                            string username = main.TryGetProperty("username_cc", out JsonElement uEl) ? uEl.GetString() ?? "-" : "-";
                            string rank = main.TryGetProperty("rank", out JsonElement rEl) ? rEl.GetString() ?? "-" : "-";
                            string rankColor = GetRankColor(rank);

                            int totalXP = sky.TryGetProperty("xp", out JsonElement xpEl) ? xpEl.GetInt32() : 0;
                            int played = sky.TryGetProperty("played", out JsonElement playedEl) ? playedEl.GetInt32() : 0;
                            int victories = sky.TryGetProperty("victories", out JsonElement vicEl) ? vicEl.GetInt32() : 0;
                            int kills = sky.TryGetProperty("kills", out JsonElement killsEl) ? killsEl.GetInt32() : 0;
                            int deaths = sky.TryGetProperty("deaths", out JsonElement deathsEl) ? deathsEl.GetInt32() : 0;

                            double kdr = deaths > 0 ? (double)kills / deaths : kills;
                            double winRate = played > 0 ? (double)victories / played * 100 : 0;
                            int level = GetLevelFromTotalXP(totalXP);
                            string levelColor = GetLevelColor(level);

                            string killsColor = GetPurpleGradient(kills);
                            string winsColor = GetPurpleGradient(victories);
                            string kdrColor = GetKDRColor(kdr);
                            string winRateColor = GetWinRateColor(winRate);

                            string js = $@"
                            (function(){{
                                const tbody = document.querySelector('#player-list tbody');
                                // remove skeletons & restore scan button once real data starts coming in

                                const row = document.createElement('tr');
                                row.innerHTML = `
                                    <td style='color:{levelColor}'>{level}</td>
                                    <td style='color:{rankColor}'>{username}</td>
                                    <td style='color:{kdrColor}'>{kdr:F2}</td>
                                    <td style='color:{winRateColor}'>{winRate:F2}%</td>
                                    <td style='color:{killsColor}'>{kills}</td>
                                    <td style='color:{winsColor}'>{victories}</td>
                                    <td>{played}</td>
                                `;
                                tbody.appendChild(row);
                                updateTableHeaderOpacity?.();
                            }})();";

                            appUI.CoreWebView2?.ExecuteScriptAsync(js);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error fetching stats for {player}: {ex.Message}");
                        }

                        await Task.Delay(250); // for rate-limiting
                    }
                }
            }
            finally
            {
                isScanning = false;
                appUI.CoreWebView2?.ExecuteScriptAsync("stopSkeletonLoading();");
            }
        }

        private static int GetLevelFromTotalXP(int totalXP)
        {
            int[] levelThresholds = new int[]
            {
            0,150,300,450,600,750,900,1050,1200,1350,
            1500,1650,1800,1950,2100,2250,2400,2550,2700,2850,
            3000,3150,3300,3450,3600,3750,3900,4050,4200,4350,
            4500,4650,4800,4950,5100,5250,5400,5550,5700,5850,
            6000,6150,6300,6450,6600,6750,6900,7050,7200,7350
            };

            int cumulative = 0;
            int level = 1;

            for (int i = 1; i <= 50; i++)
            {
                cumulative += levelThresholds[i - 1];
                if (totalXP < cumulative)
                    return i;
            }

            int extraXP = totalXP - cumulative;
            int extraLevels = extraXP / 7650;

            level = 50 + extraLevels;
            return Math.Min(level, 100);
        }

        /// <summary>Get rank color</summary>
        private string GetRankColor(string rank)
        {
            switch (rank)
            {
                case "PLUS": return "#52fc03";
                case "ULTIMATE": return "#b103fc";
                case "YOUTUBER": return "#c20000";
                default: return "grey";
            }
        }

        /// <summary>Return a linear purple shade based on count</summary>
        private string GetPurpleGradient(int value)
        {
            int max = 100000;
            value = Math.Min(value, max);

            // Light purple (#e6ccff) → dark purple (#330066)
            int rStart = 230, gStart = 204, bStart = 255;
            int rEnd = 51, gEnd = 0, bEnd = 102;

            double t = Math.Pow((double)value / max, 0.7); // gamma correction for smoother curve

            int r = rStart + (int)((rEnd - rStart) * t);
            int g = gStart + (int)((gEnd - gStart) * t);
            int b = bStart + (int)((bEnd - bStart) * t);

            return $"rgb({r},{g},{b})";
        }

        /// <summary>Return a color based on level</summary>
        private string GetLevelColor(int level)
        {
            if (level >= 100) return "blue";
            if (level >= 80) return "red";
            if (level >= 50) return "#40ff00"; // green
            if (level >= 30) return "magenta";
            if (level >= 25) return "aqua";
            if (level >= 20) return "yellow";
            if (level >= 10) return "gold";
            return "grey";
        }

        /// <summary>Return a color based on KDR value</summary>
        private string GetKDRColor(double kdr)
        {
            if (kdr < 0.5) return "#f4ff94";
            if (kdr < 1) return "#eeff00";
            if (kdr < 2) return "#ffe100";
            if (kdr < 4) return "#ffc400";
            if (kdr < 6) return "#ff4621";
            return "#c40808"; // 6+
        }

        /// <summary>Return a color based on WinRate value</summary>
        private string GetWinRateColor(double winRate)
        {
            if (winRate < 10) return "#f4ff94";
            if (winRate < 20) return "#eeff00";
            if (winRate < 40) return "#ffe100";
            if (winRate < 65) return "#ff4621";
            if (winRate < 80) return "#ff4621";
            return "#c40808"; // 80+
        }

        private async Task InitializeWebView()
        {
            try
            {
                await appUI.EnsureCoreWebView2Async();

                string localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "index.html");
                appUI.Source = new Uri(localPath);

                appUI.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    SendClientInfoToWebView();
                    await Task.Delay(5000);
                    RenderHivePlayerData();

                    var scanTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(120)
                    };
                    scanTimer.Tick += (sender, args) => RenderHivePlayerData();
                    scanTimer.Start();
                };

                appUI.CoreWebView2.WebMessageReceived += (sender, e) =>
                {
                    string message = e.WebMessageAsJson;
                    var jsonDoc = JsonDocument.Parse(message);
                    if (jsonDoc.RootElement.TryGetProperty("action", out var action) && action.GetString() == "scan")
                    {
                        Console.WriteLine("scanning...");
                        RenderHivePlayerData();
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void checkMinecraftProcess()
        {
            checkMCProcess = new DispatcherTimer();
            checkMCProcess.Interval = TimeSpan.FromSeconds(7);
            checkMCProcess.Tick += (s, e) =>
            {
                SendClientInfoToWebView();
            };
            checkMCProcess.Start();
        }

        private void SendClientInfoToWebView()
        {
            if (appUI.CoreWebView2 == null)
                return;

            string gamertag = clientInfo.GetXboxGamertag() ?? "#gamertag";
            string version = clientInfo.GetMinecraftVersion() ?? "Minecraft process not found";

            string js = $@"
                document.getElementById('gamertag').textContent = '{gamertag}';
                document.getElementById('version').textContent = 'Minecraft Version: {version}';
            ";

            appUI.CoreWebView2.ExecuteScriptAsync(js);
        }
    }
}