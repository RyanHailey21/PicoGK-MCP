using System;
using System.IO;
using System.Numerics;
using System.Threading;
using PicoGK;

namespace PicoGK.Mcp
{
    public class PicoGkRenderer
    {
        public static string? CaptureSnapshotBase64(string view = "isometric")
        {
            try
            {
                var viewer = Library.oViewer();
                viewer.ZoomToFit();

                // Apply view orientation
                switch (view.ToLowerInvariant())
                {
                    case "top":
                        viewer.qOrientation = Quaternion.CreateFromYawPitchRoll(0, (float)(-Math.PI / 2.0), 0);
                        break;
                    case "front":
                        viewer.qOrientation = Quaternion.Identity;
                        break;
                    case "side":
                    case "right":
                        viewer.qOrientation = Quaternion.CreateFromYawPitchRoll((float)(Math.PI / 2.0), 0, 0);
                        break;
                    case "isometric":
                    default:
                        // Standard 35.264° pitch, 45° yaw isometric angle
                        viewer.qOrientation = Quaternion.CreateFromYawPitchRoll((float)(Math.PI / 4.0), (float)(-Math.PI / 6.0), 0);
                        break;
                }

                viewer.RequestUpdate();

                string tempFile = Path.Combine(Path.GetTempPath(), $"picogk_snap_{Guid.NewGuid():N}.tga");
                viewer.RequestScreenShot(tempFile);

                // Wait up to 1500ms for viewer frame loop to process screenshot
                int attempts = 0;
                while (!File.Exists(tempFile) && attempts < 15)
                {
                    Thread.Sleep(100);
                    attempts++;
                }

                if (File.Exists(tempFile))
                {
                    TgaIo.LoadTga(tempFile, out Image img);
                    string pngFile = Path.ChangeExtension(tempFile, ".png");
                    img.SavePng(pngFile, 90);

                    byte[] pngBytes = File.ReadAllBytes(pngFile);

                    try { File.Delete(tempFile); } catch { }
                    try { File.Delete(pngFile); } catch { }

                    return Convert.ToBase64String(pngBytes);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[PicoGK Renderer Error] " + ex.Message);
                return null;
            }
        }
    }
}
