function setSessionCookie(name, value) {
    var expires = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toUTCString();
    document.cookie = name + "=" + encodeURIComponent(value) + ";expires=" + expires + ";path=/;SameSite=Lax";
}

function getSessionCookie(name) {
    var match = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
}

function clearSessionCookie(name) {
    document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;SameSite=Lax";
}

window.kerrandoSession = {
    setPlayerId: function (id) {
        if (!id) return;
        try {
            localStorage.setItem('kerrando_player_id', id);
            // Sauvegarde aussi dans un cookie pour robustesse multi-onglets iOS / Safari
            setSessionCookie('kerrando_player_id', id);
        } catch (e) {
            console.warn("Storage warning:", e);
        }
    },

    getPlayerId: function () {
        try {
            var localId = localStorage.getItem('kerrando_player_id');
            if (localId) return localId;

            // Fallback cookie
            var cookieId = getSessionCookie('kerrando_player_id');
            if (cookieId) return cookieId;
        } catch (e) {
            console.warn("Storage reading warning:", e);
        }
        return null;
    },

    clearPlayerId: function () {
        try {
            localStorage.removeItem('kerrando_player_id');
            clearSessionCookie('kerrando_player_id');
        } catch (e) {
            console.warn("Storage clear warning:", e);
        }
    },

    setTeamId: function (id) {
        if (!id) return;
        localStorage.setItem('kerrando_team_id', id);
        setSessionCookie('kerrando_team_id', id);
    },

    getTeamId: function () {
        return localStorage.getItem('kerrando_team_id') || getSessionCookie('kerrando_team_id');
    },

    clearTeamId: function () {
        localStorage.removeItem('kerrando_team_id');
        clearSessionCookie('kerrando_team_id');
    },

    setDeviceToken: function (token) {
        if (!token) return;
        localStorage.setItem('kerrando_device_token', token);
        setSessionCookie('kerrando_device_token', token);
    },

    getDeviceToken: function () {
        return localStorage.getItem('kerrando_device_token') || getSessionCookie('kerrando_device_token');
    },

    clearDeviceToken: function () {
        localStorage.removeItem('kerrando_device_token');
        clearSessionCookie('kerrando_device_token');
    }
};

window.kerrandoImageViewer = (() => {
    let cleanup;

    return {
        open(id) {
            if (cleanup) cleanup();
            const root = document.getElementById(id);
            const viewport = root.querySelector('.image-viewer-viewport');
            const image = root.querySelector('img');
            const output = root.querySelector('output');
            const zoomOut = root.querySelector('[data-image-action="out"]');
            const zoomIn = root.querySelector('[data-image-action="in"]');
            const previousFocus = document.activeElement;
            let scale = 1;
            let drag;

            function resize() {
                if (!image.naturalWidth) return;
                const fit = Math.min(viewport.clientWidth / image.naturalWidth,
                    viewport.clientHeight / image.naturalHeight, 1);
                image.style.width = `${image.naturalWidth * fit * scale}px`;
                image.style.height = `${image.naturalHeight * fit * scale}px`;
                output.textContent = `${Math.round(scale * 100)} %`;
                zoomOut.disabled = scale <= 1;
                zoomIn.disabled = scale >= 4;
            }

            function zoom(amount) {
                const previousScale = scale;
                const centerX = viewport.scrollLeft + viewport.clientWidth / 2;
                const centerY = viewport.scrollTop + viewport.clientHeight / 2;
                scale = Math.min(4, Math.max(1, scale + amount));
                resize();
                viewport.scrollLeft = centerX * scale / previousScale - viewport.clientWidth / 2;
                viewport.scrollTop = centerY * scale / previousScale - viewport.clientHeight / 2;
            }

            async function click(event) {
                const action = event.target.closest('[data-image-action]')?.dataset.imageAction;
                if (action === 'in') zoom(0.5);
                if (action === 'out') zoom(-0.5);
                if (action === 'reset') {
                    scale = 1;
                    resize();
                    viewport.scrollTo(0, 0);
                }
                if (action === 'fullscreen') {
                    try {
                        if (document.fullscreenElement === root) await document.exitFullscreen();
                        else if (root.requestFullscreen) await root.requestFullscreen();
                    } catch {
                    }
                }
            }

            function wheel(event) {
                event.preventDefault();
                zoom(event.deltaY < 0 ? 0.5 : -0.5);
            }

            function pointerDown(event) {
                if (event.pointerType !== 'mouse' || scale === 1) return;
                drag = { x: event.clientX, y: event.clientY, left: viewport.scrollLeft, top: viewport.scrollTop };
                viewport.setPointerCapture(event.pointerId);
            }

            function pointerMove(event) {
                if (!drag) return;
                viewport.scrollLeft = drag.left - event.clientX + drag.x;
                viewport.scrollTop = drag.top - event.clientY + drag.y;
            }

            function pointerUp() { drag = null; }

            function keyDown(event) {
                if (event.key === 'Escape' && !document.fullscreenElement) {
                    root.querySelector('[aria-label="Fermer l\'image"]').click();
                }
                if (event.key === '+' || event.key === '=') zoom(0.5);
                if (event.key === '-') zoom(-0.5);
            }

            const observer = new ResizeObserver(resize);
            observer.observe(viewport);
            root.addEventListener('click', click);
            root.addEventListener('keydown', keyDown);
            viewport.addEventListener('wheel', wheel, { passive: false });
            viewport.addEventListener('pointerdown', pointerDown);
            viewport.addEventListener('pointermove', pointerMove);
            viewport.addEventListener('pointerup', pointerUp);
            viewport.addEventListener('pointercancel', pointerUp);
            image.addEventListener('load', resize);
            viewport.focus();
            resize();

            cleanup = () => {
                observer.disconnect();
                root.removeEventListener('click', click);
                root.removeEventListener('keydown', keyDown);
                viewport.removeEventListener('wheel', wheel);
                viewport.removeEventListener('pointerdown', pointerDown);
                viewport.removeEventListener('pointermove', pointerMove);
                viewport.removeEventListener('pointerup', pointerUp);
                viewport.removeEventListener('pointercancel', pointerUp);
                image.removeEventListener('load', resize);
                previousFocus?.focus();
                cleanup = null;
            };
        },

        async close() {
            if (document.fullscreenElement?.id === 'game-image-viewer') {
                await document.exitFullscreen();
            }
            if (cleanup) cleanup();
        }
    };
})();
