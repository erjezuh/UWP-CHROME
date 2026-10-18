# Limitaciones y riesgos

1. No hay build UWP/AppX ni consola disponible en este entorno Linux. El proyecto debe ser compilado con VS en Windows; el WebView2 Xbox no se valida aquí.
2. Presupuesto documentado de app: 1 GB foreground; 5 GB aplica a juego con categoría elegible, no es un ajuste de navegador. Esta aplicación no pretende registrarse ni hacerse pasar por juego.
3. GPU acceleration, WebGL2, WASM SIMD/threading, SharedArrayBuffer, Gamepad y audio dependen de Xbox OS/runtime, web origin, políticas y firmware. El checker no puede confirmar aceleración real con una extensión renderer.
4. El Fixed Version de WebView2 se empaqueta por separado del runtime del sistema, supera 250 MB y no recibe actualizaciones automáticas; su compatibilidad con cada versión de Xbox OS debe probarse y el pin debe actualizarse manualmente.
4. La compatibilidad de WebView2 en Xbox es documentada; no se garantiza que todos los sitios acepten incrustación. DRM/streaming, ventanas emergentes, descargas, cookies/persistencia, popups y permisos pueden tener diferencias.
5. La UI MVP carece de pestañas, historial, favoritos y manejo de descargas. Gaming mode no garantiza que la web permita fullscreen.
6. Diagnóstico de memoria web no proporciona RAM total; MemoryManager mide uso/límite UWP, no clasificación entre GPU/WASM/browser.
7. `setup.ps1` comprueba pre-requisitos, no instala herramientas por descargas automáticas. Instalación de VS/SDK debe hacerse desde Visual Studio Installer oficial.
8. Proyecto comunitario Black Ops: no testado en Xbox, licencias/activos y derechos requieren revisión; no redistribuir contenido.
