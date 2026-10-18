# Arquitectura

- **Shell:** UWP x64 C# con WinUI 2.8 y WebView2 para Windows/Xbox.
- **Interfaz:** barra nativa de navegación con botones grandes, home, input URL/búsqueda, diagnóstico, pantalla Gaming y zoom CSS best-effort.
- **Motor:** WebView2 runtime proporcionado por sistema Xbox compatible. No embebe navegador alternativo.
- **Compatibilidad:** `Web/diagnostics.html` detecta APIs en el documento y descarga JSON sin elevar resultados de capacidades parciales a verificados.
- **Memoria nativa:** `Windows.System.MemoryManager` expone uso y límite del proceso UWP según plataforma.
- **Restricción de navegación:** URLs HTTP(S) solamente. Pestañas, historial persistente, favoritos, extensiones y descarga de archivos no están implementados en MVP.

MODO GAMING oculta toolbar y solicita fullscreen del host; el fullscreen de página remota puede depender de WebView2 y de la política de la página. El shell no mantiene estado web de forma garantizada al cerrar/navegar.
