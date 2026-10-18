// Real input slides the idle window. Circuit and hub traffic do not.
(function () {
    let activityPostedAt = 0;

    async function postActivity(force) {
        const now = Date.now();
        if (!force && now - activityPostedAt < 60000) {
            return;
        }
        activityPostedAt = now;
        try {
            await fetch('/account/activity', {
                method: 'POST',
                credentials: 'same-origin',
                redirect: 'manual'
            });
        } catch (err) {
            activityPostedAt = 0;
            console.warn('Activity refresh failed', err);
        }
    }

    ['pointerdown', 'keydown', 'input', 'wheel', 'touchmove'].forEach(function (type) {
        document.addEventListener(type, function () { postActivity(false); }, { capture: true, passive: true });
    });

    window.zebrahoofPostActivity = function () {
        return postActivity(true);
    };
})();
