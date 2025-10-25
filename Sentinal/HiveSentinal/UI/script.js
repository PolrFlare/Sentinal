window.addEventListener('load', () => {
    setTimeout(() => {
        const splash = document.getElementById('splash');
        splash.style.opacity = '0';

        setTimeout(() => {
            splash.style.display = 'none';
            document.getElementById('main-screen').style.display = 'flex';
        }, 1200);
    }, 500);
});

window.chrome?.webview?.addEventListener('message', event => {
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
    tbody.innerHTML = '';

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

    scanBtn.disabled = true;
    const oldText = scanBtn.innerText;
    scanBtn.innerText = 'Scanning...';

    updateTableHeaderOpacity();
    window.chrome?.webview?.postMessage({ action: 'scan' });
});

const observer = new MutationObserver(() => updateTableHeaderOpacity());
observer.observe(tbody, { childList: true });
// Run once on load
window.addEventListener('load', updateTableHeaderOpacity);
