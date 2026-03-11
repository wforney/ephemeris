// Updated: 2026-03-11
namespace Ephemeris.Astrology;

/// <summary>
/// Astrological house division systems used to partition the celestial sphere into twelve sectors.
/// </summary>
public enum HouseSystem
{
    /// <summary>
    /// Placidus — time-based semi-arc division; the most widely used modern Western system.
    /// Intermediate cusps are placed where a degree of the ecliptic has completed 1/3 or 2/3
    /// of its diurnal or nocturnal semi-arc.
    /// </summary>
    Placidus,

    /// <summary>
    /// Equal House — each house is exactly 30° wide, starting from the Ascendant.
    /// Simple and consistent at all latitudes.
    /// </summary>
    Equal,

    /// <summary>
    /// Whole Signs — each house occupies one complete zodiac sign, anchored to the sign
    /// that contains the Ascendant. Used in Hellenistic and Vedic (Jyotish) astrology.
    /// </summary>
    WholeSigns,

    /// <summary>
    /// Porphyry — each of the four quadrants (defined by the four Angles) is divided into
    /// three equal arcs of ecliptic longitude.  Attributed to Porphyry of Tyre (~3rd century CE).
    /// </summary>
    Porphyry,

    /// <summary>
    /// Koch (Geburtsort-Häusertafel) — uses the diurnal arc of the Midheaven degree
    /// projected through the birth latitude. Popular in German-speaking countries.
    /// </summary>
    Koch,

    /// <summary>
    /// Campanus — projects the prime vertical circle onto the ecliptic, dividing it into
    /// twelve equal sections. Attributed to Johannes Campanus (~13th century).
    /// </summary>
    Campanus,

    /// <summary>
    /// Regiomontanus — divides the celestial equator into twelve equal sections and projects
    /// house boundaries onto the ecliptic via great circles through the north and south points
    /// of the horizon. Used extensively in medieval astrology.
    /// </summary>
    Regiomontanus,
}
