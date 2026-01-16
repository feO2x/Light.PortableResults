using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.AspNetCore.Abstractions;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class ErrorSerializationFormatTests
{
    [Fact]
    public async Task AspNetCoreCompatible_GroupsErrorsByTarget()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Name is required", Target = "name", Category = ErrorCategory.Validation },
                new () { Message = "Name too short", Target = "name", Category = ErrorCategory.Validation },
                new () { Message = "Invalid email", Target = "email", Category = ErrorCategory.Validation }
            }
        );
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.AspNetCoreCompatible
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);
        var errorsObj = doc.RootElement.GetProperty("errors");

        errorsObj.ValueKind.Should().Be(JsonValueKind.Object);
        errorsObj.GetProperty("name").GetArrayLength().Should().Be(2);
        errorsObj.GetProperty("name")[0].GetString().Should().Be("Name is required");
        errorsObj.GetProperty("name")[1].GetString().Should().Be("Name too short");
        errorsObj.GetProperty("email").GetArrayLength().Should().Be(1);
        errorsObj.GetProperty("email")[0].GetString().Should().Be("Invalid email");
    }

    [Fact]
    public async Task AspNetCoreCompatible_ErrorsWithoutTarget_GroupedUnderEmptyString()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "General error", Category = ErrorCategory.Validation },
                new () { Message = "Another general error", Category = ErrorCategory.Validation }
            }
        );
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.AspNetCoreCompatible
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);
        var errorsObj = doc.RootElement.GetProperty("errors");

        errorsObj.GetProperty("").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task AspNetCoreCompatible_IncludesErrorDetailsWhenPresent()
    {
        var errors = new Errors(
            new Error[]
            {
                new ()
                {
                    Message = "Name is required", Target = "name", Code = "REQUIRED",
                    Category = ErrorCategory.Validation
                }
            }
        );
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.AspNetCoreCompatible
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("errorDetails", out var errorDetails).Should().BeTrue();
        errorDetails.GetArrayLength().Should().Be(1);
        errorDetails[0].GetProperty("target").GetString().Should().Be("name");
        errorDetails[0].GetProperty("index").GetInt32().Should().Be(0);
        errorDetails[0].GetProperty("code").GetString().Should().Be("REQUIRED");
        errorDetails[0].GetProperty("category").GetString().Should().Be("Validation");
    }

    [Fact]
    public async Task AspNetCoreCompatible_OmitsErrorDetailsWhenOnlyMessageAndTarget()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Name is required", Target = "name" }
            }
        );
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.AspNetCoreCompatible
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("errorDetails", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Rich_SerializesErrorsAsArrayOfObjects()
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
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.Rich
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);
        var errorsArray = doc.RootElement.GetProperty("errors");

        errorsArray.ValueKind.Should().Be(JsonValueKind.Array);
        errorsArray.GetArrayLength().Should().Be(1);

        var firstError = errorsArray[0];
        firstError.GetProperty("message").GetString().Should().Be("Name is required");
        firstError.GetProperty("code").GetString().Should().Be("REQUIRED");
        firstError.GetProperty("target").GetString().Should().Be("name");
        firstError.GetProperty("category").GetString().Should().Be("Validation");
    }

    [Fact]
    public async Task Rich_IncludesErrorMetadata()
    {
        var errorMetadata = MetadataObject.Create(("minLength", 2), ("attemptedValue", ""));
        var errors = new Errors(
            new Error
            {
                Message = "Name too short",
                Category = ErrorCategory.Validation,
                Metadata = errorMetadata
            }
        );
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.Rich
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);
        var firstError = doc.RootElement.GetProperty("errors")[0];
        var metadata = firstError.GetProperty("metadata");

        metadata.GetProperty("minLength").GetInt64().Should().Be(2);
        metadata.GetProperty("attemptedValue").GetString().Should().Be("");
    }

    [Fact]
    public async Task Rich_OmitsNullProperties()
    {
        var errors = new Errors(new Error { Message = "Error message" });
        var problemDetails = new LightProblemDetails(
            errors,
            null,
            errorFormat: ErrorSerializationFormat.Rich
        );

        var json = await SerializeToJson(problemDetails);
        var doc = JsonDocument.Parse(json);
        var firstError = doc.RootElement.GetProperty("errors")[0];

        firstError.TryGetProperty("code", out _).Should().BeFalse();
        firstError.TryGetProperty("target", out _).Should().BeFalse();
        firstError.TryGetProperty("category", out _).Should().BeFalse();
        firstError.TryGetProperty("metadata", out _).Should().BeFalse();
    }

    [Fact]
    public async Task BothFormats_ProduceValidRfc7807Json()
    {
        var errors = new Errors(new Error { Message = "Error", Category = ErrorCategory.Validation });

        foreach (var format in new[] { ErrorSerializationFormat.AspNetCoreCompatible, ErrorSerializationFormat.Rich })
        {
            var problemDetails = new LightProblemDetails(errors, null, errorFormat: format);
            var json = await SerializeToJson(problemDetails);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.TryGetProperty("type", out _).Should().BeTrue();
            root.TryGetProperty("title", out _).Should().BeTrue();
            root.TryGetProperty("status", out _).Should().BeTrue();
            root.TryGetProperty("detail", out _).Should().BeTrue();
            root.TryGetProperty("errors", out _).Should().BeTrue();
        }
    }

    private static async Task<string> SerializeToJson(LightProblemDetails problemDetails)
    {
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        await problemDetails.ExecuteAsync(httpContext);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
