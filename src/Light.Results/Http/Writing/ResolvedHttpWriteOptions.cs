using System;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;

namespace Light.Results.Http.Writing;

/// <summary>
/// Represents frozen, per-request HTTP write options derived from <see cref="LightResultsHttpWriteOptions" />.
/// This struct is created once at the top of a request and passed to both header-setting and body-writing methods.
/// </summary>
/// <param name="ValidationProblemSerializationFormat">The format for validation error serialization.</param>
/// <param name="MetadataSerializationMode">The mode controlling when metadata is serialized.</param>
/// <param name="CreateProblemDetailsInfo">Optional factory for creating custom problem details.</param>
/// <param name="FirstErrorCategoryIsLeadingCategory">Whether the first error category is the leading category.</param>
public readonly record struct ResolvedHttpWriteOptions(
    ValidationProblemSerializationFormat ValidationProblemSerializationFormat,
    MetadataSerializationMode MetadataSerializationMode,
    Func<Errors, MetadataObject?, ProblemDetailsInfo>? CreateProblemDetailsInfo,
    bool FirstErrorCategoryIsLeadingCategory
);
