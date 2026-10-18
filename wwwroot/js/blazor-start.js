// The server drops a circuit that stays quiet for ClientTimeoutInterval (30s).
// Ping every 5s, which is under half that window, and treat 20s of silence as a dropped network.
Blazor.start({
    circuit: {
        configureSignalR: function (builder) {
            builder.withKeepAliveInterval(5000);
            builder.withServerTimeout(20000);
        }
    }
});
