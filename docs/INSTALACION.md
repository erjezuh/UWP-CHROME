# Compilación en Windows

1. Instala Visual Studio con **Universal Windows Platform development**, Windows 10 SDK (10.0.19041+ recomendado) y UWP .NET Native tools. VS Community es gratuito para uso individual bajo sus términos.
2. Abre PowerShell 5.1 en la carpeta clonada. `./setup.ps1 -SkipBuild` hace comprobaciones sin instalar software; `./build.ps1` compila x64 Release; `./test.ps1` hace pruebas estáticas; `./package.ps1` intenta generar/copiar el AppX.
3. El paquete de desarrollo requiere firma/certificado y configuración de empaquetado de Visual Studio. No incluyas clave privada en Git. Si el empaquetado falla, abre el csproj en VS, confirma x64 y crea paquete de aplicación para sideload/test, usando el certificado de desarrollo generado por VS.

Compilar, crear paquete y firmar no son lo mismo. Sin herramientas Windows no se puede generar paquete desde este checkout. El script no fabrica artefactos si MSBuild no los produjo.
