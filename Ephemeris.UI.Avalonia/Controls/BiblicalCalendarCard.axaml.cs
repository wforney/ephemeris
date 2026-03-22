// Updated: 2026-03-22
using Avalonia.Controls;
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Avalonia.Controls;

/// <summary>
/// A compact information card that displays approximate biblical (Hebrew luni-solar)
/// calendar data for the current observer position and simulation time.
/// Intended to be embedded in the Research Workspace sidebar.
/// </summary>
/// <remarks>
/// Usage in a parent view:
/// <code>
///   // In AXAML:
///   &lt;controls:BiblicalCalendarCard x:Name="BiblicalCard" /&gt;
///
///   // In code-behind, whenever the time or location changes:
///   BiblicalCard.Update(biblicalDate);
/// </code>
/// The <see cref="BiblicalCalendarHelper.BiblicalDate"/> is obtained from
/// <see cref="Ephemeris.UI.Services.CelestialResearchService.GetDataAsync"/>.
/// </remarks>
public partial class BiblicalCalendarCard : UserControl
{
    /// <summary>Initialises the control and its child elements.</summary>
    public BiblicalCalendarCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Populates all displayed fields from a <see cref="BiblicalCalendarHelper.BiblicalDate"/>
    /// snapshot. Pass <see langword="null"/> to reset all fields to the placeholder dash (—).
    /// </summary>
    /// <param name="date">The biblical date to display, or <see langword="null"/> to clear.</param>
    public void Update(BiblicalCalendarHelper.BiblicalDate? date)
    {
        if (date is null)
        {
            HebrewYearText.Text = "—";
            MonthText.Text      = "—";
            DayText.Text        = "—";
            SeasonText.Text     = "—";
            SolarSignText.Text  = "—";
            CrescentText.Text   = "—";
            return;
        }

        HebrewYearText.Text = date.Year.ToString();
        MonthText.Text = $"{date.MonthName} ({BiblicalCalendarHelper.Ordinal(date.Month)})";
        DayText.Text       = date.DayOfMonth.ToString();
        SeasonText.Text    = date.Season;
        SolarSignText.Text = $"{date.SolarSign} ({date.SolarSignHebrew})";
        CrescentText.Text  = date.IsNewMoonVisibility ? "Visible" : "Not visible";
    }

    // Ordinal formatting is delegated to BiblicalCalendarHelper.Ordinal(int).
}
