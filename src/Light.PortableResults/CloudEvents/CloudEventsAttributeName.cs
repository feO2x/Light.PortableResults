using System;

namespace Light.PortableResults.CloudEvents;

/// <summary>
/// Provides low-level syntax validation for CloudEvents extension attribute names.
/// </summary>
public static class CloudEventsAttributeName
{
    /// <summary>
    /// Determines whether an attribute name contains only lowercase ASCII letters and decimal digits.
    /// </summary>
    /// <param name="attributeName">The attribute name whose character syntax is checked.</param>
    /// <returns>
    /// <see langword="true" /> when every character is a lowercase ASCII letter or decimal digit;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method validates only the character syntax. An empty name vacuously satisfies this predicate,
    /// and standard or reserved CloudEvents names can also satisfy it. Callers that write complete extension
    /// attributes must enforce those additional constraints; <c>WriteCloudEventsExtensionAttribute</c> does so.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="attributeName" /> is <see langword="null" />.
    /// </exception>
    public static bool IsValidExtensionAttributeName(string attributeName)
    {
        if (attributeName is null)
        {
            throw new ArgumentNullException(nameof(attributeName));
        }

        foreach (var character in attributeName)
        {
            if (character is (< 'a' or > 'z') and (< '0' or > '9'))
            {
                return false;
            }
        }

        return true;
    }
}
