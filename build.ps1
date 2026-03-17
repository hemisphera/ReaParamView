dotnet publish .\ReaParamView.Plugin\ReaParamView.Plugin.csproj -r win-x64 -c Release
Copy-Item -Path "$PSScriptRoot\ReaParamView.Plugin\bin\Release\net10.0\win-x64\publish\reaper_reafxview.dll" -Destination "$env:APPDATA\REAPER\UserPlugins" -Force
& "C:\Program Files\REAPER (x64)\reaper.exe"