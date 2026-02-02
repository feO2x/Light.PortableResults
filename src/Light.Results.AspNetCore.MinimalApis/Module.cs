using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.AspNetCore.Shared;
using Light.Results.AspNetCore.Shared.Serialization;
using Light.Results.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.Results.AspNetCore.MinimalApis;

public static class Module
{
    public static IServiceCollection AddLightResultsForMinimalApis(this IServiceCollection services) =>
        services
           .AddLightResultOptions()
           .AddLightResultsHttpHeaderConversionService()
           .ConfigureMinimalApiJsonOptionsForLightResults();

    public static IServiceCollection AddLightResultOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultOptions>>().Value);
        return services;
    }

    public static IServiceCollection ConfigureMinimalApiJsonOptionsForLightResults(this IServiceCollection services)
    {
        services
           .AddOptions<JsonOptions>()
           .Configure<LightResultOptions>(
                (jsonOptions, lightResultOptions) =>
                {
                    jsonOptions.SerializerOptions.AddDefaultLightResultsJsonConverters(lightResultOptions);
                }
            );
        return services;
    }

    public static void AddDefaultLightResultsJsonConverters(
        this JsonSerializerOptions serializerOptions,
        LightResultOptions options
    )
    {
        serializerOptions.Converters.Add(new MetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new MetadataValueJsonConverter());
        serializerOptions.Converters.Add(new DefaultResultJsonConverter(options));
        serializerOptions.Converters.Add(new DefaultResultJsonConverterFactory(options));
    }

    public static IServiceCollection AddLightResultsHttpHeaderConversionService(
        this IServiceCollection services,
        IEqualityComparer<string>? metadataKeyComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, HttpHeaderConverter>>(
            sp =>
            {
                var httpHeaderConverters = sp.GetServices<HttpHeaderConverter>();
                var dictionary = new Dictionary<string, HttpHeaderConverter>(metadataKeyComparer);
                foreach (var converter in httpHeaderConverters)
                {
                    foreach (var supportedKey in converter.SupportedMetadataKeys)
                    {
                        if (dictionary.TryAdd(supportedKey, converter))
                        {
                            continue;
                        }

                        var existingConverter = dictionary[supportedKey];
                        throw new InvalidOperationException(
                            $"Cannot add '{converter}' to frozen dictionary because key '{supportedKey}' is already registered by '{existingConverter}'"
                        );
                    }
                }

                return metadataKeyComparer is null ?
                    dictionary.ToFrozenDictionary() :
                    dictionary.ToFrozenDictionary(metadataKeyComparer);
            }
        );
        services.AddSingleton<IHttpHeaderConversionService, DefaultHttpHeaderConversionService>();

        return services;
    }
}
