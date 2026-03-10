// Updated: 2026-03-10
namespace Ephemeris.Phenomenology;

/// <summary>
/// Represents a single lunar occultation event — the Moon's limb covering and subsequently
/// uncovering a celestial target as seen from a specific observer location.
/// </summary>
/// <param name="Disappearance">
/// UTC time when the target disappears behind the Moon's limb (ingress).
/// <see langword="null"/> if the target is already occulted at the start of the search window.
/// </param>
/// <param name="Reappearance">
/// UTC time when the target reappears from behind the Moon's limb (egress).
/// <see langword="null"/> if the target is still occulted at the end of the search window.
/// </param>
/// <param name="TargetName">Name of the occulted target (star or planet).</param>
public readonly record struct OccultationEvent(DateTime? Disappearance, DateTime? Reappearance, string TargetName);
