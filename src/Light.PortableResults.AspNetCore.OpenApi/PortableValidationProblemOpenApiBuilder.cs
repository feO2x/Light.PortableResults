using System;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Http.Writing;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Configures a documented Light.PortableResults validation problem response.
/// </summary>
public sealed class PortableValidationProblemOpenApiBuilder
{
    private readonly ProducesPortableValidationProblemAttribute _attribute;

    internal PortableValidationProblemOpenApiBuilder(ProducesPortableValidationProblemAttribute attribute) =>
        _attribute = attribute;

    /// <summary>
    /// Narrows the top-level metadata schema to <typeparamref name="TMetadata" />.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithMetadata<TMetadata>()
    {
        _attribute.TopLevelMetadataType = typeof(TMetadata);
        return this;
    }

    /// <summary>
    /// Narrows the top-level metadata schema to the specified CLR type.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithMetadata(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        _attribute.TopLevelMetadataType = metadataType;
        return this;
    }

    /// <summary>
    /// Opts the response into globally registered metadata contracts for the provided error codes.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithErrorCodes(params string[] codes)
    {
        _attribute.ErrorCodes = PortableOpenApiBuilderUtilities.AppendStrings(_attribute.ErrorCodes, codes);
        return this;
    }

    /// <summary>
    /// Keeps the schema non-exhaustive so undocumented error codes remain representable.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder AllowUnknownErrorCodes()
    {
        _attribute.AllowUnknownErrorCodes = true;
        return this;
    }

    /// <summary>
    /// Registers an inline metadata contract for the specified error code.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithErrorMetadata(string code, Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(metadataType);

        _attribute.InlineErrorMetadataCodes = PortableOpenApiBuilderUtilities.AppendStrings(
            _attribute.InlineErrorMetadataCodes,
            code
        );
        _attribute.InlineErrorMetadataContracts = PortableOpenApiBuilderUtilities.AppendContracts(
            _attribute.InlineErrorMetadataContracts,
            ErrorMetadataContract.FromType(metadataType)
        );
        return this;
    }

    /// <summary>
    /// Registers an inline metadata contract for the specified error code.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithErrorMetadata<TMetadata>(string code)
    {
        return WithErrorMetadata(code, typeof(TMetadata));
    }

    /// <summary>
    /// Registers an inline schema-factory metadata contract for the specified error code.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithErrorMetadata(
        string code,
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        string? schemaId = null
    )
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(schemaFactory);

        _attribute.InlineErrorMetadataCodes = PortableOpenApiBuilderUtilities.AppendStrings(
            _attribute.InlineErrorMetadataCodes,
            code
        );
        _attribute.InlineErrorMetadataContracts = PortableOpenApiBuilderUtilities.AppendContracts(
            _attribute.InlineErrorMetadataContracts,
            ErrorMetadataContract.FromSchema(schemaFactory, schemaId)
        );
        return this;
    }

    /// <summary>
    /// Overrides the documented validation problem serialization format for this endpoint.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder UseFormat(ValidationProblemSerializationFormat format)
    {
        _attribute.Format = format;
        return this;
    }
}
