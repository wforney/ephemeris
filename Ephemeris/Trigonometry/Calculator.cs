namespace Ephemeris.Trigonometry;

/// <summary>
/// Provides methods for trigonometric calculations.
/// </summary>
public static class Calculator
{
    private static readonly Dictionary<string, double> _cache = [];
    private static readonly Lock _cacheLock = new();

    /// <summary>
    /// Calculates the arccosine of a value in radians and converts it to degrees.
    /// </summary>
    /// <param name="value">The value to calculate the arccosine for.</param>
    /// <returns>The angle in degrees.</returns>
    public static double Acos(double value) => ToDegrees(Math.Acos(value));

    /// <summary>
    /// Calculates the arcsine of a value in radians and converts it to degrees.
    /// </summary>
    /// <param name="value">The value to calculate the arcsine for.</param>
    /// <returns>The angle in degrees.</returns>
    public static double Asin(double value) => ToDegrees(Math.Asin(value));

    /// <summary>
    /// Calculates the arctangent of a value in radians and converts it to degrees.
    /// </summary>
    /// <param name="v">The value to calculate the arctangent for.</param>
    /// <returns>The angle in degrees.</returns>
    public static double Atan(double v) => ToDegrees(Math.Atan(v));

    /// <summary>
    /// Calculates the angle in degrees from the y-coordinate and x-coordinate using the arctangent function.
    /// </summary>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <returns>The angle in degrees.</returns>
    public static double Atan2(double y, double x) => ToDegrees(Math.Atan2(y, x));

    /// <summary>
    /// Calculates the cosine of an angle given in radians, caching the result for performance.
    /// </summary>
    /// <param name="angles">The angles in radians for which to calculate the sine.</param>
    /// <returns>The sum of the sine of the angles in radians.</returns>
    public static double CachedSin(params double[] angles)
    {
        string key = string.Join(",", angles);
        using (_cacheLock.EnterScope())
        {
            if (_cache.TryGetValue(key, out double result))
            {
                return result;
            }

            result = 0;
            foreach (double a in angles)
            {
                result += Sin(a);
            }

            _cache[key] = result;
            return result;
        }
    }

    /// <summary>
    /// Calculates the cosine of an angle given in degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees for which to calculate the cosine.</param>
    /// <returns>The cosine of the angle in degrees.</returns>
    public static double Cos(double degrees) => Math.Cos(ToRadians(degrees));

    /// <summary>
    /// Normalizes an angle in degrees to the range [0, 360).
    /// </summary>
    /// <param name="degrees">The angle in degrees to normalize.</param>
    /// <returns>The normalized angle in degrees.</returns>
    public static double NormalizeAngle(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0 ? degrees + 360.0 : degrees;
    }

    /// <summary>
    /// Normalizes an angle in radians to the range [0, 2π].
    /// </summary>
    /// <param name="radians">The angle in radians to normalize.</param>
    /// <returns>The normalized angle in radians.</returns>
    public static double NormalizeRadians(double radians)
    {
        radians %= 2.0 * Math.PI;
        return radians < 0 ? radians + (2 * Math.PI) : radians;
    }

    /// <summary>
    /// Calculates the sine of an angle given in degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees for which to calculate the sine.</param>
    /// <returns>The sine of the angle in degrees.</returns>
    public static double Sin(double degrees) => Math.Sin(ToRadians(degrees));

    /// <summary>
    /// Calculates the tangent of an angle given in degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees for which to calculate the tangent.</param>
    /// <returns>The tangent of the angle in degrees.</returns>
    public static double Tan(double degrees) => Math.Tan(ToRadians(degrees));

    /// <summary>
    /// Converts an angle in radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians to convert.</param>
    /// <returns>The angle in degrees.</returns>
    public static double ToDegrees(double radians) => double.RadiansToDegrees(radians);

    /// <summary>
    /// Converts a double value representing an angle in degrees to a formatted string.
    /// </summary>
    /// <param name="x">The input double value representing an angle in degrees.</param>
    /// <returns>A formatted string representing the degrees, minutes, and seconds.</returns>
    public static string ToDegreesString(this double x)
    {
        int degrees = (int)x;
        int minutes = (int)((x - degrees) * 60);
        double seconds = (x - degrees - (minutes / 60.0)) * 3600;
        return $"{degrees}° {minutes}' {seconds:F2}\"";
    }

    /// <summary>
    /// Converts an angle in degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees to convert.</param>
    /// <returns>The converted angle in radians.</returns>
    public static double ToRadians(double degrees) => double.DegreesToRadians(degrees);
}
