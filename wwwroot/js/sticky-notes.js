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
