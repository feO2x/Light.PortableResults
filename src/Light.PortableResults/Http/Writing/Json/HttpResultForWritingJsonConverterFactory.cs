using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.PortableResults.Http.Writing.Json;

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
    // This factory is a fallback for non-AOT scenarios. For AOT, call AddHttpResultForWritingConverter<T>()
    // on JsonSerializerOptions for each T you need. Since Converters are checked in reverse order
    // (last-added wins), the specific converter takes precedence and this method is never called for it.
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:RequiresDynamicCode",
        Justification =
            "Reflection is possible in Native AOT scenarios. Resoled HttpResultForWriting<T> types must simply be registered with the JsonSerializerContext."
    )]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(HttpResultForWritingJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType)!;
    }
}
