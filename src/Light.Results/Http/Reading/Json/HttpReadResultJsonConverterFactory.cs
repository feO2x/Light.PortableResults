using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// Creates <see cref="HttpReadResultJsonConverter{T}" /> instances for <see cref="Result{T}" />.
/// </summary>
public sealed class HttpReadResultJsonConverterFactory : JsonConverterFactory
{
    private readonly PreferSuccessPayload _preferSuccessPayload;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadResultJsonConverterFactory" />.
    /// </summary>
    public HttpReadResultJsonConverterFactory(PreferSuccessPayload preferSuccessPayload = PreferSuccessPayload.Auto) =>
        _preferSuccessPayload = preferSuccessPayload;

    /// <summary>
    /// Determines whether the factory can create a converter for the specified type.
    /// </summary>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(HttpReadResultJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType, _preferSuccessPayload)!;
    }
}
