@echo off
chcp 65001 >nul
cd /d "D:\Code\Projects\VisualStudio Projects\TimingShow"


msbuild TimingShow.sln /p:Configuration=Debug /p:Platform="Any CPU" /t:Build


set "SOURCE=TimingShow\bin\Debug"
set "TARGET=E:\Games\Steam\steamapps\common\A Dance of Fire and Ice\Mods\TimingShow"


if not exist "%TARGET%" (
    mkdir "%TARGET%"
)

del /f /s /q "%TARGET%\Settings.xml"
copy /y "%SOURCE%\TimingShow.dll" "%TARGET%\"
copy /y "%SOURCE%\TimingShow.pdb" "%TARGET%\"
copy /y "%SOURCE%\Info.json" "%TARGET%\"

set "GAME_PATH=E:\Games\Steam\steamapps\common\A Dance of Fire and Ice"
set "GAME_EXE=A Dance of Fire and Ice.exe"


start "" "%GAME_PATH%\%GAME_EXE%"


echo.
pause