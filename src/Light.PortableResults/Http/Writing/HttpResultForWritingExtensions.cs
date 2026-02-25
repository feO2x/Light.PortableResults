namespace Light.PortableResults.Http.Writing;

/// <summary>
/// Provides extension methods for constructing <see cref="HttpResultForWriting" /> and
/// <see cref="HttpResultForWriting{T}" /> wrapper structs.
/// </summary>
public static class HttpResultForWritingExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpResultForWriting" /> wrapper from the result and options.
    /// </summary>
    /// <param name="result">The result to wrap.</param>
    /// <param name="options">The mutable options to freeze into the wrapper.</param>
    /// <returns>The wrapper struct ready for JSON serialization.</returns>
    public static HttpResultForWriting ToHttpResultForWriting(
        this Result result,
        PortableResultsHttpWriteOptions options
    ) =>
        new (result, options.ToResolvedHttpWriteOptions());

    /// <summary>
    /// Creates an <see cref="HttpResultForWriting{T}" /> wrapper from the result and options.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to wrap.</param>
    /// <param name="options">The mutable options to freeze into the wrapper.</param>
    /// <returns>The wrapper struct ready for JSON serialization.</returns>
    public static HttpResultForWriting<T> ToHttpResultForWriting<T>(
        this Result<T> result,
        PortableResultsHttpWriteOptions options
    ) =>
        new (result, options.ToResolvedHttpWriteOptions());

    /// <summary>
    /// Creates an <see cref="HttpResultForWriting" /> wrapper from the result and already-resolved options.
    /// </summary>
    /// <param name="result">The result to wrap.</param>
    /// <param name="resolvedOptions">The already-frozen options.</param>
    /// <returns>The wrapper struct ready for JSON serialization.</returns>
    public static HttpResultForWriting ToHttpResultForWriting(
        this Result result,
        ResolvedHttpWriteOptions resolvedOptions
    ) =>
        new (result, resolvedOptions);

    /// <summary>
    /// Creates an <see cref="HttpResultForWriting{T}" /> wrapper from the result and already-resolved options.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to wrap.</param>
    /// <param name="resolvedOptions">The already-frozen options.</param>
    /// <returns>The wrapper struct ready for JSON serialization.</returns>
    public static HttpResultForWriting<T> ToHttpResultForWriting<T>(
        this Result<T> result,
        ResolvedHttpWriteOptions resolvedOptions
    ) =>
        new (result, resolvedOptions);
}
