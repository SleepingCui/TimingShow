$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameDir = "E:\Games\Steam\steamapps\common\A Dance of Fire and Ice"

# 调用方自己可能就停在游戏目录（上一次运行的遗留），还原到那儿等于没还原。
$originLocation = (Get-Location).ProviderPath
if (-not $originLocation -or $originLocation -like "$gameDir*")
{
    $originLocation = $projectDir
}

try
{
    $modDir = Join-Path $gameDir "Mods\TimingShow"

    Write-Host "=== Packing embedded assets ===" -ForegroundColor Cyan
    Set-Location $projectDir
    & (Join-Path $projectDir "tools\Pack-EmbeddedAssets.ps1")
    if (-not $?) { throw "Embedded asset packing failed" }

    Write-Host "=== Building TimingShow ===" -ForegroundColor Cyan
    # Rebuild the shared assembly: an incremental build can skip embedding a
    # regenerated .dfl, leaving the old font inside TimingShow.dll.
    msbuild TimingShow\TimingShow.csproj /t:Rebuild /p:Configuration=Release /p:Platform="AnyCPU"
    if ($LASTEXITCODE -ne 0) { throw "TimingShow build failed" }

    Write-Host "=== Building TimingShow.Loader.UMM ===" -ForegroundColor Cyan
    msbuild TimingShow.Loader.UMM\TimingShow.Loader.UMM.csproj /p:Configuration=Release /p:Platform="AnyCPU"
    if ($LASTEXITCODE -ne 0) { throw "TimingShow.Loader.UMM build failed" }

    Write-Host "=== Building TimingShow.Loader.Melon ===" -ForegroundColor Cyan
    msbuild TimingShow.Loader.Melon\TimingShow.Loader.Melon.csproj /p:Configuration=Release /p:Platform="AnyCPU"
    if ($LASTEXITCODE -ne 0) { throw "TimingShow.Loader.Melon build failed" }


    Write-Host "=== Copying Mod ===" -ForegroundColor Cyan

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $modDir | Out-Null

    # 清理改名前的旧产物,避免和新的 TimingShow.dll 一起被加载
    $staleFiles = @(
        "TimingShow.Core.dll",
        "TimingShow.Core.pdb",
        "TimingShow.UMM.dll",
        "TimingShow.UMM.pdb",
        "TimingShow.Melon.dll",
        "TimingShow.Melon.pdb"
    )

    foreach ($stale in $staleFiles)
    {
        Remove-Item `
            -Path (Join-Path $modDir $stale) `
            -Force `
            -ErrorAction SilentlyContinue
    }

    # Copy all outputs to a flat directory
    Copy-Item `
        -Path ".\TimingShow\bin\Release\TimingShow.dll" `
        -Destination $modDir `
        -Force

    Copy-Item `
        -Path ".\TimingShow\bin\Release\TimingShow.pdb" `
        -Destination $modDir `
        -Force `
        -ErrorAction SilentlyContinue

    Copy-Item `
        -Path ".\TimingShow.Loader.UMM\bin\Release\TimingShow.Loader.UMM.dll" `
        -Destination $modDir `
        -Force

    Copy-Item `
        -Path ".\TimingShow.Loader.UMM\bin\Release\TimingShow.Loader.UMM.pdb" `
        -Destination $modDir `
        -Force `
        -ErrorAction SilentlyContinue

    Copy-Item `
        -Path ".\TimingShow.Loader.UMM\bin\Release\Info.json" `
        -Destination $modDir `
        -Force

    Copy-Item `
        -Path ".\TimingShow.Loader.Melon\bin\Release\TimingShow.Loader.Melon.dll" `
        -Destination $modDir `
        -Force

    Copy-Item `
        -Path ".\TimingShow.Loader.Melon\bin\Release\TimingShow.Loader.Melon.pdb" `
        -Destination $modDir `
        -Force `
        -ErrorAction SilentlyContinue

    Copy-Item `
        -Path ".\TimingShow\bin\Release\lang.json" `
        -Destination $modDir `
        -Force

    # The runtime only extracts bundled assets when the destination is missing.
    # Copy the current fonts/license over the deployed copies so a rebuilt font is
    # actually used instead of a stale file left from a previous run.
    $assetsDir = Join-Path $modDir "assets"
    New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
    Copy-Item `
        -Path ".\TimingShow\assets\SourceHanSansSC-Regular.otf" `
        -Destination $assetsDir `
        -Force
    Copy-Item `
        -Path ".\TimingShow\assets\OFL.txt" `
        -Destination $assetsDir `
        -Force


    Start-Sleep -Seconds 1

    Write-Host "=== Starting ADOFAI ===" -ForegroundColor Cyan

    Set-Location $gameDir

    Start-Process `
        ".\A Dance of Fire and Ice.exe"

    Write-Host "=== Waiting log ===" -ForegroundColor Cyan

    $log =
    "$env:USERPROFILE\AppData\LocalLow\7th Beat Games\A Dance of Fire and Ice\Player.log"

    while (!(Test-Path $log))
    {
        Start-Sleep -Milliseconds 500
    }

    Write-Host "=== Tailing log ===" -ForegroundColor Green

    $job = Start-Job -ScriptBlock {
        param($logPath)
        Get-Content $logPath -Wait -Encoding UTF8
    } -ArgumentList $log

    while ($true) {
        $process = Get-Process -Name "A Dance of Fire and Ice" -ErrorAction SilentlyContinue
        if (-not $process) {
            Write-Host "`n=== stopping log tail ===" -ForegroundColor Cyan
            Stop-Job $job
            Remove-Job $job
            break
        }

        Receive-Job $job
        Start-Sleep -Milliseconds 500
    }
}
finally
{
    Set-Location -LiteralPath $originLocation -ErrorAction SilentlyContinue
    Write-Host "=== Back at $((Get-Location).ProviderPath) ===" -ForegroundColor Cyan
}
