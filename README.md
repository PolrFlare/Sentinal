# Sentinal [Stat Tracker for Hive AND MCCI]

**Sentinal** is a Windows desktop tool (WPF + WebView2) that tracks Hive Bedrock players in your SkyWars lobby and fetches their public stats in real time. It helps you quickly see player performance, KDR, win rate, level, and other statistics.

---

## Features

- Detects all players in your **Minecraft Bedrock Edition (MCPE) 1.21.94-1.21.113 for Windows)** game lobby.
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

![Splash Screen](https://raw.githubusercontent.com/PolrFlare/Sentinal/refs/heads/main/images/image1.png)  
![Main UI](https://raw.githubusercontent.com/PolrFlare/Sentinal/refs/heads/main/images/image2.png)

---

## Installation

1. Clone the repository:  
```bash
git clone https://github.com/PolrFlare/Sentinal.git
```
2. **Open `Sentinal.sln` in Visual Studio 2022 or later.**

3. **Restore NuGet packages**  
   Visual Studio should prompt automatically to restore missing packages.

4. **Set build configuration to Release and platform to x64**  
   In Visual Studio toolbar dropdowns:  
   - **Solution Configuration:** `Release`  
   - **Solution Platform:** `x64`

5. **Build the solution.**

6. **Run the generated executable**  
   Navigate to the `bin\Release\x64` folder and run the executable.

---

### Usage

1. Launch **Minecraft Bedrock Edition 1.21.94-1.21.113 (Windows Edition)** and join a **SkyWars lobby**.  
2. Open **Sentinal**.  
3. Wait for the splash screen to disappear — the main UI will appear.  
4. Press **Scan** to fetch the player stats.  
   - The app will also scan automatically every **2 minutes**.  
5. View player statistics in the table:  
   - **Lvl** — Player SkyWars level  
   - **Name** — Username (colored by rank)  
   - **KDR** — Kill/Death ratio  
   - **Win Rate** — Percentage of victories  
   - **Kills** — Total kills  
   - **Wins** — Total wins  
   - **Games Played** — Total SkyWars games played  

---

### How it Works

- **Memory scan:** Reads Minecraft Bedrock Edition process memory to detect players in your lobby.  
- **Hive API:** Requests stats from `https://api.playhive.com/v0/game/all/all/{username}` for each detected player.  
- **Color coding:** Levels, KDR, Win Rate, kills, and wins are displayed in gradient colors for quick visual reference.  
- **WebView2 UI:** Renders a dynamic HTML table with live data updates.
