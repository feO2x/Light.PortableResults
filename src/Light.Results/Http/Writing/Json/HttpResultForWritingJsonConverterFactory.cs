using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Writing.Json;

/// <summary>
/// Creates <see cref="HttpResultForWritingJsonConverter{T}" /> instances for <see cref="HttpResultForWriting{T}" /> types.
/// This factory is stateless.
/// </summary>
public sealed class HttpResultForWritingJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(HttpResultForWriting<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(HttpResultForWritingJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}
