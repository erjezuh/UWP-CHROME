[CmdletBinding()] param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'; if($env:OS -ne 'Windows_NT'){throw 'La generación de paquete AppX requiere Windows + Windows SDK.'}
& (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration
if($LASTEXITCODE -ne 0){throw 'Build fallido; no se empaqueta.'}
$root=$PSScriptRoot; $out=Join-Path $root 'src\XboxGamingBrowser\AppPackages'
$pkg=Get-ChildItem $out -Recurse -Include *.appx,*.msix,*.appxbundle,*.msixbundle -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if(!$pkg){throw "MSBuild terminó pero no generó paquete instalable en $out. Revisa configuración de empaquetado/firmado en Visual Studio."}
$dest=Join-Path $root 'artifacts'; New-Item -ItemType Directory -Force $dest | Out-Null; Copy-Item $pkg.FullName $dest -Force
Write-Host "Paquete copiado: $(Join-Path $dest $pkg.Name)" -ForegroundColor Green
