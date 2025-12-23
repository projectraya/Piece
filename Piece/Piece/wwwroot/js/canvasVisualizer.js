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
                this.analyser.fftSize = 2048; // Much higher resolution (was 128)

                this.analyser.smoothingTimeConstant = 0.65; // Less smoothing (was 0.8)
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

        const barCount = 24; 
        const barWidth = this.canvas.width / barCount;
        const colors = [
            // Bass frequencies (blue/cyan)
            '#0066ff', '#0088ff', '#00aaff', '#00ccff',
            // Low-mids (cyan/purple)
            '#00ffff', '#00ddff', '#33bbff', '#6699ff',
            // Mids (purple/pink)
            '#9966ff', '#bb44ff', '#dd22ff', '#ff00ff',
            // High-mids (pink/magenta)
            '#ff0088', '#ff0066', '#ff0044', '#ff2255',
            // Treble (yellow/green)
            '#ff4466', '#ff6644', '#ff8822', '#ffaa00',
            '#ffcc00', '#ffff00', '#ccff00', '#88ff00'
        ];

        for (let i = 0; i < barCount; i++) {
            
            const minFreq = 0;
            const maxFreq = this.dataArray.length;

            
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(minFreq + logIndex * (maxFreq - minFreq));

            const value = this.dataArray[dataIndex] || 0;

            
            const heightPercent = 0.15 + (value / 255) * 0.85;
            const height = heightPercent * this.canvas.height;

            const x = i * barWidth + 1;
            const y = this.canvas.height - height;

            const gradient = this.ctx.createLinearGradient(0, y, 0, this.canvas.height);
            const color = colors[i % colors.length];
            gradient.addColorStop(0, color);
            gradient.addColorStop(1, color + '99');

            this.ctx.fillStyle = gradient;
            this.ctx.shadowBlur = 12;
            this.ctx.shadowColor = color;
            this.ctx.fillRect(x, y, barWidth - 2, height);
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