class VisualizerManager {
    constructor() {
        this.audioContext = null;
        this.analyser = null;
        this.dataArray = null;
        this.currentAudioElement = null;
        this.isInitialized = false;

        this.miniCanvas = null;
        this.miniCtx = null;
        this.fullCanvas = null;
        this.fullCtx = null;

        this.animationId = null;
        this.currentMode = 'bars';

        this.drawMini = true;
        this.drawFull = false;

        this.miniColor = '#8B5CF6';
        this.miniLighterColor = '#9B6AF5';
    }

    async initialize(audioElementId) {
        if (this.isInitialized) {
            console.log('[VisualizerManager] Already initialized');
            return true;
        }

        try {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 2048;
            this.analyser.smoothingTimeConstant = 0.65;

            const bufferLength = this.analyser.frequencyBinCount;
            this.dataArray = new Uint8Array(bufferLength);

            this.isInitialized = true;
            console.log('✓ VisualizerManager initialized');

            this.startAudioMonitoring(audioElementId);

            this.animate();

            return true;
        } catch (error) {
            console.error('✗ VisualizerManager error:', error);
            return false;
        }
    }

    startAudioMonitoring(audioElementId) {
        setInterval(() => {
            const audioElement = document.getElementById(audioElementId);

            if (audioElement && audioElement !== this.currentAudioElement) {
                try {
                    console.log('[VisualizerManager] Connecting to audio element...');
                    const stream = audioElement.captureStream();
                    const source = this.audioContext.createMediaStreamSource(stream);
                    source.connect(this.analyser);

                    this.currentAudioElement = audioElement;
                    console.log('✓ Connected to audio element');
                } catch (e) {
                    console.error('[VisualizerManager] Connection error:', e);
                }
            }
        }, 500);
    }

    registerMiniCanvas(canvasId) {
        this.miniCanvas = document.getElementById(canvasId);
        if (this.miniCanvas) {
            this.miniCtx = this.miniCanvas.getContext('2d');
            console.log('✓ Mini canvas registered');
        }
    }

    registerFullCanvas(canvasId) {
        this.fullCanvas = document.getElementById(canvasId);
        if (this.fullCanvas) {
            this.fullCanvas.width = window.innerWidth;
            this.fullCanvas.height = window.innerHeight;
            this.fullCtx = this.fullCanvas.getContext('2d');

            window.addEventListener('resize', () => {
                if (this.fullCanvas) {
                    this.fullCanvas.width = window.innerWidth;
                    this.fullCanvas.height = window.innerHeight;
                }
            });

            console.log('✓ Full canvas registered');
        }
    }

    setDrawMini(enabled) {
        this.drawMini = enabled;
        console.log(`[VisualizerManager] Draw mini: ${enabled}`);
    }

    setDrawFull(enabled) {
        this.drawFull = enabled;
        console.log(`[VisualizerManager] Draw full: ${enabled}`);
    }

    setMode(mode) {
        this.currentMode = mode;
        console.log(`[VisualizerManager] Mode: ${mode}`);
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        if (!this.analyser || !this.dataArray) return;

        this.analyser.getByteFrequencyData(this.dataArray);

        if (this.drawMini && this.miniCanvas && this.miniCtx) {
            this.drawMiniVisualizer();
        }

        if (this.drawFull && this.fullCanvas && this.fullCtx) {
            this.drawFullVisualizer();
        }
    }

    drawMiniVisualizer() {
        this.miniCtx.clearRect(0, 0, this.miniCanvas.width, this.miniCanvas.height);

        const barCount = 24;
        const barWidth = this.miniCanvas.width / barCount;

        const baseColor = this.miniColor || '#8B5CF6';
        const lighterColor = this.miniLighterColor || '#9B6AF5';

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;
            const heightPercent = 0.15 + (value / 255) * 0.85;
            const height = heightPercent * this.miniCanvas.height;

            const x = i * barWidth + 1;
            const y = this.miniCanvas.height - height;

            const gradient = this.miniCtx.createLinearGradient(0, y, 0, this.miniCanvas.height);
            gradient.addColorStop(0, lighterColor);
            gradient.addColorStop(1, baseColor);

            this.miniCtx.fillStyle = gradient;
            this.miniCtx.shadowBlur = 12;
            this.miniCtx.shadowColor = baseColor;
            this.miniCtx.fillRect(x, y, barWidth - 2, height);
            this.miniCtx.shadowBlur = 0;
        }
    }

    updateMiniColors(baseColor, lighterColor) {
        this.miniColor = baseColor || '#8B5CF6';
        this.miniLighterColor = lighterColor || '#9B6AF5';
        console.log('[VisualizerManager] Updated mini colors:', this.miniColor, '→', this.miniLighterColor);
    }

    drawFullVisualizer() {
        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.15)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

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
            case 'particles':
                this.drawParticleFountain();
                break;
            case 'dna':
                this.drawDNAHelix();
                break;
        }
    }

    drawBars() {
        const barCount = 48;
        const spacing = this.fullCanvas.width / barCount;
        const barWidth = spacing * 0.7;
        const centerY = this.fullCanvas.height / 2;
        const time = Date.now() / 1000;

        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.15)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const halfHeight = (value / 255) * (this.fullCanvas.height * 0.45);
            const x = i * spacing + (spacing - barWidth) / 2;

            const t = i / barCount;
            const hue = (260 + t * 80 + time * 20) % 360;
            const lightness = 50 + (value / 255) * 25;

            const gradient = this.fullCtx.createLinearGradient(0, centerY - halfHeight, 0, centerY + halfHeight);
            gradient.addColorStop(0, `hsla(${hue}, 90%, ${lightness}%, 0.3)`);
            gradient.addColorStop(0.5, `hsla(${hue}, 90%, ${lightness + 15}%, 1)`);
            gradient.addColorStop(1, `hsla(${hue}, 90%, ${lightness}%, 0.3)`);

            this.fullCtx.shadowBlur = 18;
            this.fullCtx.shadowColor = `hsla(${hue}, 90%, 60%, 0.7)`;

            this.fullCtx.fillStyle = gradient;
            this.fullCtx.beginPath();
            this.fullCtx.roundRect(x, centerY - halfHeight, barWidth, halfHeight * 2, barWidth / 2);
            this.fullCtx.fill();

            this.fullCtx.shadowBlur = 0;

            if (value > 60) {
                this.fullCtx.fillStyle = `rgba(255, 255, 255, ${(value / 255) * 0.6})`;
                this.fullCtx.fillRect(x, centerY - 1, barWidth, 2);
            }
        }
    }

    drawCircle() {
        const centerX = this.fullCanvas.width / 2;
        const centerY = this.fullCanvas.height / 2;
        const minRadius = 100;
        const barCount = 180;

        for (let i = 0; i < barCount; i++) {
            const angle = (i / barCount) * Math.PI * 2;
            const dataIndex = Math.floor((i / barCount) * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const barLength = (value / 255) * 300;
            const innerRadius = minRadius;
            const outerRadius = minRadius + barLength;

            const t = i / barCount;
            const hue = 240 + (t * 60);
            const saturation = 70 + (t * 30);
            const lightness = 60;

            this.fullCtx.strokeStyle = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
            this.fullCtx.lineWidth = 3;

            for (let mirror = 0; mirror < 2; mirror++) {
                const mirrorAngle = angle + (mirror * Math.PI);

                const x1 = centerX + Math.cos(mirrorAngle) * innerRadius;
                const y1 = centerY + Math.sin(mirrorAngle) * innerRadius;
                const x2 = centerX + Math.cos(mirrorAngle) * outerRadius;
                const y2 = centerY + Math.sin(mirrorAngle) * outerRadius;

                this.fullCtx.beginPath();
                this.fullCtx.moveTo(x1, y1);
                this.fullCtx.lineTo(x2, y2);
                this.fullCtx.stroke();
            }
        }
    }

    drawWaveform() {
        const centerY = this.fullCanvas.height / 2;

       
        const gradient = this.fullCtx.createLinearGradient(0, 0, this.fullCanvas.width, 0);
        gradient.addColorStop(0, '#0066ff');
        gradient.addColorStop(0.5, '#9966ff');
        gradient.addColorStop(1, '#ff00ff');

        this.fullCtx.strokeStyle = gradient;
        this.fullCtx.lineWidth = 4;
        this.fullCtx.lineCap = 'round';
        this.fullCtx.beginPath();

        const pointCount = Math.floor(this.fullCanvas.width / 5);
        const usableDataLength = Math.floor(this.dataArray.length * 0.8);

        for (let i = 0; i <= pointCount; i++) {
            const dataIndex = Math.floor((i / pointCount) * usableDataLength);
            const value = this.dataArray[dataIndex] || 0;

            const amplitude = (value / 255) * (this.fullCanvas.height * 0.4);

            const x = (i / pointCount) * this.fullCanvas.width;

            const wavePosition = (i / pointCount) * Math.PI * 2;
            const y = centerY + (Math.sin(wavePosition) * amplitude);

            if (i === 0) {
                this.fullCtx.moveTo(x, y);
            } else {
                this.fullCtx.lineTo(x, y);
            }
        }

        this.fullCtx.shadowBlur = 25;
        this.fullCtx.shadowColor = '#9966ff';
        this.fullCtx.stroke();
        this.fullCtx.shadowBlur = 0;
    }

    drawParticleFountain() {
        if (!this.dustParticles) {
            this.dustParticles = [];
            for (let i = 0; i < 120; i++) {
                this.dustParticles.push({
                    x: Math.random() * this.fullCanvas.width,
                    y: Math.random() * this.fullCanvas.height,
                    size: 0.5 + Math.random() * 2.5,
                    speedY: 0.2 + Math.random() * 0.6,
                    speedX: (Math.random() - 0.5) * 0.3,
                    hue: 240 + Math.random() * 60,
                    phase: Math.random() * Math.PI * 2,
                    opacity: 0.2 + Math.random() * 0.6
                });
            }
        }

        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.12)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

        const avg = this.dataArray.reduce((a, b) => a + b, 0) / this.dataArray.length;
        const energy = avg / 255;

        const time = Date.now() / 1000;

        for (let p of this.dustParticles) {
            p.y -= p.speedY * (1 + energy * 20);
            p.x += p.speedX + Math.sin(time + p.phase) * 0.3;

            if (p.y < -5) p.y = this.fullCanvas.height + 5;
            if (p.x < -5) p.x = this.fullCanvas.width + 5;
            if (p.x > this.fullCanvas.width + 5) p.x = -5;

            const pulse = 1 + energy * 12;
            const drawSize = p.size * pulse;

            const hue = (p.hue + time * 15) % 360;
            const alpha = p.opacity * (0.5 + energy * 0.5);

            this.fullCtx.beginPath();
            this.fullCtx.arc(p.x, p.y, drawSize, 0, Math.PI * 2);
            this.fullCtx.fillStyle = `hsla(${hue}, 80%, 65%, ${alpha})`;
            this.fullCtx.shadowBlur = drawSize * 3;
            this.fullCtx.shadowColor = `hsla(${hue}, 80%, 65%, 0.4)`;
            this.fullCtx.fill();
            this.fullCtx.shadowBlur = 0;
        }
    }

    drawDNAHelix() {
        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.15)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

        const centerY = this.fullCanvas.height / 2;
        const time = Date.now() / 1000;
        const pointCount = 200;

        const colorShift = Math.sin(time * 0.3) * 30;

        const rotationAngle = time * 0.8;

        for (let helix = 0; helix < 2; helix++) {
            this.fullCtx.beginPath();

            const points = [];

            for (let i = 0; i < pointCount; i++) {
                const t = i / pointCount;
                const x = t * this.fullCanvas.width;

                const dataIndex = Math.floor(t * this.dataArray.length * 0.7);
                const value = this.dataArray[dataIndex] || 0;
                const amplitude = (value / 255) * 100 + 50;

                const offset = helix * Math.PI;
                const helixAngle = t * Math.PI * 4 + offset;

                const yBase = Math.sin(helixAngle) * amplitude;
                const zBase = Math.cos(helixAngle) * amplitude; 
                const yRotated = yBase * Math.cos(rotationAngle) - zBase * Math.sin(rotationAngle);
                const zRotated = yBase * Math.sin(rotationAngle) + zBase * Math.cos(rotationAngle);

                const y = centerY + yRotated;

                points.push({ x, y, z: zRotated, value });
            }

            for (let i = 1; i < points.length; i++) {
                const p1 = points[i - 1];
                const p2 = points[i];

                const maxAmplitude = 150; 
                const avgZ = (p1.z + p2.z) / 2;
                const depthFactor = (avgZ + maxAmplitude) / (maxAmplitude * 2); // 0 to 1
                const opacity = 0.3 + depthFactor * 0.7; // Min 30%, max 100%

                // Line thickness varies with depth (closer = thicker)
                const lineWidth = 2 + depthFactor * 4;

                const baseHue = helix === 0 ? 240 : 280;
                const hue = baseHue + colorShift;
                const saturation = 70;
                const lightness = 55 + Math.sin(time * 0.5) * 10;

                this.fullCtx.beginPath();
                this.fullCtx.moveTo(p1.x, p1.y);
                this.fullCtx.lineTo(p2.x, p2.y);
                this.fullCtx.strokeStyle = `hsla(${hue}, ${saturation}%, ${lightness}%, ${opacity})`;
                this.fullCtx.lineWidth = lineWidth;
                this.fullCtx.lineCap = 'round';
                this.fullCtx.shadowBlur = 15 * depthFactor;
                this.fullCtx.shadowColor = `hsla(${hue}, ${saturation}%, ${lightness}%, ${opacity})`;
                this.fullCtx.stroke();
                this.fullCtx.shadowBlur = 0;
            }
        }

        for (let i = 0; i < pointCount; i += 8) {
            const t = i / pointCount;
            const x = t * this.fullCanvas.width;

            const dataIndex = Math.floor(t * this.dataArray.length * 0.7);
            const value = this.dataArray[dataIndex] || 0;
            const amplitude = (value / 255) * 100 + 50;

            const angle1 = t * Math.PI * 4;
            const angle2 = t * Math.PI * 4 + Math.PI;

            const y1Base = Math.sin(angle1) * amplitude;
            const z1Base = Math.cos(angle1) * amplitude;
            const y2Base = Math.sin(angle2) * amplitude;
            const z2Base = Math.cos(angle2) * amplitude;

            const y1 = centerY + (y1Base * Math.cos(rotationAngle) - z1Base * Math.sin(rotationAngle));
            const y2 = centerY + (y2Base * Math.cos(rotationAngle) - z2Base * Math.sin(rotationAngle));
            const z1 = y1Base * Math.sin(rotationAngle) + z1Base * Math.cos(rotationAngle);
            const z2 = y2Base * Math.sin(rotationAngle) + z2Base * Math.cos(rotationAngle);

            const maxAmplitude = 150;
            const avgZ = (z1 + z2) / 2;
            const depthFactor = (avgZ + maxAmplitude) / (maxAmplitude * 2);
            const opacity = 0.2 + depthFactor * 0.5;

            const rungHue = 260 + colorShift;

            this.fullCtx.beginPath();
            this.fullCtx.moveTo(x, y1);
            this.fullCtx.lineTo(x, y2);
            this.fullCtx.strokeStyle = `hsla(${rungHue}, 60%, 70%, ${opacity})`;
            this.fullCtx.lineWidth = 2;
            this.fullCtx.stroke();
        }
    }

    async initThreeSphere(canvasId) {
        if (!this.analyser || !this.dataArray) {
            console.error('[VisualizerManager] Audio not initialized yet');
            return false;
        }

        await new Promise(resolve => {
            const check = () => {
                const canvas = document.getElementById(canvasId);
                if (canvas && canvas.offsetWidth > 300 && canvas.offsetHeight > 150) {
                    resolve();
                } else {
                    requestAnimationFrame(check);
                }
            };
            check();
        });

        window.threeSphere.initialize(canvasId, this.analyser, this.dataArray);
        console.log('[VisualizerManager] ThreeSphere initialized');
    }
}

window.visualizerManager = new VisualizerManager();