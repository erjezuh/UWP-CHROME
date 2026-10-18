# Matriz de compatibilidad

| Prueba | Qué comprueba | Estado actual |
|---|---|---|
| WebAssembly | instancia módulo mínimo | implementado, no probado en Xbox |
| WebGL 2 | crea contexto independiente | implementado, no probado en Xbox |
| WebGL 1 | fallback con contexto independiente | implementado, no probado en Xbox |
| Búsqueda web | Google predeterminado; selector Google/Bing/DuckDuckGo; consulta codificada en URL | implementado; requiere conexión y validación en Xbox |
| Runtime/navegación | Intenta WebView2 del sistema, después Fixed Version incluido; si ambos fallan activa el control UWP WebView heredado | fallback con timeout, etapas de inicio y logs persistentes detallados y visor multilínea en 0.3.7.0; pendiente de probar en Xbox; WebView heredado no equivale a Chromium |
| Ratón/puntero | Pointer Events, pointer lock y media pointer; identidad de paquete experimental basada en JavaUWP `main-native` | parcial/no verificado; requiere ratón físico en Xbox y versión concreta de OS |
| Aceleración GPU/renderer | extensión de renderer solo si permitida | parcial; renderer no prueba hardware acceleration |
| SharedArrayBuffer | constructor y `crossOriginIsolated` | implementado, depende de sitio y runtime |
| Atomics | objeto expuesto | API no equivale a multithreading operativo |
| Workers | Worker dedicado de blob | implementado, no prueba pool/threads WASM |
| mando | Gamepad API y polling inicial | parcial; Xbox input puede ser integrado por host y requiere prueba física |
| audio | AudioContext creado | parcial; no se mide salida audible/dispositivo |
| memoria web | `performance.memory` opcional | puede no exponerse; métrica JS no es RAM de proceso |
| memoria proceso UWP | MemoryManager AppMemoryUsage/Limit | implementado; medir en Xbox |
| aislamiento recursos | explicación visible | manual por web/origen; no hay auditoría completa |
| demo 3D/FPS | contexto WebGL | no hay benchmark sostenido/FPS en MVP |

Los estados concretos exportados son COMPATIBLE, NO COMPATIBLE, COMPATIBILIDAD PARCIAL y NO SE PUEDE VERIFICAR. Exporta JSON desde diagnóstico. Comprueba resultados primero en runtime Xbox de destino; pruebas en PC no demuestran Xbox.
