namespace Light.Results.Metadata;

/// <summary>
/// Specifies how metadata keys should be handled when merging two <see cref="MetadataObject" /> instances.
/// </summary>
public enum MetadataMergeStrategy
{
    /// <summary>
    /// Keys from the incoming metadata overwrite existing keys.
    /// For nested objects, merge recursively. Arrays are replaced wholesale.
    /// </summary>
    AddOrReplace = 0,

    /// <summary>
    /// Keep original values, ignore duplicates from incoming metadata.
    /// </summary>
    PreserveExisting = 1,

    /// <summary>
    /// Throw if the same key is present in both sources.
    /// </summary>
    FailOnConflict = 2
}
