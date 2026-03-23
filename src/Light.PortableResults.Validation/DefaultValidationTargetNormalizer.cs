using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace Light.PortableResults.Validation;

/// <summary>
/// The built-in validation target normalizer for caller-expression, relative, and absolute validation targets.
/// </summary>
public sealed class DefaultValidationTargetNormalizer : IValidationTargetNormalizer
{
    /// <summary>
    /// The default threshold at which paths are normalized into a rented buffer instead of a stack buffer.
    /// </summary>
    public const int DefaultStackBufferThreshold = 512;

    private readonly ArrayPool<char> _arrayPool;
    private readonly ConcurrentDictionary<string, string> _callerExpressionCache = new (StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _pathCache = new (StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultValidationTargetNormalizer" />.
    /// </summary>
    /// <param name="casing">The casing convention for normalized member segments derived from raw caller expressions.</param>
    /// <param name="arrayPool">
    /// The array pool to use for paths (optional). If null is specified, <see cref="ArrayPool{T}.Shared" /> is used.
    /// </param>
    /// <param name="arrayPoolThreshold">
    /// The threshold at which paths are normalized into a rented buffer instead of a stack buffer. Defaults to
    /// <see cref="DefaultStackBufferThreshold" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayPoolThreshold" /> is less than 0.</exception>
    public DefaultValidationTargetNormalizer(
        ValidationTargetCasing casing = ValidationTargetCasing.CamelCase,
        ArrayPool<char>? arrayPool = null,
        int arrayPoolThreshold = DefaultStackBufferThreshold
    )
    {
        if (arrayPoolThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayPoolThreshold));
        }

        Casing = casing;
        _arrayPool = arrayPool ?? ArrayPool<char>.Shared;
        ArrayPoolThreshold = arrayPoolThreshold;
    }

    /// <summary>
    /// Gets the casing convention applied by this normalizer.
    /// </summary>
    public ValidationTargetCasing Casing { get; }

    /// <summary>
    /// Gets the threshold at which paths are normalized into a rented buffer instead of a stack buffer.
    /// </summary>
    public int ArrayPoolThreshold { get; }

    /// <inheritdoc />
    public string Normalize(string rawPath, ValidationTargetSemantics semantics)
    {
        if (rawPath is null)
        {
            throw new ArgumentNullException(nameof(rawPath));
        }

        if (!semantics.IsDefined())
        {
            throw new ArgumentOutOfRangeException(nameof(semantics));
        }

        return semantics == ValidationTargetSemantics.CallerExpression ?
            _callerExpressionCache.GetOrAdd(rawPath, NormalizeCallerExpressionCore) :
            _pathCache.GetOrAdd(rawPath, NormalizePathCore);
    }

    private string NormalizeCallerExpressionCore(string rawPath) =>
        NormalizeCore(rawPath, ValidationTargetSemantics.CallerExpression);

    private string NormalizePathCore(string rawPath) => NormalizeCore(rawPath, ValidationTargetSemantics.Relative);

    private string NormalizeCore(string rawPath, ValidationTargetSemantics semantics)
    {
        var trimmedPath = rawPath.AsSpan().Trim();
        if (trimmedPath.IsEmpty)
        {
            return string.Empty;
        }

        var parsingStart = semantics == ValidationTargetSemantics.CallerExpression ?
            FindCallerExpressionParsingStart(trimmedPath) :
            0;
        if (parsingStart >= trimmedPath.Length)
        {
            return string.Empty;
        }

        if (trimmedPath.Length <= ArrayPoolThreshold)
        {
            Span<char> stackBuffer = stackalloc char[trimmedPath.Length];
            return NormalizeIntoString(trimmedPath, parsingStart, stackBuffer);
        }

        var rentedBuffer = _arrayPool.Rent(trimmedPath.Length);
        try
        {
            return NormalizeIntoString(trimmedPath, parsingStart, rentedBuffer.AsSpan(0, trimmedPath.Length));
        }
        finally
        {
            _arrayPool.Return(rentedBuffer);
        }
    }

    private static int FindCallerExpressionParsingStart(ReadOnlySpan<char> rawPath)
    {
        var indexOfDot = rawPath.IndexOf('.');
        return indexOfDot == -1 ? 0 : indexOfDot + 1;
    }

    private string NormalizeIntoString(ReadOnlySpan<char> rawPath, int startIndex, Span<char> buffer)
    {
        var length = NormalizeIntoBuffer(rawPath, startIndex, buffer);
        return CreateString(buffer, length);
    }

    private int NormalizeIntoBuffer(ReadOnlySpan<char> rawPath, int startIndex, Span<char> buffer)
    {
        var builder = new TargetBuffer(buffer);
        var segmentStart = startIndex;
        var index = startIndex;
        while (index < rawPath.Length)
        {
            var current = rawPath[index];
            if (current == '.')
            {
                AppendSegment(rawPath, segmentStart, index, ref builder);
                builder.Append('.');
                index++;
                segmentStart = index;
                continue;
            }

            if (current != '[')
            {
                index++;
                continue;
            }

            AppendSegment(rawPath, segmentStart, index, ref builder);

            var closingBracket = index + 1;
            while (closingBracket < rawPath.Length && rawPath[closingBracket] != ']')
            {
                closingBracket++;
            }

            if (closingBracket >= rawPath.Length)
            {
                builder.Append(rawPath.Slice(index));
                return builder.Length;
            }

            builder.Append(rawPath.Slice(index, closingBracket - index + 1));
            index = closingBracket + 1;
            segmentStart = index;
            if (index < rawPath.Length && rawPath[index] == '.')
            {
                builder.Append('.');
                index++;
                segmentStart = index;
            }
        }

        AppendSegment(rawPath, segmentStart, rawPath.Length, ref builder);
        return builder.Length;
    }

    private void AppendSegment(ReadOnlySpan<char> rawPath, int startIndex, int endIndex, ref TargetBuffer builder)
    {
        while (startIndex < endIndex && char.IsWhiteSpace(rawPath[startIndex]))
        {
            startIndex++;
        }

        while (endIndex > startIndex && char.IsWhiteSpace(rawPath[endIndex - 1]))
        {
            endIndex--;
        }

        if (startIndex >= endIndex)
        {
            return;
        }

        if (rawPath[startIndex] == '@')
        {
            startIndex++;
        }

        if (startIndex >= endIndex)
        {
            return;
        }

        switch (Casing)
        {
            case ValidationTargetCasing.CamelCase:
                builder.Append(char.ToLowerInvariant(rawPath[startIndex]));
                break;
            case ValidationTargetCasing.PascalCase:
                builder.Append(char.ToUpperInvariant(rawPath[startIndex]));
                break;
            default:
                builder.Append(rawPath[startIndex]);
                break;
        }

        if (endIndex - startIndex > 1)
        {
            builder.Append(rawPath.Slice(startIndex + 1, endIndex - startIndex - 1));
        }
    }

    private static string CreateString(Span<char> buffer, int length)
    {
        return length == 0 ? string.Empty : buffer.Slice(0, length).ToString();
    }

    private ref struct TargetBuffer
    {
        private readonly Span<char> _buffer;

        public TargetBuffer(Span<char> buffer)
        {
            _buffer = buffer;
            Length = 0;
        }

        public int Length { get; private set; }

        public void Append(char value)
        {
            _buffer[Length] = value;
            Length++;
        }

        public void Append(ReadOnlySpan<char> value)
        {
            value.CopyTo(_buffer.Slice(Length));
            Length += value.Length;
        }
    }
}
