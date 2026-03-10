// Updated: 2026-03-10
namespace Ephemeris.UI;

internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var launcher = new LauncherForm();
        Application.Run(launcher);
    }
}

