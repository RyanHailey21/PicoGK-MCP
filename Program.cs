using System;
using System.IO;
using System.Runtime.InteropServices;
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
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int WH_CBT = 5;
        private const int HCBT_CREATEWND = 3;
        private const int SW_HIDE = 0;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct CBT_CREATEWND
        {
            public IntPtr lpcs;
            public IntPtr hwndInsertAfter;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CREATESTRUCT
        {
            public IntPtr lpCreateParams;
            public IntPtr hInstance;
            public IntPtr hMenu;
            public IntPtr hwndParent;
            public int cy;
            public int cx;
            public int y;
            public int x;
            public int style;
        }

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
                IntPtr hHook = IntPtr.Zero;
                HookProc? hookProc = null;

                try
                {
                    bool showViewer = Environment.GetEnvironmentVariable("PICOGK_SHOW_VIEWER") == "1";

                    // On Windows, intercept window creation with WH_CBT to strip WS_VISIBLE and position offscreen.
                    // This guarantees zero screen flicker and zero window popups when MCP clients connect.
                    if (OperatingSystem.IsWindows() && !showViewer)
                    {
                        hookProc = (nCode, wParam, lParam) =>
                        {
                            if (nCode == HCBT_CREATEWND)
                            {
                                try
                                {
                                    var cbt = Marshal.PtrToStructure<CBT_CREATEWND>(lParam);
                                    var cs = Marshal.PtrToStructure<CREATESTRUCT>(cbt.lpcs);
                                    cs.style &= ~0x10000000; // Remove WS_VISIBLE
                                    cs.x = -32000;
                                    cs.y = -32000;
                                    Marshal.StructureToPtr(cs, cbt.lpcs, false);
                                }
                                catch { }
                            }
                            return CallNextHookEx(hHook, nCode, wParam, lParam);
                        };

                        uint tid = GetCurrentThreadId();
                        hHook = SetWindowsHookEx(WH_CBT, hookProc, IntPtr.Zero, tid);
                    }

                    string windowTitle = $"PicoGK MCP Active Session ({Environment.ProcessId})";

                    Library.Go(
                        0.5f,
                        () =>
                        {
                            if (hHook != IntPtr.Zero)
                            {
                                UnhookWindowsHookEx(hHook);
                                hHook = IntPtr.Zero;
                            }

                            if (OperatingSystem.IsWindows() && !showViewer)
                            {
                                IntPtr hWnd = FindWindow(null, windowTitle);
                                if (hWnd != IntPtr.Zero)
                                {
                                    SetWindowPos(hWnd, IntPtr.Zero, -32000, -32000, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                                    ShowWindow(hWnd, SW_HIDE);
                                }
                            }

                            Console.Error.WriteLine($"[PicoGK-MCP] Engine initialized (v{Library.strVersion()}) in headless background mode.");
                            picoGkReady.Set();

                            while (Library.bContinueTask())
                            {
                                Thread.Sleep(50);
                            }
                        },
                        strLogFilePath: Path.Combine(Path.GetTempPath(), $"PicoGK_MCP_{Environment.ProcessId}.log"),
                        bEndAppWithTask: true,
                        strWindowTitle: windowTitle,
                        strLightsFile: string.Empty
                    );
                }
                catch (Exception ex)
                {
                    if (hHook != IntPtr.Zero)
                    {
                        UnhookWindowsHookEx(hHook);
                    }
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
                picoGkThread.Join(2000);
            }
            catch { }
        }
    }
}
