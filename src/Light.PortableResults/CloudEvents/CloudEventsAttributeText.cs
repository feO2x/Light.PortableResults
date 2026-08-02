using System;

namespace Light.PortableResults.CloudEvents;

/// <summary>
/// Provides validation for the CloudEvents <c>String</c> context-attribute type.
/// </summary>
public static class CloudEventsAttributeText
{
    /// <summary>
    /// Finds the first character that is not allowed by the CloudEvents <c>String</c> type.
    /// </summary>
    /// <param name="text">The text to inspect.</param>
    /// <returns>
    /// The UTF-16 index of the first disallowed Unicode code point, or <c>-1</c> when the text conforms.
    /// </returns>
    /// <remarks>
    /// C0 and C1 control characters, Unicode noncharacters, and unpaired UTF-16 surrogates are disallowed.
    /// A valid surrogate pair is treated as one Unicode scalar value. The JSON extension-attribute writer
    /// always applies this rule. A custom conversion service can call it earlier when failure before
    /// serialization is more important than avoiding the writer's second validation scan.
    /// </remarks>
    public static int IndexOfDisallowedCharacter(ReadOnlySpan<char> text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (character <= '\u007E')
            {
                if (character < '\u0020')
                {
                    return index;
                }

                continue;
            }

            if (character <= '\u009F')
            {
                return index;
            }

            if (character < '\uD800')
            {
                continue;
            }

            if (character > '\uDFFF')
            {
                if (character is >= '\uFDD0' and <= '\uFDEF' || character >= '\uFFFE')
                {
                    return index;
                }

                continue;
            }

            if (character >= '\uDC00' ||
                index + 1 >= text.Length ||
                text[index + 1] is < '\uDC00' or > '\uDFFF')
            {
                return index;
            }

            var codePoint = 0x10000 + ((character - '\uD800') << 10) + text[index + 1] - '\uDC00';
            if ((codePoint & 0xFFFF) >= 0xFFFE)
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}
