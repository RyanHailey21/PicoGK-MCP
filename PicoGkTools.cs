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
    }
}
