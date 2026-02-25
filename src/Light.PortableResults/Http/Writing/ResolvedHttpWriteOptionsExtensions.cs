namespace Light.PortableResults.Http.Writing;

/// <summary>
/// Provides extension methods for converting <see cref="PortableResultsHttpWriteOptions" /> to
/// <see cref="ResolvedHttpWriteOptions" />.
/// </summary>
public static class ResolvedHttpWriteOptionsExtensions
{
    /// <summary>
    /// Creates a frozen <see cref="ResolvedHttpWriteOptions" /> snapshot from the mutable options instance.
    /// This is the single conversion point used by the ASP.NET Core integration layers.
    /// </summary>
    /// <param name="options">The mutable options to freeze.</param>
    /// <returns>A frozen snapshot of the options.</returns>
    public static ResolvedHttpWriteOptions ToResolvedHttpWriteOptions(this PortableResultsHttpWriteOptions options) =>
        new (
            options.ValidationProblemSerializationFormat,
            options.MetadataSerializationMode,
            options.CreateProblemDetailsInfo,
            options.FirstErrorCategoryIsLeadingCategory
        );
}
