[CmdletBinding()] param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$OutputDirectory=(Join-Path $PSScriptRoot 'artifacts'))
$ErrorActionPreference='Stop'; if($env:OS -ne 'Windows_NT'){throw 'La generación de AppX requiere Windows + Windows SDK.'}
$root=$PSScriptRoot; $tmp=Join-Path $env:TEMP ('xgb-sign-'+[guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $tmp | Out-Null
$cert=$null; $pfx=Join-Path $tmp 'build-signing.pfx'; $cer=Join-Path $tmp 'XboxGamingBrowser-signing.cer'; $password=[guid]::NewGuid().ToString('N')+'Aa1!'
try {
 $publisher='CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
 # This is a locally generated development certificate, not a Microsoft-issued certificate.
 # Its subject must match Package.appxmanifest's Publisher exactly for APPX signing.
 $cert=New-SelfSignedCertificate -Type Custom -Subject $publisher -FriendlyName 'XboxGamingBrowser personal dev signing (Edge-namespace identity)' -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy Exportable -KeyUsage DigitalSignature -CertStoreLocation 'Cert:\CurrentUser\My' -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')
 if($cert.Subject -ne $publisher){throw "Certificate subject does not match manifest publisher. Expected '$publisher'; got '$($cert.Subject)'"}
 Export-PfxCertificate -Cert $cert -FilePath $pfx -Password (ConvertTo-SecureString $password -AsPlainText -Force) | Out-Null
 Export-Certificate -Cert $cert -FilePath $cer | Out-Null
 & (Join-Path $root 'build.ps1') -Configuration $Configuration -SigningPfxPath $pfx -SigningPassword $password
 if($LASTEXITCODE -ne 0){throw 'Build fallido; no se empaqueta.'}
 $out=Join-Path $root 'src\XboxGamingBrowser\AppPackages'
 $pkg=Get-ChildItem $out -Recurse -Include *.appx,*.msix,*.appxbundle,*.msixbundle -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch '\.appxupload$' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
 if(!$pkg){throw "MSBuild terminó pero no generó paquete en $out. Revisa el log de build."}
 New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
 $dest=Join-Path $OutputDirectory 'XboxGamingBrowser-x64.appx'; Copy-Item $pkg.FullName $dest -Force; Copy-Item $cer (Join-Path $OutputDirectory 'XboxGamingBrowser-signing.cer') -Force
 $hash=(Get-FileHash $dest -Algorithm SHA256).Hash; Set-Content -Path (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Value "$hash  XboxGamingBrowser-x64.appx" -Encoding ascii
 Write-Host "Paquete firmado listo: $dest" -ForegroundColor Green; Write-Host "SHA-256: $hash"
} finally {
 if($cert){Remove-Item -LiteralPath ("Cert:\CurrentUser\My\"+$cert.Thumbprint) -Force -ErrorAction SilentlyContinue}
 Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
