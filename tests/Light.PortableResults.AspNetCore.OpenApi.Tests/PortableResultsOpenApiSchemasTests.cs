using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableResultsOpenApiSchemasTests
{
    [Fact]
    public void InstallInto_ShouldAddTheCanonicalSchemaCatalog()
    {
        var document = new OpenApiDocument();

        PortableResultsOpenApiSchemas.InstallInto(document);

        document.Components.Should().NotBeNull();
        document.Components!.Schemas.Should().NotBeNull();
        document.Components.Schemas.Keys.Should().BeEquivalentTo(
            new HashSet<string>
            {
                "ErrorCategory",
                "PortableError",
                "PortableValidationErrorDetail",
                "PortableProblemDetails",
                "PortableRichValidationProblemDetails",
                "PortableAspNetCoreValidationProblemDetails"
            }
        );

        var portableError =
            (OpenApiSchema) document.Components.Schemas[PortableResultsOpenApiSchemas.PortableErrorSchemaId];
        var metadataSchema = (OpenApiSchema) portableError.Properties!["metadata"];
        metadataSchema.Type.Should().Be(JsonSchemaType.Object | JsonSchemaType.Null);
        metadataSchema.AdditionalPropertiesAllowed.Should().BeTrue();
    }

    [Fact]
    public void PropertyFactories_ShouldReturnFreshCopies()
    {
        var document = new OpenApiDocument();

        var firstProblemProperties = PortableResultsOpenApiSchemas.CreatePortableProblemDetailsProperties(document);
        var secondProblemProperties = PortableResultsOpenApiSchemas.CreatePortableProblemDetailsProperties(document);
        var aspNetCoreProperties =
            PortableResultsOpenApiSchemas.CreatePortableAspNetCoreValidationProblemDetailsProperties(document);
        var firstProblemRequired = PortableResultsOpenApiSchemas.CreatePortableProblemDetailsRequired();
        var secondProblemRequired = PortableResultsOpenApiSchemas.CreatePortableProblemDetailsRequired();
        var aspNetCoreRequired =
            PortableResultsOpenApiSchemas.CreatePortableAspNetCoreValidationProblemDetailsRequired();
        var secondAspNetCoreRequired =
            PortableResultsOpenApiSchemas.CreatePortableAspNetCoreValidationProblemDetailsRequired();

        firstProblemProperties.Should().NotBeSameAs(secondProblemProperties);
        firstProblemProperties.Should().ContainKeys(
            "type",
            "title",
            "status",
            "detail",
            "instance",
            "errors",
            "metadata"
        );
        aspNetCoreProperties.Should().ContainKeys(
            "type",
            "title",
            "status",
            "detail",
            "instance",
            "errors",
            "errorDetails",
            "metadata"
        );
        firstProblemRequired.Should().BeEquivalentTo("type", "title", "status", "errors");
        aspNetCoreRequired.Should().BeEquivalentTo("type", "title", "status", "errors");

        firstProblemProperties.Remove("metadata");
        secondProblemProperties.Should().ContainKey("metadata");
        firstProblemRequired.Remove("errors");
        secondProblemRequired.Should().Contain("errors");
        aspNetCoreRequired.Remove("errors");
        secondAspNetCoreRequired.Should().Contain("errors");
    }
}
