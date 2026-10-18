# Paquete compilado por GitHub Actions

El workflow `.github/workflows/build-xbox-package.yml` corre en runner Windows al hacer push a la rama Arena o manualmente desde **Actions → Build Xbox UWP package → Run workflow**. Ejecuta pruebas estáticas, compila UWP x64 en Release, firma el AppX con un certificado efímero cuyo `CN=ArenaDev` coincide con `Publisher`, y publica el AppX, el certificado público y el SHA-256 como artefacto descargable por 90 días.

Para un enlace de descarga permanente de GitHub Releases, el workflow también publica los mismos archivos cuando se compila un tag `v*`. En GitHub, desde Releases crea un tag/version (por ejemplo `v0.1.0`) apuntando a un commit que incluya el workflow; al correr el workflow de ese tag, la release contendrá el paquete. No se sube ni conserva el PFX/clave privada. El paquete está firmado para desarrollo y no es Microsoft Store signing.

## Instalación con Device Portal

1. Descarga el artefacto ZIP de una ejecución exitosa (Actions → ejecución → Artifacts), o descarga `XboxGamingBrowser-x64.appx` de una Release `v*`.
2. Extrae el artefacto en Windows. Comprueba `SHA256SUMS.txt` con `Get-FileHash .\XboxGamingBrowser-x64.appx -Algorithm SHA256`.
3. En Dev Home habilita Device Portal. En PC abre la URL HTTPS mostrada por la consola, acepta el certificado propio del portal en el navegador si corresponde e inicia sesión allí.
4. En Device Portal abre **Apps manager → Deploy apps**. Selecciona el `.appx` y también el archivo `.cer` incluido en el mismo ZIP (deben proceder de la misma ejecución de CI), luego continúa e instala. El portal documenta elegir el certificado asociado al paquete para el flujo de sideload. El artefacto incluye solo el certificado público, nunca la clave privada `.pfx`. No instales el `.cer` en el PC; selecciónalo en el Device Portal de Xbox.
5. Si ya hiciste una instalación fallida, elimina la entrada incompleta desde **Apps manager** si aparece. Si una versión anterior con otra firma está instalada, desinstálala antes de probar esta compilación: cada build de CI genera un certificado de desarrollo temporal distinto. Después de instalar, inicia la app desde Apps. Para depurar incompatibilidades WebView2, revisa que el Xbox esté en OS 2310 o posterior y que el build realmente se haya completado.

Las Actions artifacts no son releases permanentes y requieren acceso al repositorio; Release assets permanecen disponibles mientras se conserve la release. Una compilación verde confirma que CI construyó el paquete, **no** que haya sido instalado o probado en Xbox. El dispositivo no es accesible para CI.
