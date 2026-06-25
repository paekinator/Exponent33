mergeInto(LibraryManager.library, {
  BackShiftMouse_Init: function () {
    if (typeof window === "undefined" || typeof document === "undefined") {
      return;
    }

    if (window.__backShiftMouseInitialized) {
      return;
    }

    window.__backShiftMouseInitialized = true;
    window.__backShiftMouseDeltaX = 0;
    window.__backShiftMouseDeltaY = 0;

    window.__backShiftGetCanvas = function () {
      if (typeof Module !== "undefined" && Module.canvas) {
        return Module.canvas;
      }

      return document.getElementById("unity-canvas") ||
        document.querySelector("canvas");
    };

    window.__backShiftRequestPointerLock = function () {
      var canvas = window.__backShiftGetCanvas();
      if (!canvas || document.pointerLockElement === canvas) {
        return;
      }

      if (canvas.focus) {
        canvas.focus();
      }

      var request = canvas.requestPointerLock ||
        canvas.mozRequestPointerLock ||
        canvas.webkitRequestPointerLock;

      if (request) {
        request.call(canvas);
      }
    };

    window.addEventListener("mousemove", function (event) {
      var canvas = window.__backShiftGetCanvas();
      if (!canvas || document.pointerLockElement !== canvas) {
        return;
      }

      var movementX = event.movementX || event.mozMovementX || event.webkitMovementX || 0;
      var movementY = event.movementY || event.mozMovementY || event.webkitMovementY || 0;

      // Ignore impossible one-event jumps that can appear immediately after
      // focus/pointer-lock transitions. Normal fast mouse movement still gets
      // through unchanged; this only drops browser transition garbage.
      if (Math.abs(movementX) > 400 || Math.abs(movementY) > 400) {
        return;
      }

      window.__backShiftMouseDeltaX += movementX;
      window.__backShiftMouseDeltaY += movementY;
    }, true);

    document.addEventListener("pointerlockchange", function () {
      window.__backShiftMouseDeltaX = 0;
      window.__backShiftMouseDeltaY = 0;
    }, true);

    window.addEventListener("mousedown", function () {
      window.__backShiftRequestPointerLock();
    }, true);

    window.addEventListener("keydown", function () {
      window.__backShiftRequestPointerLock();
    }, true);
  },

  BackShiftMouse_RequestPointerLock: function () {
    if (typeof window === "undefined" || !window.__backShiftRequestPointerLock) {
      return;
    }

    window.__backShiftRequestPointerLock();
  },

  BackShiftMouse_ConsumeDeltaX: function () {
    var value = window.__backShiftMouseDeltaX || 0;
    window.__backShiftMouseDeltaX = 0;
    return value;
  },

  BackShiftMouse_ConsumeDeltaY: function () {
    var value = window.__backShiftMouseDeltaY || 0;
    window.__backShiftMouseDeltaY = 0;
    return value;
  },

  BackShiftMouse_IsPointerLocked: function () {
    if (typeof document === "undefined") {
      return 0;
    }

    if (typeof window === "undefined" || !window.__backShiftGetCanvas) {
      return 0;
    }

    var canvas = window.__backShiftGetCanvas();
    return canvas && document.pointerLockElement === canvas ? 1 : 0;
  }
});
