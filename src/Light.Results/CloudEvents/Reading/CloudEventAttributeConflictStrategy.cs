namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Specifies how conflicts are handled when multiple CloudEvent attributes map to the same metadata key.
/// </summary>
public enum CloudEventAttributeConflictStrategy
{
    /// <summary>
    /// Throw an <see cref="System.InvalidOperationException" /> when conflicts occur.
    /// </summary>
    Throw,

    /// <summary>
    /// Use the most recently parsed value for conflicting keys.
    /// </summary>
    LastWriteWins
}
