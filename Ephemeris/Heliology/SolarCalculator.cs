using Ephemeris.Chronology;
using Ephemeris.Trigonometry;

namespace Ephemeris.Heliology;

/// <summary>
/// Heliology class for calculating solar events such as sunrise, sunset, and twilight times.
/// </summary>
public static class SolarCalculator
{
    /// <summary>
    /// Calculates the Julian Day (JD) for a given UTC time.
    /// </summary>
    /// <param name="utcTime">The UTC time to convert to Julian Day.</param>
    /// <param name="latitudeDeg">The latitude in degrees.</param>
    /// <param name="longitudeDeg">The longitude in degrees.</param>
    /// <returns>The solar coordinates including right ascension, declination, azimuth, and altitude.</returns>
    public static SolarCoordinates GetSolarCoordinates(DateTime utcTime, double latitudeDeg, double longitudeDeg)
    {
        double jd = JulianDay.GetJulianDay(utcTime);
        //_ = (jd - 2451545.0) / 36525.0;

        // ΔT in seconds (approximate model from Meeus)
        double deltaT = JulianDay.GetDeltaT(utcTime.Year, utcTime.Month);
        double jde = jd + deltaT / 86400.0; // JD with ΔT applied
        double tT = (jde - 2451545.0) / 36525.0;

        // Sun mean longitude (L0), mean anomaly (M), and eccentricity
        double L0 = Trigonometry.Calculator.NormalizeAngle(280.46646 + 36000.76983 * tT + 0.0003032 * tT * tT);
        double M = Trigonometry.Calculator.NormalizeAngle(357.52911 + 35999.05029 * tT - 0.0001537 * tT * tT);
        double eccentricity = 0.016708634 - 0.000042037 * tT - 0.0000001267 * tT * tT;

        // Sun's equation of center
        double C = (1.914602 - 0.004817 * tT - 0.000014 * tT * tT) * Math.Sin(double.DegreesToRadians(M))
                 + (0.019993 - 0.000101 * tT) * Math.Sin(double.DegreesToRadians(2 * M))
                 + 0.000289 * Math.Sin(double.DegreesToRadians(3 * M));

        // Sun's true longitude and apparent longitude
        double trueLongitude = L0 + C;
        double omega = 125.04 - 1934.136 * tT;
        double lambda = trueLongitude - 0.00569 - 0.00478 * Math.Sin(double.DegreesToRadians(omega));
        double lambdaRad = double.DegreesToRadians(lambda);

        // Mean obliquity of the ecliptic
        double epsilon0 = 23.43929111
                        - 0.013004167 * tT
                        - 0.0000001639 * tT * tT
                        + 0.0000005036 * tT * tT * tT;
        double epsilon = epsilon0 + 0.00256 * Math.Cos(double.DegreesToRadians(omega));
        double epsilonRad = double.DegreesToRadians(epsilon);

        // Right ascension and declination
        double alphaRad = Math.Atan2(
            Math.Cos(epsilonRad) * Math.Sin(lambdaRad),
            Math.Cos(lambdaRad));
        double deltaRad = Math.Asin(
            Math.Sin(epsilonRad) * Math.Sin(lambdaRad));

        double alphaDeg = Calculator.NormalizeAngle(double.RadiansToDegrees(alphaRad));
        double deltaDeg = double.RadiansToDegrees(deltaRad);

        // Local Sidereal Time (LST) in degrees
        double lstDeg = SiderealTime.CalculateSiderealTime(jd, longitudeDeg);
        double lstRad = double.DegreesToRadians(lstDeg);

        // Hour angle in radians
        double hourAngleRad = lstRad - alphaRad;
        hourAngleRad = Calculator.NormalizeRadians(hourAngleRad);

        // Observer’s latitude in radians
        double latRad = double.DegreesToRadians(latitudeDeg);

        // Altitude (elevation) and azimuth calculations
        double sinAlt = Math.Sin(deltaRad) * Math.Sin(latRad) + Math.Cos(deltaRad) * Math.Cos(latRad) * Math.Cos(hourAngleRad);
        double altRad = Math.Asin(sinAlt);

        double cosAz = (Math.Sin(deltaRad) - Math.Sin(altRad) * Math.Sin(latRad)) / (Math.Cos(altRad) * Math.Cos(latRad));
        cosAz = Math.Clamp(cosAz, -1.0, 1.0); // Ensure within domain

        double azRad = Math.Acos(cosAz);
        if (Math.Sin(hourAngleRad) > 0)
        {
            azRad = 2 * Math.PI - azRad;
        }

        return new SolarCoordinates(double.RadiansToDegrees(altRad), double.RadiansToDegrees(azRad), deltaDeg, alphaDeg);
    }

    /// <summary>
    /// Formats a time in UT (Universal Time) as a string in "HH:MM" format.
    /// </summary>
    /// <param name="utTime">
    /// The time in UT (Universal Time) as a double value, where the integer part represents hours
    /// </param>
    /// <returns>A string representing the time in "HH:MM" format.</returns>
    public static string HrsMin(double utTime)
    {
        int hours = (int)Math.Floor(utTime);
        int minutes = (int)Math.Round((utTime - hours) * 60.0);

        // Correct possible rounding overflows
        if (minutes >= 60)
        {
            minutes -= 60;
            hours += 1;
        }
        if (hours >= 24)
        {
            hours -= 24;
        }

        return $"{hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Calculates the right ascension (ra) and declination (dec) of the Sun at a given time.
    /// </summary>
    /// <param name="t">The time parameter.</param>
    /// <returns>
    /// A tuple containing the right ascension (ra) and declination (dec) of the Sun at the
    /// specified time.
    /// </returns>
    /// <remarks>
    /// takes t (julin centuries since 32000.0), and empty variables ra and dec, sets ra and dec to
    /// the value of the Sun coordinates at t. positions claimed to be within 1 arc min by
    /// Montenbruck and Pfleger.
    /// </remarks>
    public static (double ra, double dec) MiniSun(double t)
    {
        double p2 = 2 * Math.PI;
        double coseps = 0.91748;
        double sineps = 0.39778;

        double m = p2 * Fraction(0.993133 + 99.997361 * t);
        double dl = 6893d * Math.Sin(m) + 72d * Math.Sin(2 * m);
        double l = p2 * Fraction(0.7859453 + m / p2 + (6191.2 * t + dl) / 1296000);
        double sinL = Math.Sin(l);
        double x = Math.Cos(l);
        double y = coseps * sinL;
        double z = sineps * sinL;
        double rho = Math.Sqrt(1 - z * z);
        var dec = 360d / p2 * Math.Atan(z / rho);
        var ra = 48d / p2 * Math.Atan(y / (x + rho));
        if (ra < 0)
        {
            ra += 24;
        }

        return (ra, dec);
    }

    /// <summary>
    /// Calculates the fractional part of a double value.
    /// </summary>
    /// <param name="value">The input value.</param>
    /// <returns>The fractional part of the value.</returns>
    private static double Fraction(double value)
    {
        return value - Math.Floor(value);
    }

    /// <summary>
    /// Calculates the sine of the solar altitude at a given date.
    /// </summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date (1-12).</param>
    /// <param name="day">The day of the month (1-31).</param>
    /// <returns>The Modified Julian Date (MJD) calculated for the given date.</returns>
    public static double ModifiedJulianDate(int year, int month, int day)
    {
        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + A / 4;

        double mjd = Math.Floor(365.25 * (year + 4716)) +
                     Math.Floor(30.6001 * (month + 1)) +
                     day + B - 1524.5 - 2400000.5;

        return mjd;
    }

    /// <summary>
    /// Calculates the hour angle of the Sun at a given date and time.
    /// </summary>
    /// <param name="ym">The sine of the solar altitude at the previous hour.</param>
    /// <param name="yz">The sine of the solar altitude at the current hour.</param>
    /// <param name="yp">The sine of the solar altitude at the next hour.</param>
    /// <returns>
    /// A tuple containing the number of roots (nz), the first root (z1), the second root (z2), the
    /// x-coordinate of the extremum (xe), and the y-coordinate of the extremum (ye).
    /// </returns>
    /// <remarks>
    /// finds the parabola through the three points(-1, ym), (0,yz), (1, yp) and sets the
    /// coordinates of the max/min(if any) xe, ye the values of x where the parabola crosses
    /// zero(z1, z2) and the nz number of roots(0, 1 or 2) within the interval[-1, 1]
    /// </remarks>
    public static (int nz, double z1, double z2, double xe, double ye) QuadraticInterpolation(
        double ym,
        double yz,
        double yp)
    {
        double z1 = 0;
        double z2 = 0;
        double a = 0.5 * (ym + yp) - yz;
        double b = 0.5 * (yp - ym);
        double c = yz;
        int nz = 0;
        double xe = 0;
        double ye = 0;

        if (a == 0.0)
        {
            if (b == 0.0)
            {
                return (nz, z1, z2, xe, ye);
            }

            double root = -c / b;
            if (Math.Abs(root) <= 1.0)
            {
            }

                return (nz, z1, z2, xe, ye);
        }

        double disc = b * b - 4.0 * a * c;
        if (disc < 0.0)
        {
            return (nz, z1, z2, xe, ye);
        }

        double sqrtDisc = Math.Sqrt(disc);
        double dx1 = (-b + sqrtDisc) / (2.0 * a);
        double dx2 = (-b - sqrtDisc) / (2.0 * a);
        int count = 0;

        if (Math.Abs(dx1) <= 1.0)
        {
            z1 = dx1;
            count++;
        }
        if (Math.Abs(dx2) <= 1.0)
        {
            if (count == 0)
            {
                z1 = dx2;
            }
            else
            {
                if (dx2 < dx1)
                {
                    z2 = z1;
                    z1 = dx2;
                }
                else
                {
                    z2 = dx2;
                }
            }
            count++;
        }

        nz = count;

        xe = -b / (2.0 * a);
        ye = (a * xe + b) * xe + c;

        return (nz, z1, z2, xe, ye);
    }

    /// <summary>
    /// Calculates the sine of the altitude of the Sun at a given date and time.
    /// </summary>
    /// <param name="ddate">
    /// The Modified Julian Date (MJD) at which to calculate the sine of the altitude.
    /// </param>
    /// <param name="hour">
    /// The hour of the day (in decimal format) at which to calculate the sine of the altitude.
    /// </param>
    /// <param name="glong">The geographical longitude in degrees.</param>
    /// <param name="cglat">The cosine of the geographical latitude in radians.</param>
    /// <param name="sglat">The sine of the geographical latitude in radians.</param>
    /// <returns>The sine of the solar altitude.</returns>
    public static double SinAltSun(double ddate, double hour, double glong, double cglat, double sglat)
    {
        const double Rads = Math.PI / 180.0;

        // Julian Day (UTC)
        double jd = ddate + 2400000.5 + hour / 24.0;
        double t = (jd - 2451545.0) / 36525.0;

        // Mean longitude of the Sun (deg)
        double L0 = 280.46646 + 36000.76983 * t + 0.0003032 * t * t;
        L0 = Calculator.NormalizeAngle(L0);

        // Mean anomaly of the Sun (deg)
        double M = 357.52911 + 35999.05029 * t - 0.0001537 * t * t;
        M = Calculator.NormalizeAngle(M);

        // Eccentricity of Earth's orbit
        double eccentricity = 0.016708634 - 0.000042037 * t - 0.0000001267 * t * t;

        // Sun’s equation of center
        double C = (1.914602 - 0.004817 * t - 0.000014 * t * t) * Math.Sin(Rads * M)
                 + (0.019993 - 0.000101 * t) * Math.Sin(Rads * 2 * M)
                 + 0.000289 * Math.Sin(Rads * 3 * M);

        // Sun's true longitude (deg)
        double trueLongitude = L0 + C;

        // Apparent longitude (deg)
        double omega = 125.04 - 1934.136 * t;
        double lambda = trueLongitude - 0.00569 - 0.00478 * Math.Sin(Rads * omega);
        lambda = Calculator.NormalizeAngle(lambda);

        // Obliquity of the ecliptic
        double epsilon0 = 23.43929111
                        - 0.013004167 * t
                        - 0.0000001639 * t * t
                        + 0.0000005036 * t * t * t;
        double epsilon = epsilon0 + 0.00256 * Math.Cos(Rads * omega);

        // Sun's declination (radians)
        double delta = Math.Asin(Math.Sin(Rads * epsilon) * Math.Sin(Rads * lambda));

        // Sun’s right ascension (radians)
        double alpha = Math.Atan2(Math.Cos(Rads * epsilon) * Math.Sin(Rads * lambda), Math.Cos(Rads * lambda));

        // Local sidereal time (deg)
        double T0 = 280.46061837 + 360.98564736629 * (jd - 2451545.0);
        double lst = Calculator.NormalizeAngle(T0 + glong); // degrees

        // Hour angle (radians)
        double H = Rads * (lst - alpha * 180.0 / Math.PI); // convert alpha from radians to degrees
        H = Calculator.NormalizeRadians(H);

        // Return sine of Sun’s altitude
        double sinAlt = sglat * Math.Sin(delta) + cglat * Math.Cos(delta) * Math.Cos(H);
        return sinAlt;
    }

    /// <summary>
    /// Calculates the Modified Julian Date (MJD) for a given date.
    /// </summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date (1-12).</param>
    /// <param name="day">The day of the month (1-31).</param>
    /// <param name="tz">The timezone offset in hours from UTC.</param>
    /// <param name="glong">The geographical longitude in degrees.</param>
    /// <param name="glat">The geographical latitude in degrees.</param>
    /// <param name="eventType">The type of solar event (1 for sunrise, -1 for sunset).</param>
    /// <returns>A formatted string indicating the time of the solar event.</returns>
    public static string SunEvent(int year, int month, int day, double tz, double glong, double glat, int eventType)
    {
        double sglat, cglat, ddate, ym;
        double yz, yp, utrise = 0, utset = 0;
        int above = 0, rise = 0, sett = 0;
        double hour = 1.0;

        const double Rads = 0.0174532925;
        string AlwaysUp = "****";
        string AlwaysDown = "....";
        string NoEvent = "----";
        string outString = "";

        // Set up the sinho array for different twilight types
        double[] sinho = new double[5];
        sinho[1] = Math.Sin(Rads * -0.833);  // Sunset upper limb with refraction
        sinho[2] = Math.Sin(Rads * -6.0);    // Civil twilight
        sinho[3] = Math.Sin(Rads * -12.0);   // Nautical twilight
        sinho[4] = Math.Sin(Rads * -18.0);   // Astronomical twilight

        sglat = Math.Sin(Rads * glat);
        cglat = Math.Cos(Rads * glat);
        ddate = ModifiedJulianDate(year, month, day) - tz / 24.0;

        int j = Math.Abs(eventType);
        ym = SineOfSolarAltitude(ddate, hour - 1.0, glong, cglat, sglat) - sinho[j];
        if (ym > 0.0)
        {
            above = 1;
        }

        // Main loop to find rise/set times
        while (hour < 25 && (sett == 0 || rise == 0))
        {
            yz = SineOfSolarAltitude(ddate, hour, glong, cglat, sglat) - sinho[j];
            yp = SineOfSolarAltitude(ddate, hour + 1.0, glong, cglat, sglat) - sinho[j];
            QuadraticInterpolation(ym, yz, yp, out int nz, out double z1, out double z2, out double _, out double ye);

            if (nz == 1)
            {
                if (ym < 0.0)
                {
                    utrise = hour + z1;
                    rise = 1;
                }
                else
                {
                    utset = hour + z1;
                    sett = 1;
                }
            }

            if (nz == 2)
            {
                if (ye < 0.0)
                {
                    utrise = hour + z2;
                    utset = hour + z1;
                }
                else
                {
                    utrise = hour + z1;
                    utset = hour + z2;
                }
                rise = 1;
                sett = 1;
            }

            ym = yp;
            hour += 2.0;
        }

        // Determine output based on event type and flags
        if (rise == 1 || sett == 1)
        {
            if (rise == 1)
            {
                if (eventType > 0)
                {
                    outString = HrsMin(utrise);
                }
            }
            else
            {
                if (eventType > 0)
                {
                    outString = NoEvent;
                }
            }

            if (sett == 1)
            {
                if (eventType < 0)
                {
                    outString = HrsMin(utset);
                }
            }
            else
            {
                if (eventType < 0)
                {
                    outString = NoEvent;
                }
            }
        }
        else
        {
            outString = above == 1 ? AlwaysUp : AlwaysDown;
        }

        return outString;
    }
}
