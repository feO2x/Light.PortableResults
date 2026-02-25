using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Light.PortableResults.CloudEvents;

/// <summary>
/// Holds shared CloudEvents constants used by Light.PortableResults.
/// </summary>
public static class CloudEventsConstants
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
    /// The reserved extension attribute used by Light.PortableResults to classify outcomes.
    /// </summary>
    public const string PortableResultsOutcomeAttributeName = "lproutcome";

    /// <summary>
    /// Gets the set of reserved attribute names that cannot be populated from metadata conversion.
    /// </summary>
    public static FrozenSet<string> ForbiddenConvertedAttributeNames { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "data",
            "data_base64",
            PortableResultsOutcomeAttributeName
        }.ToFrozenSet();

    /// <summary>
    /// Gets the set of standard CloudEvents attribute names.
    /// </summary>
    public static FrozenSet<string> StandardAttributeNames { get; } =
        new HashSet<string>(StringComparer.Ordinal)
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
        }.ToFrozenSet();
}
