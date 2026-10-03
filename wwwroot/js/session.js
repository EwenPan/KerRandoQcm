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

window.kerrandoDialogs = {
    open(id) {
        const dialog = document.getElementById(id);
        if (dialog && !dialog.open) dialog.showModal();
    }
};

window.kerrandoImageViewer = (() => {
    let cleanup;

    return {
        open(id) {
            if (cleanup) cleanup();
            const root = document.getElementById(id);
            const viewport = root.querySelector('.image-viewer-viewport');
            const stage = root.querySelector('.image-viewer-stage');
            const image = root.querySelector('img');
            const output = root.querySelector('output');
            const zoomOut = root.querySelector('[data-image-action="out"]');
            const zoomIn = root.querySelector('[data-image-action="in"]');
            const previousFocus = document.activeElement;
            const previousOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
            let scale = 1;
            const pointers = new Map();

            function resize() {
                if (!image.naturalWidth) return;
                const fit = Math.min(viewport.clientWidth / image.naturalWidth,
                    viewport.clientHeight / image.naturalHeight, 1);
                const width = image.naturalWidth * fit * scale;
                const height = image.naturalHeight * fit * scale;
                image.style.width = `${width}px`;
                image.style.height = `${height}px`;
                stage.style.width = `${Math.max(viewport.clientWidth, width)}px`;
                stage.style.height = `${Math.max(viewport.clientHeight, height)}px`;
                output.textContent = `${Math.round(scale * 100)} %`;
                zoomOut.disabled = scale <= 1;
                zoomIn.disabled = scale >= 4;
            }

            function zoomTo(nextScale, anchorX = viewport.clientWidth / 2, anchorY = viewport.clientHeight / 2,
                targetX = anchorX, targetY = anchorY) {
                if (!image.clientWidth || !image.clientHeight) return;
                const offsetX = Math.max(0, (viewport.clientWidth - image.clientWidth) / 2);
                const offsetY = Math.max(0, (viewport.clientHeight - image.clientHeight) / 2);
                const imageX = (viewport.scrollLeft + anchorX - offsetX) / image.clientWidth;
                const imageY = (viewport.scrollTop + anchorY - offsetY) / image.clientHeight;
                scale = Math.min(4, Math.max(1, nextScale));
                resize();
                viewport.scrollLeft = Math.max(0, (viewport.clientWidth - image.clientWidth) / 2)
                    + imageX * image.clientWidth - targetX;
                viewport.scrollTop = Math.max(0, (viewport.clientHeight - image.clientHeight) / 2)
                    + imageY * image.clientHeight - targetY;
            }

            function zoom(amount) {
                zoomTo(scale + amount);
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
                if (event.pointerType === 'mouse' && event.button !== 0) return;
                pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
                viewport.setPointerCapture(event.pointerId);
            }

            function pointerMove(event) {
                const previous = pointers.get(event.pointerId);
                if (!previous) return;
                const oldPoints = [...pointers.values()];
                pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
                if (pointers.size === 2) {
                    const newPoints = [...pointers.values()];
                    const oldDistance = Math.hypot(oldPoints[1].x - oldPoints[0].x, oldPoints[1].y - oldPoints[0].y);
                    const newDistance = Math.hypot(newPoints[1].x - newPoints[0].x, newPoints[1].y - newPoints[0].y);
                    if (oldDistance === 0) return;
                    const rect = viewport.getBoundingClientRect();
                    zoomTo(scale * newDistance / oldDistance,
                        (oldPoints[0].x + oldPoints[1].x) / 2 - rect.left,
                        (oldPoints[0].y + oldPoints[1].y) / 2 - rect.top,
                        (newPoints[0].x + newPoints[1].x) / 2 - rect.left,
                        (newPoints[0].y + newPoints[1].y) / 2 - rect.top);
                } else if (pointers.size === 1) {
                    viewport.scrollLeft -= event.clientX - previous.x;
                    viewport.scrollTop -= event.clientY - previous.y;
                }
            }

            function pointerUp(event) { pointers.delete(event.pointerId); }

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
                document.body.style.overflow = previousOverflow;
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
