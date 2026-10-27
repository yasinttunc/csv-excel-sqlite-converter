using System.Windows;
using System.Windows.Threading;

namespace DataConverter.Desktop;

public partial class App : Application
{
    public const string ErrorCaption = "Data Converter - error";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        // A file passed on the command line (or dropped on the .exe) is opened straight away.
        if (e.Args.Length > 0)
        {
            window.OpenPath(e.Args[0]);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Something went wrong: {e.Exception.Message}", ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
