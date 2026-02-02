using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// <para>
/// Result interface that enables fluent extension methods returning the same type.
/// </para>
/// <para>
/// This interface is implemented by structs and should primarily be used to constrain generic parameters.
/// </para>
/// </summary>
public interface IResultObject : IHasOptionalMetadata
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
    /// Gets the value indicating whether this result has a value.
    /// </summary>
    bool HasValue { get; }
}

/// <summary>
/// <para>
/// Result interface for types that carry a success value.
/// </para>
/// </summary>
/// <para>
/// This interface is implemented by structs and should primarily be used to constrain generic parameters.
/// </para>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public interface IResultObject<out TValue> : IResultObject
{
    /// <summary>
    /// Gets the success value (throws if invalid).
    /// </summary>
    TValue Value { get; }
}
