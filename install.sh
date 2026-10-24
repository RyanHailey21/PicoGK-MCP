#!/usr/bin/env bash
set -e

PICOGK_HOME="${HOME}/.picogk"
SHAPEKERNEL_DIR="${PICOGK_HOME}/LEAP71_ShapeKernel"
LIB_DIR="${PICOGK_HOME}/lib"

echo "Setting up LEAP 71 ShapeKernel globally in ${PICOGK_HOME}..."
mkdir -p "${PICOGK_HOME}"
mkdir -p "${LIB_DIR}"

if [ ! -d "${SHAPEKERNEL_DIR}/.git" ]; then
    echo "Cloning official LEAP71_ShapeKernel..."
    git clone https://github.com/leap71/LEAP71_ShapeKernel.git "${SHAPEKERNEL_DIR}"
else
    echo "Updating LEAP71_ShapeKernel..."
    git -C "${SHAPEKERNEL_DIR}" pull --quiet || true
fi

cat > "${SHAPEKERNEL_DIR}/ShapeKernel/ShapeKernel.csproj" <<'EOF'
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
EOF

echo "Compiling global ShapeKernel.dll..."
dotnet build "${SHAPEKERNEL_DIR}/ShapeKernel/ShapeKernel.csproj" -c Release -o "${LIB_DIR}" --nologo -v q

echo "Building and Packaging PicoGK-MCP..."
dotnet pack -c Release -o ./nupkg --nologo -v q

echo "Installing PicoGK-MCP as a Global .NET Tool..."
dotnet tool update -g --add-source ./nupkg PicoGK.Mcp --no-cache 2>/dev/null || dotnet tool install -g --add-source ./nupkg PicoGK.Mcp

echo "PicoGK-MCP and ShapeKernel are installed globally!"
echo 'You can now configure any MCP client with: "command": "picogk-mcp"'
