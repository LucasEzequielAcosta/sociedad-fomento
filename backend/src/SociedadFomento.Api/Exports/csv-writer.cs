using System.Text;

namespace SociedadFomento.Api.Exports;

/// <summary>Serializes tabular data into RFC 4180 comma-separated values.</summary>
public static class CsvWriter
{
    /// <summary>Builds CSV content with a header row and escaped fields.</summary>
    public static string Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        StringBuilder builder = new();
        AppendRow(builder, headers);
        foreach (IReadOnlyList<string?> row in rows)
        {
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    /// <summary>Returns the CSV payload as UTF-8 bytes with a BOM for spreadsheet compatibility.</summary>
    public static byte[] ToUtf8Bytes(string csv) => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string?> fields)
    {
        builder.AppendJoin(',', fields.Select(Escape));
        builder.Append("\r\n");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
