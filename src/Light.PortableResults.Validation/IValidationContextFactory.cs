namespace Light.PortableResults.Validation;

/// <summary>
/// Creates root validation contexts.
/// </summary>
public interface IValidationContextFactory
{
    /// <summary>
    /// Creates a new root validation context.
    /// </summary>
    ValidationContext CreateValidationContext();
}
