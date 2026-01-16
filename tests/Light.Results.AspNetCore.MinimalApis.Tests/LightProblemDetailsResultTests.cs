using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.AspNetCore.Shared;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class LightProblemDetailsResultTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var errors = new Errors(new Error { Message = "Not found", Category = ErrorCategory.NotFound });
        var metadata = MetadataObject.Create(("traceId", "abc123"));

        var problemDetails = new LightProblemDetailsResult(
            errors,
            metadata,
            instance: "/api/orders/123"
        );

        problemDetails.Status.Should().Be(404);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Be("The requested resource was not found.");
        problemDetails.Instance.Should().Be("/api/orders/123");
        problemDetails.Errors.Should().Equal(errors);
        problemDetails.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Constructor_WithDefaultErrors_ThrowsArgumentException()
    {
        var errors = default(Errors);

        var action = () => new LightProblemDetailsResult(errors, null);

        action.Should().Throw<ArgumentException>().WithMessage("The errors argument must contain at least one error");
    }

    [Fact]
    public void Constructor_WithUnclassifiedCategory_Returns500()
    {
        var errors = new Errors(new Error { Message = "Something went wrong", Category = ErrorCategory.Unclassified });

        var problemDetails = new LightProblemDetailsResult(errors, null);

        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    [Fact]
    public async Task ExecuteAsync_SetsCorrectContentTypeAndStatusCode()
    {
        var errors = new Errors(new Error { Message = "Bad request", Category = ErrorCategory.Validation });
        var problemDetails = new LightProblemDetailsResult(errors, null);

        var httpContext = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };

        await problemDetails.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ExecuteAsync_WritesValidJson()
    {
        var errors = new Errors(
            new Error { Target = "email", Message = "Your email is invalid", Category = ErrorCategory.Validation }
        );
        var problemDetails = new LightProblemDetailsResult(errors, null);

        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        root.GetProperty("title").GetString().Should().Be("Bad Request");
        root.GetProperty("status").GetInt32().Should().Be(400);
        root.GetProperty("detail").GetString().Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task ExecuteAsync_RichFormat_WritesErrorsAsArray()
    {
        var errors = new Errors(
            new Error[]
            {
                new ()
                {
                    Message = "Name is required", Code = "REQUIRED", Target = "name",
                    Category = ErrorCategory.Validation
                }
            }
        );
        var problemDetails = new LightProblemDetailsResult(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.Rich
        );

        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var errorsArray = doc.RootElement.GetProperty("errors");

        errorsArray.GetArrayLength().Should().Be(1);
        var firstError = errorsArray[0];
        firstError.GetProperty("message").GetString().Should().Be("Name is required");
        firstError.GetProperty("code").GetString().Should().Be("REQUIRED");
        firstError.GetProperty("target").GetString().Should().Be("name");
        firstError.GetProperty("category").GetString().Should().Be("Validation");
    }

    [Fact]
    public async Task ExecuteAsync_AspNetCoreCompatibleFormat_WritesErrorsAsDictionary()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Name is required", Target = "name", Category = ErrorCategory.Validation },
                new () { Message = "Name too short", Target = "name", Category = ErrorCategory.Validation },
                new () { Message = "Invalid email", Target = "email", Category = ErrorCategory.Validation }
            }
        );
        var problemDetails = new LightProblemDetailsResult(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.AspNetCoreCompatible
        );

        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var errorsObj = doc.RootElement.GetProperty("errors");

        errorsObj.GetProperty("name").GetArrayLength().Should().Be(2);
        errorsObj.GetProperty("email").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithMetadata_WritesAsExtensionProperties()
    {
        var errors = new Errors(new Error { Message = "Error", Category = ErrorCategory.InternalError });
        var metadata = MetadataObject.Create(
            ("traceId", "abc123"),
            ("correlationId", "xyz789")
        );
        var problemDetails = new LightProblemDetailsResult(errors, metadata);

        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("traceId").GetString().Should().Be("abc123");
        root.GetProperty("correlationId").GetString().Should().Be("xyz789");
    }

    [Fact]
    public async Task ExecuteAsync_ErrorWithMetadata_WritesMetadataInError()
    {
        var errorMetadata = MetadataObject.Create(("attemptedValue", "not-an-email"));
        var errors = new Errors(
            new Error
            {
                Message = "Invalid email",
                Category = ErrorCategory.Validation,
                Metadata = errorMetadata
            }
        );
        var problemDetails = new LightProblemDetailsResult(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.Rich
        );

        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var firstError = doc.RootElement.GetProperty("errors")[0];
        var metadata = firstError.GetProperty("metadata");

        metadata.GetProperty("attemptedValue").GetString().Should().Be("not-an-email");
    }
}
