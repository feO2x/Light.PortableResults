namespace Light.PortableResults.Validation.Assertions;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    // public static Check<T> IsNotNull<T>(this Check<T> check)
    // {
    //     if (check.IsShortCircuited || !check.IsValueNull)
    //     {
    //         return check;
    //     }
    //
    //     return check.AddError(check.Context.ErrorTemplates.NotNull);
    // }
}
