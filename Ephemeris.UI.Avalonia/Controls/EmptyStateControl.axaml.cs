// Updated: 2026-03-22
using Avalonia;
using Avalonia.Controls;

namespace Ephemeris.UI.Avalonia.Controls;

/// <summary>
/// A reusable empty-state placeholder that displays a large icon, a title, and a subtitle
/// when a panel has no content to show yet.
/// </summary>
/// <remarks>
/// Bind <see cref="Icon"/>, <see cref="Title"/>, and <see cref="Subtitle"/> in XAML or
/// code-behind. Toggle visibility with a standard <c>IsVisible</c> binding on the host panel.
/// </remarks>
public partial class EmptyStateControl : UserControl
{
    /// <summary>Avalonia property for the large emoji/icon string.</summary>
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<EmptyStateControl, string>(nameof(Icon), "🌌");

    /// <summary>Avalonia property for the bold title text.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<EmptyStateControl, string>(nameof(Title), string.Empty);

    /// <summary>Avalonia property for the smaller explanatory subtitle.</summary>
    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<EmptyStateControl, string>(nameof(Subtitle), string.Empty);

    /// <summary>Gets or sets the large emoji or icon character displayed above the title.</summary>
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the bold title line.</summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the smaller explanatory subtitle beneath the title.</summary>
    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Initialises the control and wires property-change callbacks to the named TextBlocks.</summary>
    public EmptyStateControl()
    {
        InitializeComponent();
        UpdateTexts();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty
            || change.Property == TitleProperty
            || change.Property == SubtitleProperty)
        {
            UpdateTexts();
        }
    }

    private void UpdateTexts()
    {
        if (IconText is not null)    IconText.Text    = Icon;
        if (TitleText is not null)   TitleText.Text   = Title;
        if (SubtitleText is not null) SubtitleText.Text = Subtitle;
    }
}
