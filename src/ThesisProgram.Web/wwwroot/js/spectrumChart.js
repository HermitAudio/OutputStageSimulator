window.spectrumChart = {
    _chart: null,
    // Pascal `graf` draws each bar from a fixed floor of -140 up to the dB
    // value (move(i,-140); line(i,main[i].re)), and skips the bar entirely
    // if the value is at or below that floor. Mirrored here: without an
    // explicit floor, Chart.js's default 0-baseline bar chart makes bar
    // *length* proportional to |value|, which inverts the intended meaning
    // for these already-negative dB values (the quietest bins, deepest
    // below 0 dB, would draw the longest bars).
    _floor: -140,

    _clip: function (data) {
        return data.map(v => (v <= this._floor ? null : v));
    },

    _gradient: function (ctx) {
        const g = ctx.createLinearGradient(0, 0, 0, ctx.canvas.height);
        g.addColorStop(0, '#f97316');
        g.addColorStop(0.5, '#8b5cf6');
        g.addColorStop(1, '#0ea5e9');
        return g;
    },

    render: function (canvasId, labels, data, title) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const clipped = this._clip(data);

        // A page revisit recreates the canvas DOM node with the same id; a
        // chart bound to the old (now-detached) node must be recreated.
        if (this._chart && this._chart.canvas !== canvas) {
            this._chart.destroy();
            this._chart = null;
        }

        if (this._chart) {
            this._chart.data.labels = labels;
            this._chart.data.datasets[0].data = clipped;
            this._chart.options.plugins.title.text = title || 'Spectrum';
            this._chart.update();
            return;
        }

        const ctx = canvas.getContext('2d');
        this._chart = new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Amplitude [dB]',
                    data: clipped,
                    backgroundColor: this._gradient(ctx),
                    base: this._floor,
                    borderRadius: 2,
                }],
            },
            options: {
                animation: false,
                scales: {
                    y: {
                        min: this._floor,
                        max: 0,
                        title: { display: true, text: 'dB' },
                        grid: { color: 'rgba(148, 163, 184, 0.25)' },
                    },
                    x: {
                        title: { display: true, text: 'Harmonic (N * fundamental)' },
                        grid: { display: false },
                    },
                },
                plugins: {
                    legend: { display: false },
                    title: {
                        display: true,
                        text: title || 'Spectrum',
                        font: { size: 16, weight: 'bold' },
                        color: '#6d28d9',
                    },
                },
            },
        });
    },
};
