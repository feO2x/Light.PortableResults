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
    /// Ensures that the specified result is not the default instance of its struct type.
    /// </para>
    /// <para>
    /// A default instance is neither a success nor a failure: it reports <see cref="IResultObject.IsValid" /> as
    /// <see langword="false" /> while its <see cref="IResultObject.Errors" /> collection is empty. Such an instance
    /// carries no information that could be written to a transport, thus every write boundary of this library
    /// rejects it up front. <c>default(Result&lt;T>)</c> takes this shape whenever <c>T</c> is a reference type or
    /// a nullable value type.
    /// </para>
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="parameterName">
    /// The name of the parameter the result was passed to (optional). This value is automatically set to the
    /// caller expression of <paramref name="result" /> when you do not specify it.
    /// </param>
    /// <typeparam name="TResult">The concrete result struct implementing <see cref="IResultObject" />.</typeparam>
    /// <returns>The unchanged <paramref name="result" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="result" /> is the default instance.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult MustNotBeDefaultInstance<TResult>(
        this TResult result,
        [CallerArgumentExpression("result")] string? parameterName = null
    )
        where TResult : struct, IResultObject
    {
        // No result created via Result.Ok or Result.Fail can be invalid and carry no errors at the same time,
        // thus this condition identifies the default instance exactly.
        if (result is { IsValid: false, Errors.Count: 0 })
        {
            ThrowDefaultInstance(parameterName);
        }

        return result;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultInstance(string? parameterName) =>
        throw new ArgumentException(
            "The result is the default instance which is neither a success nor a failure and thus cannot be " +
            "written. Create results with Result.Ok or Result.Fail.",
            parameterName
        );
}
