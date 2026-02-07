namespace Light.Results.Http;

/// <summary>
/// Specifies how header name conflicts are handled when multiple headers map to the same metadata key.
/// </summary>
public enum HeaderConflictStrategy
{
    /// <summary>
    /// Throw an exception when multiple headers map to the same metadata key.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The last parsed header wins and overwrites previous metadata values.
    /// </summary>
    LastWriteWins = 1
}
