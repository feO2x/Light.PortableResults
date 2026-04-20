using System;
using Light.PortableResults.Http.Writing;

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
        _attribute.InlineErrorMetadataTypes = PortableOpenApiBuilderUtilities.AppendTypes(
            _attribute.InlineErrorMetadataTypes,
            metadataType
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
    /// Overrides the documented validation problem serialization format for this endpoint.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder UseFormat(ValidationProblemSerializationFormat format)
    {
        _attribute.Format = format;
        return this;
    }
}
