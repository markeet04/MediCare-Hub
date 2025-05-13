// Add this script to your _Host.cshtml or as a separate JS file
// and reference it in your project

// Load 3rd party libraries for charts and animations
function loadScripts() {
    // Load Chart.js
    if (!window.Chart) {
        const chartScript = document.createElement('script');
        chartScript.src = 'https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.9.1/chart.min.js';
        chartScript.integrity = 'sha512-ElRFoEQdI5Ht6kZvyzXhYG9NqjtkmlkfYk0wr6wHxU9JEHakS7UJZNeml5ALk+USUh00S9kLUeD1xW1hIKGBYQ==';
        chartScript.crossOrigin = 'anonymous';
        chartScript.referrerPolicy = 'no-referrer';
        document.head.appendChild(chartScript);
    }
    
    // Load Animate.css
    if (!document.querySelector('link[href*="animate.css"]')) {
        const animateCss = document.createElement('link');
        animateCss.rel = 'stylesheet';
        animateCss.href = 'https://cdnjs.cloudflare.com/ajax/libs/animate.css/4.1.1/animate.min.css';
        document.head.appendChild(animateCss);
    }
}

// Load animation library
function loadAnimateCSS() {
    loadScripts();
}

// Initialize counters with animation
function initCounters() {
    const counters = document.querySelectorAll('.counter');
    
    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute('data-target'));
        const duration = 1500; // Animation duration in milliseconds
        const startTime = performance.now();
        const startValue = 0;
        
        function updateCounter(currentTime) {
            const elapsedTime = currentTime - startTime;
            if (elapsedTime < duration) {
                const value = Math.floor(easeOutQuad(elapsedTime, startValue, target, duration));
                counter.textContent = value;
                requestAnimationFrame(updateCounter);
            } else {
                counter.textContent = target;
            }
        }
        
        requestAnimationFrame(updateCounter);
    });
}

// Easing function for smooth counter animation
function easeOutQuad(t, b, c, d) {
    t /= d;
    return -c * t * (t - 2) + b;
}

// Chart initialization
let activityChart;

function initCharts() {
    loadScripts();
    
    // Wait for Chart.js to load
    const checkChartLoaded = setInterval(() => {
        if (window.Chart) {
            clearInterval(checkChartLoaded);
            initActivityChart('weekly');
        }
    }, 100);
}

// Initialize patient activity chart
function initActivityChart(timeframe) {
    const ctx = document.getElementById('activityChart');
    if (!ctx) return;
    
    // Define chart data based on timeframe
    const data = getChartData(timeframe);
    
    // Chart configuration
    const config = {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        boxWidth: 6
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    backgroundColor: 'rgba(52, 58, 64, 0.8)',
                    padding: 10,
                    cornerRadius: 8,
                    caretSize: 6
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                }
            },
            elements: {
                line: {
                    tension: 0.4
                },
                point: {
                    radius: 3,
                    hoverRadius: 6
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    };
    
    // Create or update chart
    if (activityChart) {
        activityChart.data = data;
        activityChart.update();
    } else {
        activityChart = new Chart(ctx, config);
    }
}

// Update chart data when filter changes
function updateChartData(timeframe) {
    const data = getChartData(timeframe);
    
    if (activityChart) {
        activityChart.data = data;
        activityChart.update();
    }
}

// Get chart data based on timeframe
function getChartData(timeframe) {
    // Sample data for each timeframe
    if (timeframe === 'weekly') {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    label: 'Appointments',
                    data: [5, 8, 12, 7, 10, 3, 2],
                    borderColor: '#20c997',
                    backgroundColor: 'rgba(32, 201, 151, 0.1)',
                    fill: true
                },
                {
                    label: 'Lab Results',
                    data: [3, 5, 8, 4, 6, 2, 1],
                    borderColor: '#0dcaf0',
                    backgroundColor: 'rgba(13, 202, 240, 0.1)',
                    fill: true
                }
            ]
        };
    } else if (timeframe === 'monthly') {
        return {
            labels: ['Week 1', 'Week 2', 'Week 3', 'Week 4'],
            datasets: [
                {
                    label: 'Appointments',
                    data: [42, 38, 45, 40],
                    borderColor: '#20c997',
                    backgroundColor: 'rgba(32, 201, 151, 0.1)',
                    fill: true
                },
                {
                    label: 'Lab Results',
                    data: [28, 32, 35, 30],
                    borderColor: '#0dcaf0',
                    backgroundColor: 'rgba(13, 202, 240, 0.1)',
                    fill: true
                }
            ]
        };
    } else {
        return {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
            datasets: [
                {
                    label: 'Appointments',
                    data: [150, 170, 180, 190, 185, 175, 165, 155, 170, 180, 190, 195],
                    borderColor: '#20c997',
                    backgroundColor: 'rgba(32, 201, 151, 0.1)',
                    fill: true
                },
                {
                    label: 'Lab Results',
                    data: [100, 110, 125, 130, 135, 125, 115, 105, 120, 130, 140, 145],
                    borderColor: '#0dcaf0',
                    backgroundColor: 'rgba(13, 202, 240, 0.1)',
                    fill: true
                }
            ]
        };
    }
}

// Show toast notifications
function showToast(toastId, message) {
    const toastElement = document.getElementById(toastId);
    if (!toastElement) return;
    
    // Set message
    const messageElement = document.getElementById('successMessage');
    if (messageElement) {
        messageElement.textContent = message;
    }
    
    // Create Bootstrap toast object and show it
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
}

// Initialize everything when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Load scripts
    loadScripts();
});