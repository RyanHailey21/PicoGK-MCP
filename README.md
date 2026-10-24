# PicoGK-MCP: Universal Model Context Protocol Server for Computational Geometry

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![MCP Compliant](https://img.shields.io/badge/MCP-2024--11--05%20%2F%202026-green.svg)](https://modelcontextprotocol.io)
[![LEAP 71](https://img.shields.io/badge/Engine-PicoGK%20%2B%20ShapeKernel-orange.svg)](https://leap71.com)

**PicoGK-MCP** bridges AI assistants (Claude, Cursor, Antigravity, OpenAI Codex, Windsurf, VS Code Copilot) with the **[PicoGK](https://picogk.org)** computational engineering kernel and **[ShapeKernel](https://github.com/leap71/LEAP71_ShapeKernel)** parametric library developed by [LEAP 71](https://leap71.com).

Unlike traditional CAD kernels (which choke on micro-features and organic lattices), **PicoGK-MCP** provides an **In-Memory Computational Geometry Sandbox & Visual Inspector**.

---

## Quickstart (Deploy on Any Machine in 1 Minute)

### 1. Clone & Install
```bash
git clone https://github.com/RyanHailey21/PicoGK-MCP.git
cd PicoGK-MCP

# On Windows:
.\install.ps1

# On Linux / macOS:
chmod +x ./install.sh && ./install.sh
```

This compiles and registers `picogk-mcp` as a **global system command**. You can now invoke it from anywhere with:
```bash
picogk-mcp
```

---

## Universal Client Configuration

Because `picogk-mcp` is a global tool, **no hardcoded file paths are required**. Simply reference `"command": "picogk-mcp"` in your client config.

### The Recommended Pair: PicoGK-MCP + Context7
Pairing **PicoGK-MCP** with **[Context7](https://context7.com)** gives your AI agent the complete computational engineering stack:
- **Context7** provides instant knowledge lookup across 600+ verified PicoGK code snippets (`/leap71/picogk`).
- **PicoGK-MCP** provides the in-memory execution engine, visual rendering (`ImageContent`), and 3D printing export.

---

### Configuration by Client

#### 1. Claude Desktop (`claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "picogk": {
      "command": "picogk-mcp"
    },
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp"]
    }
  }
}
```

#### 2. Claude Code (CLI)
```bash
claude mcp add picogk picogk-mcp
claude mcp add --transport http context7 https://mcp.context7.com/mcp
```

#### 3. Cursor (`.cursor/mcp.json`)
```json
{
  "mcpServers": {
    "picogk": {
      "command": "picogk-mcp"
    },
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp"]
    }
  }
}
```

#### 4. Google Antigravity / Gemini CLI (`mcp_config.json`)
```json
{
  "mcpServers": {
    "picogk": {
      "command": "picogk-mcp"
    },
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp"]
    }
  }
}
```

#### 5. Windsurf / VS Code Copilot (`mcp.json`)
```json
{
  "mcpServers": {
    "picogk": {
      "command": "picogk-mcp"
    },
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp"]
    }
  }
}
```

---

## Available MCP Tools

| Tool | Parameters | Description |
| :--- | :--- | :--- |
| **`evaluate`** | `script` (*string*), `captureSnapshot` (*bool*), `view` (*string*) | Executes C# PicoGK & ShapeKernel code in-memory. Returns volumetric metrics ($mm^3$, bounding box, triangle count) and multi-angle 3D snapshot images (`ImageContent` PNG). |
| **`render_snapshot`** | `view` (*string*: `"isometric"`, `"top"`, `"front"`, `"side"`) | Captures and returns an off-screen rendered 3D image of the current in-memory model. |
| **`scaffold_project`** | `projectName` (*string*), `targetDirectory` (*string*), `template` (*string*) | Generates a complete, ready-to-run .NET 9 project on disk with `PicoGK 2.3.0` and starter templates (`gyroid_tpms`, `conformal_lattice`, `neuromorphic`, `blank`). |
| **`audit_manufacturing`** | `minWallThicknessMM` (*float*), `maxOverhangAngleDeg` (*float*) | Inspects active geometry for 3D printing: verifies watertight manifold topology, bounding box envelope fit, and thin-wall/overhang risks. |
| **`export`** | `format` (*string*: `"stl"`, `"cli"`, `"vdb"`), `outputPath` (*string*), `layerThicknessMM` (*float*) | Exports the model to watertight **STL** (meshes), **CLI** (industrial laser powder bed fusion slice toolpaths), or **VDB** (OpenVDB voxel grids). |

---

## Writing Scripts in `evaluate`

Your AI agent has direct access to both `PicoGK` and `Leap71.ShapeKernel`:

```csharp
// Generate a Bionic Gyroid Scaffold in 5 lines
IImplicit gyroid = new ImplicitGyroid(fCellSizeMM: 6.0f, fWallThicknessMM: 0.9f);
IImplicit sphere = new ImplicitSphere(vecCenter: Vector3.Zero, fRadius: 15.0f);
BBox3 bounds = new BBox3(new Vector3(-18), new Vector3(18));

// Intersect implicit fields and register
Voxels voxScaffold = new Voxels(sphere, bounds).voxIntersectImplicit(gyroid);
SetModel(voxScaffold, "BionicScaffold");

Library.oViewer().Add(voxScaffold, 0);
return "Scaffold generated with ShapeKernel!";
```

---

## Containerized Deployment (Docker)
```bash
docker build -t picogk-mcp .
docker run -i --rm picogk-mcp
```

---

## 📄 License
Released under the [Apache 2.0 License](LICENSE).
PicoGK and ShapeKernel are developed by [LEAP 71](https://github.com/leap71).
Model Context Protocol is an open standard developed by [Anthropic](https://modelcontextprotocol.io).
