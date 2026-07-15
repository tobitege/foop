using System.IO;

namespace Foop;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        EnsureWindowsDirectoryEnvironment();

        var application = new App();
        application.InitializeComponent();
        application.Run();
    }

    private static void EnsureWindowsDirectoryEnvironment()
    {
        const string windowsDirectoryVariable = "windir";
        var windowsDirectory = Environment.GetEnvironmentVariable(windowsDirectoryVariable);
        if (!string.IsNullOrWhiteSpace(windowsDirectory) && Directory.Exists(windowsDirectory))
        {
            return;
        }

        windowsDirectory = Environment.GetEnvironmentVariable("SystemRoot");
        if (string.IsNullOrWhiteSpace(windowsDirectory) || !Directory.Exists(windowsDirectory))
        {
            throw new InvalidOperationException("Das Windows-Verzeichnis konnte nicht ermittelt werden.");
        }

        Environment.SetEnvironmentVariable(
            windowsDirectoryVariable,
            windowsDirectory,
            EnvironmentVariableTarget.Process);
    }
}
