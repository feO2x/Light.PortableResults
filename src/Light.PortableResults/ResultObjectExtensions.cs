using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Light.PortableResults;

/// <summary>
/// Provides guard clauses for types implementing <see cref="IResultObject" />.
/// </summary>
public static class ResultObjectExtensions
{
    /// <summary>
    /// <para>
    /// Ensures that the specified result represents either a success or a failure.
    /// </para>
    /// <para>
    /// A result that reports <see cref="IResultObject.IsValid" /> as <see langword="false" /> while its
    /// <see cref="IResultObject.Errors" /> collection is empty is neither a success nor a failure and carries no
    /// information that could be written to a transport. Every write boundary of this library rejects that state
    /// up front. <c>default(Result&lt;T>)</c> takes this shape whenever <c>T</c> is a reference type or a nullable
    /// value type; default results with non-nullable value types, including <c>default(Result)</c>, are successes.
    /// </para>
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="parameterName">
    /// The name of the parameter the result was passed to (optional). This value is automatically set to the
    /// caller expression of <paramref name="result" /> when you do not specify it.
    /// </param>
    /// <typeparam name="TResult">The concrete result struct implementing <see cref="IResultObject" />.</typeparam>
    /// <returns>The unchanged <paramref name="result" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="result" /> is invalid while carrying no errors.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult MustNotBeDefaultInstance<TResult>(
        this TResult result,
        [CallerArgumentExpression("result")] string? parameterName = null
    )
        where TResult : struct, IResultObject
    {
        // No built-in result created via Result.Ok or Result.Fail can be invalid and carry no errors at the same
        // time. Custom IResultObject implementations must uphold the same invariant at the write boundaries.
        if (result is { IsValid: false, Errors.Count: 0 })
        {
            ThrowInvalidWithoutErrors(parameterName);
        }

        return result;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidWithoutErrors(string? parameterName) =>
        throw new ArgumentException(
            "The result is invalid while carrying no errors and thus cannot be written. This usually indicates " +
            "the default instance. Create results with Result.Ok or Result.Fail.",
            parameterName
        );
}
