using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

internal static class ValidationOpenApiDocumentTestUtilities
{
    internal static WebApplication CreateApp(
        Action<PortableErrorMetadataContractsBuilder> configureContracts,
        Action<WebApplication> configureEndpoints,
        Action<OpenApiOptions>? configureOpenApi = null
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddPortableResultsOpenApi();
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options => options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
        );
        builder.Services.ConfigureErrorMetadataContracts(configureContracts);
        builder.Services.AddOpenApi(options => configureOpenApi?.Invoke(options));

        var app = builder.Build();
        configureEndpoints(app);
        return app;
    }

    internal static async Task<OpenApiDocument> GetOpenApiDocumentAsync(WebApplication app)
    {
        await app.StartAsync(TestContext.Current.CancellationToken);
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        return await provider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
    }

    internal static OpenApiSchema GetProblemItems(OpenApiDocument document, string path)
    {
        var response = (OpenApiResponse) document.Paths[path]
           .Operations![HttpMethod.Get]
           .Responses![StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture)];
        var schema = (OpenApiSchemaReference) response.Content!["application/problem+json"].Schema!;
        var component = GetSchemaComponent(document, GetSchemaReferenceId(schema));
        var extension = (OpenApiSchema) component.AllOf![1];
        var propertyName = extension.Properties!.ContainsKey("errorDetails") ? "errorDetails" : "errors";
        return (OpenApiSchema) ((OpenApiSchema) extension.Properties[propertyName]).Items!;
    }

    internal static OpenApiSchema GetSchemaComponent(OpenApiDocument document, string schemaId)
    {
        return (OpenApiSchema) document.Components!.Schemas![schemaId];
    }

    internal static string GetSchemaReferenceId(OpenApiSchemaReference schemaReference)
    {
        var referenceId = schemaReference.Reference.Id ?? schemaReference.Id;
        referenceId.Should().NotBeNull();
        return referenceId;
    }

    internal static bool SchemaIncludesType(OpenApiSchema schema, JsonSchemaType type)
    {
        return schema.Type.HasValue && (schema.Type.Value & type) == type;
    }
}
