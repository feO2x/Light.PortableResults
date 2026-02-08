namespace Light.Results.Http.Reading;

/// <summary>
/// Specifies which HTTP headers should be read into result metadata.
/// </summary>
public enum HeaderSelectionMode
{
    /// <summary>
    /// Do not read any headers.
    /// </summary>
    None = 0,

    /// <summary>
    /// Read all headers.
    /// </summary>
    All = 1,

    /// <summary>
    /// Read only headers from the allow list.
    /// </summary>
    AllowList = 2,

    /// <summary>
    /// Read all headers except those in the deny list.
    /// </summary>
    DenyList = 3
}
