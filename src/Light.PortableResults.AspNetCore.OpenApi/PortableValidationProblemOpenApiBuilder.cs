using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationProblemOpenApiBuilder" />.
    /// </summary>
    /// <param name="attribute">The validation problem attribute to configure.</param>
    public PortableValidationProblemOpenApiBuilder(ProducesPortableValidationProblemAttribute attribute) =>
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

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
    /// Adds an error entry that should appear in the response-level OpenAPI example for this endpoint.
    /// </summary>
    public PortableValidationProblemOpenApiBuilder WithErrorExample(
        string code,
        string? target,
        string? message = null,
        IReadOnlyDictionary<string, object?>? metadata = null
    )
    {
        _attribute.ErrorExamples = PortableOpenApiBuilderUtilities.AppendExamples(
            _attribute.ErrorExamples,
            new PortableOpenApiErrorExampleEntry(code, target, message, metadata)
        );
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
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
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
