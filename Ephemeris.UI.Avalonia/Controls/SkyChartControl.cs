// Updated: 2026-03-22
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Ephemeris;
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Stellarography;
using Ephemeris.UI;

namespace Ephemeris.UI.Avalonia.Controls;

/// <summary>
/// 2D azimuthal sky chart — renders a planisphere (all-sky view) using Avalonia's
/// built-in 2D drawing API (Skia). Stars, Sun, Moon, and the five naked-eye planets
/// are plotted using their real-time azimuth/altitude for the observer location and
/// simulation time held in the bound <see cref="SkyViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Projection:</b> azimuthal equidistant centred on the zenith.  The horizon is the
/// outer circle; altitude 60° is one-third of the radius from centre; altitude 30° is
/// two-thirds out.  North is at the top; East is to the <em>left</em> (sky-chart
/// convention when looking up at the sky).
/// </para>
/// <para>
/// Stars brighter than magnitude 5.5 are rendered as discs whose radius and colour
/// reflect their visual brightness and Harvard spectral class.  Named stars at or
/// brighter than magnitude 2.0 carry a text label.
/// </para>
/// <para>
/// Bodies below the horizon are not drawn inside the chart; a small indicator symbol
/// is drawn just outside the horizon ring to show the direction they will rise from.
/// </para>
/// </remarks>
public sealed class SkyChartControl : Control
{
    // ── Planet table ──────────────────────────────────────────────────────
    private static readonly (string Name, byte R, byte G, byte B, double Radius)[] s_planets =
    [
        ("Mercury", 180, 180, 180, 3.5),
        ("Venus",   255, 245, 180, 5.0),
        ("Mars",    255,  90,  50, 4.5),
        ("Jupiter", 230, 210, 155, 6.0),
        ("Saturn",  220, 210, 130, 5.5),
    ];

    // ── View-model ────────────────────────────────────────────────────────
    private readonly SkyViewModel _vm;
    private IReadOnlyList<FixedStar> _stars = [];

    // ─────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Initialises the sky chart bound to the provided view-model.</summary>
    /// <param name="vm">
    /// Shared observable view-model supplying observer longitude, latitude,
    /// and simulated UTC time.
    /// </param>
    public SkyChartControl(SkyViewModel vm)
    {
        _vm = vm;
        _vm.PropertyChanged += OnViewModelChanged;
        _stars = StarCatalog.LoadBuiltIn();
        ClipToBounds = true;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) =>
        Dispatcher.UIThread.Post(InvalidateVisual);

    // ─────────────────────────────────────────────────────────────────────
    // Rendering
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        double cx     = w / 2;
        double cy     = h / 2;
        double radius = Math.Min(cx, cy) - 32;
        if (radius <= 10) return;

        // Fill the control background
        context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));

        var skyRect    = new Rect(cx - radius, cy - radius, radius * 2, radius * 2);
        var clipRegion = new RoundedRect(skyRect, radius);

        // Everything inside the horizon circle is clipped
        using (context.PushClip(clipRegion))
        {
            DrawSkyBackground(context, skyRect);

            double jd = TimeZoneUtils.ToJulianDay(_vm.SimTime);
            DrawStars(context, jd, cx, cy, radius);
            DrawSunAndMoon(context, cx, cy, radius);
            DrawPlanets(context, cx, cy, radius);
        }

        // Grid lines and labels sit on top of (and outside) the sky disc
        DrawGrid(context, cx, cy, radius);
        DrawBelowHorizonIndicators(context, cx, cy, radius);
    }

    // ── Sky gradient background ───────────────────────────────────────────

    private void DrawSkyBackground(DrawingContext dc, Rect skyRect)
    {
        // Colour the sky based on current Sun altitude (day/dusk/night)
        int yr = _vm.SimTime.Year;
        int mo = _vm.SimTime.Month;
        int dy = _vm.SimTime.Day;
        double hr = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        double sunAlt;
        try
        {
            var sun = EphemerisCalculator.GetSunPosition(yr, mo, dy, hr, _vm.Longitude, _vm.Latitude);
            sunAlt = sun.Altitude;
        }
        catch
        {
            sunAlt = -20;
        }

        Color innerColor, outerColor;

        if (sunAlt > 15)
        {
            // Daytime – blue sky
            innerColor = Color.FromRgb(80, 130, 210);
            outerColor = Color.FromRgb(50, 100, 180);
        }
        else if (sunAlt > -6)
        {
            // Civil twilight – blue/orange gradient sky
            double t = (sunAlt + 6) / 21.0;
            innerColor = BlendColor(Color.FromRgb(20, 30, 80), Color.FromRgb(80, 130, 210), t);
            outerColor = BlendColor(Color.FromRgb(10, 20, 60), Color.FromRgb(50, 100, 180), t);
        }
        else
        {
            // Night – deep navy
            innerColor = Color.FromRgb(4, 9, 28);
            outerColor = Color.FromRgb(8, 18, 50);
        }

        var grad = new RadialGradientBrush
        {
            Center         = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX        = new RelativeScalar(0.5, RelativeUnit.Relative),
            RadiusY        = new RelativeScalar(0.5, RelativeUnit.Relative),
            GradientStops  =
            {
                new GradientStop(innerColor, 0.0),
                new GradientStop(outerColor, 1.0),
            }
        };
        dc.DrawRectangle(grad, null, skyRect);
    }

    // ── Stars ─────────────────────────────────────────────────────────────

    private void DrawStars(DrawingContext dc, double jd, double cx, double cy, double radius)
    {
        foreach (var star in _stars)
        {
            if (star.Magnitude > 5.5) continue;

            var eq = star.AtEpoch(jd);
            var hz = ObserverGeometry.EquatorialToHorizontal(
                eq.RightAscension, eq.Declination, jd, _vm.Longitude, _vm.Latitude);
            if (hz.Altitude < 0) continue;

            var (px, py) = Project(hz.Azimuth, hz.Altitude, cx, cy, radius);
            double dotR  = Math.Clamp(3.5 - star.Magnitude * 0.5, 0.6, 3.5);
            var (r, g, b) = SpectralColor(star.SpectralType, (float)star.Magnitude);
            byte alpha = (byte)Math.Clamp(230 - (int)(star.Magnitude * 22), 70, 230);

            dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(alpha,
                    (byte)(r * 255), (byte)(g * 255), (byte)(b * 255))),
                null,
                new Point(px, py), dotR, dotR);

            if (star.Magnitude <= 2.0 && !string.IsNullOrEmpty(star.CommonName))
                DrawLabel(dc, star.CommonName, px + 5, py - 4,
                    Color.FromArgb(150, 200, 210, 235), 9.5);
        }
    }

    // ── Sun and Moon ──────────────────────────────────────────────────────

    private void DrawSunAndMoon(DrawingContext dc, double cx, double cy, double radius)
    {
        int yr    = _vm.SimTime.Year;
        int mo    = _vm.SimTime.Month;
        int dy    = _vm.SimTime.Day;
        double hr = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        try
        {
            var sun = EphemerisCalculator.GetSunPosition(yr, mo, dy, hr, _vm.Longitude, _vm.Latitude);
            if (sun.Altitude >= 0)
            {
                var (px, py) = Project(sun.Azimuth, sun.Altitude, cx, cy, radius);
                // Soft glow halo
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(50, 255, 220, 100)),
                    null, new Point(px, py), 11, 11);
                // Sun disc
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromRgb(255, 240, 100)),
                    null, new Point(px, py), 6, 6);
                DrawLabel(dc, "Sun", px + 8, py - 5,
                    Color.FromArgb(230, 255, 240, 130), 11);
            }
        }
        catch { /* skip */ }

        try
        {
            var moon = EphemerisCalculator.GetMoonPosition(yr, mo, dy, hr, _vm.Longitude, _vm.Latitude);
            if (moon.Altitude >= 0)
            {
                float illum = (float)(moon.Illumination ?? 0.5);
                byte  lum   = (byte)(140 + (int)(illum * 115));
                var (px, py) = Project(moon.Azimuth, moon.Altitude, cx, cy, radius);
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromRgb(lum, lum, (byte)Math.Min(255, lum + 15))),
                    null, new Point(px, py), 5, 5);
                DrawLabel(dc, "Moon", px + 7, py - 5,
                    Color.FromArgb(210, 220, 225, 245), 11);
            }
        }
        catch { /* skip */ }
    }

    // ── Planets ───────────────────────────────────────────────────────────

    private void DrawPlanets(DrawingContext dc, double cx, double cy, double radius)
    {
        foreach (var (name, pr, pg, pb, sz) in s_planets)
        {
            try
            {
                var obs = EphemerisCalculator.GetPlanetPosition(
                    name, _vm.SimTime, TimeZoneInfo.Utc.Id, _vm.Longitude, _vm.Latitude);
                if (obs.Altitude >= 0)
                {
                    var (px, py) = Project(obs.Azimuth, obs.Altitude, cx, cy, radius);
                    dc.DrawEllipse(
                        new SolidColorBrush(Color.FromRgb(pr, pg, pb)),
                        null, new Point(px, py), sz, sz);
                    DrawLabel(dc, name, px + sz + 3, py - 5,
                        Color.FromArgb(200, pr, pg, pb), 10);
                }
            }
            catch { /* skip */ }
        }
    }

    // ── Grid and cardinal directions ──────────────────────────────────────

    private static void DrawGrid(DrawingContext dc, double cx, double cy, double radius)
    {
        var horizonPen = new Pen(new SolidColorBrush(Color.FromArgb(190, 100, 145, 210)), 1.5);
        var altRingPen = new Pen(new SolidColorBrush(Color.FromArgb(45,  80, 115, 175)), 1.0);
        var spokePen   = new Pen(new SolidColorBrush(Color.FromArgb(35, 100, 135, 200)), 0.8);

        // Altitude rings at 30° and 60°
        foreach (int altDeg in new[] { 30, 60 })
        {
            double r = (90.0 - altDeg) / 90.0 * radius;
            dc.DrawEllipse(null, altRingPen, new Point(cx, cy), r, r);
        }

        // Azimuth spokes every 45°
        for (int az = 0; az < 360; az += 45)
        {
            var (ex, ey) = Project(az, 0.0, cx, cy, radius);
            dc.DrawLine(spokePen, new Point(cx, cy), new Point(ex, ey));
        }

        // Horizon ring (drawn last so it's on top of spokes)
        dc.DrawEllipse(null, horizonPen, new Point(cx, cy), radius, radius);

        // Cardinal labels
        DrawCardinalLabel(dc, "N",   0,   cx, cy, radius);
        DrawCardinalLabel(dc, "S", 180,   cx, cy, radius);
        DrawCardinalLabel(dc, "E",  90,   cx, cy, radius);
        DrawCardinalLabel(dc, "W", 270,   cx, cy, radius);

        // Altitude labels
        DrawAltitudeLabel(dc, "30°", 30, cx, cy, radius);
        DrawAltitudeLabel(dc, "60°", 60, cx, cy, radius);
    }

    // ── Below-horizon indicators ──────────────────────────────────────────

    private void DrawBelowHorizonIndicators(DrawingContext dc, double cx, double cy, double radius)
    {
        int yr    = _vm.SimTime.Year;
        int mo    = _vm.SimTime.Month;
        int dy    = _vm.SimTime.Day;
        double hr = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        try
        {
            var sun = EphemerisCalculator.GetSunPosition(yr, mo, dy, hr, _vm.Longitude, _vm.Latitude);
            if (sun.Altitude < 0)
                DrawEdgeIndicator(dc, "☀", sun.Azimuth, cx, cy, radius,
                    Color.FromArgb(120, 255, 200, 50));
        }
        catch { /* skip */ }

        try
        {
            var moon = EphemerisCalculator.GetMoonPosition(yr, mo, dy, hr, _vm.Longitude, _vm.Latitude);
            if (moon.Altitude < 0)
                DrawEdgeIndicator(dc, "☽", moon.Azimuth, cx, cy, radius,
                    Color.FromArgb(100, 200, 215, 245));
        }
        catch { /* skip */ }

        foreach (var (name, pr, pg, pb, _) in s_planets)
        {
            try
            {
                var obs = EphemerisCalculator.GetPlanetPosition(
                    name, _vm.SimTime, TimeZoneInfo.Utc.Id, _vm.Longitude, _vm.Latitude);
                if (obs.Altitude < 0)
                    DrawEdgeIndicator(dc, "·", obs.Azimuth, cx, cy, radius,
                        Color.FromArgb(70, pr, pg, pb));
            }
            catch { /* skip */ }
        }
    }

    // ── Drawing helpers ───────────────────────────────────────────────────

    private static void DrawCardinalLabel(DrawingContext dc, string text, double azDeg,
        double cx, double cy, double radius)
    {
        double azRad  = double.DegreesToRadians(azDeg);
        double offset = 18;
        double px     = cx - (radius + offset) * Math.Sin(azRad);
        double py     = cy - (radius + offset) * Math.Cos(azRad);
        DrawLabel(dc, text, px - 6, py - 8, Color.FromArgb(210, 150, 185, 255), 13);
    }

    private static void DrawAltitudeLabel(DrawingContext dc, string text, double altDeg,
        double cx, double cy, double radius)
    {
        double r = (90.0 - altDeg) / 90.0 * radius;
        DrawLabel(dc, text, cx + 3, cy - r - 11, Color.FromArgb(85, 110, 148, 205), 9);
    }

    private static void DrawEdgeIndicator(DrawingContext dc, string symbol, double azDeg,
        double cx, double cy, double radius, Color color)
    {
        double azRad = double.DegreesToRadians(azDeg);
        double px    = cx - (radius + 18) * Math.Sin(azRad);
        double py    = cy - (radius + 18) * Math.Cos(azRad);
        DrawLabel(dc, symbol, px - 7, py - 8, color, 13);
    }

    private static void DrawLabel(DrawingContext dc, string text, double x, double y,
        Color color, double fontSize)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Arial, sans-serif"),
            fontSize,
            new SolidColorBrush(color));
        dc.DrawText(ft, new Point(x, y));
    }

    // ── Projection ────────────────────────────────────────────────────────

    /// <summary>
    /// Azimuthal equidistant projection: zenith (alt 90°) → centre,
    /// horizon (alt 0°) → outer circle.
    /// East is to the left (sky-map convention when looking up at the sky).
    /// </summary>
    /// <param name="azDeg">Azimuth in degrees (0 = N, 90 = E, 180 = S, 270 = W).</param>
    /// <param name="altDeg">Altitude in degrees above horizon.</param>
    /// <param name="cx">Centre X of the chart disc.</param>
    /// <param name="cy">Centre Y of the chart disc.</param>
    /// <param name="radius">Radius of the horizon circle in pixels.</param>
    /// <returns>Screen coordinates of the projected point.</returns>
    private static (double X, double Y) Project(
        double azDeg, double altDeg, double cx, double cy, double radius)
    {
        double r     = (90.0 - altDeg) / 90.0 * radius;
        double azRad = double.DegreesToRadians(azDeg);
        return (cx - r * Math.Sin(azRad), cy - r * Math.Cos(azRad));
    }

    // ── Colour helpers ────────────────────────────────────────────────────

    /// <summary>Returns an approximate RGB colour tuple for a star from its Harvard spectral class.</summary>
    private static (float R, float G, float B) SpectralColor(string spectralType, float magnitude)
    {
        char cls = spectralType.Length > 0 ? spectralType[0] : 'G';
        float dim = Math.Clamp(1f - (magnitude - 1f) / 8f, 0.3f, 1f);
        return cls switch
        {
            'O' => (0.60f * dim, 0.70f * dim, 1.00f * dim),
            'B' => (0.70f * dim, 0.80f * dim, 1.00f * dim),
            'A' => (0.90f * dim, 0.90f * dim, 1.00f * dim),
            'F' => (1.00f * dim, 1.00f * dim, 0.90f * dim),
            'G' => (1.00f * dim, 0.95f * dim, 0.70f * dim),
            'K' => (1.00f * dim, 0.70f * dim, 0.40f * dim),
            'M' => (1.00f * dim, 0.40f * dim, 0.20f * dim),
            _   => (0.90f * dim, 0.90f * dim, 0.90f * dim),
        };
    }

    private static Color BlendColor(Color a, Color b, double t)
    {
        double s = 1.0 - t;
        return Color.FromRgb(
            (byte)(a.R * s + b.R * t),
            (byte)(a.G * s + b.G * t),
            (byte)(a.B * s + b.B * t));
    }
}
