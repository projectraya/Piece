class CanvasVisualizer {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.audioContext = null;
        this.analyser = null;
        this.dataArray = null;
        this.animationId = null;
        this.currentAudioElement = null;
    }

    async initialize(canvasId, audioElementId) {
        try {
            this.canvas = document.getElementById(canvasId);
            if (!this.canvas) {
                console.error('Canvas not found');
                return false;
            }
            this.ctx = this.canvas.getContext('2d');

            if (!this.audioContext) {
                this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            }

            if (!this.analyser) {
                this.analyser = this.audioContext.createAnalyser();
                this.analyser.fftSize = 128;
                this.analyser.smoothingTimeConstant = 0.8;

                const bufferLength = this.analyser.frequencyBinCount;
                this.dataArray = new Uint8Array(bufferLength);
            }

            console.log('✓ Canvas visualizer ready');

            this.startAudioMonitoring(audioElementId);
            this.animate();

            return true;
        } catch (error) {
            console.error('✗ Canvas visualizer error:', error);
            return false;
        }
    }

    startAudioMonitoring(audioElementId) {
       
        setInterval(() => {
            const audioElement = document.getElementById(audioElementId);

            if (audioElement && audioElement !== this.currentAudioElement) {
                try {
                    console.log('New audio element detected, connecting...');

                    const stream = audioElement.captureStream();
                    const source = this.audioContext.createMediaStreamSource(stream);
                    source.connect(this.analyser);
                    
                    this.currentAudioElement = audioElement;
                    console.log('✓ Connected to new audio element');
                } catch (e) {
                    console.error('Connection error:', e);
                }
            }
        }, 500);
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        if (!this.ctx || !this.analyser || !this.dataArray) return;

        this.analyser.getByteFrequencyData(this.dataArray);

        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        const barCount = 12;
        const barWidth = this.canvas.width / barCount;
        const colors = [
            '#00ffff', '#00d4ff', '#a855f7', '#ec4899',
            '#ff00ff', '#ffff00', '#00ff88', '#ff0088',
            '#00ffff', '#00d4ff', '#a855f7', '#ec4899'
        ];

        for (let i = 0; i < barCount; i++) {
            const dataIndex = Math.floor((i / barCount) * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;
            const heightPercent = 0.3 + (value / 255) * 0.65;
            const height = heightPercent * this.canvas.height;

            const x = i * barWidth + 2;
            const y = this.canvas.height - height;

            const gradient = this.ctx.createLinearGradient(0, y, 0, this.canvas.height);
            const color = colors[i];
            gradient.addColorStop(0, color);
            gradient.addColorStop(1, color + '80');

            this.ctx.fillStyle = gradient;
            this.ctx.shadowBlur = 15;
            this.ctx.shadowColor = color;
            this.ctx.fillRect(x, y, barWidth - 4, height);
            this.ctx.shadowBlur = 0;
        }
    }

    destroy() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
        }
    }
}

window.canvasVisualizer = new CanvasVisualizer();