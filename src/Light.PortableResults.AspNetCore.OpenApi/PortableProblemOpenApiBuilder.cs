using System;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Configures a documented Light.PortableResults problem response.
/// </summary>
public sealed class PortableProblemOpenApiBuilder
{
    private readonly ProducesPortableProblemAttribute _attribute;

    internal PortableProblemOpenApiBuilder(ProducesPortableProblemAttribute attribute) => _attribute = attribute;

    /// <summary>
    /// Narrows the top-level metadata schema to <typeparamref name="TMetadata" />.
    /// </summary>
    public PortableProblemOpenApiBuilder WithMetadata<TMetadata>()
    {
        _attribute.TopLevelMetadataType = typeof(TMetadata);
        return this;
    }

    /// <summary>
    /// Narrows the top-level metadata schema to the specified CLR type.
    /// </summary>
    public PortableProblemOpenApiBuilder WithMetadata(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        _attribute.TopLevelMetadataType = metadataType;
        return this;
    }

    /// <summary>
    /// Opts the response into globally registered metadata contracts for the provided error codes.
    /// </summary>
    public PortableProblemOpenApiBuilder WithErrorCodes(params string[] codes)
    {
        _attribute.ErrorCodes = PortableOpenApiBuilderUtilities.AppendStrings(_attribute.ErrorCodes, codes);
        return this;
    }

    /// <summary>
    /// Registers an inline metadata contract for the specified error code.
    /// </summary>
    public PortableProblemOpenApiBuilder WithErrorMetadata(string code, Type metadataType)
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
    public PortableProblemOpenApiBuilder WithErrorMetadata<TMetadata>(string code)
    {
        return WithErrorMetadata(code, typeof(TMetadata));
    }
}
