# PicoGK-MCP: Model Context Protocol Server for Computational Geometry

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![MCP Compliant](https://img.shields.io/badge/MCP-2024--11--05-green.svg)](https://modelcontextprotocol.io)

**PicoGK-MCP** bridges AI assistants (Claude, Antigravity, ChatGPT, Cursor, VS Code Copilot) with the **[PicoGK](https://picogk.org)** open-source computational engineering kernel developed by [LEAP 71](https://leap71.com).

Rather than exposing low-level, high-latency geometry primitives over RPC, **PicoGK-MCP** acts as an **In-Memory Computational Geometry Sandbox & Visual Inspector**. It equips AI agents with:
- **Dynamic C# Computational Geometry Execution** (powered by Roslyn Scripting).
- **Autonomous Visual Sight (`ImageContent`)**: Off-screen rendered snapshots returned directly in the tool response for multimodal inspection.
- **Additive Manufacturing Metrology**: Automated checks for bounding boxes, voxel counts, watertight verification, minimum wall thicknesses, and overhangs.
- **Industrial 3D Printing Export**: Direct export to watertight **STL** (meshes), **CLI** (industrial laser powder bed fusion slice toolpaths), and **VDB** (volumetric OpenVDB datasets).

---

## Architecture & Context Division

```
┌────────────────────────────────────────────────────────┐
│                   AI Assistant / Host                   │
│          (Claude Desktop, Antigravity, Cursor)         │
└───────────────▲────────────────────────▲───────────────┘
                │                        │
       Context7 MCP Server       PicoGK-MCP Server
       (/leap71/picogk docs)     (In-Memory Compute Engine)
                │                        │
        API docs, recipes         • Roslyn C# In-Memory Scripting
        & code snippets           • Real-time OpenGL 3D Render
                                  • Metrology & Manufacturing Audit
                                  • Direct STL / CLI Toolpath Export
```

> **Note on Documentation**: For rich API signatures, tutorials, and recipes, pair this server with **Context7** (`/leap71/picogk`), which indexes 600+ verified PicoGK code snippets.

---

## Tools

### 1. `evaluate`
Compiles and executes a C# PicoGK computational geometry script in-memory against the active session. Updates the active model and optionally returns a rendered 3D snapshot image (Base64 PNG).

**Parameters:**
- `script` (*string*, required): C# script body. You have direct access to `PicoGK`, `System.Numerics`, `SetModel(vox)`, `SetModel(msh)`, and `Library.oViewer().Add(vox, group)`.
- `captureSnapshot` (*bool*, optional, default: `true`): Whether to capture and return an image snapshot.
- `view` (*string*, optional, default: `"isometric"`): Camera viewpoint (`"isometric"`, `"top"`, `"front"`, `"side"`).

### 2. `render_snapshot`
Captures and returns a high-resolution 3D rendered snapshot of the current in-memory geometry without re-running the script.

**Parameters:**
- `view` (*string*, optional, default: `"isometric"`): Camera viewpoint (`"isometric"`, `"top"`, `"front"`, `"side"`).

### 3. `audit_manufacturing`
Performs an additive manufacturing and printability audit on the current active geometry. OpenVDB Dual Contouring guarantees watertight manifold topology; this tool checks physical bounding boxes, build-plate fit, and thin-wall/overhang thresholds.

**Parameters:**
- `minWallThicknessMM` (*float*, optional, default: `0.5`): Minimum permissible wall thickness in millimeters.
- `maxOverhangAngleDeg` (*float*, optional, default: `45.0`): Maximum permissible overhang angle relative to build plate.

### 4. `export`
Exports the current in-memory computational geometry to disk.

**Parameters:**
- `format` (*string*, optional, default: `"stl"`): Target format (`"stl"`, `"cli"`, `"vdb"`).
- `outputPath` (*string*, optional): Destination file path. Defaults to your user Documents folder.
- `layerThicknessMM` (*float*, optional, default: `0.05`): Layer slice thickness in mm (used for CLI slicing).

---

## Installation & Setup

### Prerequisites
- **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (Windows x64 or Linux x64)

### Building from Source
```bash
git clone https://github.com/<your-username>/PicoGK-MCP.git
cd PicoGK-MCP
dotnet build -c Release
```

The executable will be located at:
`bin/Release/net9.0/PicoGK.Mcp.exe` (Windows) or `PicoGK.Mcp` (Linux).

---

## Client Configuration

### Claude Desktop (`claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "picogk": {
      "command": "C:\\Users\\<USER>\\Documents\\PicoGK-MCP\\bin\\Release\\net9.0\\PicoGK.Mcp.exe"
    }
  }
}
```

Or using `dotnet run`:
```json
{
  "mcpServers": {
    "picogk": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Users\\<USER>\\Documents\\PicoGK-MCP\\PicoGK.Mcp.csproj",
        "-c",
        "Release"
      ]
    }
  }
}
```

---

## Example Script for `evaluate`

```csharp
// Generate a Triply Periodic Minimal Surface (Gyroid)
public class Gyroid : IImplicit {
    float k = 2f * (float)Math.PI / 10f;
    public float fSignedDistance(in Vector3 p) {
        float x = p.X * k, y = p.Y * k, z = p.Z * k;
        return (float)(Math.Sin(x)*Math.Cos(y) + Math.Sin(y)*Math.Cos(z) + Math.Sin(z)*Math.Cos(x));
    }
}

BBox3 bounds = new BBox3(new Vector3(-20), new Vector3(20));
Voxels voxGyroid = new Voxels(new Gyroid(), bounds);
Voxels voxSphere = Voxels.voxSphere(Vector3.Zero, 18f);

// Boolean intersection & organic smoothing
Voxels result = (voxGyroid & voxSphere);
result.Smoothen(0.4f);

SetModel(result, "PorousBoneScaffold");
Library.oViewer().Add(result, 0);
return "Scaffold successfully generated.";
```

---

## License
Released under the [Apache 2.0 License](LICENSE).
PicoGK is developed by [LEAP 71](https://github.com/leap71/PicoGK).
Model Context Protocol is an open standard developed by [Anthropic](https://modelcontextprotocol.io).
