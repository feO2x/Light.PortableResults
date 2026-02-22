using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Creates converters for generic CloudEvents success payload types.
/// </summary>
public sealed class CloudEventsSuccessPayloadJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(CloudEventsAutoSuccessPayload<>) ||
               genericTypeDefinition == typeof(CloudEventsBareSuccessPayload<>) ||
               genericTypeDefinition == typeof(CloudEventsWrappedSuccessPayload<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();

        var genericConverterTypeDefinition = genericTypeDefinition == typeof(CloudEventsAutoSuccessPayload<>) ?
            typeof(CloudEventsAutoSuccessPayloadJsonConverter<>) :
            genericTypeDefinition == typeof(CloudEventsBareSuccessPayload<>) ?
                typeof(CloudEventsBareSuccessPayloadJsonConverter<>) :
                typeof(CloudEventsWrappedSuccessPayloadJsonConverter<>);

        var converterType = genericConverterTypeDefinition.MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}
