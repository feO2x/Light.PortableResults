using System;
using System.Text.Json.Serialization.Metadata;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Naming conventions for the Light.PortableResults schema-only types so that
/// <c>Microsoft.AspNetCore.OpenApi</c> emits readable schema names such as
/// <c>PortableError</c> or <c>PortableProblemDetails</c> instead of the default
/// <c>PortableErrorOfObject</c> or <c>PortableProblemDetailsOfObjectAndObject</c>.
/// </summary>
/// <remarks>
/// Register the convention when configuring OpenAPI, composing it with the default naming:
/// <code>
/// builder.Services.AddOpenApi(options =&gt;
/// {
///     options.CreateSchemaReferenceId = type =&gt;
///         PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId(type) ??
///         OpenApiOptions.CreateDefaultSchemaReferenceId(type);
/// });
/// </code>
/// The helper only produces custom names for Light.PortableResults schema-only types whose
/// generic arguments are all <see cref="object" />. Any other type returns <see langword="null" />
/// and is expected to be handled by the caller's fallback.
/// </remarks>
public static class PortableResultsOpenApiNamingConventions
{
    private const string SchemaNamespace = "Light.PortableResults.AspNetCore.Shared";

    /// <summary>
    /// Attempts to compute an OpenAPI schema reference id for a Light.PortableResults schema-only
    /// type whose generic arguments are all <see cref="object" />. For
    /// <c>PortableError&lt;object&gt;</c> this returns <c>"PortableError"</c>, for
    /// <c>PortableProblemDetails&lt;object, object&gt;</c> this returns
    /// <c>"PortableProblemDetails"</c>, and so on.
    /// </summary>
    /// <param name="typeInfo">The JSON type info for which the OpenAPI schema reference id is built.</param>
    /// <returns>
    /// A custom schema reference id for recognized Light.PortableResults schema-only types,
    /// or <see langword="null" /> when the default naming should be used.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo" /> is null.</exception>
    public static string? TryCreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return TryGetSimpleNameForAllObjectGenericArgs(typeInfo.Type);
    }

    private static string? TryGetSimpleNameForAllObjectGenericArgs(Type type)
    {
        if (type.Namespace != SchemaNamespace)
        {
            return null;
        }

        if (!type.IsGenericType || type.IsGenericTypeDefinition)
        {
            return null;
        }

        var genericArguments = type.GetGenericArguments();
        for (var i = 0; i < genericArguments.Length; i++)
        {
            if (genericArguments[i] != typeof(object))
            {
                return null;
            }
        }

        var name = type.Name;
        var tickIndex = name.IndexOf('`');
        return tickIndex < 0 ? name : name.Substring(0, tickIndex);
    }
}
