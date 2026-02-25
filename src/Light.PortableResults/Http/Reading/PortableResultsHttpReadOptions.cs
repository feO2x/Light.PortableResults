using System.Text.Json;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading;

/// <summary>
/// Options controlling how <see cref="System.Net.Http.HttpResponseMessage" /> responses are read into Light.PortableResults.
/// </summary>
public sealed record PortableResultsHttpReadOptions
{
    /// <summary>
    /// Gets the default options instance for HTTP response deserialization.
    /// </summary>
    public static PortableResultsHttpReadOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets the header parsing service used to transform headers into metadata entries.
    /// The default value is <see cref="ParseNoHttpHeadersService.Instance" />, which skips all headers.
    /// </summary>
    public IHttpHeaderParsingService HeaderParsingService { get; init; } = ParseNoHttpHeadersService.Instance;

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
    /// Gets or sets whether problem details content types should be treated as failures even when the status code
    /// of the HTTP response indicates success.
    /// </summary>
    public bool TreatProblemDetailsAsFailure { get; init; } = true;

    /// <summary>
    /// Gets or sets serializer options used to deserialize Result payloads with <see cref="JsonSerializer" />.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; init; } = Module.DefaultSerializerOptions;
}
