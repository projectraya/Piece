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

            // Handle resize
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
        const barCount = 64;
        const barWidth = (this.fullCanvas.width / barCount) * 0.8;
        const spacing = this.fullCanvas.width / barCount;

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const height = (value / 255) * this.fullCanvas.height * 0.85;
            const x = i * spacing + (spacing - barWidth) / 2;
            const y = this.fullCanvas.height - height;

            const time = Date.now() / 300;
            const wave1 = Math.sin(time + i * 0.2) * 15;
            const wave2 = Math.cos(time * 1.3 + i * 0.15) * 10;
            const waveHeight = wave1 + wave2;

            const t = i / barCount;
            const hue = 240 + (t * 60);
            const saturation = 70 + (t * 30);
            const lightness = 50 + (value / 255) * 30;

            this.fullCtx.beginPath();
            this.fullCtx.moveTo(x, this.fullCanvas.height);

            this.fullCtx.quadraticCurveTo(
                x - 5,
                this.fullCanvas.height - height / 2,
                x,
                y + 20
            );

            this.fullCtx.bezierCurveTo(
                x + barWidth * 0.25, y + waveHeight - 10,
                x + barWidth * 0.75, y - waveHeight - 10,
                x + barWidth, y + 20
            );

            this.fullCtx.quadraticCurveTo(
                x + barWidth + 5,
                this.fullCanvas.height - height / 2,
                x + barWidth,
                this.fullCanvas.height
            );

            this.fullCtx.closePath();

            const gradient = this.fullCtx.createLinearGradient(0, y, 0, this.fullCanvas.height);
            gradient.addColorStop(0, `hsla(${hue}, ${saturation}%, ${lightness + 20}%, 0.9)`);
            gradient.addColorStop(1, `hsla(${hue}, ${saturation}%, ${lightness}%, 0.7)`);

            this.fullCtx.fillStyle = gradient;
            this.fullCtx.fill();

            this.fullCtx.shadowBlur = 25;
            this.fullCtx.shadowColor = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
            this.fullCtx.fill();
            this.fullCtx.shadowBlur = 0;

            this.fullCtx.strokeStyle = `rgba(255, 255, 255, ${0.4 + (value / 255) * 0.4})`;
            this.fullCtx.lineWidth = 3;
            this.fullCtx.lineCap = 'round';
            this.fullCtx.beginPath();
            this.fullCtx.bezierCurveTo(
                x + barWidth * 0.25, y + waveHeight - 10,
                x + barWidth * 0.75, y - waveHeight - 10,
                x + barWidth, y + 20
            );
            this.fullCtx.stroke();

            if (value > 180) {
                const dripY = y + 30 + Math.sin(time * 2 + i) * 5;
                this.fullCtx.beginPath();
                this.fullCtx.arc(x + barWidth / 2, dripY, 3, 0, Math.PI * 2);
                this.fullCtx.fillStyle = `hsla(${hue}, ${saturation}%, ${lightness}%, 0.6)`;
                this.fullCtx.fill();
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
        if (!this.particles) {
            this.particles = [];
        }

        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.05)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

        const baseY = this.fullCanvas.height;

        const streamCount = 48; 
        for (let i = 0; i < streamCount; i++) {
            const dataIndex = Math.floor((i / streamCount) * this.dataArray.length * 0.6);
            const value = this.dataArray[dataIndex] || 0;

            const spawnChance = (value / 255) * 0.8; 
            if (Math.random() < spawnChance && value > 20) {
               
                const xPos = (i / streamCount) * this.fullCanvas.width;

                const maxVelocity = Math.sqrt(2 * 0.4 * this.fullCanvas.height);
                const velocity = 8 + (value / 255) * (maxVelocity - 8);
                const t = i / streamCount;
                const hue = 240 + (t * 60);

                this.particles.push({
                    x: xPos,
                    y: baseY,
                    vx: (Math.random() - 0.5) * 3, 
                    vy: -velocity,
                    life: 1,
                    hue: hue,
                    size: 2 + (value / 255) * 5,
                    brightness: 50 + (value / 255) * 30
                });
            }
        }

        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];

            p.vy += 0.4;
            p.x += p.vx;
            p.y += p.vy;
            p.life -= 0.008; 

            if (p.life <= 0 || p.y > this.fullCanvas.height + 100) {
                this.particles.splice(i, 1);
                continue;
            }

            const alpha = p.life;

            this.fullCtx.beginPath();
            this.fullCtx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
            this.fullCtx.fillStyle = `hsla(${p.hue}, 80%, ${p.brightness}%, ${alpha})`;
            this.fullCtx.shadowBlur = 20;
            this.fullCtx.shadowColor = `hsla(${p.hue}, 80%, ${p.brightness}%, ${alpha})`;
            this.fullCtx.fill();
            this.fullCtx.shadowBlur = 0;

            this.fullCtx.beginPath();
            this.fullCtx.arc(p.x, p.y, p.size * 0.4, 0, Math.PI * 2);
            this.fullCtx.fillStyle = `hsla(${p.hue}, 100%, 90%, ${alpha * 0.8})`;
            this.fullCtx.fill();
        }

        if (this.particles.length > 3000) {
            this.particles = this.particles.slice(-3000);
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

    initThreeSphere(canvasId) {
        if (!this.analyser || !this.dataArray) {
            console.error('[VisualizerManager] Audio not initialized yet');
            return false;
        }

        window.threeSphere.initialize(canvasId, this.analyser, this.dataArray);
        console.log('[VisualizerManager] ThreeSphere initialized');
    }
}

window.visualizerManager = new VisualizerManager();