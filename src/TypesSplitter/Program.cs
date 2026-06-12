using System.Runtime.InteropServices;
using Avalonia;
using TypesSplitter.Core;

namespace TypesSplitter;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // The exe is a WinExe (no console of its own); when run from a terminal
        // in CLI mode, attach to the parent console so output is visible.
        if (args.Length != 0 && OperatingSystem.IsWindows())
            AttachConsole(AttachParentProcess);

        // Headless CLI mode: DayZ-Types-Splitter.exe <types.xml> <output dir>
        if (args.Length == 2)
            return RunCli(args[0], args[1]);
        // One arg = open the GUI with that file preloaded ("Open with…" support)
        if (args.Length == 1 && File.Exists(args[0]))
            InitialFile = args[0];
        else if (args.Length != 0)
        {
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  DayZ-Types-Splitter                        GUI");
            Console.Error.WriteLine("  DayZ-Types-Splitter <types.xml> <outdir>   split without GUI");
            return 2;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>File passed on the command line, loaded by MainWindow on startup.</summary>
    public static string? InitialFile { get; private set; }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);

    private static int RunCli(string input, string outputDir)
    {
        try
        {
            var doc = TypesDocument.Load(input);
            var results = doc.Split(outputDir, progress: Console.WriteLine);
            Console.WriteLine($"Done: {results.Sum(r => r.Count)} types into {results.Count} files in {outputDir}");
            Console.WriteLine();
            Console.WriteLine("Register them in cfgeconomycore.xml:");
            Console.WriteLine(TypesDocument.CfgEconomyCoreSnippet(results));
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"ERROR: {e.Message}");
            return 1;
        }
    }
}
