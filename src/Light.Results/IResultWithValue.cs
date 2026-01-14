namespace Light.Results;

/// <summary>
/// Result interface for types that carry a success value.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public interface IResultWithValue<TSelf, out TValue> : IResult<TSelf>
    where TSelf : struct, IResultWithValue<TSelf, TValue>, IResult<TSelf>
{
    /// <summary>
    /// Gets the success value (throws if invalid).
    /// </summary>
    TValue Value { get; }
}
