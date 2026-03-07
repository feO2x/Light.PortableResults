using System;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Provides a polyfill for <see cref="CallerArgumentExpressionAttribute" /> on target frameworks that do not define it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CallerArgumentExpressionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="CallerArgumentExpressionAttribute" />.
    /// </summary>
    /// <param name="parameterName">The name of the parameter whose expression should be captured.</param>
    public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

    /// <summary>
    /// Gets the name of the parameter whose expression should be captured.
    /// </summary>
    public string ParameterName { get; }
}
