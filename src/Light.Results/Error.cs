using System;
using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// Represents an error with a message, optional code, target, metadata, source, correlation ID, and category.
/// </summary>
public readonly record struct Error
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
    /// Gets the value indicating whether this instance is the default instance. This
    /// usually happens when the 'default' keyword is used: <c>Error error = default;</c>.
    /// </summary>
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public bool IsDefaultInstance => Message is null;
}
