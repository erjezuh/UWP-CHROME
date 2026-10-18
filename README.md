# XboxGamingBrowser

Navegador UWP de prueba para Xbox Series X|S Dev Mode, centrado en juegos web. **Estado: prototipo no probado en consola.** La compatibilidad real depende de la versión de Xbox OS/WebView2 instalada; no se distribuye Chromium ni se ejecutan binarios Win32.

## Arquitectura y viabilidad

Se elige UWP C# + WinUI 2.8 + WebView2 (Edge/Chromium) para Xbox. Microsoft documenta WebView2 para Xbox; su SDK pasó a GA y está disponible desde Xbox OS 2310 (231018-2200). La aplicación integra página de inicio, navegación, un panel de compatibilidad inyectado, modo gaming, controles grandes de mando y lectura documentada de memoria UWP. WebView2 y las funciones modernas deben comprobarse en la consola concreta. El control UWP WebView heredado es alternativa documentada en Xbox, pero no constituye un motor Chromium moderno apropiado para estos objetivos; no se usa como fallback silencioso.

**Memoria:** Microsoft documenta 1 GB para apps y hasta 5 GB para juegos UWP en primer plano; el depurador altera estas restricciones. No se puede asumir la categoría de juego para una app sideloaded. No se intenta eludirla. 4K playback tiene un camino documentado separado, no se habilita porque no aplica al navegador. La app mostrará `MemoryManager.AppMemoryUsage` y `AppMemoryUsageLimit`, no RAM física libre ni heap interno exacto de Chromium.

## Requisitos de compilación

Windows 10/11, Visual Studio con carga de trabajo UWP, Windows 10 SDK (10.0.19041.0 recomendado), NuGet y .NET Native/UWP toolchain. En Xbox: Dev Mode activado, OS Xbox 2310 o posterior para WebView2, y Device Portal habilitado o despliegue remoto de Visual Studio. Este contenedor Linux no puede producir ni validar un paquete UWP.

## Uso

En Windows PowerShell: `./setup.ps1`; luego `./build.ps1`, `./test.ps1`, `./package.ps1`. Para desplegar: `./deploy-xbox.ps1 -DeviceAddress <IP-o-host>` y completar el PIN/configuración de Xbox cuando se solicite. No se requieren credenciales de Microsoft en los scripts.

Ver [investigación](docs/INVESTIGACION.md), [compilación e instalación](docs/INSTALACION.md), [descarga del paquete compilado por GitHub](docs/GITHUB-INSTALABLE.md), [compatibilidad](docs/COMPATIBILIDAD.md) y [limitaciones](docs/LIMITACIONES.md). GitHub Actions crea un AppX x64 descargable; los tags `v*` publican assets en GitHub Releases.

## Licencia
Código del proyecto: MIT. WinUI/WebView2 y Windows SDK conservan sus licencias originales. No se incluyen juegos ni recursos comerciales.
