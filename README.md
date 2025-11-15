# Sentinal [Stat Tracker for Hive AND MCCI]

**Sentinal** is a Windows desktop tool (WPF + WebView2) that tracks Hive Bedrock players in your SkyWars lobby and fetches their public stats in real time. It helps you quickly see player performance, KDR, win rate, level, and other statistics.

---

## Features

- Detects all players in your **Minecraft Bedrock Edition (1.21.94–1.21.114 for Windows)** lobby (Hive)  
- Detects all players in **Minecraft Java Edition 1.21+** (MCCI) SkyBattle games via a Fabric mod (`SentinalUtils.jar`)  
- Fetches stats directly from the APIs:  
  - **Hive:** `playhive.com`  
  - **MCCI:** `gateway.noxcrew.com`  
- Supports stats like:  
  - Level, KDR, WLR (for MCCI), kills, wins, SkyLord, blocks placed  
  - Player rank (Hive) and colored rank display  
- Color-coded stats for easier scanning  
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
![Splash Screen](https://raw.githubusercontent.com/PolrFlare/Sentinal/refs/heads/main/images/image4.png)
![Main UI](https://raw.githubusercontent.com/PolrFlare/Sentinal/refs/heads/main/images/image3.png)

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

### 🟡 Hive (Bedrock)
1. Launch **Minecraft Bedrock Edition 1.21.94–1.21.114 (Windows Edition)**.  
2. Join a **SkyWars** lobby.

### 🔵 MCCI (Java)
1. Launch **Minecraft Java Edition 1.21+**.  
2. Start a **SkyBattle** game with the **SentinalUtils.jar** Fabric mod installed.

### 🧭 Using Sentinal
1. Open **Sentinal**.  
2. Wait for the splash screen to disappear — the main UI will appear.  
3. Press **Scan** to fetch player stats.  
4. The app also performs **automatic scans every 2 minutes**.  
5. View player statistics in the table for **Hive SkyWars** or **MCCI SkyBattle**.

---

## 🟨 Hive SkyWars Stats (Bedrock)

| Column | Description |
|:-------|:-------------|
| **Lvl** | Player SkyWars level |
| **Name** | Username (colored by rank) |
| **KDR** | Kill/Death ratio |
| **Win Rate** | Percentage of victories |
| **Kills** | Total kills |
| **Wins** | Total wins |
| **Games Played** | Total SkyWars games played |

---

## 🟦 MCCI SkyBattle Stats (Java 1.21+)

| Column | Description |
|:-------|:-------------|
| **Score** | Total points earned |
| **Name** | Username |
| **KDR** | Kill/Death ratio |
| **WLR** | Win/Loss ratio (team-based) |
| **SkyLord** | SkyLord achievements |
| **Kills** | Total kills |
| **Wins** | Total wins |
| **Blocks Placed** | Total blocks placed |

---

## ⚙️ How It Works

### Memory Scan / Mod Integration
- **Hive:** Reads the **Minecraft Bedrock Edition** process memory to detect players in your lobby.  
- **MCCI:** Uses the **SentinalUtils.jar Fabric mod** to detect players in SkyBattle games.

### API Requests
- **Hive:** Requests stats from  
  `https://api.playhive.com/v0/game/all/all/{username}`  
  for each detected player.  
- **MCCI:** Sends detected players to the  
  `https://gateway.noxcrew.com/`  
  API for stats retrieval.

### Color Coding & WebView2 UI
- Levels, KDR, WLR, kills, wins, and scores are displayed in **gradient colors** for quick visual reference.  
- A **dynamic HTML table** updates in real time for both Hive and MCCI stats.

### Automatic Refresh
- Player stats are **updated automatically every 2 minutes**.

---

## 🔑 How to Get Your MCCI API Key

1. Go to **[MCCI Gateway](https://gateway.noxcrew.com/)**.  
2. Click **GO TO GATEWAY ACCOUNT** or sign in using your **Minecraft Java Edition** account.  
3. Navigate to the **Developer Section** in your account settings.  
4. Scroll down to **Developers** and create a new API key.  
5. Copy this API key into **Sentinal’s API key input**.

> ⚠️ **Note:** MCCI API keys refresh approximately every **4 days**, so you may need to update it periodically.
