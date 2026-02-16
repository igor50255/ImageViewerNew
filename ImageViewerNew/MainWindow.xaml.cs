using ImageViewerNew.CreateStart;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

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

        // ✅ переносим document.title -> Title окна WPF
        Browser.CoreWebView2.DocumentTitleChanged += (_, __) =>
        {
            Dispatcher.Invoke(() =>
            {
                Title = Browser.CoreWebView2.DocumentTitle; // или $"ImageViewer — {Browser.CoreWebView2.DocumentTitle}"
            });
        };

        Browser.NavigationCompleted += async (_, __) =>
        {
            // фокус WPF -> WebView2
            Browser.Focus();
            Keyboard.Focus(Browser);

            // фокус внутри страницы (иногда нужен дополнительно)
            await Browser.ExecuteScriptAsync("window.focus();");
        };

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

    private bool _isWpfFullscreen;

    private WindowStyle _prevWindowStyle;
    private WindowState _prevWindowState;
    private ResizeMode _prevResizeMode;
    private bool _prevTopmost;

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("type", out var typeEl))
            {
                var type = typeEl.GetString();
                if (type == "toggle-window-fullscreen")
                {
                    ToggleWpfFullscreen();
                }
                if (type == "exit-window-fullscreen")
                {
                    if (_isWpfFullscreen) ToggleWpfFullscreen();
                }
            }
        }
        catch
        {
            // можно логировать, но можно и молча игнорировать
        }
    }

    // развернуть и свернуть окно WPF в полноэкранный режим (без рамки) двойным кликом по окну
    private void ToggleWpfFullscreen()
    {
        if (!_isWpfFullscreen)
        {
            // сохраняем текущее состояние
            _prevWindowStyle = this.WindowStyle;
            _prevWindowState = this.WindowState;
            _prevResizeMode = this.ResizeMode;
            _prevTopmost = this.Topmost;

            // включаем fullscreen без рамки
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true; // опционально, чтобы поверх панели задач
            this.WindowState = WindowState.Maximized;

            _isWpfFullscreen = true;
        }
        else
        {
            // возвращаем как было
            this.Topmost = _prevTopmost;
            this.WindowStyle = _prevWindowStyle;
            this.ResizeMode = _prevResizeMode;
            this.WindowState = _prevWindowState;

            _isWpfFullscreen = false;
        }

        // после смены стиля фокус иногда теряется
        Browser.Focus();
        Keyboard.Focus(Browser);
    }
}