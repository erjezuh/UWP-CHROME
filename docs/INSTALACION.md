# Compilación en Windows

1. Instala Visual Studio con **Universal Windows Platform development**, Windows 10 SDK (10.0.19041+ recomendado) y UWP .NET Native tools. VS Community es gratuito para uso individual bajo sus términos.
2. Abre PowerShell 5.1 en la carpeta clonada. `./setup.ps1 -SkipBuild` hace comprobaciones sin instalar software; `./build.ps1` compila x64 Release; `./test.ps1` hace pruebas estáticas; `./package.ps1` genera un certificado PFX efímero de desarrollo, firma y copia el AppX y certificado público a `artifacts/`. Si prefieres no compilar localmente, usa [GITHUB-INSTALABLE.md](GITHUB-INSTALABLE.md) para descargar el paquete ya construido por Actions/Release.
3. `package.ps1` crea y elimina una identidad de firma de desarrollo temporal, firma con `CN=ArenaDev` (igual al Publisher del manifiesto) y deja el AppX firmado más el certificado público en `artifacts/`. Nunca se guarda el PFX ni su clave privada en Git. Para instalar fácilmente sin preparar herramientas, consulta [GITHUB-INSTALABLE.md](GITHUB-INSTALABLE.md).

Compilar, crear paquete y firmar no son lo mismo. Sin herramientas Windows no se puede generar paquete desde este checkout. El script no fabrica artefactos si MSBuild no los produjo.
