const rawMaterialsChartCtx = document.getElementById('rawMaterialsChart').getContext('2d');

const baseUrl = 'https://electronics-supplier.tevlen.co.za/';

async function apiFetch(path, options = {}, overridePath) {
  // Default headers for all API calls. You can add Authorization or other
  // headers via the `options.headers` argument. Example usage:
  // apiFetch('dashboard/earnings/total', { headers: { Authorization: 'Bearer <token>' } })
  // For cross-origin requests where credentials (cookies) are required, add
  // `credentials: 'include'` to the options and configure CORS on the server
  // to allow credentials.
  const defaultHeaders = {
    'Accept': 'application/json',
    'Client-Id': 'electronics-supplier'
  };

  const merged = {
    // Keep any provided options (method, body, credentials, mode, etc.)
    ...options,
    headers: {
      ...defaultHeaders,
      ...(options.headers || {})
    }
  };

  const finalPath = overridePath || baseUrl + path;

  const response = await fetch(finalPath, merged);
  if (!response.ok) {
    // Try to get error body for a more descriptive message
    const text = await response.text().catch(() => '');
    throw new Error(`API ${response.status}: ${text}`);
  }
  return response;
}


async function fetchCurrentSupply() {
  const response = await apiFetch(`dashboard/supplies/current`);
  const data = await response.json();
  

  return {
    labels: data.map(x => x.materialName),
    counts: data.map(x => x.availableSupply)
  };
}

let rawMaterialsChartInstance = null;
async function initRawMaterialsChart() {
  const ctx = document.getElementById('rawMaterialsChart').getContext('2d');
  const { labels, counts } = await fetchCurrentSupply();

  const chart = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: labels,
      datasets: [{
        label: 'Available Supply (Unprocessed)',
        data: counts,
        backgroundColor: [
          'rgba(255, 99, 132, 1)',
          'rgba(54, 162, 235, 1)'
        ],
        borderColor: [
          'rgba(255,99,132,1)',
          'rgba(54, 162, 235, 1)'
        ],
        borderWidth: 1,
        borderRadius: 10
      }]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            precision: 0
          }
        }
      }
    }
  });

  return chart;
}

let machinesStatusChartInstance = null;
async function initMachinesStatusChart() {
  const ctx = document.getElementById('machinesStatusChart').getContext('2d');
  const { labels, counts } = await fetchMachinesStatus();

  const chart = new Chart(ctx, {
    type: 'pie',
    data: {
      labels: labels,
      datasets: [{
        data: counts,
        backgroundColor: [
          'rgba(75, 192, 192, 0.6)',
          'rgba(255, 99, 132, 0.6)',
          'rgba(255, 206, 86, 0.6)'
        ],
      }]
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: 'bottom',
        }
      }
    }
  });
  return chart;
}

async function fetchEarnings() {
  const response = await apiFetch(`dashboard/earnings/total`);
  const data = await response.json();
  return data.totalEarnings ?? 0;
}

async function fetchElectronicsStock() {
  const response = await apiFetch(`dashboard/electronics/stock`);
  return await response.json();
}

async function fetchMachinesStatusRaw() {
  const response = await apiFetch(`dashboard/machines/status`);
  return await response.json();
}

// Convenience wrapper that returns { labels, counts } suitable for charts
async function fetchMachinesStatus() {
  const raw = await fetchMachinesStatusRaw();
  // Expecting an array like [{ status: 'IN_USE', count: 3 }, ...]
  const labels = raw.map(r => r.status);
  const counts = raw.map(r => r.count);
  return { labels, counts };
}

async function updatedashboardSummary() {
  const earnings = await fetchEarnings();
  document.getElementById('earningsValue').textContent = earnings.toLocaleString('en-ZA', {
    style: 'currency',
    currency: 'ZAR'
  });
  console.log('here');
  

  // Fetch and update bank balance
  try {
    
    const response = await apiFetch('', {}, `https://commercial-bank-api.subspace.site/api/account/me/balance`);
    const data = await response.json();
    
    document.getElementById('bankBalanceValue').textContent = data.balance.toLocaleString('en-ZA', {
      style: 'currency',
      currency: 'ZAR'
    });
  } catch (err) {
    document.getElementById('bankBalanceValue').textContent = 'Error';
  }

  const electronics = await fetchElectronicsStock();
  document.getElementById('electronicsStock').textContent = electronics.availableStock ?? '-';
  document.getElementById('electronicsPrice').textContent = electronics.pricePerUnit
    ? electronics.pricePerUnit.toLocaleString('en-ZA', { style: 'currency', currency: 'ZAR' })
    : '-';

  const machinesStatus = await fetchMachinesStatusRaw();
  let inUse = 0, broken = 0, available = 0;
  machinesStatus.forEach(ms => {
    if (ms.status === 'IN_USE' || ms.status === 'IN USE') inUse = ms.count;
    else if (ms.status === 'BROKEN') broken = ms.count;
    else if (ms.status === 'AVAILABLE' || ms.status === 'STANDBY') available = ms.count;
  });
  document.getElementById('machinesInUse').textContent = inUse;
  document.getElementById('machinesBroken').textContent = broken;
  document.getElementById('machinesAvailable').textContent = available;
}

async function fetchOrders() {
  // Use apiFetch so default headers / error handling apply
  const response = await apiFetch(`dashboard/orders`);
  return await response.json();
}

function formatDate(decimalTimestamp) {
  // 2050-01-01 00:00:00 UTC in UNIX timestamp (seconds)
  const BASE_UNIX_SECONDS_2050 = 2524608000;

  // Your stored timestamps are "seconds since 2050"
  const actualUnixTimestamp = BASE_UNIX_SECONDS_2050 + Number(decimalTimestamp);

  const date = new Date(actualUnixTimestamp * 1000); // Convert to milliseconds
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });
}


function getStatusClass(status) {
  // Map status strings to CSS classes for coloring
  switch(status.toUpperCase()) {
    case 'COMPLETED': return 'status-done';
    case 'PENDING': return 'status-pending';
    case 'REJECTED': return 'status-rejected';
    case 'IN_PROGRESS': return 'status-in-progress';
    default: return '';
  }
}

async function populateOrdersTable() {
  const orders = await fetchOrders();

  const tbody = document.querySelector('section[aria-label="Orders"] tbody');
  tbody.innerHTML = ''; // Clear existing rows

  orders.forEach(order => {
    const tr = document.createElement('tr');

    // Format date for human-readable
    const dateStr = formatDate(order.dateOfTransaction);

    tr.innerHTML = `
      <td><time datetime="${dateStr}">${dateStr}</time></td>
      <td>${order.companyName}</td>
      <td>${order.item || 'Phone electronics'}</td>
      <td>${order.accountNo}</td>
      <td>${order.amount}</td>
      <td><strong class="${getStatusClass(order.status)}">${order.status}</strong></td>
    `;

    tbody.appendChild(tr);
  });
}


function convertToDate(decimalTimestamp) {
  const BASE_UNIX_SECONDS_2050 = 2524608000;
  const actualUnixTimestamp = BASE_UNIX_SECONDS_2050 + Number(decimalTimestamp);
  return new Date(actualUnixTimestamp * 1000); // JS Date object
}

async function fetchBankBalanceHistory() {
  // Return the client-side maintained bank balance history (labels, balances)
  // This history is appended by calls to updateBankBalanceHistory() which
  // fetches the live balance from the bank API and stores a local point.
  const labels = bankBalanceHistory.map(pt => new Date(pt.ts).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' }));
  const balances = bankBalanceHistory.map(pt => pt.balance);
  return { labels, balances };
}

// Bank balance history is kept in memory and persisted to localStorage so the
// chart has a small historical window even if the server doesn't provide a
// time series endpoint. Each point: { ts: <ms-since-epoch>, balance: <number> }
const BANK_API_URL = 'https://commercial-bank-api.subspace.site/api/account/me/balance';
const BANK_HISTORY_STORAGE_KEY = 'bankBalanceHistory:v1';
let bankBalanceHistory = [];

// Load persisted history on startup
try {
  const raw = localStorage.getItem(BANK_HISTORY_STORAGE_KEY);
  if (raw) bankBalanceHistory = JSON.parse(raw);
} catch (e) {
  console.warn('Failed to load bank balance history from localStorage', e);
  bankBalanceHistory = [];
}

function saveBankBalanceHistory() {
  try {
    localStorage.setItem(BANK_HISTORY_STORAGE_KEY, JSON.stringify(bankBalanceHistory));
  } catch (e) {
    // If storage is full or not available, just ignore silently.
    console.warn('Failed to save bank balance history', e);
  }
}

async function fetchCurrentBankBalancePoint() {
  // Calls the external bank API (overridePath usage) and returns { ts, balance }
  try {
    const response = await apiFetch('', {}, BANK_API_URL);
    const data = await response.json();
    // prefer server-provided timestamp if present (ms or seconds), otherwise use now
    let ts = Date.now();
    if (data.timestamp) {
      // timestamp may be seconds or milliseconds; normalize heuristically
      const asNum = Number(data.timestamp);
      ts = asNum > 1e12 ? asNum : asNum * 1000; // if small, assume seconds
    }
    const balance = Number(data.balance ?? data.amount ?? 0);
    return { ts, balance };
  } catch (err) {
    console.warn('fetchCurrentBankBalancePoint failed', err);
    throw err;
  }
}

async function updateBankBalanceHistory({ limit = 72 } = {}) {
  // Fetch current balance and append to history. Keep only `limit` latest points.
  try {
    const pt = await fetchCurrentBankBalancePoint();
    // avoid pushing duplicate consecutive timestamps
    const last = bankBalanceHistory[bankBalanceHistory.length - 1];
    if (!last || last.ts !== pt.ts) {
      bankBalanceHistory.push(pt);
      // cap history length
      if (bankBalanceHistory.length > limit) bankBalanceHistory = bankBalanceHistory.slice(-limit);
      saveBankBalanceHistory();
    }
  } catch (err) {
    // Don't throw — callers should handle chart update failures separately
    console.warn('Unable to update bank balance history', err);
  }
}

let bankBalanceChartInstance = null;
async function initBankBalanceChart() {
  const ctx = document.getElementById('bankAccountChart').getContext('2d');
  const { labels, balances } = await fetchBankBalanceHistory();
  const chart = new Chart(ctx, {
    type: 'line',
    data: {
      labels: labels,
      datasets: [{
        label: 'Bank Balance (ZAR)',
        data: balances,
        fill: true,
        backgroundColor: 'rgba(17, 231, 124, 0.2)',
        borderColor: 'rgba(17, 231, 124, 1)',
        tension: 0.3
      }]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            callback: value => 'R' + value.toLocaleString()
          },
          title: {
            display: true,
            text: 'Balance (ZAR)'
          }
        },
        x: {
          title: {
            display: true,
            text: 'Date'
          }
        }
      }
    }
  });
  return chart;
}

// Call the initialization on page load
window.addEventListener('DOMContentLoaded', async () => {
  // create charts and keep references
  rawMaterialsChartInstance = await initRawMaterialsChart();
  machinesStatusChartInstance = await initMachinesStatusChart();
  bankBalanceChartInstance = await initBankBalanceChart();

  // initial UI updates
  await updatedashboardSummary();
  populateOrdersTable();
  updateSimulationStatus();

  // start refresh loop (recursive setTimeout approach)
  const REFRESH_MS = 10_000; // 10 seconds

  let stopped = false; // can be used to stop if needed

  async function refreshOnceAndSchedule() {
    if (stopped) return;

    try {
      // 1) refresh summary values
      await updatedashboardSummary();

      // 2) refresh raw materials chart
      try {
        const { labels, counts } = await fetchCurrentSupply();
        if (rawMaterialsChartInstance) {
          rawMaterialsChartInstance.data.labels = labels;
          rawMaterialsChartInstance.data.datasets[0].data = counts;
          rawMaterialsChartInstance.update();
        }
      } catch (err) {
        console.warn('refresh raw materials failed', err);
      }

      // 3) refresh machines status chart
      try {
        const { labels, counts } = await fetchMachinesStatus();
        if (machinesStatusChartInstance) {
          machinesStatusChartInstance.data.labels = labels;
          machinesStatusChartInstance.data.datasets[0].data = counts;
          machinesStatusChartInstance.update();
        }
      } catch (err) {
        console.warn('refresh machines status failed', err);
      }

      // 4) refresh bank balance chart
      try {
        // first, append the latest bank balance point
        await updateBankBalanceHistory();
        const { labels, balances } = await fetchBankBalanceHistory();
        if (bankBalanceChartInstance) {
          bankBalanceChartInstance.data.labels = labels;
          bankBalanceChartInstance.data.datasets[0].data = balances;
          bankBalanceChartInstance.update();
        }
      } catch (err) {
        console.warn('refresh bank balance failed', err);
      }

      // 5) refresh orders table (optional if you want live updates)
      try { await populateOrdersTable(); } catch (err) { console.warn('refresh orders failed', err); }

      // 6) refresh simulation status too
      try { await updateSimulationStatus(); } catch (err) { console.warn('refresh sim status failed', err); }
    } catch (err) {
      console.error('Unexpected refresh error', err);
    } finally {
      // schedule next run
      setTimeout(refreshOnceAndSchedule, REFRESH_MS);
    }
  }

  // start the loop
  setTimeout(refreshOnceAndSchedule, REFRESH_MS);

  // optional: stop when page unloads
  window.addEventListener('beforeunload', () => { stopped = true; });
});

async function updateSimulationStatus() {
  try {
    const response = await apiFetch(`simulation`);
    const sim = await response.json();
    const statusText = sim.isRunning ? 'Running' : 'Stopped';
    document.getElementById('simulationStatus').textContent = statusText;
    if (sim.startTimeUtc) {
      const date = new Date(sim.startTimeUtc);
      document.getElementById('simulationLastStarted').textContent = date.toLocaleString();
    } else {
      document.getElementById('simulationLastStarted').textContent = '-';
    }
  } catch (err) {
    document.getElementById('simulationStatus').textContent = 'Error';
    document.getElementById('simulationLastStarted').textContent = '-';
  }
}
