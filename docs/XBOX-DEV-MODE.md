# Xbox Series X Dev Mode

1. Activa Developer Mode con la aplicación oficial y cambia/reinicia la consola a Dev Mode. El proceso puede requerir cuenta de desarrollador Microsoft/Partner Center; no compartas códigos de activación.
2. En Dev Home habilita Xbox Device Portal y configura sus credenciales localmente. Mantén la consola y PC en una red confiable; el portal usa HTTPS y certificado propio.
3. Para despliegue con Visual Studio: abre proyecto UWP, selecciona x64 y Remote Machine, conecta IP de consola, empareja PIN mostrado por Dev Home y usa Deploy. Para carga manual, usa Device Portal > Apps > Install app y el paquete firmado más dependencias si aparecen.
4. `deploy-xbox.ps1 -DeviceAddress <IP>` abre portal, pero deja instalación/autenticación manual. No conoce PIN ni almacena credenciales.
5. Confirma que la versión de Xbox OS es al menos 2310 para WebView2. Lanza app y abre Pruebas, después Memoria; guarda JSON desde página y fotografía/registro localmente los valores nativos.
6. Realiza cargas progresivas en sesión no depurada para estimar el comportamiento real. La depuración de VS modifica los límites publicados. No continúes si la consola reporta presión o cierres.

No se ha instalado ni ejecutado esta aplicación en una consola.
