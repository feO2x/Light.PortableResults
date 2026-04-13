using Light.PortableResults.AspNetCore.MinimalApis.Serialization;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.JsonSerialization;

public static class JsonSerializationModule
{
    public static IServiceCollection ConfigureJsonSerialization(this IServiceCollection services) =>
        services
           .Configure<JsonOptions>(
                options =>
                {
                    options.SerializerOptions.TypeInfoResolverChain.Insert(0, MovieRatingJsonContext.Default);
                    options.SerializerOptions.TypeInfoResolverChain.Insert(
                        1,
                        PortableResultsMinimalApiJsonContext.Default
                    );
                    // Register the specific converter for MovieRating so that in NativeAOT
                    // the factory's MakeGenericType is never called. Converters are checked
                    // in reverse order (last-added wins), so this overrides the factory.
                    // options.SerializerOptions.AddHttpResultForWritingConverter<MovieRating>();
                }
            )
           .Configure<PortableResultsHttpWriteOptions>(
                x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
            );
}
