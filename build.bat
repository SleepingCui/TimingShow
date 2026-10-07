@echo off
echo === Building TimingShow ===
msbuild TimingShow\TimingShow.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Loader.UMM ===
msbuild TimingShow.Loader.UMM\TimingShow.Loader.UMM.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Loader.Melon ===
msbuild TimingShow.Loader.Melon\TimingShow.Loader.Melon.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Packaging ===
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow\bin\Release\TimingShow.dll','TimingShow.Loader.UMM\bin\Release\TimingShow.Loader.UMM.dll','TimingShow.Loader.Melon\bin\Release\TimingShow.Loader.Melon.dll','TimingShow.Loader.UMM\bin\Release\Info.json','TimingShow\bin\Release\lang.json' -DestinationPath 'TimingShow.zip' -Force"

echo OK
