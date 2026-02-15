using ImageViewerNew.CreateStart;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;

namespace ImageViewerNew;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string host = "app";
    private const string hostImg = "hostimg";
    string? _currentFilePath = null;
    public MainWindow(string? currentFilePath)
    {
        _currentFilePath = currentFilePath;

        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Browser.EnsureCoreWebView2Async();

        CreateImagesJs(_currentFilePath);

        await InitializeWebView2Async();
    }

    private async Task InitializeWebView2Async()
    {
        // 1) Маппим папку wwwroot на виртуальный домен https://app/
        string wwwRoot = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot");

        Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
            host,
            wwwRoot,
            CoreWebView2HostResourceAccessKind.Allow
        );

        // 2) Слушаем сообщения JS -> C#
        Browser.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // 3) Открываем страницу
        Browser.CoreWebView2.Navigate($"https://{host}/index.html");
    }

    // --- Создание файла images.js с информацией о картинках ---
    private void CreateImagesJs(string? currentFilePath)
    {
        Browser.CoreWebView2.ClearVirtualHostNameToFolderMapping(hostImg);

        string sourcePath = currentFilePath is null
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot/images")
            : Path.GetDirectoryName(currentFilePath)!;

        Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
            hostImg, 
            sourcePath, 
            CoreWebView2HostResourceAccessKind.Allow);

        // ------- Создание файла images.js с информацией о картинках -------
        new WorkImagesInfo().CreateFile(sourcePath!, currentFilePath, hostImg);
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {

    }
}