using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Disables automatic null-validation errors.
/// </summary>
public sealed class NoOpAutomaticNullErrorProvider : IAutomaticNullErrorProvider
{
    private NoOpAutomaticNullErrorProvider() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static NoOpAutomaticNullErrorProvider Instance { get; } = new ();

    /// <inheritdoc />
    public bool TryCreateError<T>(in ValidationErrorMessageContext<T> context, out Error error)
    {
        error = default;
        return false;
    }
}
