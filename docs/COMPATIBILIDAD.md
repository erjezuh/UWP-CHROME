# Matriz de compatibilidad

| Prueba | Qué comprueba | Estado actual |
|---|---|---|
| WebAssembly | instancia módulo mínimo | implementado, no probado en Xbox |
| WebGL 2 | crea contexto | implementado, no probado en Xbox |
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
