[CmdletBinding()] param([Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$DeviceAddress,[string]$PackagePath=(Join-Path $PSScriptRoot 'artifacts\XboxGamingBrowser.appx'))
$ErrorActionPreference='Stop'; if(!(Test-Path $PackagePath)){throw "Paquete no encontrado: $PackagePath. Ejecuta package.ps1 primero."}
$device=$DeviceAddress.Trim().TrimEnd('/'); if($device -notmatch '^https?://'){$device="https://$device"}; if($device -notmatch '^https://'){throw 'El Device Portal de Xbox debe usarse por HTTPS.'}
Write-Host "Abriendo Device Portal de Xbox: $device (autenticación manual segura en navegador)." -ForegroundColor Cyan
Start-Process $device
Write-Host 'En el portal autenticado, abre Apps > Install app, selecciona el paquete y sus dependencias requeridas. Este script no solicita ni almacena credenciales y no simula el despliegue.' -ForegroundColor Yellow
