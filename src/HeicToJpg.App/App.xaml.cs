using System.Windows;

namespace HeicToJpg.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        if (e.Args.Length > 0)
        {
            mainWindow.OpenFiles(e.Args);
        }

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
