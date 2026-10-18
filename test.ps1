[CmdletBinding()] param()
$ErrorActionPreference='Stop'; $root=$PSScriptRoot
$checks=@('src/XboxGamingBrowser/XboxGamingBrowser.csproj','src/XboxGamingBrowser/MainPage.xaml','src/XboxGamingBrowser/MainPage.xaml.cs','src/XboxGamingBrowser/Web/diagnostics.html','src/XboxGamingBrowser/Package.appxmanifest')
foreach($rel in $checks){if(!(Test-Path (Join-Path $root $rel))){throw "Falta archivo requerido: $rel"}; Write-Host "OK: $rel" -ForegroundColor Green}
$html=Get-Content (Join-Path $root 'src/XboxGamingBrowser/Web/diagnostics.html') -Raw
foreach($token in @('WebAssembly','webgl2','SharedArrayBuffer','Atomics','getGamepads','crossOriginIsolated','Exportar informe JSON')){if(!$html.Contains($token)){throw "Prueba web incompleta: $token"}}
[xml]$null=Get-Content (Join-Path $root 'src/XboxGamingBrowser/Package.appxmanifest')
Write-Host 'Pruebas estáticas correctas. No prueban motor ni consola Xbox.' -ForegroundColor Green
