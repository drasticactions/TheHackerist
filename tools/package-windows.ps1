# Packages the desktop Windows app (NativeAOT, win-x64) as a portable zip: <out>\Hackerist-<version>-win-x64.zip,
# holding the folder Hackerist-<version>-win-x64. Run on Windows with the .NET SDK and Visual Studio Build Tools
# (NativeAOT links with their linker).
#
# Signing hook, unsigned by default: with HACKERIST_WINDOWS_CERT (a .pfx path) and HACKERIST_WINDOWS_CERT_PASSWORD set,
# signtool signs the exe and every DLL (SHA-256, RFC 3161 timestamp from HACKERIST_WINDOWS_TIMESTAMP_URL, default
# DigiCert's). Unsigned, SmartScreen warns on the first run.
# Usage: pwsh tools/package-windows.ps1 [-Out <folder, default out\>]
param([string]$Out)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $Out) { $Out = Join-Path $root 'out' }
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$Out = (Resolve-Path $Out).Path

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

# NativeAOT finds the linker through vcvarsall.bat, which in newer Visual Studio runs vswhere.exe from PATH. CI images
# have it there; a desktop install keeps it in the Visual Studio Installer folder.
$installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue) -and (Test-Path (Join-Path $installer 'vswhere.exe'))) {
    $env:PATH = "$installer;$env:PATH"
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("hackerist-package-" + [Guid]::NewGuid().ToString('N'))
$name = "Hackerist-$version-win-x64"
$folder = Join-Path $work $name
try {
    dotnet publish (Join-Path $root 'src\TheHackerist.Desktop') -c Release -r win-x64 -o (Join-Path $work 'publish') -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

    # The program and its native DLLs (symbols and .pdb files stay out).
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    Copy-Item (Join-Path $work 'publish\Hackerist.exe') $folder
    Copy-Item (Join-Path $work 'publish\*.dll') $folder
    Copy-Item (Join-Path $root 'LICENSE.md'), (Join-Path $root 'THIRD-PARTY-NOTICES.md') $folder

    if ($env:HACKERIST_WINDOWS_CERT -and $env:HACKERIST_WINDOWS_CERT_PASSWORD) {
        $timestamp = if ($env:HACKERIST_WINDOWS_TIMESTAMP_URL) { $env:HACKERIST_WINDOWS_TIMESTAMP_URL } else { 'http://timestamp.digicert.com' }
        $files = @(Get-ChildItem $folder -Include *.exe, *.dll -Recurse | ForEach-Object FullName)
        signtool sign /fd sha256 /f $env:HACKERIST_WINDOWS_CERT /p $env:HACKERIST_WINDOWS_CERT_PASSWORD /tr $timestamp /td sha256 $files
        if ($LASTEXITCODE -ne 0) { throw "signtool failed ($LASTEXITCODE)" }
    } else {
        Write-Host 'HACKERIST_WINDOWS_CERT is not set: the app is unsigned.'
    }

    $zip = Join-Path $Out "$name.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path $folder -DestinationPath $zip
    Write-Host $zip
}
finally {
    if (Test-Path $work) { Remove-Item -Recurse -Force $work }
}
