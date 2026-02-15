using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace ImageViewerNew;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        string? filePath = null;

        base.OnStartup(e);

        // Аргументы командной строки
        if (e.Args.Length > 0)
        {
            filePath = e.Args[0];
        }
        
        var mainWindow = new MainWindow(filePath);
        mainWindow.Show();
    }
}

