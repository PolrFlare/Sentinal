// Fade out splash screen and show main screen
window.addEventListener('load', () => {
    setTimeout(() => {
        const splash = document.getElementById('splash');
        splash.style.opacity = '0';

        setTimeout(() => {
            splash.style.display = 'none';
            document.getElementById('main-screen').style.display = 'flex';
        }, 1200); // match the CSS transition
    }, 500);
});

window.chrome?.webview?.addEventListener('message', event => {
    console.log("Received from C#: ", event.data);
});

function clearPlayerTable() {
    const tbody = document.querySelector('#player-list tbody');
    tbody.innerHTML = '';
    updateTableHeaderOpacity();
}

const scanBtn = document.getElementById('scan-btn');
const tbody = document.querySelector('#player-list tbody');
const thead = document.querySelector('#player-list thead');

function updateTableHeaderOpacity() {
    if (tbody.children.length === 0) {
        thead.style.opacity = '0.085';
    } else {
        thead.style.opacity = '1';
    }
}

function stopSkeletonLoading() {
    const tbody = document.querySelector('#player-list tbody');
    const skeletons = tbody.querySelectorAll('.skeleton-row');

    if (skeletons.length > 0) {
        // remove all skeletons
        skeletons.forEach(row => row.remove());

        // re-enable scan button
        scanBtn.disabled = false;
        scanBtn.innerText = 'Scan';
    }

    updateTableHeaderOpacity();
}

function revertScanButton() {
    scanBtn.disabled = false;
    scanBtn.innerText = 'Scan';
}

scanBtn.addEventListener('click', () => {
    // 1️⃣ Clear old rows
    tbody.innerHTML = '';

    // 2️⃣ Add skeleton rows to simulate loading
    for (let i = 0; i < 8; i++) {
        const tr = document.createElement('tr');
        tr.classList.add('skeleton-row');
        tr.innerHTML = `
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
            <td><span class="skeleton-bar"></span></td>
        `;
        tbody.appendChild(tr);
    }

    // 3️⃣ Change button text while scanning
    scanBtn.disabled = true;
    const oldText = scanBtn.innerText;
    scanBtn.innerText = 'Scanning...';

    // 4️⃣ Update header opacity to show loading state
    updateTableHeaderOpacity();

    // 5️⃣ Send message to C#
    window.chrome?.webview?.postMessage({ action: 'scan' });
});

// 🧠 MutationObserver: Automatically update header opacity if table content changes
const observer = new MutationObserver(() => updateTableHeaderOpacity());
observer.observe(tbody, { childList: true });
// Run once on load
window.addEventListener('load', updateTableHeaderOpacity);

// Example: populate player list
// const players = [
//     {Lvl: 45, Name: "PolrFlar3", KDR: 2.1, WinRate: "56%", Kills: 150, Wins: 85, GamesPlayed: 120},
//     {Lvl: 30, Name: "Player2", KDR: 1.5, WinRate: "40%", Kills: 60, Wins: 24, GamesPlayed: 60},
//     {Lvl: 50, Name: "GamerX", KDR: 3.2, WinRate: "70%", Kills: 300, Wins: 210, GamesPlayed: 300},
//     {Lvl: 22, Name: "NoobMaster", KDR: 0.8, WinRate: "20%", Kills: 15, Wins: 3, GamesPlayed: 20},
//     {Lvl: 37, Name: "AceHunter", KDR: 2.5, WinRate: "60%", Kills: 200, Wins: 120, GamesPlayed: 180},
//     {Lvl: 41, Name: "Shadow", KDR: 1.9, WinRate: "50%", Kills: 180, Wins: 90, GamesPlayed: 150},
//     {Lvl: 28, Name: "QuickShot", KDR: 1.2, WinRate: "35%", Kills: 50, Wins: 18, GamesPlayed: 60},
//     {Lvl: 33, Name: "Blaze", KDR: 1.8, WinRate: "45%", Kills: 100, Wins: 45, GamesPlayed: 100},
//     {Lvl: 47, Name: "Titan", KDR: 2.8, WinRate: "65%", Kills: 250, Wins: 160, GamesPlayed: 245},
//     {Lvl: 25, Name: "Rookie", KDR: 1.0, WinRate: "30%", Kills: 30, Wins: 9, GamesPlayed: 30}
// ];

// //const tbody = document.querySelector("#player-list tbody");

// players.forEach(p => {
//     const tr = document.createElement("tr");
//     tr.innerHTML = `
//         <td>${p.Lvl}</td>
//         <td>${p.Name}</td>
//         <td>${p.KDR}</td>
//         <td>${p.WinRate}</td>
//         <td>${p.Kills}</td>
//         <td>${p.Wins}</td>
//         <td>${p.GamesPlayed}</td>
//     `;
//     tbody.appendChild(tr);
// });