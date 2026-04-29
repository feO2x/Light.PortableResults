using System;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a documented metadata contract for a portable error code.
/// </summary>
public abstract class PortableErrorMetadataContract
{
    private static readonly PortableErrorMetadataNoMetadataContract SharedNoMetadata = new ();

    private protected PortableErrorMetadataContract() { }

    /// <summary>
    /// Gets the singleton contract for error codes that do not emit metadata.
    /// </summary>
    public static PortableErrorMetadataContract NoMetadata => SharedNoMetadata;

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
    /// <returns>The metadata contract.</returns>
    public static PortableErrorMetadataContract FromSchema(Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory)
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        return new PortableErrorMetadataSchemaContract(schemaFactory);
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
    public PortableErrorMetadataSchemaContract(Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory)
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        SchemaFactory = schemaFactory;
    }

    /// <summary>
    /// Gets the factory that creates a fresh metadata schema for the requested OpenAPI version.
    /// </summary>
    public Func<OpenApiSpecVersion, OpenApiSchema> SchemaFactory { get; }
}

/// <summary>
/// Represents a metadata contract for error codes that do not emit metadata.
/// </summary>
public sealed class PortableErrorMetadataNoMetadataContract : PortableErrorMetadataContract
{
    internal PortableErrorMetadataNoMetadataContract() { }
}
