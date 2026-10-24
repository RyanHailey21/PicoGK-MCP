using System;
using System.IO;
using System.Numerics;
using PicoGK;

namespace PicoGK.Mcp
{
    public class GeometryStats
    {
        public bool HasActiveModel { get; set; }
        public string ModelType { get; set; } = "None";
        public BBox3 BoundingBox { get; set; }
        public Vector3 SizeMM { get; set; }
        public float VolumeMM3 { get; set; }
        public int TriangleCount { get; set; }
        public long MemoryBytes { get; set; }
        public float VoxelSizeMM { get; set; }
    }

    public class GeometrySession
    {
        private static readonly object s_lock = new();
        private Voxels? m_voxActive;
        private Mesh? m_mshActive;
        private Lattice? m_latActive;
        private string m_modelName = "active";

        public void SetModel(Voxels vox, string name = "active")
        {
            lock (s_lock)
            {
                m_voxActive = vox;
                m_mshActive = null;
                m_latActive = null;
                m_modelName = name;
            }
        }

        public void SetModel(Mesh msh, string name = "active")
        {
            lock (s_lock)
            {
                m_voxActive = null;
                m_mshActive = msh;
                m_latActive = null;
                m_modelName = name;
            }
        }

        public void SetModel(Lattice lat, string name = "active")
        {
            lock (s_lock)
            {
                m_latActive = lat;
                m_voxActive = new Voxels(lat);
                m_mshActive = null;
                m_modelName = name;
            }
        }

        public (Voxels? vox, Mesh? msh, Lattice? lat) GetActiveModel()
        {
            lock (s_lock)
            {
                return (m_voxActive, m_mshActive, m_latActive);
            }
        }

        public Mesh GetOrGenerateMesh()
        {
            lock (s_lock)
            {
                if (m_mshActive != null)
                    return m_mshActive;

                if (m_voxActive != null)
                {
                    m_mshActive = m_voxActive.mshAsMesh();
                    return m_mshActive;
                }

                throw new InvalidOperationException("No active 3D geometry in current session.");
            }
        }

        public GeometryStats GetStats()
        {
            lock (s_lock)
            {
                var stats = new GeometryStats
                {
                    VoxelSizeMM = Library.fVoxelSizeMM
                };

                if (m_voxActive != null)
                {
                    stats.HasActiveModel = true;
                    stats.ModelType = "Voxels (Signed Distance Field)";
                    stats.BoundingBox = m_voxActive.oCalculateBoundingBox();
                    stats.SizeMM = stats.BoundingBox.vecSize();
                    stats.MemoryBytes = m_voxActive.nMemUsage();
                    
                    var mesh = m_mshActive ?? m_voxActive.mshAsMesh();
                    m_mshActive = mesh;
                    stats.TriangleCount = mesh.nTriangleCount();
                    return stats;
                }

                if (m_mshActive != null)
                {
                    stats.HasActiveModel = true;
                    stats.ModelType = "Polygonal Mesh";
                    stats.TriangleCount = m_mshActive.nTriangleCount();
                    return stats;
                }

                stats.HasActiveModel = false;
                return stats;
            }
        }

        public string ExportStl(string? outputPath = null)
        {
            var mesh = GetOrGenerateMesh();
            string path = outputPath ?? Path.Combine(Utils.strDocumentsFolder(), $"{m_modelName}.stl");
            mesh.SaveToStlFile(path);
            return path;
        }

        public string ExportCli(string? outputPath = null, float layerThicknessMM = 0.05f)
        {
            lock (s_lock)
            {
                if (m_voxActive == null)
                {
                    if (m_mshActive != null)
                        m_voxActive = new Voxels(m_mshActive);
                    else
                        throw new InvalidOperationException("No active geometry to slice.");
                }

                string path = outputPath ?? Path.Combine(Utils.strDocumentsFolder(), $"{m_modelName}.cli");
                m_voxActive.SaveToCliFile(path, layerThicknessMM, CliIo.EFormat.UseEmptyFirstLayer, false, new ProgressNoop());
                return path;
            }
        }

        public string ExportVdb(string? outputPath = null)
        {
            lock (s_lock)
            {
                if (m_voxActive == null)
                    throw new InvalidOperationException("Active model is not a voxel field.");

                string path = outputPath ?? Path.Combine(Utils.strDocumentsFolder(), $"{m_modelName}.vdb");
                m_voxActive.SaveToVdbFile(path);
                return path;
            }
        }
    }
}
