mergeInto(LibraryManager.library, {

    _WebSocketConnect: function(instanceId) {
        var instance = webSocketState.instances[instanceId];
        if (!instance) return -1;
        if (instance.ws) return -2;

        instance.ws = new WebSocket(instance.url);
        instance.ws.binaryType = "arraybuffer";

        instance.ws.onopen = function() {
            if (webSocketState.debug) console.log("[JSLIB WebSocket] Connected.");
            if (webSocketState.onOpen) {
                // ✅ Parche moderno para Unity 2022+ con tres llaves obligatorias
                {{{ makeDynCall("vi", "webSocketState.onOpen") }}}(instanceId);
            }
        };

        instance.ws.onmessage = function(ev) {
            if (webSocketState.debug) console.log("[JSLIB WebSocket] Received message:", ev.data);
            if (!webSocketState.onMessage) return;

            if (ev.data instanceof ArrayBuffer) {
                var dataBuffer = new Uint8Array(ev.data);
                var buffer = _malloc(dataBuffer.length);
                HEAPU8.set(dataBuffer, buffer);
                try {
                    // ✅ Parche con tres llaves firma viii
                    {{{ makeDynCall("viii", "webSocketState.onMessage") }}}(instanceId, buffer, dataBuffer.length);
                } finally {
                    _free(buffer);
                }
            } else if (typeof ev.data == "string") {
                var arrBuffer = new ArrayBuffer(ev.data.length);
                var dataBuffer = new Uint8Array(arrBuffer);
                for (var i = 0, len = ev.data.length; i < len; i++) {
                    dataBuffer[i] = ev.data.charCodeAt(i);
                }
                var buffer = _malloc(dataBuffer.length);
                HEAPU8.set(dataBuffer, buffer);
                try {
                    {{{ makeDynCall("viii", "webSocketState.onMessage") }}}(instanceId, buffer, dataBuffer.length);
                } finally {
                    _free(buffer);
                }
            }
        };

        instance.ws.onerror = function(ev) {
            if (webSocketState.debug) console.log("[JSLIB WebSocket] Error occurred.");
            if (webSocketState.onError) {
                var msg = "WebSocket error.";
                var msgBytes = lengthBytesUTF8(msg);
                var msgBuffer = _malloc(msgBytes + 1);
                stringToUTF8(msg, msgBuffer, msgBytes);
                try {
                    // ✅ Parche con tres llaves firma vii
                    {{{ makeDynCall("vii", "webSocketState.onError") }}}(instanceId, msgBuffer);
                } finally {
                    _free(msgBuffer);
                }
            }
        };

        instance.ws.onclose = function(ev) {
            if (webSocketState.debug) console.log("[JSLIB WebSocket] Closed.");
            if (webSocketState.onClose) {
                // ✅ Parche con tres llaves firma vii
                {{{ makeDynCall("vii", "webSocketState.onClose") }}}(instanceId, ev.code);
            }
            delete instance.ws;
        };

        return 0;
    }

});