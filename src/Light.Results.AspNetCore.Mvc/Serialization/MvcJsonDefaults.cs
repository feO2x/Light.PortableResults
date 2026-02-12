using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.Results.AspNetCore.Mvc.Serialization;

/// <summary>
/// Provides helper APIs for retrieving consistent <see cref="JsonSerializerOptions" />
/// instances within ASP.NET Core MVC applications.
/// </summary>
public static class MvcJsonDefaults
{
    /// <summary>
    /// Resolves the <see cref="JsonSerializerOptions" /> to use, optionally honoring
    /// explicitly provided overrides before falling back to the application's
    /// configured <see cref="JsonOptions" />.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="JsonOptions" />.</param>
    /// <param name="overrideOptions">An optional set of serializer options that take precedence when supplied.</param>
    /// <returns>The serializer options determined by the provided arguments and configured defaults.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <see cref="JsonOptions" /> instance cannot be resolved from the service provider.
    /// </exception>
    public static JsonSerializerOptions ResolveMvcJsonSerializerOptions(
        this IServiceProvider serviceProvider,
        JsonSerializerOptions? overrideOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return overrideOptions ??
               serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }
}
