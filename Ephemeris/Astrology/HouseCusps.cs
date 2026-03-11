// Updated: 2026-03-11
namespace Ephemeris.Astrology;

/// <summary>
/// The twelve astrological house cusps, the four Angles (Ascendant, Midheaven,
/// Descendant, Imum Coeli), and the house system used to compute them.
/// All ecliptic longitudes are in degrees [0, 360).
/// </summary>
/// <param name="Ascendant">
/// Ecliptic longitude of the Ascendant — the ecliptic degree crossing the eastern horizon.
/// In quadrant-based systems (Placidus, Porphyry, Koch, Campanus, Regiomontanus) this equals
/// <c>Cusps[0]</c> (house 1 cusp). In sign-based systems (Equal, Whole Signs) the house 1
/// cusp may be defined differently and may not equal the Ascendant. Degrees [0, 360).
/// </param>
/// <param name="Midheaven">
/// Ecliptic longitude of the Midheaven (MC) — the ecliptic degree on the upper meridian.
/// In quadrant-based systems this equals <c>Cusps[9]</c> (house 10 cusp). In Equal houses
/// the house 10 cusp is <c>Ascendant + 270°</c> and will differ from the Midheaven. Degrees [0, 360).
/// </param>
/// <param name="Descendant">
/// Ecliptic longitude of the Descendant — the ecliptic degree crossing the western horizon.
/// Always 180° opposite the Ascendant. In quadrant-based systems this aligns with
/// <c>Cusps[6]</c> (house 7 cusp); sign-based systems may define the house 7 cusp differently.
/// Degrees [0, 360).
/// </param>
/// <param name="ImumCoeli">
/// Ecliptic longitude of the Imum Coeli (IC) — the ecliptic degree on the lower meridian.
/// Always 180° opposite the Midheaven. In quadrant-based systems this aligns with
/// <c>Cusps[3]</c> (house 4 cusp); sign-based systems may define the house 4 cusp differently.
/// Degrees [0, 360).
/// </param>
/// <param name="Cusps">
/// Ecliptic longitudes of the twelve house cusps indexed 0–11 (house 1 through house 12).
/// These are house-system-specific values and may or may not coincide with the four Angles.
/// <list type="bullet">
///   <item><c>Cusps[0]</c>  = House 1 cusp (= Ascendant in quadrant-based systems; sign boundary in Whole Signs)</item>
///   <item><c>Cusps[3]</c>  = House 4 cusp (≈ Imum Coeli in quadrant-based systems)</item>
///   <item><c>Cusps[6]</c>  = House 7 cusp (= Descendant in quadrant-based systems)</item>
///   <item><c>Cusps[9]</c>  = House 10 cusp (= Midheaven in quadrant-based systems; Ascendant + 270° in Equal)</item>
/// </list>
/// </param>
/// <param name="HouseSystem">The house division system used to compute these cusps.</param>
public readonly record struct HouseCusps(
    double Ascendant,
    double Midheaven,
    double Descendant,
    double ImumCoeli,
    IReadOnlyList<double> Cusps,
    HouseSystem HouseSystem);
