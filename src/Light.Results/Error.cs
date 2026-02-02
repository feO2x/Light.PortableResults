using System;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// Represents an error with a message, optional code, target, metadata, source, correlation ID, and category.
/// </summary>
public readonly struct Error : IEquatable<Error>
{
    /// <summary>
    /// Gets or initializes the message of the error. This value is required to be set.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains only white space.</exception>
    public required string Message
    {
        get;
        init
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(Message));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{nameof(Message)} cannot be empty or contain only white space",
                    nameof(Message)
                );
            }

            field = value;
        }
    }

    /// <summary>
    /// <para>
    /// Gets or initializes the error code.
    /// </para>
    /// <para>
    /// PLEASE NOTE: although this value is optional by design, it is highly recommended that you
    /// assign all different error types a dedicated error code so that clients can easily point to it.
    /// </para>
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or initializes the target of the error. This value is optional. It usually refers to a key in a JSON object
    /// uses as a Data Transfer Object (DTO) or the name of an HTTP header which is erroneous. Use it to identify an
    /// optional subelement that is the cause of the error.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// <para>
    /// Gets or initializes the category of the error. The default category is <see cref="ErrorCategory.Unclassified" />.
    /// </para>
    /// <para>
    /// PLEASE NOTE: we highly encourage you to set a category for each error. This allows for proper mapping to
    /// a serialized format.
    /// </para>
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Gets or initializes the metadata of the error. This value is optional.
    /// </summary>
    public MetadataObject? Metadata { get; init; }

    /// <summary>
    /// <para>
    /// Gets or initializes the exception that caused this error. This value is optional.
    /// </para>
    /// <para>
    /// Exceptions are never serialized by Light.Results and will not cross process boundaries.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the value indicating whether this instance is the default instance. This
    /// usually happens when the 'default' keyword is used: <c>Error error = default;</c>.
    /// </summary>
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public bool IsDefaultInstance => Message is null;

    /// <summary>
    /// Checks if this instance is equal to another instance by comparing all properties, including
    /// <see cref="Metadata" />.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns><c>true</c> if this instance is equal to <paramref name="other" />; otherwise, <c>false</c>.</returns>
    public bool Equals(Error other) => Equals(other, compareMetadata: true);

    /// <summary>
    /// Checks if this instance is equal to another instance by comparing all properties. You can decide
    /// whether to compare the <see cref="Metadata" /> property.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <param name="compareMetadata">The value indicating whether to compare the <see cref="Metadata" /> property.</param>
    /// <returns><c>true</c> if this instance is equal to <paramref name="other" />; otherwise, <c>false</c>.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool Equals(Error other, bool compareMetadata)
    {
        return Message.Equals(other.Message, StringComparison.Ordinal) &&
               string.Equals(Code, other.Code, StringComparison.Ordinal) &&
               string.Equals(Target, other.Target, StringComparison.Ordinal) &&
               Category == other.Category &&
               ReferenceEquals(Exception, other.Exception) &&
               (!compareMetadata || Metadata == other.Metadata);
    }

    /// <summary>
    /// Check if this instance is equal to another instance by comparing all properties, including
    /// <see cref="Metadata" />.
    /// </summary>
    /// <param name="obj">The other object to compare to.</param>
    /// <returns><c>true</c> if this instance is equal to <paramref name="obj" />; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    /// <summary>
    /// Gets the hash code of this instance.
    /// </summary>
    /// <returns>The hash code of this instance.</returns>
    public override int GetHashCode() => GetHashCode(includeMetadata: true);

    /// <summary>
    /// Gets the hash code of this instance.
    /// </summary>
    /// <param name="includeMetadata">
    /// The value indicating whether to include the <see cref="Metadata" /> property in the hash code.
    /// </param>
    /// <returns>The hash code of this instance.</returns>
    // ReSharper disable once MemberCanBePrivate.Global -- public API
    public int GetHashCode(bool includeMetadata)
    {
        var hashCode = new HashCode();
        hashCode.Add(Message);
        hashCode.Add(Code);
        hashCode.Add(Target);
        hashCode.Add(Category);
        hashCode.Add(Exception);
        if (includeMetadata)
        {
            hashCode.Add(Metadata);
        }

        return hashCode.ToHashCode();
    }
}
