// Updated: 2026-03-10
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ephemeris.UI.Avalonia.Views;

namespace Ephemeris.UI.Avalonia;

/// <summary>
/// Root Avalonia application class.
/// Responsible for initialising the XAML infrastructure and opening the launcher window.
/// </summary>
public class App : Application
{
    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new HomeWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
