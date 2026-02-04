using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.AspNetCore.Shared.Serialization;

/// <summary>
/// Creates <see cref="DefaultResultJsonConverter{T}" /> instances for <see cref="Result{T}" /> types.
/// </summary>
public sealed class DefaultResultJsonConverterFactory : JsonConverterFactory
{
    private readonly object[] _constructorArguments;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResultJsonConverterFactory" />.
    /// </summary>
    /// <param name="options">The Light.Results options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public DefaultResultJsonConverterFactory(LightResultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
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
