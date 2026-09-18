@echo off
echo === Building TimingShow ===
msbuild TimingShow\TimingShow.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.UMM ===
msbuild TimingShow.UMM\TimingShow.UMM.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Melon ===
msbuild TimingShow.Melon\TimingShow.Melon.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Packaging ===
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow\bin\Release\TimingShow.dll','TimingShow.UMM\bin\Release\TimingShow.UMM.dll','TimingShow.Melon\bin\Release\TimingShow.Melon.dll','TimingShow.UMM\bin\Release\Info.json','TimingShow\bin\Release\lang.json' -DestinationPath 'TimingShow.zip' -Force"

echo OK