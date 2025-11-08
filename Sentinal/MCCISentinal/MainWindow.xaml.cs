using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

namespace MCCISentinal
{
    public partial class MainWindow : SourceChord.FluentWPF.AcrylicWindow
    {
        private string apiKey = string.Empty;
        private readonly string apiKeyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_key.txt");
        private DispatcherTimer playerListTimer;
        private MinecraftWorldReader worldReader;
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
            AddSentinalUtils.Run();

            if (System.IO.File.Exists(apiKeyFile))
            {
                apiKey = System.IO.File.ReadAllText(apiKeyFile).Trim();
            }
            StartPlayerListScan();
        }

        private void StartPlayerListScan()
        {
            string minecraftFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            worldReader = new MinecraftWorldReader(minecraftFolder);
            UpdatePlayerList();
            playerListTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(120)
            };
            playerListTimer.Tick += (s, e) => UpdatePlayerList();
            playerListTimer.Start();
        }

        private void UpdatePlayerList()
        {
            if (worldReader == null) return;

            var players = worldReader.GetPlayers();
            Console.WriteLine($"Found {players.Count} real players:");
            foreach (var p in players)
            {
                Console.WriteLine(p);
            }

            RenderMCCIPlayerData(players);
        }

        private async void RenderMCCIPlayerData(List<string> players)
        {
            if (isScanning) return;
            isScanning = true;

            try
            {
                if (players == null || players.Count == 0) return;

                appUI.CoreWebView2?.ExecuteScriptAsync("clearPlayerTable();");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                    foreach (var player in players)
                    {
                        string query = $@"{{""query"":""query {{
                        playerByUsername(username: \""{player}\"") {{
                            username
                            ranks
                            statistics {{
                                gamesPlayed: rotationValue(statisticKey: \""sky_battle_quads_games_played\"", rotation: LIFETIME)
                                playersKilled: rotationValue(statisticKey: \""sky_battle_quads_players_killed\"", rotation: LIFETIME)
                                skyLord: rotationValue(statisticKey: \""sky_battle_quads_sky_lord\"", rotation: LIFETIME)
                                blocksPlaced: rotationValue(statisticKey: \""sky_battle_quads_blocks_placed\"", rotation: LIFETIME)
                                totalScore: rotationValue(statisticKey: \""sky_battle_quads_total_score_earned\"", rotation: LIFETIME)
                                teamPlacement1: rotationValue(statisticKey: \""sky_battle_quads_team_placement_1\"", rotation: LIFETIME)
                                survivalFirst: rotationValue(statisticKey: \""sky_battle_quads_survival_first_place\"", rotation: LIFETIME)
                            }}
                        }}
                    }}""}}";

                        try
                        {
                            var content = new StringContent(query, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync("https://api.mccisland.net/graphql", content);
                            string json = await response.Content.ReadAsStringAsync();

                            var doc = JsonDocument.Parse(json);

                            if (!doc.RootElement.TryGetProperty("data", out var dataEl) ||
                                !dataEl.TryGetProperty("playerByUsername", out var playerData) ||
                                playerData.ValueKind == JsonValueKind.Null)
                            {
                                Console.WriteLine($"No data for {player}, skipping.");
                                continue;
                            }

                            string username = playerData.TryGetProperty("username", out var uname) ? uname.GetString() ?? player : player;

                            // ranks bub
                            string rank = "";
                            if (playerData.TryGetProperty("ranks", out var ranksEl) && ranksEl.ValueKind == JsonValueKind.Array)
                            {
                                var firstRank = ranksEl.EnumerateArray().FirstOrDefault();
                                rank = firstRank.ValueKind == JsonValueKind.String ? firstRank.GetString() ?? "" : "";
                            }

                            // big stuff bub
                            int gamesPlayed = 0, kills = 0, skyLord = 0, blocksPlaced = 0, score = 0, wins = 0, survivalFirst = 0;
                            if (playerData.TryGetProperty("statistics", out var statsEl) && statsEl.ValueKind == JsonValueKind.Object)
                            {
                                gamesPlayed = statsEl.TryGetProperty("gamesPlayed", out var gp) ? gp.GetInt32() : 0;
                                kills = statsEl.TryGetProperty("playersKilled", out var k) ? k.GetInt32() : 0;
                                skyLord = statsEl.TryGetProperty("skyLord", out var sl) ? sl.GetInt32() : 0;
                                blocksPlaced = statsEl.TryGetProperty("blocksPlaced", out var bp) ? bp.GetInt32() : 0;
                                score = statsEl.TryGetProperty("totalScore", out var s) ? s.GetInt32() : 0;
                                wins = statsEl.TryGetProperty("teamPlacement1", out var w) ? w.GetInt32() : 0;
                                survivalFirst = statsEl.TryGetProperty("survivalFirst", out var sf) ? sf.GetInt32() : 0;
                            }

                            int deaths = Math.Max(0, gamesPlayed - survivalFirst); // personal deaths
                            double kdr = deaths > 0 ? (double)kills / deaths : kills;

                            double wlr = (gamesPlayed - wins) > 0 ? (double)wins / (gamesPlayed - wins) : wins;
                            string formattedScore = FormatNumberAbbreviated(score);

                            string kdrText = FormatStatValue(kdr);
                            string wlrText = FormatStatValue(wlr);
                            string skyLordText = FormatIntStat(skyLord);
                            string killsText = FormatIntStat(kills);
                            string winsText = FormatIntStat(wins);
                            string blocksText = FormatIntStat(blocksPlaced);
       
                            string kdrColor = GetTieredColor(kdr, new double[] { 0.5, 1, 2, 4 });    
                            string wlrColor = GetTieredColor(wlr, new double[] { 0.25, 0.5, 1, 2 });
                            string skyLordColor = GetTieredColor(skyLord, new double[] { 10, 25, 50, 100 });

                            string scoreColor = GetGradientColor(score, 1200000, "#0c3600", "#39ff00");
                            string nameColor = GetRankColor(rank);
                            string killsColor = GetGradientColor(kills, 175000, "#ff00f2", "#7700ff");
                            string winsColor = GetGradientColor(wins, 1500, "#ff00f2", "#7700ff");
                            string blocksColor = GetGradientColor(wins, 1500, "#a39999", "#a30000");

                            string js = $@"
                            (function(){{
                                const tbody = document.querySelector('#player-list tbody');
                                const row = document.createElement('tr');
                                row.innerHTML = `
                                    <td style='color:{scoreColor}'>{formattedScore}</td>
                                    <td style='color:{nameColor}'>{username}</td>
                                    <td style='color:{kdrColor}'>{kdrText}</td>
                                    <td style='color:{wlrColor}'>{wlrText}</td>
                                    <td style='color:{skyLordColor}'>{skyLordText}</td>
                                    <td style='color:{killsColor}'>{killsText}</td>
                                    <td style='color:{winsColor}'>{winsText}</td>
                                    <td style='color:{blocksColor}'>{blocksText}</td>
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

                        await Task.Delay(250); // for rate limitz
                    }
                }
            }
            finally
            {
                isScanning = false;
                appUI.CoreWebView2?.ExecuteScriptAsync("stopSkeletonLoading();");
            }
        }

        private static readonly Random rand = new Random();
        private string FormatStatValue(double value, string format = "F2")
        {
            if (value == 0)
                return rand.NextDouble() < 0.5 ? "?" : "0";

            return value % 1 == 0 ? ((int)value).ToString() : value.ToString(format);
        }

        private string FormatIntStat(int value)
        {
            if (value == 0)
            {
                return new Random().Next(2) == 0 ? "?" : "0";
            }

            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string FormatNumberAbbreviated(int value)
        {
            if (value >= 1_000_000)
                return $"{value / 1_000_000.0:F1}M+";
            if (value >= 1_000)
                return $"{value / 1_000.0:F1}K+";
            return value.ToString();
        }

        private string GetGradientColor(double value, double max, string startColor, string endColor)
        {
            value = Math.Min(value, max);
            double t = Math.Pow(value / max, 0.7);

            Color start = (Color)ColorConverter.ConvertFromString(startColor);
            Color end = (Color)ColorConverter.ConvertFromString(endColor);

            int r = (int)(start.R + (end.R - start.R) * t);
            int g = (int)(start.G + (end.G - start.G) * t);
            int b = (int)(start.B + (end.B - start.B) * t);

            return $"rgb({r},{g},{b})";
        }

        // a bit of cubelify inspiration
        private string GetTieredColor(double value, double[] thresholds)
        {
            string[] tierColors = {
                "#F3F352", // 0
                "#ED9E01", // 1
                "#F15151", // 2
                "#AA0000", // 3
                "#FF55FF"  // 4 (max)
            };

            for (int i = thresholds.Length - 1; i >= 0; i--)
            {
                if (value >= thresholds[i])
                    return tierColors[Math.Min(i + 1, tierColors.Length - 1)];
            }

            return tierColors[0];
        }

        private string GetRankColor(string rank)
        {
            switch (rank)
            {
                case "CHAMP": return "#027aeb";
                case "GRAND_CHAMP": return "#007ecc";
                case "GRAND_CHAMP_ROYALE": return "#cc5200";
                case "GRAND_CHAMP_SUPREME": return "#020bf5";
                case "CREATOR": return "#ff00c3";
                case "CONTESTANT": return "#eb73ce";
                case "MODERATOR": return "#8307db";
                case "NOXCREW": return "#cc0000";
                default: return "grey";
            }
        }


        private async Task InitializeWebView()
        {
            await appUI.EnsureCoreWebView2Async();

            string localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "index.html");
            appUI.Source = new Uri(localPath);

            appUI.CoreWebView2.WebMessageReceived += async (sender, e) =>
            {
                string message = e.WebMessageAsJson;
                var jsonDoc = JsonDocument.Parse(message);

                if (jsonDoc.RootElement.TryGetProperty("action", out var action))
                {
                    string actionStr = action.GetString();

                    if (actionStr == "saveApiKey")
                    {
                        if (jsonDoc.RootElement.TryGetProperty("key", out var keyProp))
                        {
                            apiKey = keyProp.GetString();
                            System.IO.File.WriteAllText(apiKeyFile, apiKey);
                            Console.WriteLine($"API Key saved: {apiKey}");
                        }
                    }
                    else if (actionStr == "scan")
                    {
                        Console.WriteLine("Manual scan triggered from WebView.");

                        if (isScanning)
                        {
                            Console.WriteLine("Scan already running — ignoring duplicate.");
                            return;
                        }

                        UpdatePlayerList();
                        await StartScanCooldownAsync();
                    }
                }
            };

            appUI.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (System.IO.File.Exists(apiKeyFile))
                {
                    apiKey = System.IO.File.ReadAllText(apiKeyFile).Trim();
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        string js = $@"
                        const input = document.getElementById('api-key-input');
                        if(input) input.value = '{apiKey}';
                    ";
                        appUI.CoreWebView2.ExecuteScriptAsync(js);
                    }
                }
            };
        }

        private async Task StartScanCooldownAsync()
        {
            try
            {
                appUI.CoreWebView2?.ExecuteScriptAsync("scanBtn.disabled = true; scanBtn.innerText = 'Cooldown...';");

                for (int i = 20; i >= 1; i--)
                {
                    appUI.CoreWebView2?.ExecuteScriptAsync($"scanBtn.innerText = 'Cooldown [{i}]...';");
                    await Task.Delay(1000);
                }
            }
            catch { }

            appUI.CoreWebView2?.ExecuteScriptAsync("revertScanButton();");
        }

        private void SendClientInfoToWebView()
        {
            if (appUI.CoreWebView2 == null)
                return;

            //string gamertag = clientInfo.GetXboxGamertag() ?? "#gamertag";
            //string version = clientInfo.GetMinecraftVersion() ?? "Minecraft process not found";

            //string js = $@"
            //    document.getElementById('gamertag').textContent = '{gamertag}';
            //    document.getElementById('version').textContent = 'Minecraft Version: {version}';
            //";

            //appUI.CoreWebView2.ExecuteScriptAsync(js);
        }
    }
}
