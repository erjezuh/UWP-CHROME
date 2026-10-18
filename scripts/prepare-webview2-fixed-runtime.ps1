[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') {
    throw 'El runtime WebView2 fijo requiere expand.exe de Windows; ejecútalo en Windows o en el runner Windows de GitHub Actions.'
}

$root = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $root 'config\webview2-fixed-runtime.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($config.architecture -ne 'x64' -or $config.version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'La configuración del runtime WebView2 debe fijar una versión x64 válida.'
}

$expectedFile = "Microsoft.WebView2.FixedVersionRuntime.$($config.version).x64.cab"
$uri = [Uri]$config.cabUrl
if ($uri.Scheme -ne 'https' -or $uri.Host -ne 'msedge.sf.dl.delivery.mp.microsoft.com' -or $uri.AbsolutePath -notlike "*/$expectedFile") {
    throw 'La URL debe ser el CAB x64 fijado de Microsoft, y coincidir con la versión configurada.'
}

$temp = Join-Path $env:TEMP ('xgb-webview2-' + [guid]::NewGuid().ToString('N'))
$cab = Join-Path $temp $expectedFile
$extract = Join-Path $temp 'expanded'
$fixedRoot = Join-Path $root 'src\XboxGamingBrowser\FixedRuntime'
$destination = Join-Path $fixedRoot $config.version
New-Item -ItemType Directory -Force -Path $temp, $extract | Out-Null

try {
    Write-Host "Descargando WebView2 Fixed Version $($config.version) x64 desde el CDN oficial de Microsoft..."
    Invoke-WebRequest -Uri $config.cabUrl -OutFile $cab -UseBasicParsing
    if ((Get-Item -LiteralPath $cab).Length -lt 100MB) {
        throw 'El CAB descargado es demasiado pequeño; se cancela para evitar empaquetar una descarga incompleta.'
    }
    $cabHash = (Get-FileHash -LiteralPath $cab -Algorithm SHA256).Hash.ToLowerInvariant()

    $expand = Join-Path $env:SystemRoot 'System32\expand.exe'
    & $expand $cab '-F:*' $extract
    if ($LASTEXITCODE -ne 0) {
        throw "expand.exe falló con código $LASTEXITCODE. No se empaqueta un runtime incompleto."
    }

    $runtimeExe = Get-ChildItem -LiteralPath $extract -Filter 'msedgewebview2.exe' -File -Recurse | Select-Object -First 1
    if (-not $runtimeExe) {
        throw 'El CAB no contiene msedgewebview2.exe; se cancela el empaquetado.'
    }

    New-Item -ItemType Directory -Force -Path $fixedRoot | Out-Null
    Get-ChildItem -LiteralPath $fixedRoot -Directory | Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Get-ChildItem -LiteralPath $runtimeExe.Directory.FullName -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force
    }
    if (-not (Test-Path -LiteralPath (Join-Path $destination 'msedgewebview2.exe'))) {
        throw 'La extracción no dejó msedgewebview2.exe en el directorio fijo esperado.'
    }

    Set-Content -LiteralPath (Join-Path $fixedRoot 'version.txt') -Value $config.version -Encoding ascii
    Set-Content -LiteralPath (Join-Path $fixedRoot 'SHA256.txt') -Value "$cabHash  $expectedFile" -Encoding ascii
    Write-Host "Runtime preparado: $destination"
    Write-Host "CAB SHA-256: $cabHash"
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
