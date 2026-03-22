// Updated: 2026-03-22
using System.Text.Json;

namespace Ephemeris.UI.Models;

/// <summary>
/// Persists a named research session: observer location, simulation time, optional
/// scenario, and free-form notes.  Sessions are serialised as UTF-8 JSON.
/// </summary>
public class SessionModel
{
    /// <summary>Human-readable session name shown in the session list.</summary>
    public string Name { get; set; } = "Untitled Session";

    /// <summary>UTC instant at which this session was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>The simulated UTC time that was active when the session was saved.</summary>
    public DateTime SimTime { get; set; }

    /// <summary>Observer longitude in degrees (East positive).</summary>
    public double Longitude { get; set; }

    /// <summary>Observer latitude in degrees (North positive).</summary>
    public double Latitude { get; set; }

    /// <summary>Optional free-form research notes attached to this session.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Name of the active <see cref="ScenarioModel"/> preset, or <see langword="null"/>
    /// if no preset is loaded.
    /// </summary>
    public string? ScenarioName { get; set; }

    /// <summary>
    /// Creates a new <see cref="SessionModel"/> populated from the current
    /// workspace view-model state.
    /// </summary>
    /// <param name="vm">The workspace view-model to snapshot.</param>
    /// <returns>A new <see cref="SessionModel"/> reflecting the current workspace state.</returns>
    public static SessionModel FromWorkspace(ViewModels.WorkspaceViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        return new SessionModel
        {
            Name         = vm.ActiveScenarioName ?? "Untitled Session",
            CreatedUtc   = DateTime.UtcNow,
            SimTime      = vm.SimTime,
            Longitude    = vm.Longitude,
            Latitude     = vm.Latitude,
            ScenarioName = vm.ActiveScenarioName,
        };
    }

    /// <summary>
    /// Deserialises a <see cref="SessionModel"/> from a JSON file.
    /// </summary>
    /// <param name="filePath">Absolute path to the JSON session file.</param>
    /// <returns>
    /// The deserialised <see cref="SessionModel"/>, or <see langword="null"/> if
    /// the file does not exist or cannot be parsed.
    /// </returns>
    public static async Task<SessionModel?> LoadAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            return null;

        await using FileStream stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<SessionModel>(stream).ConfigureAwait(false);
    }

    /// <summary>
    /// Serialises this session to a UTF-8 JSON file at <paramref name="filePath"/>.
    /// The file is created if it does not exist and overwritten if it does.
    /// </summary>
    /// <param name="filePath">Absolute path at which to write the session file.</param>
    public async Task SaveAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using FileStream stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, this, new JsonSerializerOptions { WriteIndented = true })
                            .ConfigureAwait(false);
    }

    /// <summary>
    /// Serialises this session as UTF-8 JSON to an already-opened writable stream.
    /// </summary>
    /// <param name="stream">A writable stream to write the JSON to.</param>
    public async Task SaveAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.SetLength(0);
        await JsonSerializer.SerializeAsync(stream, this, new JsonSerializerOptions { WriteIndented = true })
                            .ConfigureAwait(false);
    }
}
