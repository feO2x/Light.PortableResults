using System;

namespace Light.Results;

/// <summary>
/// Provides extension methods for <see cref="ErrorCategory" /> and <see cref="Errors" />.
/// </summary>
public static class ErrorCategoryExtensions
{
    /// <summary>
    /// Determines the leading error category from a collection of errors.
    /// </summary>
    /// <param name="errors">The errors collection (must contain at least one error).</param>
    /// <param name="firstCategoryIsLeadingCategory">
    /// If true, returns the category of the first error.
    /// If false, returns the common category if all errors share it, otherwise Unclassified.
    /// </param>
    /// <returns>The leading error category.</returns>
    /// <exception cref="InvalidOperationException">Thrown when errors is empty.</exception>
    public static ErrorCategory GetLeadingCategory(
        this Errors errors,
        bool firstCategoryIsLeadingCategory = false
    )
    {
        if (errors.IsDefaultInstance)
        {
            throw new InvalidOperationException("Errors collection must contain at least one error.");
        }

        if (firstCategoryIsLeadingCategory)
        {
            return errors.First.Category;
        }

        var firstCategory = errors.First.Category;
        foreach (var error in errors)
        {
            if (error.Category != firstCategory)
            {
                return ErrorCategory.Unclassified;
            }
        }

        return firstCategory;
    }

    /// <summary>
    /// Converts an ErrorCategory to its corresponding HTTP status code.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <returns>The HTTP status code.</returns>
    public static int ToHttpStatusCode(this ErrorCategory category)
    {
        return category == ErrorCategory.Unclassified ? 500 : (int) category;
    }
}
