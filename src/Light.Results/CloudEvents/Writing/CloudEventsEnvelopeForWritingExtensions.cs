using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Provides helper methods for evaluating metadata serialization behavior on CloudEvents envelopes.
/// </summary>
public static class CloudEventsEnvelopeForWritingExtensions
{
    /// <summary>
    /// Determines whether the envelope's metadata should be written into the CloudEvents <c>data</c> section
    /// when the result is valid.
    /// </summary>
    /// <typeparam name="TEnvelope">
    /// A CloudEvents envelope type that exposes metadata via
    /// <see cref="ICloudEventsEnvelopeForWritingWithMetadata{TResult}" />.
    /// </typeparam>
    /// <typeparam name="TResult">The type of the result associated with the envelope.</typeparam>
    /// <param name="envelope">The envelope whose metadata serialization behavior is being evaluated.</param>
    /// <returns>
    /// <see langword="true" /> if metadata exists and the resolved options require it to be emitted with the
    /// CloudEvents data; otherwise, <see langword="false" />.
    /// </returns>
    public static bool CheckIfMetadataShouldBeWrittenForValidResult<TEnvelope, TResult>(this TEnvelope envelope)
        where TEnvelope : struct, ICloudEventsEnvelopeForWritingWithMetadata<TResult>
        where TResult : struct, IHasOptionalMetadata
    {
        return envelope.ResolvedOptions.MetadataSerializationMode == MetadataSerializationMode.Always &&
               envelope.Data.Metadata.HasValue &&
               envelope.Data.Metadata.Value.HasAnyValuesWithAnnotation(
                   MetadataValueAnnotation.SerializeInCloudEventsData
               );
    }
}
