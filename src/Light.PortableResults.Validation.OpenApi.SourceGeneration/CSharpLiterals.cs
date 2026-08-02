using System.Globalization;
using System.Text;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// Renders C# string literals and escaped characters with deterministic escaping for generated source.
/// </summary>
internal static class CSharpLiterals
{
    public static string ToStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            builder.Append(EscapeChar(c));
        }

        return builder.Append('"').ToString();
    }

    public static string EscapeChar(char value) =>
        value switch
        {
            '\\' => @"\\",
            '"' => "\\\"",
            '\'' => "\\'",
            '\0' => "\\0",
            '\a' => "\\a",
            '\b' => "\\b",
            '\f' => "\\f",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\v' => "\\v",
            _ => char.IsControl(value) ?
                "\\u" + ((int) value).ToString("x4", CultureInfo.InvariantCulture) :
                value.ToString()
        };
}
