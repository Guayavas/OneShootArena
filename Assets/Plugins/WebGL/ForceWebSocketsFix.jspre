// Este bloque se inyecta directamente al inicio del framework de Unity 2022
if (typeof unityFramework !== 'undefined' || typeof Module !== 'undefined') {
    setupGlobalHook();
} else {
    var checkModuleExist = setInterval(function() {
        if (typeof Module !== 'undefined') {
            setupGlobalHook();
            clearInterval(checkModuleExist);
        }
    }, 5);
}

function setupGlobalHook() {
    // Definimos el puente directo en el objeto global que el código viejo busca con desespero
    if (typeof window.getDynCaller === 'undefined') {
        window.getDynCaller = function(sig, cb) {
            return function() {
                // Redirección forzada usando el sistema de Emscripten en Unity 2022
                if (typeof Module !== 'undefined') {
                    if (Module['makeDynCall']) {
                        return Module['makeDynCall'](sig, cb).apply(null, arguments);
                    } else if (Module['dynCall_' + sig]) {
                        return Module['dynCall_' + sig].apply(null, [cb].concat(Array.prototype.slice.call(arguments)));
                    }
                }
                console.warn("[PARCHE] Intentando llamar a C# pero el módulo WebAssembly aún no está listo.");
            };
        };
        console.log("%c[PARCHE RED] getDynCaller inyectado con éxito para Unity 2022.", "color: green; font-weight: bold;");
    }
}