using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.PortableResults.AspNetCore.Shared.Tests;

public sealed class HttpExtensionsTests
{
    [Fact]
    public void SetStatusCodeFromResult_ShouldThrow_WhenResponseIsNull()
    {
        HttpResponse? response = null;
        var result = Result.Ok();

        var act = () => response!.SetStatusCodeFromResult(result);

        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("httpResponse");
    }

    [Fact]
    public void SetStatusCodeFromResult_ShouldSetOk_WhenResultIsSuccessfulAndNoOverrideWasProvided()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok();

        response.SetStatusCodeFromResult(result);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void SetStatusCodeFromResult_ShouldUseProvidedSuccessStatusCode_WhenResultIsSuccessful()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok();

        response.SetStatusCodeFromResult(result, HttpStatusCode.Created);

        response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public void SetStatusCodeFromResult_ShouldUseFirstErrorCategory_WhenConfiguredToDoSo()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Fail(
            new[]
            {
                new Error { Message = "Not Found", Category = ErrorCategory.NotFound },
                new Error { Message = "Conflict", Category = ErrorCategory.Conflict }
            }
        );

        response.SetStatusCodeFromResult(result, firstErrorCategoryIsLeadingCategory: true);

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void SetStatusCodeFromResult_ShouldUseUnclassified_WhenMultipleErrorsHaveDifferentCategories()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Fail(
            new[]
            {
                new Error { Message = "Unauthorized", Category = ErrorCategory.Unauthorized },
                new Error { Message = "Conflict", Category = ErrorCategory.Conflict }
            }
        );

        response.SetStatusCodeFromResult(result, firstErrorCategoryIsLeadingCategory: false);

        response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldThrow_WhenResponseIsNull()
    {
        HttpResponse? response = null;
        var result = Result.Ok();

        var act = () => response!.SetContentTypeFromResult(result, MetadataSerializationMode.Always);

        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("httpResponse");
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldSetProblemJson_WhenResultIsInvalid()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Fail(new Error { Message = "Failure", Category = ErrorCategory.InternalError });

        response.SetContentTypeFromResult(result, MetadataSerializationMode.ErrorsOnly);

        response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldSetJson_WhenSuccessfulResultHasAValue()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result<int>.Ok(42);

        response.SetContentTypeFromResult(result, MetadataSerializationMode.ErrorsOnly);

        response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldLeaveContentTypeUnset_WhenSuccessfulResultHasNoValueAndNoMetadata()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok();

        response.SetContentTypeFromResult(result, MetadataSerializationMode.Always);

        response.ContentType.Should().BeNull();
    }

    [Fact]
    public void
        SetContentTypeFromResult_ShouldLeaveContentTypeUnset_WhenMetadataShouldNotBeSerializedForSuccessResults()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok(
            MetadataObject.Create(
                ("traceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpResponseBody))
            )
        );

        response.SetContentTypeFromResult(result, MetadataSerializationMode.ErrorsOnly);

        response.ContentType.Should().BeNull();
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldLeaveContentTypeUnset_WhenSuccessfulMetadataHasNoBodyAnnotatedValues()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok(
            MetadataObject.Create(
                ("traceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpHeader))
            )
        );

        response.SetContentTypeFromResult(result, MetadataSerializationMode.Always);

        response.ContentType.Should().BeNull();
    }

    [Fact]
    public void SetContentTypeFromResult_ShouldSetJson_WhenSuccessfulMetadataContainsBodyAnnotatedValues()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok(
            MetadataObject.Create(
                ("traceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpHeaderAndBody))
            )
        );

        response.SetContentTypeFromResult(result, MetadataSerializationMode.Always);

        response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void SetMetadataValuesAsHeadersIfNecessary_ShouldThrow_WhenResponseIsNull()
    {
        HttpResponse? response = null;
        var result = Result.Ok();
        var conversionService = new CapturingHttpHeaderConversionService();

        var act = () => response!.SetMetadataValuesAsHeadersIfNecessary(result, conversionService);

        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("httpResponse");
    }

    [Fact]
    public void SetMetadataValuesAsHeadersIfNecessary_ShouldThrow_WhenConversionServiceIsNull()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok();

        var act = () => response.SetMetadataValuesAsHeadersIfNecessary(result, null!);

        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("conversionService");
    }

    [Fact]
    public void SetMetadataValuesAsHeadersIfNecessary_ShouldDoNothing_WhenResultHasNoMetadata()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok();
        var conversionService = new CapturingHttpHeaderConversionService();

        response.SetMetadataValuesAsHeadersIfNecessary(result, conversionService);

        conversionService.PreparedHeaders.Should().BeEmpty();
        response.Headers.Should().BeEmpty();
    }

    [Fact]
    public void SetMetadataValuesAsHeadersIfNecessary_ShouldSkipNullAndBodyOnlyMetadataValues()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok(
            MetadataObject.Create(
                ("nullHeader", MetadataValue.FromNull(MetadataValueAnnotation.SerializeInHttpHeader)),
                ("bodyOnly", MetadataValue.FromString("body", MetadataValueAnnotation.SerializeInHttpResponseBody))
            )
        );
        var conversionService = new CapturingHttpHeaderConversionService();

        response.SetMetadataValuesAsHeadersIfNecessary(result, conversionService);

        conversionService.PreparedHeaders.Should().BeEmpty();
        response.Headers.Should().BeEmpty();
    }

    [Fact]
    public void SetMetadataValuesAsHeadersIfNecessary_ShouldAddHeaders_ForHeaderAnnotatedMetadataValues()
    {
        var response = new DefaultHttpContext().Response;
        var result = Result.Ok(
            MetadataObject.Create(
                ("X-TraceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpHeader)),
                ("bodyOnly", MetadataValue.FromString("body", MetadataValueAnnotation.SerializeInHttpResponseBody))
            )
        );
        var conversionService = new CapturingHttpHeaderConversionService();

        response.SetMetadataValuesAsHeadersIfNecessary(result, conversionService);

        conversionService.PreparedHeaders.Should().ContainSingle();
        response.Headers["X-TraceId"].Should().BeEquivalentTo();
    }

    [Fact]
    public void ResolvePortableResultsHttpWriteOptions_ShouldThrow_WhenHttpContextIsNull()
    {
        HttpContext? httpContext = null;

        Action act = () => httpContext!.ResolvePortableResultsHttpWriteOptions();

        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("httpContext");
    }

    [Fact]
    public void ResolvePortableResultsHttpWriteOptions_ShouldReturnOverride_WhenProvided()
    {
        var httpContext = CreateHttpContext();
        var expectedOptions = new PortableResultsHttpWriteOptions
        {
            MetadataSerializationMode = MetadataSerializationMode.Always
        };

        var actualOptions = httpContext.ResolvePortableResultsHttpWriteOptions(expectedOptions);

        actualOptions.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public void ResolvePortableResultsHttpWriteOptions_ShouldResolveOptionsFromRequestServices()
    {
        var expectedOptions = new PortableResultsHttpWriteOptions
        {
            MetadataSerializationMode = MetadataSerializationMode.Always,
            FirstErrorCategoryIsLeadingCategory = false
        };
        var httpContext = CreateHttpContext(expectedOptions);

        var actualOptions = httpContext.ResolvePortableResultsHttpWriteOptions();

        actualOptions.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public void ResolvePortableResultsHttpWriteOptions_ShouldThrow_WhenNeitherOverrideNorRegisteredOptionsAreAvailable()
    {
        var httpContext = CreateHttpContext();

        Action act = () => httpContext.ResolvePortableResultsHttpWriteOptions();

        act.Should()
           .ThrowExactly<InvalidOperationException>()
           .WithMessage("No PortableResultsHttpWriteOptions are configured in the DI container");
    }

    private static DefaultHttpContext CreateHttpContext(PortableResultsHttpWriteOptions? options = null)
    {
        var services = new ServiceCollection();

        if (options is not null)
        {
            services.AddSingleton(Options.Create(options));
        }

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    private sealed class CapturingHttpHeaderConversionService : IHttpHeaderConversionService
    {
        public List<KeyValuePair<string, StringValues>> PreparedHeaders { get; } = new ();

        public KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue metadataValue)
        {
            metadataValue.TryGetString(out var stringValue).Should().BeTrue();

            var preparedHeader = new KeyValuePair<string, StringValues>(metadataKey, new StringValues(stringValue));
            PreparedHeaders.Add(preparedHeader);
            return preparedHeader;
        }
    }
}
