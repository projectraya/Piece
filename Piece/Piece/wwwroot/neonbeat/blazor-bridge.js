window.NeonBeatBridge = {
    loadSongFromBytes: async function (title, bpm, dotnetBytes) {
        const arrayBuffer = dotnetBytes.buffer.slice(
            dotnetBytes.byteOffset,
            dotnetBytes.byteOffset + dotnetBytes.byteLength
        );

        const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        const audioBuffer = await audioCtx.decodeAudioData(arrayBuffer);
        audioCtx.close();

        if (!window._game) {
            if (typeof Game === 'undefined') {
                console.error('[NeonBeatBridge] Game class not found');
                return;
            }
            window._game = new Game();
        }

        window._game.startGame({
            song: { title, bpm, audioBuffer },
            playMode: 'auto-medium'
        });
    }
};