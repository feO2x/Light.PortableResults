using System;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a documented metadata contract for a portable error code.
/// </summary>
public abstract class PortableErrorMetadataContract
{
    private protected PortableErrorMetadataContract() { }

    /// <summary>
    /// Gets the singleton contract for error codes that do not emit metadata.
    /// </summary>
    public static PortableErrorMetadataContract NoMetadata { get; } = new PortableNoMetadataContract();

    /// <summary>
    /// Creates a contract backed by a CLR metadata type.
    /// </summary>
    /// <param name="metadataType">The CLR metadata type.</param>
    /// <returns>The metadata contract.</returns>
    public static PortableErrorMetadataContract FromType(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        return new PortableErrorMetadataTypeContract(metadataType);
    }

    /// <summary>
    /// Creates a contract backed by a schema factory.
    /// </summary>
    /// <param name="schemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="diagnosticName">The optional diagnostic name used in duplicate-contract errors.</param>
    /// <returns>The metadata contract.</returns>
    public static PortableErrorMetadataContract FromSchema(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        [CallerArgumentExpression(nameof(schemaFactory))] string? diagnosticName = null
    )
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        return new PortableErrorMetadataSchemaContract(schemaFactory, diagnosticName);
    }
}

/// <summary>
/// Represents a metadata contract backed by a CLR type.
/// </summary>
public sealed class PortableErrorMetadataTypeContract : PortableErrorMetadataContract
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableErrorMetadataTypeContract" />.
    /// </summary>
    /// <param name="metadataType">The CLR metadata type.</param>
    public PortableErrorMetadataTypeContract(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        MetadataType = metadataType;
    }

    /// <summary>
    /// Gets the CLR metadata type.
    /// </summary>
    public Type MetadataType { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PortableErrorMetadataTypeContract other && MetadataType == other.MetadataType;

    /// <inheritdoc />
    public override int GetHashCode() => MetadataType.GetHashCode();
}

/// <summary>
/// Represents a metadata contract backed by an OpenAPI schema factory.
/// </summary>
public sealed class PortableErrorMetadataSchemaContract : PortableErrorMetadataContract
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableErrorMetadataSchemaContract" />.
    /// </summary>
    /// <param name="schemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="diagnosticName">The optional diagnostic name used in duplicate-contract errors.</param>
    public PortableErrorMetadataSchemaContract(
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
        obj is PortableErrorMetadataSchemaContract other &&
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

/// <summary>
/// Represents a metadata contract for error codes that do not emit metadata.
/// </summary>
public sealed class PortableNoMetadataContract : PortableErrorMetadataContract
{
    internal PortableNoMetadataContract() { }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PortableNoMetadataContract;

    /// <inheritdoc />
    public override int GetHashCode() => 0;
}
