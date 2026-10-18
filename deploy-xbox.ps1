[CmdletBinding()] param([Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$DeviceAddress,[string]$PackagePath)
$ErrorActionPreference='Stop'; if($env:OS -ne 'Windows_NT'){throw 'La preparación de despliegue está diseñada para PowerShell en Windows.'}
$root=$PSScriptRoot
if(!$PackagePath){
 $repo='erjezuh/UWP-CHROME'; $api="https://api.github.com/repos/$repo/releases/latest"
 Write-Host "Buscando la última Release de $repo en GitHub..." -ForegroundColor Cyan
try { $release=Invoke-RestMethod -Uri $api -Headers @{ 'User-Agent'='XboxGamingBrowser-Deploy'; 'Accept'='application/vnd.github+json' } }
catch { throw "No se pudo consultar la última Release. Comprueba que exista una release v* publicada y que el repositorio sea accesible; también puedes pasar -PackagePath. Detalle: $($_.Exception.Message)" }
$asset=$release.assets | Where-Object name -EQ 'XboxGamingBrowser-x64.appx' | Select-Object -First 1
$certAsset=$release.assets | Where-Object name -EQ 'XboxGamingBrowser-signing.cer' | Select-Object -First 1
if(!$asset){throw "La última Release ($($release.tag_name)) no contiene XboxGamingBrowser-x64.appx. Comprueba que el workflow terminó correctamente."}
if(!$certAsset){throw "La Release no incluye el certificado público asociado. No instales solo el paquete; publica de nuevo el artefacto completo."}
$PackagePath=Join-Path $root 'artifacts\XboxGamingBrowser-x64.appx'; $certPath=Join-Path $root 'artifacts\XboxGamingBrowser-signing.cer'; New-Item -ItemType Directory -Force (Split-Path $PackagePath) | Out-Null
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $PackagePath -Headers @{ 'User-Agent'='XboxGamingBrowser-Deploy' }
Invoke-WebRequest -Uri $certAsset.browser_download_url -OutFile $certPath -Headers @{ 'User-Agent'='XboxGamingBrowser-Deploy' }
Write-Host "Descargados desde GitHub: $PackagePath y $certPath" -ForegroundColor Green
}
if(!(Test-Path $PackagePath)){throw "Paquete no encontrado: $PackagePath"}
$device=$DeviceAddress.Trim().TrimEnd('/'); if($device -notmatch '^https?://'){$device="https://$device"}; if($device -notmatch '^https://'){throw 'El Device Portal de Xbox debe usarse por HTTPS.'}
$hash=(Get-FileHash $PackagePath -Algorithm SHA256).Hash; Write-Host "SHA-256 del paquete: $hash" -ForegroundColor Cyan
Write-Host "Abriendo Device Portal de Xbox: $device (autenticación manual segura en navegador)." -ForegroundColor Cyan
Start-Process $device
Write-Host 'En el portal autenticado, abre Apps > Install app, selecciona el AppX descargado. El script no solicita ni almacena credenciales, ni simula el despliegue.' -ForegroundColor Yellow
