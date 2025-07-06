using Ephemeris.Heliology;

namespace Ephemeris.Tests;

public class Tests
{
    [Test]
    public async Task Test()
    {
        var calc = new EphemerisCalculator();
        double jd = calc.JulianDay(2025, 7, 6, 12.0);  // Noon UTC
        double T = calc.JulianCentury(jd);
        double gmst = calc.GMST(jd);
        double trueST = calc.TrueSiderealTime(jd);
        var (psi, eps) = calc.Nutation(T);
        double epsMean = calc.MeanObliquity(T);

        Console.WriteLine($"Julian Day: {jd}");
        Console.WriteLine($"GMST: {gmst:F6}°");
        Console.WriteLine($"True Sidereal Time: {trueST:F6}°");
        Console.WriteLine($"Nutation ΔΨ: {psi:F6}°, Δε: {eps:F6}°");
        Console.WriteLine($"Mean Obliquity: {epsMean:F6}°");
    }
}
