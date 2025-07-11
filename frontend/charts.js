const rawMaterialsChartCtx = document.getElementById('rawMaterialsChart').getContext('2d');

async function fetchCurrentSupply() {
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/current-supply');
  const data = await response.json();

  return {
    labels: data.map(x => x.materialName),
    counts: data.map(x => x.availableSupply)
  };
}

async function initRawMaterialsChart() {
  const rawMaterialsChartCtx = document.getElementById('rawMaterialsChart').getContext('2d');
  const { labels, counts } = await fetchCurrentSupply();

  new Chart(rawMaterialsChartCtx, {
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
}

async function fetchMachinesStatus() {
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/machines-status');
  const data = await response.json();

  return {
    labels: data.map(x => x.status),
    counts: data.map(x => x.count)
  };
}

async function initMachinesStatusChart() {
  const ctx = document.getElementById('machinesStatusChart').getContext('2d');
  const { labels, counts } = await fetchMachinesStatus();

  new Chart(ctx, {
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
}

async function fetchEarnings() {
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/total-earnings');
  const data = await response.json();
  return data.totalEarnings ?? 0;
}

async function fetchElectronicsStock() {
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/electronics-stock');
  return await response.json();
}

async function fetchMachinesStatusRaw() {
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/machines-status');
  return await response.json();
}

async function updateDashboardSummary() {
  const earnings = await fetchEarnings();
  document.getElementById('earningsValue').textContent = earnings.toLocaleString('en-ZA', {
    style: 'currency',
    currency: 'ZAR'
  });

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
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/orders');
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
  const response = await fetch('https://electronics-supplier-api.projects.bbdgrad.com:444/api/DashboardData/payments');
  const data = await response.json();

  return {
    labels: data.map(p => {
      const BASE_UNIX_SECONDS_2050 = 2524608000;
      const actualUnixTimestamp = BASE_UNIX_SECONDS_2050 + Number(p.timestamp);
      return new Date(actualUnixTimestamp * 1000).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
    }),
    balances: data.map(p => p.cumulativeBalance)
  };
}

async function initBankBalanceChart() {
  const ctx = document.getElementById('bankAccountChart').getContext('2d');
  const { labels, balances } = await fetchBankBalanceHistory();

  new Chart(ctx, {
    type: 'line',
    data: {
      labels: labels,
      datasets: [{
        label: 'Bank Balance (ZAR)',
        data: balances,
        fill: true,
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        borderColor: 'rgba(75, 192, 192, 1)',
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
}




// Call the initialization on page load
window.addEventListener('DOMContentLoaded', () => {
  initMachinesStatusChart();
  initRawMaterialsChart();
  initBankBalanceChart();
  updateDashboardSummary();
  populateOrdersTable();
});
