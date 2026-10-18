[CmdletBinding()] param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Este proyecto requiere Windows para compilar UWP. No se ha simulado una compilación.' }
if ($PSVersionTable.PSVersion.Major -lt 5) { throw 'Se requiere Windows PowerShell 5.1 o posterior.' }
$root=$PSScriptRoot; New-Item -ItemType Directory -Force -Path (Join-Path $root 'logs'),(Join-Path $root 'artifacts') | Out-Null
$log=Join-Path $root ('logs/setup-{0:yyyyMMdd-HHmmss}.log' -f (Get-Date)); Start-Transcript -Path $log
try {
 Write-Host 'Comprobando herramientas UWP...' -ForegroundColor Cyan
$git=Get-Command git -ErrorAction SilentlyContinue; if($git){git --version}else{Write-Warning 'Git no está instalado; no es necesario para compilar.'}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if(!(Test-Path $vswhere)){throw 'No se encontró Visual Studio Installer/vswhere. Instala Visual Studio con la carga UWP.'}
$vs=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Workload.Universal -property installationPath
if(!$vs){throw 'No se detectó la carga Universal Windows Platform en Visual Studio.'}
$sdk=Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -ErrorAction SilentlyContinue | Where-Object Name -Match '^10\.' | Sort-Object Name -Descending | Select-Object -First 1
if(!$sdk){throw 'No se detectó Windows 10 SDK. Añádelo desde Visual Studio Installer.'}
Write-Host "Visual Studio: $vs`nSDK: $($sdk.Name)" -ForegroundColor Green
if(!$SkipBuild){ & (Join-Path $root 'build.ps1'); if($LASTEXITCODE -ne 0){throw 'Falló la compilación.'} }
Write-Host "Preparación terminada. Registro: $log" -ForegroundColor Green
} catch { Write-Error $_; throw } finally { Stop-Transcript }
