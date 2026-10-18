# Arquitectura

- **Shell:** UWP x64 C# con WinUI 2.8.
- **Interfaz:** barra nativa de navegación con controles grandes, home renovado, input URL/búsqueda, diagnóstico, memoria, logs locales persistentes/exportables, pantalla Gaming y zoom best-effort.
- **Motor principal:** WebView2, primero con runtime Xbox/sistema y después con Fixed Version incluido. Si ambos fallan, se intenta el control UWP WebView heredado para navegación básica; no equivale a Chromium ni garantiza juegos modernos.
- **Compatibilidad:** `Web/diagnostics.html` detecta APIs en el documento y descarga JSON sin elevar resultados de capacidades parciales a verificados.
- **Memoria nativa:** `Windows.System.MemoryManager` expone uso y límite del proceso UWP según plataforma.
- **Restricción de navegación:** URLs HTTP(S) solamente. Pestañas, historial persistente, favoritos, extensiones y descarga de archivos no están implementados en MVP.

MODO GAMING oculta toolbar y solicita fullscreen del host; el fullscreen de página remota puede depender de WebView2 y de la política de la página. El shell no mantiene estado web de forma garantizada al cerrar/navegar.
