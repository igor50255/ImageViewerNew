using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;

namespace ImageViewerNew;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Browser.EnsureCoreWebView2Async();

        // 1) Маппим папку wwwroot (из Output) на виртуальный домен https://app/
        string wwwRoot = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot");
        Directory.CreateDirectory(wwwRoot);

        Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app",
            wwwRoot,
            CoreWebView2HostResourceAccessKind.Allow
        );

        // 2) Слушаем сообщения JS -> C#
        Browser.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // 3) Открываем страницу
        Browser.CoreWebView2.Navigate("https://app/index.html");
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {

    }
}