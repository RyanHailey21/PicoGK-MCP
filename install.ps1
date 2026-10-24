# Automated Installer for PicoGK-MCP (Windows)
$ErrorActionPreference = "Stop"

$picogkHome = "$HOME\.picogk"
$shapeKernelDir = "$picogkHome\LEAP71_ShapeKernel"
$libDir = "$picogkHome\lib"

# 1. Global Setup of LEAP 71 ShapeKernel
Write-Host "Setting up LEAP 71 ShapeKernel globally in $picogkHome..." -ForegroundColor Cyan

if (-not (Test-Path $picogkHome)) {
    New-Item -ItemType Directory -Path $picogkHome -Force | Out-Null
}

if (-not (Test-Path "$shapeKernelDir\.git")) {
    Write-Host "Cloning official LEAP71_ShapeKernel..." -ForegroundColor Gray
    git clone https://github.com/leap71/LEAP71_ShapeKernel.git $shapeKernelDir
} else {
    Write-Host "Updating LEAP71_ShapeKernel..." -ForegroundColor Gray
    git -C $shapeKernelDir pull --quiet
}

# Ensure standalone .csproj exists for ShapeKernel
$shapeKernelCsproj = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="PicoGK" Version="2.3.0" />
  </ItemGroup>
</Project>
'@
Set-Content -Path "$shapeKernelDir\ShapeKernel\ShapeKernel.csproj" -Value $shapeKernelCsproj -Force

Write-Host "Compiling global ShapeKernel.dll..." -ForegroundColor Gray
dotnet build "$shapeKernelDir\ShapeKernel\ShapeKernel.csproj" -c Release -o $libDir --nologo -v q

# 2. Build and Package PicoGK-MCP
Write-Host "Building and Packaging PicoGK-MCP..." -ForegroundColor Cyan
dotnet pack -c Release -o ./nupkg --nologo -v q

# 3. Global Tool Installation
Write-Host "Installing PicoGK-MCP as a Global .NET Tool..." -ForegroundColor Cyan
try {
    dotnet tool update -g --add-source ./nupkg PicoGK.Mcp --no-cache
} catch {
    dotnet tool install -g --add-source ./nupkg PicoGK.Mcp
}

Write-Host "PicoGK-MCP and ShapeKernel are installed globally!" -ForegroundColor Green
Write-Host 'You can now configure any MCP client with: "command": "picogk-mcp"' -ForegroundColor White
