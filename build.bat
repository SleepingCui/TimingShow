@echo off
echo === Building TimingShow ===
msbuild TimingShow\TimingShow.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Updater ===
msbuild TimingShow.Updater\TimingShow.Updater.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Loader.UMM ===
msbuild TimingShow.Loader.UMM\TimingShow.Loader.UMM.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Loader.Melon ===
msbuild TimingShow.Loader.Melon\TimingShow.Loader.Melon.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.UpdateBootstrap ===
msbuild TimingShow.UpdateBootstrap\TimingShow.UpdateBootstrap.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Packaging UMM ===
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow\bin\Release\TimingShow.dll','TimingShow.Updater\bin\Release\TimingShow.Updater.dll','TimingShow.Loader.UMM\bin\Release\TimingShow.Loader.UMM.dll','TimingShow.Loader.UMM\bin\Release\Info.json','TimingShow\bin\Release\lang.json' -DestinationPath 'TimingShow.UMM.zip' -Force"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Packaging Melon ===
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = Join-Path $env:TEMP 'TimingShow.Melon.zip'; if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }; $archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create'); $files = @(@{ Name = 'Mods/TimingShow.dll'; Path = 'TimingShow\bin\Release\TimingShow.dll' }, @{ Name = 'Mods/TimingShow.Updater.dll'; Path = 'TimingShow.Updater\bin\Release\TimingShow.Updater.dll' }, @{ Name = 'Mods/TimingShow.Loader.Melon.dll'; Path = 'TimingShow.Loader.Melon\bin\Release\TimingShow.Loader.Melon.dll' }, @{ Name = 'Mods/lang.json'; Path = 'TimingShow\bin\Release\lang.json' }, @{ Name = 'Plugins/TimingShow.UpdateBootstrap.dll'; Path = 'TimingShow.UpdateBootstrap\bin\Release\TimingShow.UpdateBootstrap.dll' }); try { foreach ($file in $files) { [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Resolve-Path $file.Path).ProviderPath, $file.Name) } } finally { $archive.Dispose() }; $moved = $false; for ($i = 0; $i -lt 5; $i++) { try { Move-Item -LiteralPath $zip -Destination 'TimingShow.Melon.zip' -Force -ErrorAction Stop; $moved = $true; break } catch { Start-Sleep -Seconds 2 } }; if (-not $moved) { throw 'TimingShow.Melon.zip is locked by another process.' }"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo OK
