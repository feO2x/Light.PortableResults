using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.AspNetCore.Shared.Serialization;

public sealed class DefaultResultJsonConverterFactory : JsonConverterFactory
{
    private readonly object[] _constructorArguments;

    public DefaultResultJsonConverterFactory(LightResultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _constructorArguments = [options];
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification =
            "DefaultResultJsonConverter is not removed by the Trimmer as it is directly referenced in this factory."
    )]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(DefaultResultJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter) Activator.CreateInstance(converterType, _constructorArguments)!;
    }
}
