namespace Light.PortableResults.Metadata;

/// <summary>
/// Provides extensions for <see cref="MetadataValue" />.
/// </summary>
public static class MetadataValueExtensions
{
    /// <summary>
    /// Determines whether the provided <paramref name="value" /> contains the specified <paramref name="annotation" /> flag.
    /// </summary>
    /// <param name="value">The metadata value whose annotations are inspected.</param>
    /// <param name="annotation">The annotation flag to look for.</param>
    /// <returns><c>true</c> if the metadata value has the annotation; otherwise, <c>false</c>.</returns>
    public static bool HasAnnotation(this MetadataValue value, MetadataValueAnnotation annotation) =>
        (value.Annotation & annotation) == annotation;
}
