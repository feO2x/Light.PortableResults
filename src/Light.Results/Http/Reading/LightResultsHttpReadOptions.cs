using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Http.Headers;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading;

/// <summary>
/// Options controlling how <see cref="System.Net.Http.HttpResponseMessage" /> responses are read into Light.Results.
/// </summary>
public sealed record LightResultsHttpReadOptions
{
    /// <summary>
    /// Gets the default options instance for HTTP response deserialization.
    /// </summary>
    public static LightResultsHttpReadOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets which headers should be read into metadata.
    /// </summary>
    public HeaderSelectionMode HeaderSelectionMode { get; init; } = HeaderSelectionMode.All;

    /// <summary>
    /// Gets or sets the allow list of header names to read when <see cref="HeaderSelectionMode" /> is AllowList.
    /// </summary>
    public IReadOnlyCollection<string>? HeaderAllowList { get; init; }

    /// <summary>
    /// Gets or sets the deny list of header names to skip when <see cref="HeaderSelectionMode" /> is DenyList.
    /// </summary>
    public IReadOnlyCollection<string>? HeaderDenyList { get; init; }

    /// <summary>
    /// Gets or sets how conflicts are handled when multiple headers map to the same metadata key.
    /// </summary>
    public HeaderConflictStrategy HeaderConflictStrategy { get; init; } = HeaderConflictStrategy.Throw;

    /// <summary>
    /// Gets or sets the merge strategy for combining body and header metadata.
    /// </summary>
    public MetadataMergeStrategy MergeStrategy { get; init; } = MetadataMergeStrategy.AddOrReplace;

    /// <summary>
    /// Gets or sets how successful payloads are interpreted.
    /// </summary>
    public PreferSuccessPayload PreferSuccessPayload { get; init; } = PreferSuccessPayload.Auto;

    /// <summary>
    /// Gets or sets whether problem details content types should be treated as failures even when status codes succeed.
    /// </summary>
    public bool TreatProblemDetailsAsFailure { get; init; } = true;

    /// <summary>
    /// Gets or sets the annotation applied to metadata values originating from headers.
    /// </summary>
    public MetadataValueAnnotation HeaderMetadataAnnotation { get; init; } =
        MetadataValueAnnotation.SerializeInHttpHeader;

    /// <summary>
    /// Gets or sets the header parsing service used to transform headers into metadata entries.
    /// </summary>
    public IHttpHeaderParsingService HeaderParsingService { get; init; } = DefaultHttpHeaderParsingService.Empty;

    /// <summary>
    /// Gets or sets optional serializer options used to deserialize Result payloads.
    /// If <see langword="null" />, default cached options are used.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; init; }
}
