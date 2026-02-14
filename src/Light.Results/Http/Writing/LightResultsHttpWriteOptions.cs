using System;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;

namespace Light.Results.Http.Writing;

/// <summary>
/// Configures how Light.Results responses are serialized for ASP.NET Core.
/// </summary>
public sealed record LightResultsHttpWriteOptions
{
    /// <summary>
    /// <para>
    /// Gets or sets the serialization format for validation errors in HTTP 400 Bad Request
    /// and HTTP 422 Unprocessable Content responses. For all other error responses,
    /// the rich format is always used regardless of this setting.
    /// </para>
    /// <para>
    /// The default value is set to <see cref="ValidationProblemSerializationFormat.AspNetCoreCompatible" /> for
    /// backwards-compatibility reasons. We encourage you to use
    /// <see cref="ValidationProblemSerializationFormat.Rich" /> to get the most out of Light.Results.
    /// </para>
    /// </summary>
    public ValidationProblemSerializationFormat ValidationProblemSerializationFormat { get; set; } =
        ValidationProblemSerializationFormat.AspNetCoreCompatible;

    /// <summary>
    /// <para>
    /// Gets or sets the value indicating whether metadata is serialized to the response body.
    /// <see cref="SharedJsonSerialization.MetadataSerializationMode.ErrorsOnly" /> will serialize metadata only for errors.
    /// <see cref="SharedJsonSerialization.MetadataSerializationMode.Always" /> will serialize metadata for both errors and success results.
    /// </para>
    /// <para>
    /// PLEASE NOTE: this does not affect headers! When a metadata value is marked with
    /// <see cref="MetadataValueAnnotation.SerializeInHttpHeader" />, the corresponding header will always be set,
    /// regardless of the configuration value here.
    /// </para>
    /// <para>
    /// The default value is set to <see cref="MetadataSerializationMode.ErrorsOnly" /> to be backwards-compatible
    /// with default ASP.NET Core behavior. We encourage you to set this to
    /// <see cref="MetadataSerializationMode.Always" /> to get the most out of Light.Results.
    /// </para>
    /// </summary>
    public MetadataSerializationMode MetadataSerializationMode { get; set; } = MetadataSerializationMode.ErrorsOnly;

    /// <summary>
    /// Gets or sets the factory for creating <see cref="ProblemDetailsInfo" /> instances from errors and metadata.
    /// </summary>
    public Func<Errors, MetadataObject?, ProblemDetailsInfo>? CreateProblemDetailsInfo { get; set; }

    /// <summary>
    /// Gets or sets the value indicating whether the first error category should be used as the leading category.
    /// </summary>
    public bool FirstErrorCategoryIsLeadingCategory { get; set; } = true;
}
