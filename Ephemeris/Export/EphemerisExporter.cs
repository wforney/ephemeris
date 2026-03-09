using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Ephemeris.Export;

/// <summary>
/// Provides serialization and deserialization of ephemeris data to/from CSV and JSON formats.
/// </summary>
public static class EphemerisExporter
{
    /// <summary>
    /// Saves a collection of data as CSV to a file.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="data">The collection of items to serialize.</param>
    /// <param name="filePath">The file path where CSV will be written.</param>
    public static void SaveCsvToFile<T>(IEnumerable<T> data, string filePath)
    {
        string csv = ToCsv(data);
        File.WriteAllText(filePath, csv);
    }

    /// <summary>
    /// Saves a collection of data as JSON to a file.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="data">The collection of items to serialize.</param>
    /// <param name="filePath">The file path where JSON will be written.</param>
    /// <param name="indented">If true, the JSON output will be formatted with indentation; otherwise, it will be compact.</param>
    public static void SaveJsonToFile<T>(IEnumerable<T> data, string filePath, bool indented = false)
    {
        string json = ToJson(data, indented);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Converts a collection of items to a CSV string.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="data">The collection of items to serialize.</param>
    /// <returns>A CSV-formatted string with headers and data rows.</returns>
    public static string ToCsv<T>(IEnumerable<T> data)
    {
        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();
        var sb = new StringBuilder();

        // Header
        _ = sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Rows
        foreach (T? item in data)
        {
            IEnumerable<string> values = properties.Select(p => FormatValue(p.GetValue(item)));
            _ = sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a collection of items to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="data">The collection of items to serialize.</param>
    /// <param name="indented">If true, the JSON output will be formatted with indentation; otherwise, it will be compact.</param>
    /// <returns>A JSON-formatted string.</returns>
    public static string ToJson<T>(IEnumerable<T> data, bool indented = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Loads ephemeris records from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>A list of parsed EphemerisRecord objects.</returns>
    public static List<EphemerisRecord> LoadCsvFromFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var header = lines.First().Split(',');
        List<EphemerisRecord> records = [];

        foreach (var line in lines.Skip(1))
        {
            var cells = line.Split(',');
            var record = new EphemerisRecord(
                TimeUtc: DateTime.Parse(cells[0], null, DateTimeStyles.RoundtripKind),
                Body: cells[1],
                RightAscension: double.Parse(cells[2], CultureInfo.InvariantCulture),
                Declination: double.Parse(cells[3], CultureInfo.InvariantCulture),
                Azimuth: double.Parse(cells[4], CultureInfo.InvariantCulture),
                Altitude: double.Parse(cells[5], CultureInfo.InvariantCulture),
                Illumination: string.IsNullOrWhiteSpace(cells[6]) ? null : double.Parse(cells[6], CultureInfo.InvariantCulture));
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Loads ephemeris records from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A list of parsed EphemerisRecord objects.</returns>
    public static List<EphemerisRecord> LoadJsonFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<EphemerisRecord>>(json) ?? [];
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            double d => d.ToString("G17", CultureInfo.InvariantCulture),
            float f => f.ToString("G9", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }
}
