class ThreeSphere {
    constructor() {
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.sphere = null;
        this.controls = null;
        this.analyser = null;
        this.dataArray = null;
        this.animationId = null;
        this.isInitialized = false;
        this.originalPositions = [];
        this.impacts = [];
        this.lastBeatTime = 0;
    }

    async initialize(canvasId, analyser, dataArray) {
        console.log('[ThreeSphere] initialize called, isInitialized:', this.isInitialized);

        if (this.isInitialized) {
            console.log('[ThreeSphere] Disposing...');
            if (this.animationId) cancelAnimationFrame(this.animationId);
            if (this.renderer) this.renderer.dispose();
            if (this.controls) this.controls.dispose();
            this.isInitialized = false;
            this.scene = null;
            this.camera = null;
            this.renderer = null;
            this.sphere = null;
            this.originalPositions = [];
            this.impacts = [];
        }

        await new Promise(resolve => setTimeout(resolve, 150));

        const canvas = document.getElementById(canvasId);
        console.log('[ThreeSphere] Canvas found:', canvas);
        console.log('[ThreeSphere] Canvas size:', canvas?.width, canvas?.height);

        // Вземи нов WebGL контекст
        const gl = canvas?.getContext('webgl2') || canvas?.getContext('webgl');
        console.log('[ThreeSphere] WebGL context:', gl);
        if (!canvas) {
            console.error('[ThreeSphere] Canvas not found:', canvasId);
            return false;
        }

        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x000000);

        this.camera = new THREE.PerspectiveCamera(
            75,
            window.innerWidth / window.innerHeight,
            0.1,
            1000
        );
        this.camera.position.z = 3;

        this.renderer = new THREE.WebGLRenderer({
            canvas: canvas,
            antialias: true
        });
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);

        const geometry = new THREE.SphereGeometry(1, 64, 64);

        const positions = geometry.attributes.position.array;
        for (let i = 0; i < positions.length; i++) {
            this.originalPositions.push(positions[i]);
        }

        geometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(positions.length), 3));

        const material = new THREE.MeshBasicMaterial({
            wireframe: true,
            vertexColors: true,
            transparent: true,
            opacity: 0.8
        });

        this.sphere = new THREE.Mesh(geometry, material);
        this.scene.add(this.sphere);

        if (typeof THREE.OrbitControls === 'undefined') {
            console.error('[ThreeSphere] OrbitControls not found');
            return false;
        }

        this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.enableZoom = true;
        this.controls.autoRotate = true;
        this.controls.autoRotateSpeed = 0.5;

        window.addEventListener('resize', () => this.onWindowResize());

        this.isInitialized = true;
        console.log('[ThreeSphere] Initialized successfully');

        this.animate();
        return true;
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        if (!this.sphere || !this.analyser || !this.dataArray) return;

        this.analyser.getByteFrequencyData(this.dataArray);

        this.detectBeats();

        this.updateSphereWithImpacts();

        this.updateColors();

        if (this.controls) {
            this.controls.update();
        }

        this.renderer.render(this.scene, this.camera);
    }

    detectBeats() {
        const bassIndex = Math.floor(this.dataArray.length * 0.05);
        const lowMidIndex = Math.floor(this.dataArray.length * 0.15);
        const midIndex = Math.floor(this.dataArray.length * 0.3);
        const highMidIndex = Math.floor(this.dataArray.length * 0.5);
        const trebleIndex = Math.floor(this.dataArray.length * 0.7);

        const bass = this.dataArray[bassIndex] || 0;
        const lowMid = this.dataArray[lowMidIndex] || 0;
        const mid = this.dataArray[midIndex] || 0;
        const highMid = this.dataArray[highMidIndex] || 0;
        const treble = this.dataArray[trebleIndex] || 0;

        const now = Date.now();
        const timeSinceLastBeat = now - this.lastBeatTime;

        if (timeSinceLastBeat > 50) {
            if (bass > 120) {
                this.createImpact('bass', -0.35);
                this.lastBeatTime = now;
            }
            else if (lowMid > 110) {
                this.createImpact('lowmid', -0.25);
                this.lastBeatTime = now;
            }
            else if (mid > 100) {
                this.createImpact('mid', 0.2);
                this.lastBeatTime = now;
            }
            else if (highMid > 90) {
                this.createImpact('highmid', 0.18);
                this.lastBeatTime = now;
            }
            else if (treble > 80) {
                this.createImpact('treble', 0.15);
                this.lastBeatTime = now;
            }
        }
    }

    createImpact(type, strength) {
        const phi = Math.random() * Math.PI;
        const theta = Math.random() * Math.PI * 2;

        const impactX = Math.sin(phi) * Math.cos(theta);
        const impactY = Math.cos(phi);
        const impactZ = Math.sin(phi) * Math.sin(theta);

        this.impacts.push({
            x: impactX,
            y: impactY,
            z: impactZ,
            strength: strength,
            radius: 0.4,
            time: 0,
            duration: 0.4,
            type: type
        });
    }

    updateSphereWithImpacts() {
        const geometry = this.sphere.geometry;
        const positions = geometry.attributes.position.array;
        const time = Date.now() / 1000;
        const deltaTime = 1 / 60;

        for (let i = this.impacts.length - 1; i >= 0; i--) {
            this.impacts[i].time += deltaTime;

            if (this.impacts[i].time >= this.impacts[i].duration) {
                this.impacts.splice(i, 1);
            }
        }

        for (let i = 0; i < positions.length; i += 3) {

            const x = this.originalPositions[i];
            const y = this.originalPositions[i + 1];
            const z = this.originalPositions[i + 2];

            const length = Math.sqrt(x * x + y * y + z * z);
            const nx = x / length;
            const ny = y / length;
            const nz = z / length;

            const wave1 = Math.sin(time * 0.4 + nx * 2) * 0.02;
            const wave2 = Math.cos(time * 0.3 + ny * 3) * 0.015;
            let totalDeformation = wave1 + wave2;

            for (let impact of this.impacts) {
                const dx = nx - impact.x;
                const dy = ny - impact.y;
                const dz = nz - impact.z;
                const dist = Math.sqrt(dx * dx + dy * dy + dz * dz);

                if (dist < impact.radius) {
                    const progress = impact.time / impact.duration;
                    const curve = Math.sin(progress * Math.PI);

                    const falloff = 1 - (dist / impact.radius);

                    const impactAmount = impact.strength * curve * falloff;
                    totalDeformation += impactAmount;
                }
            }

            const scale = 1 + totalDeformation;
            positions[i] = nx * scale;
            positions[i + 1] = ny * scale;
            positions[i + 2] = nz * scale;
        }

        geometry.attributes.position.needsUpdate = true;
        geometry.computeVertexNormals();
    }

    updateColors() {
        const geometry = this.sphere.geometry;
        const colors = geometry.attributes.color.array;
        const time = Date.now() / 1000;

        for (let i = 0; i < colors.length; i += 3) {
            const x = geometry.attributes.position.array[i];
            const y = geometry.attributes.position.array[i + 1];
            const z = geometry.attributes.position.array[i + 2];

            const angle = Math.atan2(z, x) + Math.PI;
            const hue = ((angle / (Math.PI * 2)) * 360 + time * 20) % 360;

            const rgb = this.hslToRgb(hue / 360, 0.7, 0.6);

            colors[i] = rgb[0];
            colors[i + 1] = rgb[1];
            colors[i + 2] = rgb[2];
        }

        geometry.attributes.color.needsUpdate = true;
    }

    hslToRgb(h, s, l) {
        let r, g, b;

        if (s === 0) {
            r = g = b = l;
        } else {
            const hue2rgb = (p, q, t) => {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1 / 6) return p + (q - p) * 6 * t;
                if (t < 1 / 2) return q;
                if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
                return p;
            };

            const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            const p = 2 * l - q;
            r = hue2rgb(p, q, h + 1 / 3);
            g = hue2rgb(p, q, h);
            b = hue2rgb(p, q, h - 1 / 3);
        }

        return [r, g, b];
    }

    onWindowResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
    }

    dispose() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
        }
        if (this.renderer) {
            this.renderer.dispose();
        }
        if (this.controls) {
            this.controls.dispose();
        }
        window.removeEventListener('resize', this.onWindowResize);
        this.isInitialized = false;
    }
}

window.threeSphere = new ThreeSphere();