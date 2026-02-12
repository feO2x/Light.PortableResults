using System.Linq;
using FluentAssertions;
using Light.Results.AspNetCore.Shared;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class LightResultEndpointExtensionsTests
{
    [Fact]
    public void ProducesLightResult_ShouldRegisterWrappedResponseMetadata()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var routeBuilder = app.MapGet("/test", () => "ok");
        var returned = routeBuilder.ProducesLightResult<ContactDto>();

        returned.Should().BeSameAs(routeBuilder);

        var endpointRouteBuilder = (IEndpointRouteBuilder) app;
        var endpoint = endpointRouteBuilder.DataSources.Single().Endpoints.OfType<RouteEndpoint>().Single();
        var metadataEntries = endpoint.Metadata
           .Where(item => item.GetType().Name == "ProducesResponseTypeMetadata")
           .ToArray();

        metadataEntries.Should().NotBeEmpty();
        metadataEntries
           .Select(entry => entry.GetType().GetProperty("Type")?.GetValue(entry))
           .Should()
           .Contain(typeof(WrappedResponse<ContactDto, object>));
    }

    [Fact]
    public void ProducesLightResultWithMetadataType_ShouldRegisterWrappedResponseMetadata()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var routeBuilder = app.MapGet("/test-metadata", () => "ok");
        var returned = routeBuilder.ProducesLightResult<ContactDto, MetadataObject>();

        returned.Should().BeSameAs(routeBuilder);

        var endpointRouteBuilder = (IEndpointRouteBuilder) app;
        var endpoint = endpointRouteBuilder.DataSources.Single().Endpoints.OfType<RouteEndpoint>().Single();
        var metadataEntries = endpoint.Metadata
           .Where(item => item.GetType().Name == "ProducesResponseTypeMetadata")
           .ToArray();

        metadataEntries.Should().NotBeEmpty();
        metadataEntries
           .Select(entry => entry.GetType().GetProperty("Type")?.GetValue(entry))
           .Should()
           .Contain(typeof(WrappedResponse<ContactDto, MetadataObject>));
    }
}
