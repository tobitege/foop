using System.IO;
using System.Windows;
using Foop.Services;

namespace Foop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        if (!SingleInstanceService.TryStartAsPrimaryInstance())
        {
            return;
        }

        EnsureWindowsDirectoryEnvironment();

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        finally
        {
            SingleInstanceService.Release();
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
