using System;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// Identifies the framework value represented by a reconstructed validation metadata boundary.
/// </summary>
public enum ReconstructedMetadataValueKind
{
    DateTime = 0,
    DateTimeOffset = 1,
    TimeSpan = 2,
    DateOnly = 3,
    TimeOnly = 4,
    Guid = 5,
    Uri = 6
}

/// <summary>
/// Carries a reconstructed validation metadata boundary as a deterministic structural payload.
/// </summary>
public sealed class ReconstructedMetadataValue : IEquatable<ReconstructedMetadataValue>
{
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";
    private const string DateTimeOffsetFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz";
    private const string DateOnlyFormat = "yyyy-MM-dd";
    private const string TimeOnlyFormat = "HH:mm:ss.FFFFFFF";

    private ReconstructedMetadataValue(
        ReconstructedMetadataValueKind kind,
        long primaryValue,
        long secondaryValue,
        string? text
    )
    {
        Kind = kind;
        PrimaryValue = primaryValue;
        SecondaryValue = secondaryValue;
        Text = text;
    }

    /// <summary>
    /// Gets the represented framework value kind.
    /// </summary>
    public ReconstructedMetadataValueKind Kind { get; }

    /// <summary>
    /// Gets the primary integer payload.
    /// </summary>
    public long PrimaryValue { get; }

    /// <summary>
    /// Gets the secondary integer payload.
    /// </summary>
    public long SecondaryValue { get; }

    /// <summary>
    /// Gets the ordinal text payload, when the represented kind uses one.
    /// </summary>
    public string? Text { get; }

    /// <inheritdoc />
    public bool Equals(ReconstructedMetadataValue? other) =>
        other is not null &&
        Kind == other.Kind &&
        PrimaryValue == other.PrimaryValue &&
        SecondaryValue == other.SecondaryValue &&
        string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <summary>
    /// Creates a structural value from a deterministic <see cref="System.DateTime" />.
    /// </summary>
    public static ReconstructedMetadataValue FromDateTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException(
                "Local DateTime values cannot be reconstructed deterministically.",
                nameof(value)
            );
        }

        return new ReconstructedMetadataValue(
            ReconstructedMetadataValueKind.DateTime,
            value.Ticks,
            (long) value.Kind,
            text: null
        );
    }

    /// <summary>
    /// Creates a structural value from a <see cref="System.DateTimeOffset" />.
    /// </summary>
    public static ReconstructedMetadataValue FromDateTimeOffset(DateTimeOffset value) =>
        new (
            ReconstructedMetadataValueKind.DateTimeOffset,
            value.Ticks,
            value.Offset.Ticks,
            text: null
        );

    /// <summary>
    /// Creates a structural value from a <see cref="System.TimeSpan" />.
    /// </summary>
    public static ReconstructedMetadataValue FromTimeSpan(TimeSpan value) =>
        new (ReconstructedMetadataValueKind.TimeSpan, value.Ticks, secondaryValue: 0L, text: null);

    /// <summary>
    /// Creates a structural date-only value from its day number.
    /// </summary>
    public static ReconstructedMetadataValue FromDateOnlyDayNumber(int dayNumber)
    {
        if (dayNumber < 0 || dayNumber > 3_652_058)
        {
            throw new ArgumentOutOfRangeException(nameof(dayNumber));
        }

        return new ReconstructedMetadataValue(
            ReconstructedMetadataValueKind.DateOnly,
            dayNumber,
            secondaryValue: 0L,
            text: null
        );
    }

    /// <summary>
    /// Creates a structural time-only value from its tick count.
    /// </summary>
    public static ReconstructedMetadataValue FromTimeOnlyTicks(long ticks)
    {
        if (ticks < 0L || ticks >= TimeSpan.TicksPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        return new ReconstructedMetadataValue(
            ReconstructedMetadataValueKind.TimeOnly,
            ticks,
            secondaryValue: 0L,
            text: null
        );
    }

    /// <summary>
    /// Creates a structural value from a <see cref="System.Guid" />.
    /// </summary>
    public static ReconstructedMetadataValue FromGuid(Guid value) =>
        new (
            ReconstructedMetadataValueKind.Guid,
            primaryValue: 0L,
            secondaryValue: 0L,
            value.ToString("D", CultureInfo.InvariantCulture)
        );

    /// <summary>
    /// Creates a structural URI value from its original text.
    /// </summary>
    public static ReconstructedMetadataValue FromUriOriginalString(string originalString)
    {
        if (originalString is null)
        {
            throw new ArgumentNullException(nameof(originalString));
        }

        return new ReconstructedMetadataValue(
            ReconstructedMetadataValueKind.Uri,
            primaryValue: 0L,
            secondaryValue: 0L,
            originalString
        );
    }

    /// <summary>
    /// Renders the canonical metadata text used in generated example messages.
    /// </summary>
    public string ToCanonicalString() =>
        Kind switch
        {
            ReconstructedMetadataValueKind.DateTime =>
                new DateTime(PrimaryValue, (DateTimeKind) SecondaryValue).ToString(
                    DateTimeFormat,
                    CultureInfo.InvariantCulture
                ),
            ReconstructedMetadataValueKind.DateTimeOffset =>
                new DateTimeOffset(PrimaryValue, TimeSpan.FromTicks(SecondaryValue)).ToString(
                    DateTimeOffsetFormat,
                    CultureInfo.InvariantCulture
                ),
            ReconstructedMetadataValueKind.TimeSpan => XmlConvert.ToString(TimeSpan.FromTicks(PrimaryValue)),
            ReconstructedMetadataValueKind.DateOnly =>
                new DateTime(checked(PrimaryValue * TimeSpan.TicksPerDay), DateTimeKind.Unspecified).ToString(
                    DateOnlyFormat,
                    CultureInfo.InvariantCulture
                ),
            ReconstructedMetadataValueKind.TimeOnly =>
                new DateTime(PrimaryValue, DateTimeKind.Unspecified).ToString(
                    TimeOnlyFormat,
                    CultureInfo.InvariantCulture
                ),
            ReconstructedMetadataValueKind.Guid or ReconstructedMetadataValueKind.Uri => Text!,
            _ => throw new InvalidOperationException($"Unsupported reconstructed metadata kind '{Kind}'.")
        };

    /// <summary>
    /// Renders the exact C# expression emitted into a generated OpenAPI contract.
    /// </summary>
    public string ToCSharpLiteral() =>
        Kind switch
        {
            ReconstructedMetadataValueKind.DateTime =>
                "new global::System.DateTime(" +
                PrimaryValue.ToString(CultureInfo.InvariantCulture) +
                "L, global::System.DateTimeKind." +
                GetDateTimeKindName((DateTimeKind) SecondaryValue) +
                ")",
            ReconstructedMetadataValueKind.DateTimeOffset =>
                "new global::System.DateTimeOffset(" +
                PrimaryValue.ToString(CultureInfo.InvariantCulture) +
                "L, global::System.TimeSpan.FromTicks(" +
                SecondaryValue.ToString(CultureInfo.InvariantCulture) +
                "L))",
            ReconstructedMetadataValueKind.TimeSpan =>
                "global::System.TimeSpan.FromTicks(" +
                PrimaryValue.ToString(CultureInfo.InvariantCulture) +
                "L)",
            ReconstructedMetadataValueKind.DateOnly =>
                "global::System.DateOnly.FromDayNumber(" +
                PrimaryValue.ToString(CultureInfo.InvariantCulture) +
                ")",
            ReconstructedMetadataValueKind.TimeOnly =>
                "new global::System.TimeOnly(" +
                PrimaryValue.ToString(CultureInfo.InvariantCulture) +
                "L)",
            ReconstructedMetadataValueKind.Guid =>
                "global::System.Guid.ParseExact(" + ToStringLiteral(Text!) + ", \"D\")",
            ReconstructedMetadataValueKind.Uri =>
                "new global::System.Uri(" +
                ToStringLiteral(Text!) +
                ", global::System.UriKind.RelativeOrAbsolute)",
            _ => throw new InvalidOperationException($"Unsupported reconstructed metadata kind '{Kind}'.")
        };

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ReconstructedMetadataValue);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int) Kind;
            hashCode = hashCode * 31 + PrimaryValue.GetHashCode();
            hashCode = hashCode * 31 + SecondaryValue.GetHashCode();
            hashCode = hashCode * 31 + (Text is null ? 0 : StringComparer.Ordinal.GetHashCode(Text));
            return hashCode;
        }
    }

    private static string GetDateTimeKindName(DateTimeKind kind) =>
        kind switch
        {
            DateTimeKind.Unspecified => nameof(DateTimeKind.Unspecified),
            DateTimeKind.Utc => nameof(DateTimeKind.Utc),
            DateTimeKind.Local => nameof(DateTimeKind.Local),
            _ => throw new InvalidOperationException($"Unsupported DateTime kind '{kind}'.")
        };

    private static string ToStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            builder.Append(EscapeChar(c));
        }

        return builder.Append('"').ToString();
    }

    private static string EscapeChar(char value) =>
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
