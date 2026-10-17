import { HubConnectionBuilder, HubConnectionState } from "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/esm/index.js";

let connection;
let heartbeatInterval;
let dotNetRef;
let lastInputAt = 0;
let reportedInputAt = 0;
let activityPostedAt = 0;
let inputWired = false;

function noteUserInput() {
    lastInputAt = Date.now();
}

async function postActivity() {
    const now = Date.now();
    if (now - activityPostedAt < 60000) {
        return;
    }
    activityPostedAt = now;
    try {
        await fetch("/account/activity", { method: "POST", credentials: "same-origin", redirect: "manual" });
    } catch (err) {
        console.warn("Activity refresh failed", err);
    }
}

function onUserInput() {
    noteUserInput();
    postActivity();
}

function wireUserInput() {
    if (inputWired) {
        return;
    }
    inputWired = true;
    ["pointerdown", "keydown", "input"].forEach(type => {
        document.addEventListener(type, onUserInput, true);
    });
}

export async function start(dotNet, sessionIdCookieName, refreshEndpoint) {
    dotNetRef = dotNet;
    const sessionId = getCookie(sessionIdCookieName);
    if (!sessionId) {
        return;
    }

    if (connection) {
        await connection.stop();
    }

    connection = new HubConnectionBuilder()
        .withUrl(`/hubs/session?sessionId=${sessionId}`)
        .withAutomaticReconnect()
        .build();

    connection.on("IdleWarning", async message => {
        await invokeDotNet("OnIdleWarning", message);
    });

    connection.on("IdleTimeUpdated", async (idleSeconds, absoluteSeconds) => {
        await invokeDotNet("OnIdleTimeUpdate", idleSeconds, absoluteSeconds);
    });

    connection.on("ForceLogout", async message => {
        await invokeDotNet("OnForceLogout", message ?? "Session ended");
        await stop();
        window.location.href = "/account/logout";
    });

    connection.onclose(async () => {
        clearInterval(heartbeatInterval);
        if (dotNetRef) {
            await invokeDotNet("OnIdleTimeUpdate", 0, 0);
        }
    });

    wireUserInput();
    await connection.start();
    sendHeartbeat();
    heartbeatInterval = setInterval(sendHeartbeat, 30000);

    async function sendHeartbeat() {
        try {
            if (connection && connection.state === HubConnectionState.Connected) {
                const userActive = lastInputAt > reportedInputAt;
                reportedInputAt = Date.now();
                await connection.invoke("Heartbeat", userActive);
            }
        } catch (err) {
            console.warn("Heartbeat failed", err);
        }
    }
}

export async function stop() {
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

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
        return parts.pop().split(';').shift();
    }
    return null;
}

async function invokeDotNet(method, ...args) {
    if (!dotNetRef) {
        return;
    }
    try {
        await dotNetRef.invokeMethodAsync(method, ...args);
    } catch (err) {
        console.warn("Failed to notify .NET", err);
    }
}
