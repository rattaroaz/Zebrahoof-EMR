// Other tabs of this browser leave the chart when one tab signs out.
(function () {
    var key = "zebrahoof.signOut";

    function leave() {
        if (location.pathname === "/login" || location.pathname === "/logout") {
            return;
        }
        window.location.replace("/login");
    }

    if (location.search.indexOf("logout=success") !== -1) {
        try {
            localStorage.setItem(key, String(Date.now()));
        } catch (err) {
            console.warn("Could not publish sign-out", err);
        }
    }

    document.addEventListener("click", function (event) {
        var node = event.target && event.target.closest ? event.target.closest("a[href]") : null;
        if (!node) {
            return;
        }
        var href = node.getAttribute("href") || "";
        if (href === "/logout" || href.indexOf("/logout?") === 0 || href === "/account/logout" || href.indexOf("/account/logout?") === 0) {
            try {
                sessionStorage.setItem("zebrahoof.signingOut", "1");
            } catch (err) {
                console.warn("Could not mark sign-out", err);
            }
        }
    }, true);

    window.addEventListener("storage", function (event) {
        if (event.key === key && event.newValue) {
            leave();
        }
    });
})();
