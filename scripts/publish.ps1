$OutFolderRoot = $env:OneDriveConsumer
if (-not $OutFolderRoot) {
    $OutFolderRoot = "$PSScriptRoot\..\_out"
}

$OutFolder = "$OutFolderRoot\Hemisphera\Hulp"
dotnet publish ./Hemisphera.Hulp.Plugin/Hemisphera.Hulp.Plugin.csproj -c Release -r win-x64 -o "$OutFolder/plugin"
dotnet publish ./Hemisphera.Hulp.WebApp/Hemisphera.Hulp.WebApp.csproj -c Release -o "$OutFolder/webapp"
Get-ChildItem "$OutFolder" -Filter *.pdb -Recurse | Remove-Item