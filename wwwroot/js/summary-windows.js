(function () {
    let dotNetRef = null;
    let listening = false;
    let dragging = false;

    function itemFromPoint(x, y) {
        const el = document.elementFromPoint(x, y);
        return el ? el.closest(".summary-window-item") : null;
    }

    function startDrag(item, grid) {
        dragging = true;
        item.classList.add("summary-window-dragging");
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync("BeginWindowDrag");
        }

        function onMouseMove(e) {
            e.preventDefault();
            const over = itemFromPoint(e.clientX, e.clientY);
            if (!over || over === item || over.parentElement !== grid) {
                return;
            }

            const items = Array.from(grid.querySelectorAll(".summary-window-item"));
            const from = items.indexOf(item);
            const to = items.indexOf(over);
            if (from < 0 || to < 0 || from === to) {
                return;
            }

            if (from < to) {
                over.after(item);
            } else {
                over.before(item);
            }
        }

        function onMouseUp() {
            document.removeEventListener("mousemove", onMouseMove);
            document.removeEventListener("mouseup", onMouseUp);
            item.classList.remove("summary-window-dragging");
            dragging = false;
            const keys = Array.from(grid.querySelectorAll(".summary-window-item"))
                .map(function (el) { return el.getAttribute("data-key"); })
                .filter(Boolean);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("ApplyWindowOrder", keys);
            }
        }

        document.addEventListener("mousemove", onMouseMove);
        document.addEventListener("mouseup", onMouseUp);
    }

    function onMouseDown(e) {
        if (e.button !== 0 || dragging) {
            return;
        }

        const handle = e.target.closest(".summary-window-handle");
        if (!handle) {
            return;
        }

        const item = handle.closest(".summary-window-item");
        const grid = item && item.closest(".summary-window-grid");
        if (!item || !grid) {
            return;
        }

        e.preventDefault();
        startDrag(item, grid);
    }

    window.summaryWindowDrag = {
        attach: function (ref) {
            dotNetRef = ref;
            if (!listening) {
                document.addEventListener("mousedown", onMouseDown);
                listening = true;
            }
        },
        detach: function () {
            dotNetRef = null;
        }
    };
})();
