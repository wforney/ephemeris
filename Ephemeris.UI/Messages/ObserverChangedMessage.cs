// Updated: 2026-03-10
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Ephemeris.UI.Messages;

/// <summary>Observer location snapshot carried by <see cref="ObserverChangedMessage"/>.</summary>
/// <param name="Longitude">Observer longitude in degrees (east positive).</param>
/// <param name="Latitude">Observer latitude in degrees (north positive).</param>
public readonly record struct ObserverLocation(double Longitude, double Latitude);

/// <summary>
/// Sent by <see cref="SkyViewModel"/> when the observer's longitude or latitude changes.
/// Recipients can use this to persist the last-used location across form sessions.
/// </summary>
public sealed class ObserverChangedMessage(ObserverLocation value)
    : ValueChangedMessage<ObserverLocation>(value);
