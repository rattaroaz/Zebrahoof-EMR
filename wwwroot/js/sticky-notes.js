window.stickyNoteDrag = {
    startDrag: function(elementId, dotNetRef, clientX, clientY) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return;
        }
        
        const startX = clientX;
        const startY = clientY;
        const initialX = parseFloat(element.style.left) || 0;
        const initialY = parseFloat(element.style.top) || 0;
        
        function onMouseMove(e) {
            e.preventDefault();
            const deltaX = e.clientX - startX;
            const deltaY = e.clientY - startY;
            
            let newX = Math.max(0, Math.min(initialX + deltaX, window.innerWidth - 230));
            let newY = Math.max(64, Math.min(initialY + deltaY, window.innerHeight - 200));
            
            element.style.left = newX + 'px';
            element.style.top = newY + 'px';
        }
        
        function onMouseUp(e) {
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            
            const left = parseFloat(element.style.left);
            const top = parseFloat(element.style.top);
            dotNetRef.invokeMethodAsync('UpdatePosition', left, top);
        }
        
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }
};

window.zebrahoofScrollTabs = function (element, delta) {
    if (!element) return;
    element.scrollBy({ left: delta, behavior: 'smooth' });
};

window.zebrahoofRememberInAppPath = function (path) {
    try {
        if (!path || path.charAt(0) !== '/') return;
        sessionStorage.setItem('zebrahoof.lastInAppPath', path);
    } catch (e) {
    }
};

window.zebrahoofReadInAppPath = function () {
    try {
        return sessionStorage.getItem('zebrahoof.lastInAppPath');
    } catch (e) {
        return null;
    }
};

window.zebrahoofSameOriginReferrer = function () {
    try {
        if (!document.referrer) return null;
        var url = new URL(document.referrer);
        if (url.origin !== location.origin) return null;
        if (url.pathname === location.pathname && url.search === location.search) return null;
        return url.pathname + url.search;
    } catch (e) {
        return null;
    }
};

window.zebrahoofGoBack = function () {
    if (window.history.length > 1) {
        window.history.back();
        return true;
    }
    return false;
};

// MudTabs draws its own scroll chevrons and does not give them a name.
(function () {
    function label(btn) {
        if (btn.getAttribute('aria-label') && btn.getAttribute('aria-label').indexOf('Scroll tabs') !== 0) return;
        var d = '';
        btn.querySelectorAll('path[d]').forEach(function (path) {
            var value = path.getAttribute('d') || '';
            if (value.indexOf('M0 0h24') === -1) d = value;
        });
        var left = d.indexOf('15.41') !== -1 || d.indexOf('10.83') !== -1;
        btn.setAttribute('aria-label', left ? 'Scroll tabs left' : 'Scroll tabs right');
    }
    function scan() {
        document.querySelectorAll('.mud-tabs-scroll-button button').forEach(label);
    }
    function start() {
        scan();
        var pending = false;
        new MutationObserver(function () {
            if (pending) return;
            pending = true;
            requestAnimationFrame(function () {
                pending = false;
                scan();
            });
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
})();
