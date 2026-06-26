# Example ReMedia Sync CLI commands

dotnet run --project .\src\ReMedia.Cli\ReMedia.Cli.csproj -- probe "C:\Media\Movie.mkv"

dotnet run --project .\src\ReMedia.Cli\ReMedia.Cli.csproj -- analyze `
    "C:\Media\Movie.PAL.mkv" `
    --source-fps 25 `
    --target-fps 23.976023976

dotnet run --project .\src\ReMedia.Cli\ReMedia.Cli.csproj -- export `
    "C:\Media\Movie.mkv" `
    --stream 1 `
    --stream 3 `
    --output-folder "C:\Media\Exports"
