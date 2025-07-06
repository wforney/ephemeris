namespace Ephemeris.Heliology;

/// <summary>
/// Represents the solar coordinates of the Sun in the sky.
/// </summary>
/// <param name="Altitude">The altitude (elevation above horizon) of the Sun in degrees.</param>
/// <param name="Azimuth">The azimuth of the Sun in degrees. (0° = North, 180° = South)</param>
/// <param name="Declination">The declination of the Sun in degrees. (-90 to +90)</param>
/// <param name="RightAscension">The right ascension of the Sun in degrees. (0 - 360)</param>
public readonly record struct SolarCoordinates(
    double Altitude,
    double Azimuth,
    double Declination,
    double RightAscension);
