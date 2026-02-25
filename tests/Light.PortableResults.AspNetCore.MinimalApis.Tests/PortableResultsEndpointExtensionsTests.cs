using System.Linq;
using FluentAssertions;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

public sealed class PortableResultsEndpointExtensionsTests
{
    [Fact]
    public void ProducesPortableResult_ShouldRegisterWrappedResponseMetadata()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var routeBuilder = app.MapGet("/test", () => "ok");
        var returned = routeBuilder.ProducesPortableResult<ContactDto>();

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
    public void ProducesPortableResultWithMetadataType_ShouldRegisterWrappedResponseMetadata()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var routeBuilder = app.MapGet("/test-metadata", () => "ok");
        var returned = routeBuilder.ProducesPortableResult<ContactDto, MetadataObject>();

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
