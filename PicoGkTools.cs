using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PicoGK;

namespace PicoGK.Mcp
{
    [McpServerToolType]
    public class PicoGkTools
    {
        private readonly GeometrySession _session;
        private readonly RoslynScriptRunner _runner;

        public PicoGkTools(GeometrySession session, RoslynScriptRunner runner)
        {
            _session = session;
            _runner = runner;
        }

        [McpServerTool, Description("Compiles and executes a C# PicoGK computational geometry script in-memory. Updates the active session and optionally returns rendered 3D images.")]
        public async Task<CallToolResult> EvaluateAsync(
            [Description("C# script body. You have direct access to 'PicoGK', 'System.Numerics', 'SetModel(vox)', 'SetModel(msh)', and 'Library.oViewer().Add(vox, group)'.")] string script,
            [Description("Whether to render and return a 3D snapshot image (Base64 PNG) of the generated geometry.")] bool captureSnapshot = true,
            [Description("Camera viewpoint for the snapshot: 'isometric', 'top', 'front', 'side'.")] string view = "isometric")
        {
            var contentBlocks = new List<ContentBlock>();

            try
            {
                string evalResult = await _runner.ExecuteScriptAsync(script);
                var stats = _session.GetStats();

                string summary = stats.HasActiveModel
                    ? $@"Execution Succeeded.
--------------------------------------------------
Model Type:       {stats.ModelType}
Dimensions (XYZ): {stats.SizeMM.X:F2} x {stats.SizeMM.Y:F2} x {stats.SizeMM.Z:F2} mm
Triangle Count:   {stats.TriangleCount:N0}
Voxel Memory:     {stats.MemoryBytes / (1024.0 * 1024.0):F2} MB
Voxel Resolution: {stats.VoxelSizeMM:F2} mm
Script Output:    {evalResult}"
                    : $@"Script executed successfully, but no model was registered with 'SetModel(vox)'.
Output: {evalResult}";

                contentBlocks.Add(new TextContentBlock { Text = summary });

                if (captureSnapshot && stats.HasActiveModel)
                {
                    string? base64Png = PicoGkRenderer.CaptureSnapshotBase64(view);
                    if (!string.IsNullOrEmpty(base64Png))
                    {
                        byte[] rawBytes = Convert.FromBase64String(base64Png);
                        contentBlocks.Add(new ImageContentBlock
                        {
                            Data = rawBytes,
                            MimeType = "image/png"
                        });
                    }
                }

                return new CallToolResult
                {
                    Content = contentBlocks,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                contentBlocks.Add(new TextContentBlock
                {
                    Text = $"Error during PicoGK script execution:\n{ex.Message}\n\nStack:\n{ex.StackTrace}"
                });

                return new CallToolResult
                {
                    Content = contentBlocks,
                    IsError = true
                };
            }
        }

        [McpServerTool, Description("Captures and returns a high-resolution 3D rendered snapshot of the current in-memory geometry without re-running the script.")]
        public CallToolResult RenderSnapshot(
            [Description("Camera viewpoint: 'isometric', 'top', 'front', 'side'.")] string view = "isometric")
        {
            var contentBlocks = new List<ContentBlock>();
            var stats = _session.GetStats();

            if (!stats.HasActiveModel)
            {
                contentBlocks.Add(new TextContentBlock { Text = "No active model in session to render." });
                return new CallToolResult { Content = contentBlocks, IsError = true };
            }

            string? base64Png = PicoGkRenderer.CaptureSnapshotBase64(view);
            if (!string.IsNullOrEmpty(base64Png))
            {
                byte[] rawBytes = Convert.FromBase64String(base64Png);
                contentBlocks.Add(new TextContentBlock { Text = $"Snapshot rendered from '{view}' angle." });
                contentBlocks.Add(new ImageContentBlock
                {
                    Data = rawBytes,
                    MimeType = "image/png"
                });
                return new CallToolResult { Content = contentBlocks, IsError = false };
            }

            contentBlocks.Add(new TextContentBlock { Text = "Failed to capture snapshot from viewer." });
            return new CallToolResult { Content = contentBlocks, IsError = true };
        }

        [McpServerTool, Description("Performs an additive manufacturing and printability audit on the current active geometry (manifold checks, dimensions, thin-wall thresholds).")]
        public CallToolResult AuditManufacturing(
            [Description("Minimum permissible wall thickness in millimeters.")] float minWallThicknessMM = 0.5f,
            [Description("Maximum permissible overhang angle relative to build plate in degrees.")] float maxOverhangAngleDeg = 45.0f)
        {
            var contentBlocks = new List<ContentBlock>();
            var stats = _session.GetStats();

            if (!stats.HasActiveModel)
            {
                contentBlocks.Add(new TextContentBlock { Text = "No active model in session to audit." });
                return new CallToolResult { Content = contentBlocks, IsError = true };
            }

            var audit = PicoGkAuditor.Audit(_session, minWallThicknessMM, maxOverhangAngleDeg);

            string report = $@"==================================================
 PIVOT / METROLOGY ADDITIVE MANUFACTURING AUDIT
==================================================
{audit.PrintabilitySummary}

Recommendations:
{audit.Recommendations}";

            contentBlocks.Add(new TextContentBlock { Text = report });
            return new CallToolResult { Content = contentBlocks, IsError = false };
        }

        [McpServerTool, Description("Exports the current in-memory computational geometry to STL (polygonal mesh), CLI (3D printer laser slice toolpaths), or VDB (volumetric voxel dataset).")]
        public CallToolResult Export(
            [Description("Target format: 'stl', 'cli', 'vdb'.")] string format = "stl",
            [Description("Optional absolute file path to save to. Defaults to your Documents folder.")] string? outputPath = null,
            [Description("Layer slice thickness in mm (only used when exporting to CLI format).")] float layerThicknessMM = 0.05f)
        {
            var contentBlocks = new List<ContentBlock>();
            var stats = _session.GetStats();

            if (!stats.HasActiveModel)
            {
                contentBlocks.Add(new TextContentBlock { Text = "No active model in session to export." });
                return new CallToolResult { Content = contentBlocks, IsError = true };
            }

            try
            {
                string savedPath = format.ToLowerInvariant() switch
                {
                    "cli" => _session.ExportCli(outputPath, layerThicknessMM),
                    "vdb" => _session.ExportVdb(outputPath),
                    "stl" or _ => _session.ExportStl(outputPath)
                };

                var fileInfo = new FileInfo(savedPath);
                string summary = $@"Successfully exported geometry!
Format:     {format.ToUpperInvariant()}
Path:       {savedPath}
File Size:  {fileInfo.Length / (1024.0 * 1024.0):F2} MB
Triangles:  {stats.TriangleCount:N0}";

                contentBlocks.Add(new TextContentBlock { Text = summary });
                return new CallToolResult { Content = contentBlocks, IsError = false };
            }
            catch (Exception ex)
            {
                contentBlocks.Add(new TextContentBlock { Text = $"Export failed: {ex.Message}" });
                return new CallToolResult { Content = contentBlocks, IsError = true };
            }
        }

        [McpServerTool, Description("Scaffolds a complete, ready-to-build PicoGK .NET 9 project on disk with csproj, packages, and starter computational geometry templates.")]
        public CallToolResult ScaffoldProject(
            [Description("Project name, e.g. 'TurbineHeatExchanger' or 'NeuromorphicCore'.")] string projectName,
            [Description("Target folder path on disk. Defaults to user's Documents folder.")] string? targetDirectory = null,
            [Description("Template type: 'gyroid_tpms', 'conformal_lattice', 'neuromorphic', 'blank'.")] string template = "gyroid_tpms")
        {
            var contentBlocks = new List<ContentBlock>();

            try
            {
                string baseDir = targetDirectory ?? Utils.strDocumentsFolder();
                string projectDir = Path.Combine(baseDir, projectName);

                if (!Directory.Exists(projectDir))
                {
                    Directory.CreateDirectory(projectDir);
                }

                // 1. Create .csproj
                string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""PicoGK"" Version=""2.3.0"" />
  </ItemGroup>

</Project>
";
                File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csprojContent);

                // 2. Select code template
                string programContent = GetTemplateContent(projectName, template);
                File.WriteAllText(Path.Combine(projectDir, "Program.cs"), programContent);

                string result = $@"==================================================
 PicoGK Project Successfully Scaffolded!
==================================================
Project Name:  {projectName}
Location:      {projectDir}
Template:      {template}
Framework:     .NET 9.0 (PicoGK 2.3.0)

Files Created:
- {projectName}.csproj
- Program.cs

To run & view in 3D:
cd ""{projectDir}""
dotnet run";

                contentBlocks.Add(new TextContentBlock { Text = result });
                return new CallToolResult { Content = contentBlocks, IsError = false };
            }
            catch (Exception ex)
            {
                contentBlocks.Add(new TextContentBlock { Text = $"Scaffolding failed: {ex.Message}" });
                return new CallToolResult { Content = contentBlocks, IsError = true };
            }
        }

        private static string GetTemplateContent(string projectName, string template)
        {
            return template.ToLowerInvariant() switch
            {
                "conformal_lattice" => @"using System;
using System.Numerics;
using PicoGK;

internal class Program
{
    private static void Main(string[] args)
    {
        Library.Go(0.4f, GenerateLattice);
    }

    private static void GenerateLattice()
    {
        Lattice lat = new Lattice();
        Random rnd = new Random(42);

        // Generate 3D grid with randomized strut connectivity
        for (int x = -2; x <= 2; x++)
        for (int y = -2; y <= 2; y++)
        for (int z = -2; z <= 2; z++)
        {
            Vector3 ptA = new Vector3(x * 8f, y * 8f, z * 8f);
            lat.AddSphere(ptA, 1.2f);

            if (x < 2) lat.AddBeam(ptA, 0.8f, new Vector3((x + 1) * 8f, y * 8f, z * 8f), 0.8f, true);
            if (y < 2) lat.AddBeam(ptA, 0.8f, new Vector3(x * 8f, (y + 1) * 8f, z * 8f), 0.8f, true);
            if (z < 2) lat.AddBeam(ptA, 0.8f, new Vector3(x * 8f, y * 8f, (z + 1) * 8f), 0.8f, true);
        }

        Voxels vox = new Voxels(lat);
        vox.Smoothen(0.5f);

        Library.oViewer().Add(vox);
    }
}
",
                "blank" => @"using System;
using PicoGK;

internal class Program
{
    private static void Main(string[] args)
    {
        Library.Go(0.5f, Task);
    }

    private static void Task()
    {
        Library.oViewer().Add(Utils.mshCreateCube());
    }
}
",
                "gyroid_tpms" or _ => @"using System;
using System.Numerics;
using PicoGK;

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

internal class Program
{
    private static void Main(string[] args)
    {
        Library.Go(0.4f, GenerateGyroid);
    }

    private static void GenerateGyroid()
    {
        BBox3 bounds = new BBox3(new Vector3(-20), new Vector3(20));
        Voxels voxGyroid = new Voxels(new GyroidField(10f), bounds);
        Voxels voxEnvelope = Voxels.voxSphere(Vector3.Zero, 18f);

        Voxels result = voxGyroid & voxEnvelope;
        result.Smoothen(0.4f);

        Library.oViewer().Add(result);
    }
}
"
            };
        }
    }
}
