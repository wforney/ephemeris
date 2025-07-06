using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Ephemeris;

public static class EphemerisExporter
{
    // Save CSV to file
    public static void SaveCsvToFile<T>(IEnumerable<T> data, string filePath)
    {
        string csv = ToCsv(data);
        File.WriteAllText(filePath, csv);
    }

    // Save JSON to file
    public static void SaveJsonToFile<T>(IEnumerable<T> data, string filePath, bool indented = false)
    {
        string json = ToJson(data, indented);
        File.WriteAllText(filePath, json);
    }

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

    public static string ToJson<T>(IEnumerable<T> data, bool indented = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        return JsonSerializer.Serialize(data, options);
    }

    // Load CSV from file into list of EphemerisRecord
    public static List<EphemerisRecord> LoadCsvFromFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var header = lines.First().Split(',');
        var records = new List<EphemerisRecord>();

        foreach (var line in lines.Skip(1))
        {
            var cells = line.Split(',');
            var record = new EphemerisRecord
            {
                TimeUtc = DateTime.Parse(cells[0], null, DateTimeStyles.RoundtripKind),
                Body = cells[1],
                RightAscension = double.Parse(cells[2], CultureInfo.InvariantCulture),
                Declination = double.Parse(cells[3], CultureInfo.InvariantCulture),
                Azimuth = double.Parse(cells[4], CultureInfo.InvariantCulture),
                Altitude = double.Parse(cells[5], CultureInfo.InvariantCulture),
                Illumination = string.IsNullOrWhiteSpace(cells[6]) ? null : double.Parse(cells[6], CultureInfo.InvariantCulture)
            };
            records.Add(record);
        }

        return records;
    }

    // Load JSON from file into list of EphemerisRecord
    public static List<EphemerisRecord> LoadJsonFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<EphemerisRecord>>(json) ?? new List<EphemerisRecord>();
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
