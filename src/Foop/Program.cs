using System.IO;

namespace Foop;

internal static class Program
{
    private const string SingleInstanceMutexName =
        @"Local\Foop.2A7623D8-BC9E-4D68-AE56-83A84CB135EC";

    [STAThread]
    public static void Main()
    {
        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            SingleInstanceMutexName,
            out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        EnsureWindowsDirectoryEnvironment();

        try
        {
            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
        finally
        {
            singleInstanceMutex.ReleaseMutex();
        }
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
            throw new InvalidOperationException("The Windows directory could not be determined.");
        }

        Environment.SetEnvironmentVariable(
            windowsDirectoryVariable,
            windowsDirectory,
            EnvironmentVariableTarget.Process);
    }
}
