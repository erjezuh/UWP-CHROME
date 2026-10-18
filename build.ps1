[CmdletBinding()] param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'; if($env:OS -ne 'Windows_NT'){throw 'La compilación UWP requiere Windows 10/11 + Visual Studio.'}
$root=$PSScriptRoot; $proj=Join-Path $root 'src\XboxGamingBrowser\XboxGamingBrowser.csproj'; if(!(Test-Path $proj)){throw "No existe proyecto: $proj"}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'; $vs=& $vswhere -latest -products '*' -property installationPath
$msbuild=Join-Path $vs 'MSBuild\Current\Bin\MSBuild.exe'; if(!(Test-Path $msbuild)){throw 'MSBuild no encontrado en Visual Studio.'}
New-Item -ItemType Directory -Force (Join-Path $root 'logs') | Out-Null; $log=Join-Path $root ('logs/build-{0:yyyyMMdd-HHmmss}.log' -f (Get-Date))
& $msbuild $proj /restore /m /p:Configuration=$Configuration /p:Platform=x64 /p:AppxBundle=Never /p:UapAppxPackageBuildMode=StoreUpload /v:minimal 2>&1 | Tee-Object -FilePath $log
if($LASTEXITCODE -ne 0){throw "MSBuild falló (código $LASTEXITCODE). Registro: $log"}; Write-Host "Compilación terminada. Consulta la salida del proyecto y $log" -ForegroundColor Green
