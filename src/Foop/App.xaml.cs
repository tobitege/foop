using System.Configuration;
using System.Data;
using System.Windows;
using Foop.Services;

namespace Foop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private FoopController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _controller = new FoopController();
            _controller.Start();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Foop could not be started.\n\n{exception.Message}",
                "Foop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
