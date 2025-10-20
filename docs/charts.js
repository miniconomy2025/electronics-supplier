const rawMaterialsChartCtx = document.getElementById('rawMaterialsChart').getContext('2d');

  async function fetchCurrentSupply() {
    const response = await fetch('http://localhost:5062/api/DashboardData/current-supply');
    
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
              precision: 0 // Only integers
            }
          }
        }
      }
    });
  }

    const montlyIncomeChartCtx = document.getElementById('montlyIncomeChart').getContext('2d'); 
    const montlyIncomeChart = new Chart(montlyIncomeChartCtx, {
    type: 'line', // 'line', 'pie', 'doughnut', etc.
    data: {
      labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
      datasets: [{
        label: 'Votes',
        fill:true,
        data: [12, 19, 3, 5, 2, 3],
        backgroundColor: [
          'rgba(255, 99, 132, 1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(153, 102, 255, 1)',
          'rgba(255, 159, 64, 1)'
        ],
        borderColor: [
          'rgba(255,99,132,1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(153, 102, 255, 1)',
          'rgba(255, 159, 64, 1)'
        ],
        borderWidth: 1,
        tension: 0.2,
      }]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  });

    const backAccountChartCtx = document.getElementById('backAccountChart').getContext('2d'); 
    const backAccountChart = new Chart(backAccountChartCtx, {
    type: 'bar', // 'line', 'pie', 'doughnut', etc.
    data: {
      labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
      datasets: [{
        label: 'Votes',
        fill:true,
        data: [12, 19, 3, 5, 2, 3],
        backgroundColor: [
          'rgba(255, 99, 132, 1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(153, 102, 255, 1)',
          'rgba(255, 159, 64, 1)'
        ],
        borderColor: [
          'rgba(255,99,132,1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(153, 102, 255, 1)',
          'rgba(255, 159, 64, 1)'
        ],
        borderWidth: 1,
        borderRadius: 20,
      }]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  });

  async function fetchMachinesStatus() {
    const response = await fetch('http://localhost:5062/api/DashboardData/machines-status');
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
    const response = await fetch('http://localhost:5062/api/DashboardData/total-earnings');
    const data = await response.json();
    return data.totalEarnings ?? 0;
  }

  async function fetchElectronicsStock() {
    const response = await fetch('http://localhost:5062/api/DashboardData/electronics-stock');
    return await response.json();
  }

  async function fetchMachinesStatusRaw() {
    const response = await fetch('http://localhost:5062/api/DashboardData/machines-status');
    return await response.json();
  }

  async function updateDashboardSummary() {
    // Earnings
    const earnings = await fetchEarnings();
    document.getElementById('earningsValue').textContent = earnings.toLocaleString('en-ZA', { style: 'currency', currency: 'ZAR' });

    // Electronics Stock
    const electronics = await fetchElectronicsStock();
    document.getElementById('electronicsStock').textContent = electronics.availableStock ?? '-';
    document.getElementById('electronicsPrice').textContent = electronics.pricePerUnit
      ? electronics.pricePerUnit.toLocaleString('en-ZA', { style: 'currency', currency: 'ZAR' })
      : '-';

    // Machines Status
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

  // Call the initialization on page load
  window.addEventListener('DOMContentLoaded', () => {
    initMachinesStatusChart();
    initRawMaterialsChart();
    updateDashboardSummary();
  });
