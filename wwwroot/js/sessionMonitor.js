// Classic script. The ESM build of @microsoft/signalr uses extensionless imports the browser cannot load.
window.sessionMonitor = (function () {
    let connection = null;
    let heartbeatInterval = null;
    let dotNetRef = null;
    let lastInputAt = 0;
    let reportedInputAt = 0;

    function noteUserInput() {
        lastInputAt = Date.now();
    }

    function wireUserInput() {
        if (wireUserInput.wired) {
            return;
        }
        wireUserInput.wired = true;
        ["pointerdown", "keydown", "input", "wheel", "touchmove"].forEach(function (type) {
            document.addEventListener(type, noteUserInput, { capture: true, passive: true });
        });
    }

    function getCookie(name) {
        const value = "; " + document.cookie;
        const parts = value.split("; " + name + "=");
        if (parts.length === 2) {
            return parts.pop().split(";").shift();
        }
        return null;
    }

    async function invokeDotNet(method) {
        if (!dotNetRef) {
            return;
        }
        const args = Array.prototype.slice.call(arguments, 1);
        try {
            await dotNetRef.invokeMethodAsync.apply(dotNetRef, [method].concat(args));
        } catch (err) {
            console.warn("Failed to notify .NET", err);
        }
    }

    async function sendHeartbeat() {
        try {
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                const userActive = lastInputAt > reportedInputAt;
                reportedInputAt = Date.now();
                await connection.invoke("Heartbeat", userActive);
            }
        } catch (err) {
            console.warn("Heartbeat failed", err);
        }
    }

    async function start(dotNet, sessionIdCookieName) {
        dotNetRef = dotNet;
        const sessionId = getCookie(sessionIdCookieName);
        if (!sessionId || typeof signalR === "undefined") {
            console.warn("Session monitor idle: missing session or SignalR");
            return;
        }

        if (connection) {
            await connection.stop();
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/session?sessionId=" + sessionId)
            .withAutomaticReconnect()
            .withKeepAliveInterval(5000)
            .withServerTimeout(20000)
            .build();

        connection.on("IdleWarning", function (message) {
            invokeDotNet("OnIdleWarning", message);
        });

        connection.on("IdleTimeUpdated", function (idleSeconds, absoluteSeconds) {
            invokeDotNet("OnIdleTimeUpdate", idleSeconds, absoluteSeconds);
        });

        connection.on("ForceLogout", async function (message) {
            await invokeDotNet("OnForceLogout", message || "Session ended");
            await stop();
            window.location.href = "/account/logout";
        });

        connection.onclose(function () {
            clearInterval(heartbeatInterval);
            if (dotNetRef) {
                invokeDotNet("OnIdleTimeUpdate", 0, 0);
            }
        });

        wireUserInput();
        try {
            await connection.start();
        } catch (err) {
            console.warn("Session hub did not start", err);
            return;
        }
        sendHeartbeat();
        heartbeatInterval = setInterval(sendHeartbeat, 30000);
    }

    async function noteActive() {
        lastInputAt = Date.now();
        try {
            await sendHeartbeat();
        } catch (err) {
            console.warn("Active heartbeat failed", err);
        }
    }

    async function stop() {
        if (heartbeatInterval) {
            clearInterval(heartbeatInterval);
            heartbeatInterval = null;
        }
        if (connection) {
            try {
                await connection.stop();
            } catch (err) {
                console.warn("Unable to stop session monitor", err);
            }
            connection = null;
        }
        dotNetRef = null;
    }

    return { start: start, stop: stop, noteActive: noteActive };
})();
