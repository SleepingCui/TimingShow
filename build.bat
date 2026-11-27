@echo off
setlocal

pushd "%~dp0"
if errorlevel 1 exit /b 1

for /f %%i in ('powershell -NoProfile -Command "[DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()"') do set "BUILD_START=%%i"

echo [1/3] Building TimingShow solution
msbuild TimingShow.slnx /m /p:Configuration=Release /p:Platform="Any CPU"
if errorlevel 1 goto :fail

echo [2/3] Packaging UMM
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow\bin\Release\TimingShow.dll','TimingShow.Updater\bin\Release\TimingShow.Updater.dll','TimingShow.Loader.UMM\bin\Release\TimingShow.Loader.UMM.dll','TimingShow.Loader.UMM\bin\Release\Info.json','TimingShow\bin\Release\lang.json' -DestinationPath 'TimingShow.UMM.zip' -Force"
if errorlevel 1 goto :fail

echo [3/3] Packaging Melon
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = Join-Path $env:TEMP 'TimingShow.Melon.zip'; if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }; $archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create'); $files = @(@{ Name = 'Mods/TimingShow.dll'; Path = 'TimingShow\bin\Release\TimingShow.dll' }, @{ Name = 'Mods/TimingShow.Updater.dll'; Path = 'TimingShow.Updater\bin\Release\TimingShow.Updater.dll' }, @{ Name = 'Mods/TimingShow.Loader.Melon.dll'; Path = 'TimingShow.Loader.Melon\bin\Release\TimingShow.Loader.Melon.dll' }, @{ Name = 'Mods/lang.json'; Path = 'TimingShow\bin\Release\lang.json' }, @{ Name = 'Plugins/TimingShow.UpdateBootstrap.dll'; Path = 'TimingShow.UpdateBootstrap\bin\Release\TimingShow.UpdateBootstrap.dll' }); try { foreach ($file in $files) { [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Resolve-Path $file.Path).ProviderPath, $file.Name) } } finally { $archive.Dispose() }; $moved = $false; for ($i = 0; $i -lt 5; $i++) { try { Move-Item -LiteralPath $zip -Destination 'TimingShow.Melon.zip' -Force -ErrorAction Stop; $moved = $true; break } catch { Start-Sleep -Seconds 2 } }; if (-not $moved) { throw 'TimingShow.Melon.zip is locked by another process.' }"
if errorlevel 1 goto :fail

powershell -NoProfile -Command "$elapsed = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() - [long]$env:BUILD_START; $duration = [TimeSpan]::FromMilliseconds($elapsed); $hours = $duration.Days * 24 + $duration.Hours; $elapsedText = '{0:00}:{1:00}:{2:00}' -f $hours, $duration.Minutes, $duration.Seconds; Write-Host ('OK - elapsed ' + $elapsedText)"
popd
endlocal & exit /b 0

:fail
set "exitCode=%ERRORLEVEL%"
popd
endlocal & exit /b %exitCode%
