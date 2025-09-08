param(
    [string]$Configuration = "Release"
)

$ProjectPath1 = ".\Cove\Cove.csproj"
$ProjectPath2 = ".\Cove.ChatCommands\Cove.ChatCommands.csproj"
# 
dotnet clean $ProjectPath1 --configuration $Configuration
dotnet clean $ProjectPath2 --configuration $Configuration

dotnet restore $ProjectPath1
dotnet restore $ProjectPath2

dotnet build $ProjectPath1 --configuration $Configuration --no-restore
dotnet build $ProjectPath2 --configuration $Configuration --no-restore

$CoveBuildDir = ".\Cove\bin\Release\net8.0\"
$CommandsBuildDir = ".\Cove.ChatCommands\bin\Release\net8.0\"
$CommandsPlugin = $CommandsBuildDir + "Cove.ChatCommands.dll"
$PluginDir = $CoveBuildDir + "plugins"

mkdir $PluginDir
Copy-Item $CommandsPlugin -Destination $PluginDir

$zipPath = ".\win64.zip"
Compress-Archive -Path @(
    $CoveBuildDir + "*"
) -DestinationPath $zipPath -Force