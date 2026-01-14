using Light.Results.Metadata;

namespace Light.Results.MetadataExtensions;

/// <summary>
/// Provides extension methods for tracing metadata on result types.
/// </summary>
public static class Tracing
{
    /// <summary>
    /// The key used to identify the source in metadata.
    /// </summary>
    public const string SourceKey = "source";

    /// <summary>
    /// The key used to identify the correlation ID in metadata.
    /// </summary>
    public const string CorrelationIdKey = "correlationId";

    /// <param name="result">The result to add source metadata to.</param>
    /// <typeparam name="T">The type implementing <see cref="IHasOptionalMetadata{T}" />.</typeparam>
    extension<T>(T result) where T : struct, IHasOptionalMetadata<T>
    {
        /// <summary>
        /// Returns a new result with the specified source added to metadata.
        /// </summary>
        /// <param name="source">The source identifier (e.g., service name).</param>
        /// <returns>A new result with the source metadata.</returns>
        public T WithSource(string source) => result.MergeMetadata((SourceKey, source));

        /// <summary>
        /// Returns a new result with the specified correlation ID added to metadata.
        /// </summary>
        /// <param name="correlationId">The correlation ID (e.g., trace ID, request ID).</param>
        /// <returns>A new result with the correlation ID metadata.</returns>
        public T WithCorrelationId(string correlationId) => result.MergeMetadata((CorrelationIdKey, correlationId));

        /// <summary>
        /// Returns a new result with the specified tracing metadata added.
        /// </summary>
        /// <param name="source">The source identifier (e.g., service name).</param>
        /// <param name="correlationId">The correlation ID (e.g., trace ID, request ID).</param>
        /// <returns>A new result with the tracing metadata.</returns>
        public T WithTracing(
            string source,
            string correlationId
        )
        {
            return result.MergeMetadata((SourceKey, source), (CorrelationIdKey, correlationId));
        }

        /// <summary>
        /// Attempts to retrieve the source identifier from the result's metadata.
        /// </summary>
        /// <param name="source">The source identifier if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the source was found; otherwise, <c>false</c>.</returns>
        public bool TryGetSource(out string? source)
        {
            if (result.Metadata?.TryGetString(SourceKey, out source) == true)
            {
                return true;
            }

            source = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the correlation ID from the result's metadata.
        /// </summary>
        /// <param name="correlationId">The correlation ID if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the correlation ID was found; otherwise, <c>false</c>.</returns>
        public bool TryGetCorrelationId(out string? correlationId)
        {
            if (result.Metadata?.TryGetString(CorrelationIdKey, out correlationId) == true)
            {
                return true;
            }

            correlationId = null;
            return false;
        }
    }
}
