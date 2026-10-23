# Pack default assets into DEFLATE-compressed blobs for DLL embedding.
# Regenerate after changing any file under TimingShow\assets\ :
#   ./tools/Pack-EmbeddedAssets.ps1
# The blobs are gitignored build inputs (the ORIGINALS in assets\ are the tracked
# source of truth) — CI and local builds regenerate them before msbuild.

param(
    [string]$SourceDir = (Join-Path $PSScriptRoot '..\TimingShow\assets'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\TimingShow\embedded')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression

$files = @(
    'SourceHanSansSC-Regular.otf',
    'OFL.txt'
)

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

foreach ($name in $files) {
    $src = Join-Path $SourceDir $name
    if (-not (Test-Path $src)) { throw "Source asset not found: $src" }

    $dst = Join-Path $OutDir ($name + '.dfl')
    $bytes = [IO.File]::ReadAllBytes($src)
    $ms = New-Object IO.MemoryStream
    $ds = New-Object IO.Compression.DeflateStream($ms, [IO.Compression.CompressionLevel]::Optimal, $true)
    $ds.Write($bytes, 0, $bytes.Length)
    $ds.Close()
    [IO.File]::WriteAllBytes($dst, $ms.ToArray())
    '{0,-32} {1,12:N0} -> {2,11:N0}  ({3:P0})' -f $name, $bytes.Length, $ms.Length, ($ms.Length / $bytes.Length)
}

Write-Host "Packed $($files.Count) assets into $OutDir"
