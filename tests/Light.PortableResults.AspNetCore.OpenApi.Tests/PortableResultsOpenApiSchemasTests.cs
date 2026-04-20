using System.Collections.Generic;
using FluentAssertions;
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
    }
}
