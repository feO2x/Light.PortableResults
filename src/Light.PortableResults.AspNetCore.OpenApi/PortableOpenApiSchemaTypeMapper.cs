using System;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Maps CLR types to OpenAPI schema primitives for use in schema factories.
/// </summary>
public static class PortableOpenApiSchemaTypeMapper
{
    /// <summary>
    /// Returns an <see cref="OpenApiSchema" /> that represents the JSON primitive type for
    /// the given CLR <paramref name="type" />.
    /// </summary>
    /// <remarks>
    /// <para>Recognized types and their schemas:</para>
    /// <list type="table">
    ///   <item>
    ///     <term><c>int</c>, <c>short</c>, <c>long</c>, <c>byte</c>, <c>sbyte</c>, <c>uint</c>, <c>ushort</c></term>
    ///     <description><c>{ type: integer }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>ulong</c></term>
    ///     <description><c>{ type: string, format: uint64 }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>float</c></term>
    ///     <description><c>{ type: number, format: float }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>double</c>, <c>decimal</c></term>
    ///     <description><c>{ type: number }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>bool</c></term>
    ///     <description><c>{ type: boolean }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>string</c></term>
    ///     <description><c>{ type: string }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>char</c></term>
    ///     <description><c>{ type: string, format: char }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>DateTime</c>, <c>DateTimeOffset</c></term>
    ///     <description><c>{ type: string, format: date-time }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>DateOnly</c></term>
    ///     <description><c>{ type: string, format: date }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>TimeOnly</c></term>
    ///     <description><c>{ type: string, format: time }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>TimeSpan</c></term>
    ///     <description><c>{ type: string, format: duration }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>Guid</c></term>
    ///     <description><c>{ type: string, format: uuid }</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>Uri</c></term>
    ///     <description><c>{ type: string, format: uri-reference }</c></description>
    ///   </item>
    /// </list>
    /// <para>
    /// <c>Nullable&lt;T&gt;</c> is unwrapped to its inner type before mapping.
    /// Any type not in the recognized set maps to an empty <see cref="OpenApiSchema" /> with no type
    /// constraint, preserving graceful-degradation behavior.
    /// </para>
    /// </remarks>
    public static OpenApiSchema Map(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null)
        {
            return Map(underlyingType);
        }

        if (type == typeof(int) || type == typeof(short) || type == typeof(long) ||
            type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(uint) || type == typeof(ushort))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Integer };
        }

        if (type == typeof(ulong))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uint64" };
        }

        if (type == typeof(float))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
        }

        if (type == typeof(double) || type == typeof(decimal))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Number };
        }

        if (type == typeof(bool))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Boolean };
        }

        if (type == typeof(string))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String };
        }

        if (type == typeof(char))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "char" };
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        }

        if (type == typeof(DateOnly))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
        }

        if (type == typeof(TimeOnly))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "time" };
        }

        if (type == typeof(TimeSpan))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "duration" };
        }

        if (type == typeof(Guid))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        }

        if (type == typeof(Uri))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" };
        }

        return new OpenApiSchema();
    }

    /// <summary>
    /// Returns an <see cref="OpenApiSchema" /> that represents the JSON primitive type for <typeparamref name="T" />.
    /// </summary>
    public static OpenApiSchema Map<T>() => Map(typeof(T));
}
