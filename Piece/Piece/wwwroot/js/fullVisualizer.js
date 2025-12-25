class FullVisualizer {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.audioContext = null;
        this.analyser = null;
        this.dataArray = null;
        this.animationId = null;
        this.currentMode = 'bars';
        this.currentAudioElement = null;
    }

    async initialize(canvasId, audioElementId, mode) {
        console.log('[Full Visualizer] Initialize called with:', canvasId, audioElementId);
        try {
            this.canvas = document.getElementById(canvasId);
            if (!this.canvas) return false;

            // Set canvas to full window size
            this.canvas.width = window.innerWidth;
            this.canvas.height = window.innerHeight;

            this.ctx = this.canvas.getContext('2d');
            this.currentMode = mode;

            // Handle window resize
            window.addEventListener('resize', () => {
                this.canvas.width = window.innerWidth;
                this.canvas.height = window.innerHeight;
            });

            // Create audio context
            if (!this.audioContext) {
                this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            }

            if (!this.analyser) {
                this.analyser = this.audioContext.createAnalyser();
                this.analyser.fftSize = 2048;
                this.analyser.smoothingTimeConstant = 0.7;

                const bufferLength = this.analyser.frequencyBinCount;
                this.dataArray = new Uint8Array(bufferLength);
            }

            console.log('✓ Full visualizer initialized');

            // Start monitoring for audio
            this.startAudioMonitoring(audioElementId);
            this.animate();

            return true;
        } catch (error) {
            console.error('✗ Full visualizer error:', error);
            return false;
        }
    }

    startAudioMonitoring(audioElementId) {
        setInterval(() => {
            const audioElement = document.getElementById(audioElementId);

            console.log('[Canvas] Checking audio element...', audioElement ? 'found' : 'not found');

            if (audioElement) {
                console.log('[Canvas] Audio paused:', audioElement.paused);
                console.log('[Canvas] Audio src:', audioElement.src);
                console.log('[Canvas] Current element same?', audioElement === this.currentAudioElement);
            }

            if (audioElement && audioElement !== this.currentAudioElement) {
                try {
                    console.log('[Canvas] Attempting to connect...');

                    // Check if captureStream exists
                    if (typeof audioElement.captureStream !== 'function') {
                        console.error('[Canvas] captureStream not supported!');
                        return;
                    }

                    const stream = audioElement.captureStream();
                    console.log('[Canvas] Stream created:', stream);

                    const source = this.audioContext.createMediaStreamSource(stream);
                    console.log('[Canvas] Source created:', source);

                    source.connect(this.analyser);
                    console.log('[Canvas] Source connected to analyser');

                    this.currentAudioElement = audioElement;
                    console.log('✓ Connected to audio element');
                } catch (e) {
                    console.error('[Canvas] Connection error:', e);
                }
            }
        }, 500);
    }

    switchMode(mode) {
        this.currentMode = mode;
        console.log('Switched to mode:', mode);
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        if (!this.ctx || !this.analyser || !this.dataArray) return;

        this.analyser.getByteFrequencyData(this.dataArray);

        // Clear with fade effect
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.15)';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        // Draw based on mode
        switch (this.currentMode) {
            case 'bars':
                this.drawBars();
                break;
            case 'circle':
                this.drawCircle();
                break;
            case 'waveform':
                this.drawWaveform();
                break;
        }
    }

    drawBars() {
        const barCount = 128;
        const barWidth = this.canvas.width / barCount;

        // Create gradient
        const gradient = this.ctx.createLinearGradient(0, 0, this.canvas.width, 0);
        gradient.addColorStop(0, '#0066ff');
        gradient.addColorStop(0.25, '#00aaff');
        gradient.addColorStop(0.5, '#9966ff');
        gradient.addColorStop(0.75, '#ff00ff');
        gradient.addColorStop(1, '#ffff00');

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const height = (value / 255) * this.canvas.height * 0.8;
            const x = i * barWidth;
            const y = this.canvas.height - height;

            this.ctx.fillStyle = gradient;
            this.ctx.fillRect(x, y, barWidth - 2, height);

            // Glow effect
            this.ctx.shadowBlur = 20;
            this.ctx.shadowColor = gradient;
            this.ctx.fillRect(x, y, barWidth - 2, height);
            this.ctx.shadowBlur = 0;
        }
    }

    drawCircle() {
        const centerX = this.canvas.width / 2;
        const centerY = this.canvas.height / 2;
        const minRadius = 100;
        const barCount = 180;

        for (let i = 0; i < barCount; i++) {
            const angle = (i / barCount) * Math.PI * 2;
            const dataIndex = Math.floor((i / barCount) * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const barLength = (value / 255) * 300;
            const innerRadius = minRadius;
            const outerRadius = minRadius + barLength;

            // Color based on position
            const hue = (i / barCount) * 360;
            this.ctx.strokeStyle = `hsl(${hue}, 100%, 50%)`;
            this.ctx.lineWidth = 3;

            const x1 = centerX + Math.cos(angle) * innerRadius;
            const y1 = centerY + Math.sin(angle) * innerRadius;
            const x2 = centerX + Math.cos(angle) * outerRadius;
            const y2 = centerY + Math.sin(angle) * outerRadius;

            this.ctx.beginPath();
            this.ctx.moveTo(x1, y1);
            this.ctx.lineTo(x2, y2);
            this.ctx.stroke();

            // Glow
            this.ctx.shadowBlur = 15;
            this.ctx.shadowColor = `hsl(${hue}, 100%, 50%)`;
            this.ctx.stroke();
            this.ctx.shadowBlur = 0;
        }
    }

    drawWaveform() {
        this.ctx.strokeStyle = '#00ffff';
        this.ctx.lineWidth = 3;
        this.ctx.beginPath();

        const sliceWidth = this.canvas.width / this.dataArray.length;
        let x = 0;

        for (let i = 0; i < this.dataArray.length; i++) {
            const value = this.dataArray[i] / 255;
            const y = value * this.canvas.height;

            if (i === 0) {
                this.ctx.moveTo(x, y);
            } else {
                this.ctx.lineTo(x, y);
            }

            x += sliceWidth;
        }

        this.ctx.shadowBlur = 20;
        this.ctx.shadowColor = '#00ffff';
        this.ctx.stroke();
        this.ctx.shadowBlur = 0;
    }

    destroy() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
        }
    }
}

const fullVisualizer = new FullVisualizer();

window.initFullVisualizer = (canvasId, audioId, mode) => {
    return fullVisualizer.initialize(canvasId, audioId, mode);
};

window.switchVisualizerMode = (mode) => {
    fullVisualizer.switchMode(mode);
};