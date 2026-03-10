// Updated: 2026-03-10
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Ephemeris.UI.Messages;

/// <summary>
/// Sent by <see cref="SkyViewModel"/> whenever the simulation time advances or is reset.
/// Recipients that are open alongside the sky view (or want to resume at the last-used
/// time on next launch) should handle this message.
/// </summary>
public sealed class SimTimeChangedMessage(DateTime value) : ValueChangedMessage<DateTime>(value);
