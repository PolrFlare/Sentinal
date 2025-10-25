# Sentinal [Stat Tracker for Hive]

**Sentinal** is a Windows desktop tool (WPF + WebView2) that tracks Hive Bedrock players in your SkyWars lobby and fetches their public stats in real time. It helps you quickly see player performance, KDR, win rate, level, and other statistics.

---

## Features

- Detects all players in your **Minecraft Bedrock Edition (MCPE) 1.21.x for Windows)** game lobby.
- Fetches stats directly from the Hive API (`playhive.com`) for:
  - SkyWars XP and level
  - Kills, deaths, KDR
  - Wins, games played, win rate
  - Player rank and colored rank display
- Color-coded stats for easier scanning:
  - Level, KDR, Win Rate, Kills, Wins
- Simple, interactive UI:
  - Splash screen with Hive logo
  - Main table dynamically populates with player data
  - Scan button for manual updates
- Automatic scanning every 2 minutes
- Lightweight and easy to run (no installation required beyond .NET runtime)

---

## Screenshots

![Splash Screen](Assets/splash_example.png)  
![Main UI](Assets/ui_example.png)

---

## Installation

1. Clone the repository:  
```bash
git clone https://github.com/yourusername/Sentinal-Hive-Tracker.git
