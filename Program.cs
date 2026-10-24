using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PicoGK;

namespace PicoGK.Mcp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Capture raw standard IO streams for the MCP JSON-RPC transport
            var rawStdIn = Console.OpenStandardInput();
            var rawStdOut = Console.OpenStandardOutput();

            // Redirect default Console.Out to Console.Error
            // This guarantees any console writes from PicoGK or runtime libraries go to stderr,
            // leaving stdout exclusively for JSON-RPC messages.
            Console.SetOut(Console.Error);

            // Initialize PicoGK runtime and viewer on a dedicated STA thread
            var picoGkReady = new ManualResetEventSlim(false);
            var picoGkThread = new Thread(() =>
            {
                try
                {
                    Library.Go(
                        0.5f,
                        () =>
                        {
                            Console.Error.WriteLine($"[PicoGK-MCP] Engine initialized (v{Library.strVersion()}).");
                            picoGkReady.Set();

                            while (Library.bContinueTask())
                            {
                                Thread.Sleep(50);
                            }
                        },
                        strLogFilePath: Path.Combine(Path.GetTempPath(), "PicoGK_MCP.log"),
                        bEndAppWithTask: false,
                        strWindowTitle: "PicoGK MCP Active Session",
                        strLightsFile: string.Empty
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[PicoGK-MCP] Engine startup failed: " + ex.Message);
                    picoGkReady.Set();
                }
            });

            if (OperatingSystem.IsWindows())
            {
                picoGkThread.SetApartmentState(ApartmentState.STA);
            }
            picoGkThread.IsBackground = true;
            picoGkThread.Start();

            // Wait for PicoGK engine to be warm
            picoGkReady.Wait(8000);

            // Configure MCP Server Host
            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // Register services
            builder.Services.AddSingleton<GeometrySession>();
            builder.Services.AddSingleton<RoslynScriptRunner>();
            builder.Services.AddSingleton<PicoGkTools>();

            // Register Model Context Protocol Server with clean stream transport
            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                    {
                        Name = "picogk-mcp",
                        Version = "1.0.0"
                    };
                })
                .WithStreamServerTransport(rawStdIn, rawStdOut)
                .WithToolsFromAssembly();

            var host = builder.Build();

            Console.Error.WriteLine("[PicoGK-MCP] Server listening on standard I/O (stdio). Ready for MCP connections.");

            await host.RunAsync();

            try
            {
                Library.EndTask();
                picoGkThread.Join(1000);
            }
            catch { }
        }
    }
}
