# PicoGK-MCP Assistant Guidelines

This repository provides an official Model Context Protocol (MCP) server for PicoGK.

## Capabilities & Tools
- `evaluate`: Compiles and runs in-memory C# PicoGK scripts and returns metrics + base64 PNG snapshots.
- `scaffold_project`: Generates complete .NET 9 projects with starter templates (`gyroid_tpms`, `conformal_lattice`, `blank`).
- `audit_manufacturing`: Inspects voxel bounding box, watertightness, and overhang angles.
- `export`: Exports to STL, VDB, and CLI slices.

## Syntax & Documentation
If you are unsure of PicoGK C# class signatures or methods, query Context7 for library `/leap71/picogk`.
Always use `vox.Smoothen(float fDistMM)` with an explicit distance argument.
Lattice struts use `lattice.AddBeam(start, rStart, end, rEnd, bRoundCap)`.
Implicit fields implement `IImplicit` with `float fSignedDistance(in Vector3 pt)`.
