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
