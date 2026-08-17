@echo off
echo === Building TimingShow.Core ===
msbuild TimingShow.Core\TimingShow.Core.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.UMM ===
msbuild TimingShow.UMM\TimingShow.UMM.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building TimingShow.Melon ===
msbuild TimingShow.Melon\TimingShow.Melon.csproj /p:Configuration=Release /p:Platform="AnyCPU"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Packaging ===
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow.Core\bin\Release\TimingShow.Core.dll','TimingShow.UMM\bin\Release\TimingShow.UMM.dll','TimingShow.Melon\bin\Release\TimingShow.Melon.dll','TimingShow.UMM\bin\Release\Info.json','TimingShow.Core\bin\Release\lang.json' -DestinationPath 'TimingShow.zip' -Force"

echo OK