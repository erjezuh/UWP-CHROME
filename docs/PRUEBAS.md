# Pruebas reproducibles

`python -m unittest discover -s tests -v` valida estructura, manifiesto, cadenas de detección y ausencia de activos wasm/juego. GitHub Actions corre estas comprobaciones estáticas. `test.ps1` hace chequeos semejantes desde PowerShell.

No son pruebas de compilación UWP ni pruebas del runtime web, GPU, mando, memoria o rendimiento de Xbox. No se ha probado un juego real en Xbox. Completar fases 1–6 del plan del proyecto exige paquete, consola y registro exportado; port BO1 no se carga ni distribuye.
