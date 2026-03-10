// Updated: 2026-03-10
using Avalonia;

namespace Ephemeris.UI.Avalonia;

internal static class Program
{
    /// <summary>The main entry point for the Avalonia cross-platform application.</summary>
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures and builds the Avalonia application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AppBuilder.UsePlatformDetect"/> selects the correct backend for the
    /// current OS: X11 on Linux, Quartz on macOS, Win32 on Windows.
    /// </para>
    /// <para>
    /// This overload is also used by the Avalonia designer — do not add startup logic here;
    /// put it in <see cref="App.OnFrameworkInitializationCompleted"/> instead.
    /// </para>
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .LogToTrace();
}
