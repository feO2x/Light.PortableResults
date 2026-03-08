using System;
using System.Collections.Concurrent;
using System.Text;

namespace Light.PortableResults.Validation;

/// <summary>
/// The built-in validation target normalizer that preserves member paths, removes the leading parameter root when
/// present, and applies a configurable casing convention to member segments.
/// </summary>
public sealed class DefaultValidationTargetNormalizer : IValidationTargetNormalizer
{
    private readonly ConcurrentDictionary<string, string> _cache = new (StringComparer.Ordinal);
    private readonly Func<string, string> _normalizeDelegate;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultValidationTargetNormalizer" />.
    /// </summary>
    /// <param name="casing">The casing convention for normalized member segments.</param>
    public DefaultValidationTargetNormalizer(ValidationTargetCasing casing = ValidationTargetCasing.CamelCase)
    {
        Casing = casing;
        _normalizeDelegate = NormalizeCore;
    }

    /// <summary>
    /// Gets the casing convention applied by this normalizer.
    /// </summary>
    public ValidationTargetCasing Casing { get; }

    /// <inheritdoc />
    public string Normalize(string rawPath)
    {
        if (rawPath is null)
        {
            throw new ArgumentNullException(nameof(rawPath));
        }

        return _cache.GetOrAdd(rawPath, _normalizeDelegate);
    }

    private string NormalizeCore(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        var trimmedPath = rawPath.Trim();
        var hasMemberSeparator = trimmedPath.IndexOf('.') >= 0;
        var startIndex = hasMemberSeparator ? FindFirstMemberSeparator(trimmedPath) + 1 : 0;
        if (startIndex >= trimmedPath.Length)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(trimmedPath.Length);
        var segmentStart = startIndex;
        for (var i = startIndex; i < trimmedPath.Length; i++)
        {
            var current = trimmedPath[i];
            if (current == '.')
            {
                AppendSegment(builder, trimmedPath, segmentStart, i - segmentStart);
                builder.Append('.');
                segmentStart = i + 1;
            }
            else if (current == '[')
            {
                AppendSegment(builder, trimmedPath, segmentStart, i - segmentStart);
                var closingBracket = trimmedPath.IndexOf(']', i);
                if (closingBracket < 0)
                {
                    builder.Append(trimmedPath, i, trimmedPath.Length - i);
                    return builder.ToString();
                }

                builder.Append(trimmedPath, i, closingBracket - i + 1);
                i = closingBracket;
                segmentStart = closingBracket + 1;
                if (segmentStart < trimmedPath.Length && trimmedPath[segmentStart] == '.')
                {
                    builder.Append('.');
                    i = segmentStart;
                    segmentStart++;
                }
            }
        }

        AppendSegment(builder, trimmedPath, segmentStart, trimmedPath.Length - segmentStart);
        return builder.ToString();
    }

    private static int FindFirstMemberSeparator(string value)
    {
        var firstDotIndex = value.IndexOf('.');
        return firstDotIndex < 0 ? value.Length : firstDotIndex;
    }

    private void AppendSegment(StringBuilder builder, string rawPath, int startIndex, int length)
    {
        if (length <= 0)
        {
            return;
        }

        var segment = rawPath.Substring(startIndex, length);
        var cleaned = RemoveIgnoredCharacters(segment);
        if (cleaned.Length == 0)
        {
            return;
        }

        switch (Casing)
        {
            case ValidationTargetCasing.CamelCase:
                builder.Append(ToCamelCase(cleaned));
                break;
            case ValidationTargetCasing.PascalCase:
                builder.Append(ToPascalCase(cleaned));
                break;
            default:
                builder.Append(cleaned);
                break;
        }
    }

    private static string RemoveIgnoredCharacters(string segment)
    {
        var trimmedSegment = segment.Trim();
        if (trimmedSegment.Length == 0)
        {
            return string.Empty;
        }

        if (trimmedSegment.StartsWith("this.", StringComparison.Ordinal))
        {
            trimmedSegment = trimmedSegment.Substring(5);
        }

        if (trimmedSegment.Length > 0 && trimmedSegment[0] == '@')
        {
            trimmedSegment = trimmedSegment.Substring(1);
        }

        return trimmedSegment;
    }

    private static string ToCamelCase(string segment)
    {
        if (segment.Length == 0 || char.IsLower(segment[0]))
        {
            return segment;
        }

        if (segment.Length == 1)
        {
            return char.ToLowerInvariant(segment[0]).ToString();
        }

        return char.ToLowerInvariant(segment[0]) + segment.Substring(1);
    }

    private static string ToPascalCase(string segment)
    {
        if (segment.Length == 0 || char.IsUpper(segment[0]))
        {
            return segment;
        }

        if (segment.Length == 1)
        {
            return char.ToUpperInvariant(segment[0]).ToString();
        }

        return char.ToUpperInvariant(segment[0]) + segment.Substring(1);
    }
}
