using System.Text.Json;
using Light.Results.Http.Reading.Headers;
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
    /// Gets or sets the strategy deciding which headers should be read into metadata.
    /// </summary>
    public IHttpHeaderSelectionStrategy HeaderSelectionStrategy { get; init; } = HttpHeaderSelectionStrategies.None;

    /// <summary>
    /// Gets or sets how conflicts are handled when multiple headers map to the same metadata key.
    /// </summary>
    public HeaderConflictStrategy HeaderConflictStrategy { get; init; } = HeaderConflictStrategy.Throw;

    /// <summary>
    /// Gets or sets the merge strategy for combining header and body metadata. Headers will always be read first,
    /// the body afterward. The default value is <see cref="MetadataMergeStrategy.AddOrReplace" />.
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
    public IHttpHeaderParsingService HeaderParsingService { get; init; } = DefaultHttpHeaderParsingService.Default;

    /// <summary>
    /// Gets or sets serializer options used to deserialize Result payloads with <see cref="JsonSerializer" />.
    /// When <see langword="null" />, the optimized default reader pipeline is used.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; init; }
}
