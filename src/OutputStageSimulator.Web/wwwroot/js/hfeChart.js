window.hfeChart = {
    _charts: {},

    render: function (canvasId, referencePoints, calculatedPoints, referenceLabel, calculatedLabel, mini) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        let chart = this._charts[canvasId];
        // A page revisit recreates the canvas DOM node with the same id;
        // a chart bound to the old (now-detached) node must be recreated.
        if (chart && chart.canvas !== canvas) {
            chart.destroy();
            chart = null;
            delete this._charts[canvasId];
        }

        if (chart) {
            chart.data.datasets[0].data = referencePoints;
            chart.data.datasets[0].label = referenceLabel;
            chart.data.datasets[1].data = calculatedPoints;
            chart.data.datasets[1].label = calculatedLabel;
            chart.update();
            return;
        }

        chart = new Chart(canvas, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: referenceLabel,
                        data: referencePoints,
                        borderColor: '#1d4ed8',
                        backgroundColor: '#1d4ed8',
                        yAxisID: 'y',
                        pointRadius: mini ? 1.5 : 3,
                        borderWidth: mini ? 1.75 : 2.5,
                        tension: 0.25,
                    },
                    {
                        label: calculatedLabel,
                        data: calculatedPoints,
                        borderColor: '#dc2626',
                        backgroundColor: '#dc2626',
                        yAxisID: 'y1',
                        pointRadius: 0,
                        borderWidth: mini ? 1.75 : 2.5,
                        tension: 0.25,
                    },
                ],
            },
            options: {
                animation: false,
                parsing: false,
                maintainAspectRatio: false,
                interaction: { mode: 'nearest', intersect: false },
                layout: { padding: mini ? 2 : 8 },
                scales: {
                    x: {
                        type: 'logarithmic',
                        title: { display: !mini, text: 'Ic (A)' },
                        ticks: { display: !mini, font: { size: mini ? 9 : 12 } },
                    },
                    y: {
                        type: 'linear',
                        position: 'left',
                        beginAtZero: true,
                        title: { display: !mini, text: 'hFE - reference (single device)', color: '#1d4ed8' },
                        ticks: { color: '#1d4ed8', font: { size: mini ? 9 : 12 } },
                    },
                    y1: {
                        type: 'linear',
                        position: 'right',
                        beginAtZero: true,
                        title: { display: !mini, text: 'hFE - calculated (compound, incl. driver)', color: '#dc2626' },
                        ticks: { color: '#dc2626', font: { size: mini ? 9 : 12 } },
                        grid: { drawOnChartArea: false },
                    },
                },
                plugins: {
                    legend: { display: !mini, position: 'bottom' },
                    title: {
                        display: !mini,
                        text: 'hFE vs Ic',
                        font: { size: 16, weight: 'bold' },
                        color: '#334155',
                    },
                },
            },
        });

        this._charts[canvasId] = chart;
    },
};
