using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// Creates <see cref="CloudEventsEnvelopeForWritingJsonConverter{T}" /> instances for <see cref="CloudEventsEnvelopeForWriting{T}" /> types.
/// </summary>
public sealed class CloudEventsEnvelopeForWritingJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="typeToConvert" /> is an instance of
    /// <see cref="CloudEventsEnvelopeForWriting{T}" /> with a resolved generic; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(CloudEventsEnvelopeForWriting<>);

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">An instance of <see cref="CloudEventsEnvelopeForWriting{T}" /> with a resolved generic.</param>
    /// <param name="options">The options to use for the converter.</param>
    /// <returns>The created JSON converter.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(CloudEventsEnvelopeForWritingJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}
