using System;
using System.Text.Json;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.CloudEvents.Reading;

/// <summary>
/// Options controlling how CloudEvents JSON envelopes are read into Light.PortableResults.
/// </summary>
public sealed record PortableResultsCloudEventsReadOptions
{
    /// <summary>
    /// Gets the default options instance for CloudEvents deserialization.
    /// </summary>
    public static PortableResultsCloudEventsReadOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets serializer options used to deserialize CloudEvents envelopes and data payloads.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; init; } =
        PortableResultsCloudEventsReadingModule.DefaultSerializerOptions;

    /// <summary>
    /// Gets or sets how successful generic payloads are interpreted.
    /// </summary>
    public PreferSuccessPayload PreferSuccessPayload { get; init; } = PreferSuccessPayload.Auto;

    /// <summary>
    /// Gets or sets an optional fallback callback that classifies failures based on the CloudEvents <c>type</c>.
    /// </summary>
    public Func<string, bool>? IsFailureType { get; init; }

    /// <summary>
    /// Gets or sets an optional parsing service used to convert extension attributes into metadata for tier-1
    /// methods. JSON string attributes initially have <see cref="MetadataKind.String" /> even when the writer
    /// mapped another primitive kind to canonical text; register an attribute parser to restore that kind.
    /// </summary>
    public ICloudEventsAttributeParsingService? ParsingService { get; init; }

    /// <summary>
    /// Gets or sets the merge strategy for combining envelope and payload metadata.
    /// </summary>
    public MetadataMergeStrategy MergeStrategy { get; init; } = MetadataMergeStrategy.AddOrReplace;
}
