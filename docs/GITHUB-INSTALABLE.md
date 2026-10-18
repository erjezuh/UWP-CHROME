# Paquete compilado por GitHub Actions

El workflow `.github/workflows/build-xbox-package.yml` corre en runner Windows al hacer push a la rama Arena o manualmente desde **Actions → Build Xbox UWP package → Run workflow**. Ejecuta pruebas estáticas, compila UWP x64 en Release, firma el AppX con un certificado efímero cuyo `CN=ArenaDev` coincide con `Publisher`, y publica el AppX, el certificado público y el SHA-256 como artefacto descargable por 90 días.

Para un enlace de descarga permanente de GitHub Releases, el workflow también publica los mismos archivos cuando se compila un tag `v*`. En GitHub, desde Releases crea un tag/version (por ejemplo `v0.1.0`) apuntando a un commit que incluya el workflow; al correr el workflow de ese tag, la release contendrá el paquete. No se sube ni conserva el PFX/clave privada. El paquete está firmado para desarrollo y no es Microsoft Store signing.

## Instalación con Device Portal

1. Descarga el artefacto ZIP de una ejecución exitosa (Actions → ejecución → Artifacts), o descarga `XboxGamingBrowser-x64.appx` de una Release `v*`.
2. Extrae el artefacto en Windows. Comprueba `SHA256SUMS.txt` con `Get-FileHash .\XboxGamingBrowser-x64.appx -Algorithm SHA256`.
3. En Dev Home habilita Device Portal. En PC abre la URL HTTPS mostrada por la consola, acepta el certificado propio del portal en el navegador si corresponde e inicia sesión allí.
4. En Device Portal abre **Apps → Install app** y elige el `.appx`. No se necesita desplegar clave privada; el artefacto incluye únicamente certificado público. La API oficial del Device Portal especifica que la petición a Xbox lleva el paquete y que el certificado como cuerpo adicional aplica a IoT/Desktop, no a Xbox. Si la consola rechaza el certificado del paquete, guarda el error completo y no importes certificados a almacenes a ciegas.
5. Instala/ejecuta la app desde Apps. Para depurar incompatibilidades WebView2, revisa que el Xbox esté en OS 2310 o posterior y que el build realmente se haya completado.

Las Actions artifacts no son releases permanentes y requieren acceso al repositorio; Release assets permanecen disponibles mientras se conserve la release. Una compilación verde confirma que CI construyó el paquete, **no** que haya sido instalado o probado en Xbox. El dispositivo no es accesible para CI.
