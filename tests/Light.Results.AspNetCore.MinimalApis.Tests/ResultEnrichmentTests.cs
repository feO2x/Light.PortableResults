using FluentAssertions;
using Light.Results.AspNetCore.Shared.Enrichment;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class ResultEnrichmentTests
{
    [Fact]
    public void EnricherRegistered_EnrichesFailedResult()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultEnricher, TestEnricher>();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var result = Result<string>.Fail(new Error { Message = "Error", Category = ErrorCategory.InternalError });

        var apiResult = result.ToMinimalApiResult(httpContext: httpContext);

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
        var problemDetails = (LightProblemDetailsResult) apiResult;
        problemDetails.Metadata.Should().NotBeNull();
        problemDetails.Metadata!.Value.TryGetString("enriched", out var enrichedValue);
        enrichedValue.Should().Be("true");
    }

    [Fact]
    public void NoEnricherRegistered_ReturnsOriginalResult()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var result = Result<string>.Fail(new Error { Message = "Error", Category = ErrorCategory.InternalError });

        var apiResult = result.ToMinimalApiResult(httpContext: httpContext);

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
        var problemDetails = (LightProblemDetailsResult) apiResult;
        problemDetails.Metadata.Should().BeNull();
    }

    [Fact]
    public void NullHttpContext_SkipsEnrichment()
    {
        var result = Result<string>.Fail(new Error { Message = "Error", Category = ErrorCategory.InternalError });

        var apiResult = result.ToMinimalApiResult(httpContext: null);

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
    }

    private sealed class TestEnricher : IHttpResultEnricher
    {
        public Result<T> Enrich<T>(Result<T> result, HttpContext httpContext)
        {
            return result.IsValid ? result : result.MergeMetadata(("enriched", "true"));
        }

        public Result Enrich(Result result, HttpContext httpContext)
        {
            return result.IsValid ? result : result.MergeMetadata(("enriched", "true"));
        }
    }
}
