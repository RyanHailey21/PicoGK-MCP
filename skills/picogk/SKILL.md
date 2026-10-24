---
name: picogk
description: "Computational engineering workflow using PicoGK and PicoGK-MCP. Triggers on: 'PicoGK', 'gyroid', 'TPMS', 'computational geometry', 'porous structure', 'metamaterial', 'lattice', 'conformal cooling', 'voxel solid', 'additive manufacturing', 'laser powder bed fusion', 'slicing CLI'. Guides project scaffolding, in-memory Roslyn execution, visual feedback, Context7 documentation lookup, and manufacturing audits."
---

# PicoGK Computational Engineering & MCP Workflow

This skill guides the design, procedural generation, visual inspection, and manufacturing auditing of complex 3D geometries using **PicoGK** and the **PicoGK-MCP** server.

---

## 1. When to Use PicoGK (vs Standard B-Rep CAD)

Always route to PicoGK when the engineering requirements involve:
- **Lattices & Strut Networks**: Thousands of procedural struts (bone scaffolds, lightweight aerospace structures). Traditional B-Rep kernels freeze on face stitching; PicoGK voxelizes seamlessly.
- **Triply Periodic Minimal Surfaces (TPMS)**: Gyroid, Schwarz P, Diamond manifolds for heat exchangers and microfluidic jackets.
- **Organic Filleting**: Blending dense intersections simultaneously with `vox.Smoothen(float fDistMM)`.
- **Laser Powder Bed Fusion (LPBF)**: Direct vector slice toolpaths (`vox.SaveToCliFile`) bypassing fragile polygon STLs.

---

## 2. Documentation Discovery via Context7

PicoGK is indexed in **Context7**. Before guessing API signatures:
- Call `query-docs` with `libraryId: "/leap71/picogk"`
- Search for topics: `Voxels`, `Lattice`, `ScalarField`, `IImplicit`, `Mesh`.

---

## 3. Workflow Modes

### Mode A: Fast Interactive Prototyping (via PicoGK-MCP)
1. **Execute & Inspect**:
   Call `picogk_evaluate` with C# code. The server compiles via Roslyn, updates the in-memory voxel session, and returns metrics + a 3D rendered snapshot image (`ImageContent`).
2. **Audit Printability**:
   Call `picogk_audit_manufacturing` to check watertightness, bounding box, and thin-wall risks.
3. **Export**:
   Call `picogk_export` with format `"stl"` or `"cli"`.

### Mode B: Standalone Disk Project Scaffolding
Call `picogk_scaffold_project(projectName, template: "gyroid_tpms"|"conformal_lattice"|"blank")`.
This generates:
- `<ProjectName>.csproj` targeting `.NET 9.0` with `PicoGK 2.3.0`.
- Starter `Program.cs` with an active 3D viewer setup.
- Run natively via `dotnet run`.

---

## 4. Key API Patterns

### A. Triply Periodic Minimal Surface (Gyroid)
```csharp
public class GyroidField : IImplicit
{
    private readonly float m_k;
    public GyroidField(float fCellSize) => m_k = 2f * (float)Math.PI / fCellSize;

    public float fSignedDistance(in Vector3 p)
    {
        float x = p.X * m_k, y = p.Y * m_k, z = p.Z * m_k;
        float val = (float)(Math.Sin(x) * Math.Cos(y) + Math.Sin(y) * Math.Cos(z) + Math.Sin(z) * Math.Cos(x));
        return Math.Abs(val) - 0.35f;
    }
}
```

### B. Procedural Lattices & Organic Smoothing
```csharp
Lattice lat = new Lattice();
lat.AddSphere(vecCenter, 1.5f);
lat.AddBeam(vecStart, 0.8f, vecEnd, 0.6f, bRoundCap: true);

Voxels vox = new Voxels(lat);
vox.Smoothen(0.5f); // 0.5 mm continuous organic fillet
```

### C. Direct Laser Toolpath Slicing (CLI)
```csharp
vox.SaveToCliFile("toolpaths.cli", 0.05f, CliIo.EFormat.UseEmptyFirstLayer, false, new ProgressNoop());
```
