// Updated: 2026-03-22
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Models;

/// <summary>
/// Serialisable snapshot of a research session that can be saved to disk and restored.
/// </summary>
/// <remarks>
/// Call <see cref="FromWorkspace"/> to create a snapshot from the current
/// <see cref="WorkspaceViewModel"/>, then <see cref="SaveAsync"/> to write it to a
/// <c>.json</c> file.  <see cref="LoadAsync"/> restores a previously saved session.
/// </remarks>
public sealed class SessionModel
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>User-assigned session name.</summary>
    public string Name { get; set; } = "Unnamed Session";

    /// <summary>Simulated UTC time at the time the session was saved.</summary>
    public DateTime SimTime { get; set; } = DateTime.UtcNow;

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public double Longitude { get; set; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public double Latitude { get; set; }

    /// <summary>Free-form research notes accumulated during the session.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Name of the active scenario, if any.</summary>
    public string? ScenarioName { get; set; }

    // ─────────────────────────────────────────────────────────────────────
    // Factory / persistence
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SessionModel"/> snapshot from the current workspace state.
    /// </summary>
    /// <param name="vm">The workspace view-model to snapshot.</param>
    /// <returns>A new <see cref="SessionModel"/> populated from <paramref name="vm"/>.</returns>
    public static SessionModel FromWorkspace(WorkspaceViewModel vm) => new()
    {
        SimTime      = vm.SimTime,
        Longitude    = vm.Longitude,
        Latitude     = vm.Latitude,
        ScenarioName = vm.ActiveScenarioName,
    };

    /// <summary>
    /// Serialises this session to a JSON file at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Destination file path (typically <c>.json</c>).</param>
    public async Task SaveAsync(string path)
    {
        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, this, s_jsonOptions).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserialises a <see cref="SessionModel"/> from a JSON file at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Source file path.</param>
    /// <returns>The deserialised <see cref="SessionModel"/>.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the file cannot be deserialised as a <see cref="SessionModel"/>.
    /// </exception>
    public static async Task<SessionModel> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var model = await JsonSerializer.DeserializeAsync<SessionModel>(stream, s_jsonOptions)
            .ConfigureAwait(false);
        return model ?? throw new InvalidDataException($"Failed to deserialise session from '{path}'.");
    }
}
