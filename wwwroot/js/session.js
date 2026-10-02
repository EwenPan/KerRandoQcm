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
