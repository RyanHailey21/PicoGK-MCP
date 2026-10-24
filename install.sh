#!/usr/bin/env bash
set -e

echo "Building and Packaging PicoGK-MCP..."
dotnet pack -c Release -o ./nupkg

echo "Installing PicoGK-MCP as a Global .NET Tool..."
dotnet tool update -g --add-source ./nupkg PicoGK.Mcp 2>/dev/null || dotnet tool install -g --add-source ./nupkg PicoGK.Mcp

echo "PicoGK-MCP installed successfully!"
echo "You can now configure any MCP client with:"
echo '  "command": "picogk-mcp"'
