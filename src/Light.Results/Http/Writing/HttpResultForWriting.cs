namespace Light.Results.Http.Writing;

/// <summary>
/// Wraps a <see cref="Result" /> together with frozen <see cref="ResolvedHttpWriteOptions" />
/// for serialization through the standard System.Text.Json pipeline.
/// </summary>
/// <param name="Data">The result to serialize.</param>
/// <param name="ResolvedOptions">The frozen serialization options.</param>
public readonly record struct HttpResultForWriting(
    Result Data,
    ResolvedHttpWriteOptions ResolvedOptions
);

/// <summary>
/// Wraps a <see cref="Result{T}" /> together with frozen <see cref="ResolvedHttpWriteOptions" />
/// for serialization through the standard System.Text.Json pipeline.
/// </summary>
/// <typeparam name="T">The type of the success value in the result.</typeparam>
/// <param name="Data">The typed result to serialize.</param>
/// <param name="ResolvedOptions">The frozen serialization options.</param>
public readonly record struct HttpResultForWriting<T>(
    Result<T> Data,
    ResolvedHttpWriteOptions ResolvedOptions
);
