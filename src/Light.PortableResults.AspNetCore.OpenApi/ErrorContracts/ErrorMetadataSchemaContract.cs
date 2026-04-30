using System;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a metadata contract backed by an OpenAPI schema factory.
/// </summary>
public sealed class ErrorMetadataSchemaContract : ErrorMetadataContract
{
    /// <summary>
    /// Initializes a new instance of <see cref="ErrorMetadataSchemaContract" />.
    /// </summary>
    /// <param name="schemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="diagnosticName">The optional diagnostic name used in duplicate-contract errors.</param>
    public ErrorMetadataSchemaContract(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        [CallerArgumentExpression(nameof(schemaFactory))] string? diagnosticName = null
    )
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        SchemaFactory = schemaFactory;
        DiagnosticName = CreateDiagnosticName(schemaFactory, diagnosticName);
    }

    /// <summary>
    /// Gets the factory that creates a fresh metadata schema for the requested OpenAPI version.
    /// </summary>
    public Func<OpenApiSpecVersion, OpenApiSchema> SchemaFactory { get; }

    /// <summary>
    /// Gets the diagnostic name used in duplicate-contract errors.
    /// </summary>
    public string DiagnosticName { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ErrorMetadataSchemaContract other &&
        string.Equals(DiagnosticName, other.DiagnosticName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => DiagnosticName.GetHashCode(StringComparison.Ordinal);

    private static string CreateDiagnosticName(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        string? diagnosticName
    )
    {
        if (!string.IsNullOrWhiteSpace(diagnosticName))
        {
            return diagnosticName;
        }

        var method = schemaFactory.Method;
        var methodName = method.Name;
        var declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name;
        if (string.IsNullOrWhiteSpace(methodName) || methodName.Contains("<", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A schema-based error metadata contract requires a meaningful diagnostic name. " +
                "Pass the diagnosticName argument explicitly when registering anonymous or compiler-generated schema factories."
            );
        }

        if (!string.IsNullOrWhiteSpace(declaringTypeName) && !string.IsNullOrWhiteSpace(methodName))
        {
            return $"{declaringTypeName}.{methodName}";
        }

        throw new InvalidOperationException(
            "A schema-based error metadata contract requires a meaningful diagnostic name. " +
            "Pass the diagnosticName argument explicitly when registering anonymous or compiler-generated schema factories."
        );
    }
}
