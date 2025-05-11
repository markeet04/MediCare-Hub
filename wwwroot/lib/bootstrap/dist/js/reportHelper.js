// Helper functions for the report generation system

// Function to download a file
function downloadFile(fileName, contentBase64, contentType) {
    // Create a Blob from the base64 data
    const byteCharacters = atob(contentBase64);
    const byteArrays = [];
    
    for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        const slice = byteCharacters.slice(offset, offset + 512);
        
        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }
        
        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }
    
    const blob = new Blob(byteArrays, { type: contentType });
    
    // Create a link and trigger the download
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    
    // Append to the document, click it, and remove it
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Generate a chart for report data visualization
function generateChart(elementId, chartType, labels, datasets, options) {
    // Check if the element exists
    const canvas = document.getElementById(elementId);
    if (!canvas) {
        console.error(`Element with ID ${elementId} not found`);
        return;
    }
    
    // Check if Chart.js is loaded
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        return;
    }
    
    // Create chart configuration
    const config = {
        type: chartType,
        data: {
            labels: labels,
            datasets: datasets
        },
        options: options || {
            responsive: true,
            maintainAspectRatio: false
        }
    };
    
    // Create the chart
    new Chart(canvas.getContext('2d'), config);
}

// Display a preview of the report in a container
function displayReportPreview(elementId, reportData, reportType) {
    const container = document.getElementById(elementId);
    if (!container) {
        console.error(`Element with ID ${elementId} not found`);
        return;
    }
    
    // Clear the container
    container.innerHTML = '';
    
    // Create preview based on report type
    switch (reportType) {
        case 'user-activity':
            createUserActivityPreview(container, reportData);
            break;
        case 'system-performance':
            createSystemPerformancePreview(container, reportData);
            break;
        case 'security-audit':
            createSecurityAuditPreview(container, reportData);
            break;
        default:
            container.innerHTML = '<div class="alert alert-warning">Unknown report type</div>';
    }
}

// Create user activity report preview
function createUserActivityPreview(container, data) {
    // Create a header
    const header = document.createElement('div');
    header.className = 'mb-4';
    header.innerHTML = `
        <h4>${data.title}</h4>
        <p class="text-muted">Generated on ${new Date().toLocaleDateString()}</p>
        <p>Report period: ${data.startDate} to ${data.endDate}</p>
    `;
    container.appendChild(header);
    
    // Create charts if included
    if (data.includeCharts) {
        const chartsRow = document.createElement('div');
        chartsRow.className = 'row mb-4';
        chartsRow.innerHTML = `
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">User Logins</h6>
                        <canvas id="loginChart" height="200"></canvas>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">System Access by Role</h6>
                        <canvas id="accessChart" height="200"></canvas>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(chartsRow);
        
        // Initialize charts (would use real data in a production app)
        setTimeout(() => {
            generateChart('loginChart', 'line', 
                ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                [{
                    label: 'Logins',
                    data: [65, 59, 80, 81, 56, 40, 70],
                    borderColor: 'rgb(75, 192, 192)',
                    tension: 0.1
                }]
            );
            
            generateChart('accessChart', 'pie',
                ['Admin', 'Manager', 'User', 'Guest'],
                [{
                    data: [12, 19, 45, 6],
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.7)',
                        'rgba(54, 162, 235, 0.7)',
                        'rgba(255, 206, 86, 0.7)',
                        'rgba(75, 192, 192, 0.7)'
                    ]
                }]
            );
        }, 100);
    }
    
    // Create data table
    const tableContainer = document.createElement('div');
    tableContainer.className = 'table-responsive';
    
    const table = document.createElement('table');
    table.className = 'table table-striped';
    table.innerHTML = `
        <thead>
            <tr>
                <th>User</th>
                <th>Action</th>
                <th>Date & Time</th>
                <th>IP Address</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
            ${data.records.map(record => `
                <tr>
                    <td>${record.user}</td>
                    <td>${record.action}</td>
                    <td>${record.dateTime}</td>
                    <td>${record.ipAddress}</td>
                    <td><span class="badge ${record.success ? 'bg-success' : 'bg-danger'}">${record.success ? 'Success' : 'Failed'}</span></td>
                </tr>
            `).join('')}
        </tbody>
    `;
    
    tableContainer.appendChild(table);
    container.appendChild(tableContainer);
}

// Create system performance report preview
function createSystemPerformancePreview(container, data) {
    // Create a header
    const header = document.createElement('div');
    header.className = 'mb-4';
    header.innerHTML = `
        <h4>${data.title}</h4>
        <p class="text-muted">Generated on ${new Date().toLocaleDateString()}</p>
        <p>Report period: ${data.startDate} to ${data.endDate}</p>
    `;
    container.appendChild(header);
    
    // Create charts if included
    if (data.includeCharts) {
        const chartsRow = document.createElement('div');
        chartsRow.className = 'row mb-4';
        chartsRow.innerHTML = `
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">CPU & Memory Usage</h6>
                        <canvas id="resourceChart" height="200"></canvas>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">Response Time</h6>
                        <canvas id="responseChart" height="200"></canvas>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(chartsRow);
        
        // Initialize charts (would use real data in a production app)
        setTimeout(() => {
            generateChart('resourceChart', 'line', 
                data.records.map(r => r.timestamp.substring(0, 5)),
                [{
                    label: 'CPU Usage',
                    data: data.records.map(r => r.cpuUsage),
                    borderColor: 'rgb(255, 99, 132)',
                    tension: 0.1
                },
                {
                    label: 'Memory Usage',
                    data: data.records.map(r => r.memoryUsage),
                    borderColor: 'rgb(54, 162, 235)',
                    tension: 0.1
                }]
            );
            
            generateChart('responseChart', 'line',
                data.records.map(r => r.timestamp.substring(0, 5)),
                [{
                    label: 'Response Time (ms)',
                    data: data.records.map(r => r.responseTime),
                    borderColor: 'rgb(75, 192, 192)',
                    tension: 0.1
                }]
            );
        }, 100);
    }
    
    // Create data table
    const tableContainer = document.createElement('div');
    tableContainer.className = 'table-responsive';
    
    const table = document.createElement('table');
    table.className = 'table table-striped';
    table.innerHTML = `
        <thead>
            <tr>
                <th>Time</th>
                <th>CPU</th>
                <th>Memory</th>
                <th>Disk</th>
                <th>Network</th>
                <th>Response</th>
                <th>Users</th>
            </tr>
        </thead>
        <tbody>
            ${data.records.map(record => `
                <tr>
                    <td>${record.timestamp}</td>
                    <td>${record.cpuUsage}%</td>
                    <td>${record.memoryUsage}%</td>
                    <td>${record.diskUsage}%</td>
                    <td>${record.networkUsage}%</td>
                    <td>${record.responseTime}ms</td>
                    <td>${record.activeUsers}</td>
                </tr>
            `).join('')}
        </tbody>
    `;
    
    tableContainer.appendChild(table);
    container.appendChild(tableContainer);
}

// Create security audit report preview
function createSecurityAuditPreview(container, data) {
    // Create a header
    const header = document.createElement('div');
    header.className = 'mb-4';
    header.innerHTML = `
        <h4>${data.title}</h4>
        <p class="text-muted">Generated on ${new Date().toLocaleDateString()}</p>
        <p>Report period: ${data.startDate} to ${data.endDate}</p>
    `;
    container.appendChild(header);
    
    // Create charts if included
    if (data.includeCharts) {
        const chartsRow = document.createElement('div');
        chartsRow.className = 'row mb-4';
        chartsRow.innerHTML = `
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">Security Events by Type</h6>
                        <canvas id="eventsChart" height="200"></canvas>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">Events by Severity</h6>
                        <canvas id="severityChart" height="200"></canvas>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(chartsRow);
        
        // Initialize charts (would use real data in a production app)
        setTimeout(() => {
            generateChart('eventsChart', 'bar', 
                ['Login', 'Password Reset', 'Permission Change', 'File Access', 'Admin Action'],
                [{
                    label: 'Events',
                    data: [25, 12, 8, 15, 6],
                    backgroundColor: 'rgba(54, 162, 235, 0.7)'
                }]
            );
            
            generateChart('severityChart', 'pie',
                ['Low', 'Medium', 'High', 'Critical'],
                [{
                    data: [45, 27, 12, 5],
                    backgroundColor: [
                        'rgba(75, 192, 192, 0.7)',
                        'rgba(255, 206, 86, 0.7)',
                        'rgba(255, 159, 64, 0.7)',
                        'rgba(255, 99, 132, 0.7)'
                    ]
                }]
            );
        }, 100);
    }
    
    // Create data table
    const tableContainer = document.createElement('div');
    tableContainer.className = 'table-responsive';
    
    const table = document.createElement('table');
    table.className = 'table table-striped';
    table.innerHTML = `
        <thead>
            <tr>
                <th>Time</th>
                <th>Event</th>
                <th>User</th>
                <th>IP Address</th>
                <th>Status</th>
                <th>Severity</th>
            </tr>
        </thead>
        <tbody>
            ${data.records.map(record => `
                <tr>
                    <td>${record.timestamp}</td>
                    <td>${record.eventType}</td>
                    <td>${record.userId}</td>
                    <td>${record.ipAddress}</td>
                    <td><span class="badge ${record.success ? 'bg-success' : 'bg-danger'}">${record.success ? 'Success' : 'Failed'}</span></td>
                    <td><span class="badge ${getSeverityClass(record.severity)}">${record.severity}</span></td>
                </tr>
            `).join('')}
        </tbody>
    `;
    
    tableContainer.appendChild(table);
    container.appendChild(tableContainer);
}

// Helper function to get severity badge class
function getSeverityClass(severity) {
    switch (severity.toLowerCase()) {
        case 'low':
            return 'bg-info';
        case 'medium':
            return 'bg-warning';
        case 'high':
            return 'bg-danger';
        case 'critical':
            return 'bg-danger';
        default:
            return 'bg-secondary';
    }
}