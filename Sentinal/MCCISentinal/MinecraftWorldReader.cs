using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MCCISentinal
{
    internal class MinecraftWorldReader
    {
        private readonly string playerListFile;

        public MinecraftWorldReader(string minecraftFolder)
        {
            playerListFile = Path.Combine(minecraftFolder, "playerlist.txt");
        }

        /// <summary>
        /// Reads the playerlist.txt and returns all real player names,
        /// ignoring MCCTabPlayer#, MCC_NPC#, and header lines.
        /// </summary>
        public List<string> GetPlayers()
        {
            var players = new List<string>();

            if (!File.Exists(playerListFile))
                return players;

            var lines = File.ReadAllLines(playerListFile);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith("MCCTabPlayer#")) continue;
                if (trimmed.StartsWith("MCC_NPC#")) continue;
                if (trimmed == "=== Player List ===") continue;

                players.Add(trimmed);
            }

            return players;
        }
    }
}
