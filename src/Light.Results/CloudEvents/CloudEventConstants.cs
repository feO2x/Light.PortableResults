using System;
using System.Collections.Generic;

namespace Light.Results.CloudEvents;

/// <summary>
/// Holds shared CloudEvents constants used by Light.Results.
/// </summary>
public static class CloudEventConstants
{
    /// <summary>
    /// The CloudEvents specversion written and accepted by this integration.
    /// </summary>
    public const string SpecVersion = "1.0";

    /// <summary>
    /// The MIME type for CloudEvents JSON envelopes.
    /// </summary>
    public const string CloudEventsJsonContentType = "application/cloudevents+json";

    /// <summary>
    /// The MIME type used for JSON data payloads.
    /// </summary>
    public const string JsonContentType = "application/json";

    /// <summary>
    /// The reserved extension attribute used by Light.Results to classify outcomes.
    /// </summary>
    public const string LightResultsOutcomeAttributeName = "lroutcome";

    /// <summary>
    /// Gets the set of reserved attribute names that cannot be populated from metadata conversion.
    /// </summary>
    public static ISet<string> ForbiddenConvertedAttributeNames { get; } = new HashSet<string>(
        StringComparer.Ordinal
    )
    {
        "data",
        "data_base64",
        LightResultsOutcomeAttributeName
    };

    /// <summary>
    /// Gets the set of standard CloudEvents attribute names.
    /// </summary>
    public static ISet<string> StandardAttributeNames { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "specversion",
        "type",
        "source",
        "subject",
        "id",
        "time",
        "datacontenttype",
        "dataschema",
        "data",
        "data_base64"
    };
}
