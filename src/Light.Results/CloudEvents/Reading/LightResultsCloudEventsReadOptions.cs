using System;
using System.Text.Json;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Options controlling how CloudEvents JSON envelopes are read into Light.Results.
/// </summary>
public sealed record LightResultsCloudEventsReadOptions
{
    /// <summary>
    /// Gets the default options instance for CloudEvents deserialization.
    /// </summary>
    public static LightResultsCloudEventsReadOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets serializer options used to deserialize CloudEvents envelopes and data payloads.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; init; } =
        Module.DefaultSerializerOptions;

    /// <summary>
    /// Gets or sets how successful generic payloads are interpreted.
    /// </summary>
    public PreferSuccessPayload PreferSuccessPayload { get; init; } = PreferSuccessPayload.Auto;

    /// <summary>
    /// Gets or sets an optional fallback callback that classifies failures based on the CloudEvents <c>type</c>.
    /// </summary>
    public Func<string, bool>? IsFailureType { get; init; }

    /// <summary>
    /// Gets or sets an optional parsing service used to convert extension attributes into metadata for tier-1 methods.
    /// </summary>
    public ICloudEventsAttributeParsingService? ParsingService { get; init; }

    /// <summary>
    /// Gets or sets the merge strategy for combining envelope and payload metadata.
    /// </summary>
    public MetadataMergeStrategy MergeStrategy { get; init; } = MetadataMergeStrategy.AddOrReplace;
}
