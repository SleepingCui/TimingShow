@echo off
msbuild TimingShow.sln /p:Configuration=Release /p:Platform="Any CPU" /v:m
powershell -NoProfile -Command "Compress-Archive -Path 'TimingShow\bin\Release\TimingShow.dll','TimingShow\bin\Release\Info.json','TimingShow\bin\Release\lang.json' -DestinationPath 'TimingShow.zip' -Force"
echo OK
pause