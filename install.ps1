# Automated Installer for PicoGK-MCP (Windows)
$ErrorActionPreference = "Stop"

Write-Host "Building and Packaging PicoGK-MCP..." -ForegroundColor Cyan
dotnet pack -c Release -o ./nupkg

Write-Host "Installing PicoGK-MCP as a Global .NET Tool..." -ForegroundColor Cyan
try {
    dotnet tool update -g --add-source ./nupkg PicoGK.Mcp
} catch {
    dotnet tool install -g --add-source ./nupkg PicoGK.Mcp
}

Write-Host "PicoGK-MCP installed successfully!" -ForegroundColor Green
Write-Host "You can now configure any MCP client with:" -ForegroundColor White
Write-Host '  "command": "picogk-mcp"' -ForegroundColor Yellow
