using System;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

namespace Light.PortableResults.AspNetCore.OpenApi;

internal static class PortableOpenApiBuilderUtilities
{
    internal static string[] AppendStrings(string[]? existingValues, string newValue)
    {
        ArgumentNullException.ThrowIfNull(newValue);

        if (existingValues is null)
        {
            return [newValue];
        }

        var combinedValues = new string[existingValues.Length + 1];
        Array.Copy(existingValues, combinedValues, existingValues.Length);
        combinedValues[^1] = newValue;
        return combinedValues;
    }

    internal static string[] AppendStrings(string[]? existingValues, string[] newValues)
    {
        ArgumentNullException.ThrowIfNull(newValues);

        if (newValues.Length == 0)
        {
            return existingValues ?? [];
        }

        if (existingValues is null)
        {
            var copy = new string[newValues.Length];
            Array.Copy(newValues, copy, newValues.Length);
            return copy;
        }

        var combinedValues = new string[existingValues.Length + newValues.Length];
        Array.Copy(existingValues, combinedValues, existingValues.Length);
        Array.Copy(newValues, 0, combinedValues, existingValues.Length, newValues.Length);
        return combinedValues;
    }

    internal static ErrorMetadataContract[] AppendContracts(
        ErrorMetadataContract[]? existingValues,
        ErrorMetadataContract newValue
    )
    {
        ArgumentNullException.ThrowIfNull(newValue);

        if (existingValues is null)
        {
            return [newValue];
        }

        var combinedValues = new ErrorMetadataContract[existingValues.Length + 1];
        Array.Copy(existingValues, combinedValues, existingValues.Length);
        combinedValues[^1] = newValue;
        return combinedValues;
    }

    internal static PortableOpenApiErrorExampleEntry[] AppendExamples(
        PortableOpenApiErrorExampleEntry[]? existingValues,
        PortableOpenApiErrorExampleEntry newValue
    )
    {
        ArgumentNullException.ThrowIfNull(newValue);

        if (existingValues is null)
        {
            return [newValue];
        }

        var combinedValues = new PortableOpenApiErrorExampleEntry[existingValues.Length + 1];
        Array.Copy(existingValues, combinedValues, existingValues.Length);
        combinedValues[^1] = newValue;
        return combinedValues;
    }
}
