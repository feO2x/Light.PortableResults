using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// Creates <see cref="CloudEventWriteResultJsonConverter{T}" /> instances for <see cref="Result{T}" /> types.
/// </summary>
public sealed class CloudEventWriteResultJsonConverterFactory : JsonConverterFactory
{
    private readonly object[] _constructorArguments;

    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventWriteResultJsonConverterFactory" />.
    /// </summary>
    /// <param name="options">The CloudEvent write options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public CloudEventWriteResultJsonConverterFactory(LightResultsCloudEventWriteOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _constructorArguments = [options];
    }

    /// <summary>
    /// Determines whether the factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <returns><see langword="true" /> if the type is a <see cref="Result{T}" />; otherwise, <see langword="false" />.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The created converter.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(CloudEventWriteResultJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType, _constructorArguments)!;
    }
}
