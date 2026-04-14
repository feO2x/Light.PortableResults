namespace Light.PortableResults.Validation.ConfigurationIntegration;

/// <summary>
/// Defines constants used in the configuration integration.
/// </summary>
public static class ConfigurationConstants
{
    /// <summary>
    /// The well-known key used to store the options name in the validation context.
    /// </summary>
    public static readonly ValidationContextKey<string?> OptionsNameKey = new ("OptionsName");
}
