using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PicoGK;

namespace PicoGK.Mcp
{
    public class ScriptContext
    {
        private readonly GeometrySession m_session;
        public ScriptContext(GeometrySession session) => m_session = session;

        public void SetModel(Voxels vox, string name = "active") => m_session.SetModel(vox, name);
        public void SetModel(Mesh msh, string name = "active") => m_session.SetModel(msh, name);
        public void SetModel(Lattice lat, string name = "active") => m_session.SetModel(lat, name);

        public void AddToViewer(Voxels vox, int group = 0) => Library.oViewer().Add(vox, group);
        public void AddToViewer(Mesh msh, int group = 0) => Library.oViewer().Add(msh, group);
        public void AddToViewer(PolyLine poly, int group = 0) => Library.oViewer().Add(poly, group);

        public void Log(string message) => Console.Error.WriteLine("[Script] " + message);
    }

    public class RoslynScriptRunner
    {
        private readonly GeometrySession m_session;
        private readonly ScriptOptions m_options;

        public RoslynScriptRunner(GeometrySession session)
        {
            m_session = session;
            m_options = ScriptOptions.Default
                .AddReferences(
                    typeof(object).Assembly,
                    typeof(Console).Assembly,
                    typeof(Vector3).Assembly,
                    typeof(Library).Assembly,
                    typeof(GeometrySession).Assembly
                )
                .AddImports(
                    "System",
                    "System.IO",
                    "System.Numerics",
                    "System.Collections.Generic",
                    "PicoGK",
                    "PicoGK.Mcp"
                );
        }

        public async Task<string> ExecuteScriptAsync(string csharpScript)
        {
            var context = new ScriptContext(m_session);
            var state = await CSharpScript.RunAsync(csharpScript, m_options, globals: context, globalsType: typeof(ScriptContext));
            return state.ReturnValue?.ToString() ?? "Script executed successfully.";
        }
    }
}
