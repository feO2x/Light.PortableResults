namespace Light.Results;

/// <summary>
/// Result interface that enables fluent extension methods returning the same type.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface IResult<TSelf>
    where TSelf : struct, IResult<TSelf>
{
    /// <summary>
    /// Gets whether this result represents a successful operation.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the errors collection (empty on success).
    /// </summary>
    Errors Errors { get; }

    /// <summary>
    /// Gets the first error (throws if no errors).
    /// </summary>
    Error FirstError { get; }
}
