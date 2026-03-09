namespace Ephemeris;

/// <summary>
/// Represents a record of ephemeris data for a celestial body at a specific time.
/// </summary>
/// <param name="TimeUtc">The time of the observation in UTC.</param>
/// <param name="Body">The name of the celestial body.</param>
/// <param name="RightAscension">Right ascension in degrees [0, 360).</param>
/// <param name="Declination">Declination in degrees [−90, 90].</param>
/// <param name="Azimuth">Azimuth in degrees [0, 360), measured from North clockwise.</param>
/// <param name="Altitude">Altitude in degrees [−90, 90], positive = above horizon.</param>
/// <param name="Illumination">Illumination fraction [0, 1] for the Moon; <see langword="null"/> for other bodies.</param>
/// <param name="Distance">Distance from Earth. AU for Sun and planets; km for Moon. <see langword="null"/> if not computed.</param>
/// <param name="AngularDiameter">Apparent angular diameter in arcseconds. <see langword="null"/> if not computed.</param>
/// <param name="Magnitude">Apparent visual magnitude (V-band). <see langword="null"/> if not computed.</param>
public readonly record struct EphemerisRecord(
    DateTime TimeUtc,
    string Body,
    double RightAscension,
    double Declination,
    double Azimuth,
    double Altitude,
    double? Illumination,
    double? Distance = null,
    double? AngularDiameter = null,
    double? Magnitude = null);
