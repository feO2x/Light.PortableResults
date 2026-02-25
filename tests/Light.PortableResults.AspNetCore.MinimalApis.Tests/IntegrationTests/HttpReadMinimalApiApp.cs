using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.MinimalApis.Serialization;
using Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;
using Light.PortableResults.Http;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: AssemblyFixture(typeof(HttpReadMinimalApiApp))]

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class HttpReadMinimalApiApp : IAsyncLifetime
{
    private static readonly PortableResultsHttpWriteOptions AlwaysSerializeMetadataOptions = new ()
    {
        MetadataSerializationMode = MetadataSerializationMode.Always
    };

    private static readonly PortableResultsHttpWriteOptions RichValidationOptions = new ()
    {
        ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich,
        CreateProblemDetailsInfo = (_, _) => new ProblemDetailsInfo
        {
            Type = "https://example.org/problems/validation",
            Title = "Validation failed",
            Status = HttpStatusCode.BadRequest,
            Detail = "",
            Instance = "/instances/http-read"
        }
    };

    public HttpReadMinimalApiApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.Configure<JsonOptions>(
            options =>
            {
                ConfigureTypeInfoResolver(options.SerializerOptions);
            }
        );

        App = builder.Build();
        App.MapGet("/api/read/bare-int", GetBareInteger);
        App.MapGet("/api/read/wrapped-string", GetWrappedString);
        App.MapGet("/api/read/problem-shape-success", GetProblemDetailsLikeSuccessPayload);
        App.MapGet("/api/read/wrapper-missing-value", GetWrapperMissingValuePayload);
        App.MapGet("/api/read/non-generic-metadata", GetNonGenericMetadata);
        App.MapGet("/api/read/non-generic-unexpected", GetNonGenericUnexpectedPayload);
        App.MapGet("/api/read/failure-rich", GetRichFailure);
        App.MapGet("/api/read/failure-aspnet", GetAspNetCompatibleFailure);
        App.MapGet("/api/read/header-alias", GetHeaderAliasPayload);
        App.MapGet("/api/read/header-values", GetPrimitiveHeaderPayload);
        App.MapGet("/api/read/empty-success", static () => Results.Empty);
        App.MapGet(
            "/api/read/empty-failure",
            static () => Results.StatusCode(StatusCodes.Status400BadRequest)
        );
        App.MapGet("/api/read/context-success", GetContextSuccess);
        App.MapGet("/api/read/context-failure", GetContextFailure);
    }

    public WebApplication App { get; }

    public async ValueTask InitializeAsync() => await App.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }

    public HttpClient CreateHttpClient() => App.GetTestClient();

    private static void ConfigureTypeInfoResolver(JsonSerializerOptions options)
    {
        options.TypeInfoResolverChain.Insert(0, ExtendedMinimalApiJsonContext.Default);
        options.TypeInfoResolverChain.Insert(1, PortableResultsMinimalApiJsonContext.Default);
    }

    private static LightResult<int> GetBareInteger()
    {
        var result = Result<int>.Ok(42);
        return result.ToMinimalApiResult();
    }

    private static LightResult<string> GetWrappedString()
    {
        var metadata = MetadataObject.Create(("trace", MetadataValue.FromString("t-1")));
        var result = Result<string>.Ok("ok", metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static IResult GetProblemDetailsLikeSuccessPayload()
    {
        const string json =
            """
            {
              "type": "dto",
              "title": "Successful DTO",
              "status": 200,
              "errors": []
            }
            """;

        return Results.Text(json, contentType: "application/json");
    }

    private static IResult GetWrapperMissingValuePayload()
    {
        const string json = """{"metadata":{"trace":"t-1"}}""";
        return Results.Text(json, contentType: "application/json");
    }

    private static LightResult GetNonGenericMetadata()
    {
        var metadata = MetadataObject.Create(("note", MetadataValue.FromString("hi")));
        var result = Result.Ok(metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static IResult GetNonGenericUnexpectedPayload()
    {
        const string json = """{"value":"ok"}""";
        return Results.Text(json, contentType: "application/json");
    }

    private static LightResult<ContactDto> GetRichFailure()
    {
        var error = new Error
        {
            Message = "Name is required",
            Code = "NameRequired",
            Target = "name",
            Category = ErrorCategory.Validation
        };
        var metadata = MetadataObject.Create(("traceId", MetadataValue.FromString("abc")));

        var result = Result<ContactDto>.Fail(error, metadata);
        return result.ToMinimalApiResult(overrideOptions: RichValidationOptions);
    }

    private static LightResult<ContactDto> GetAspNetCompatibleFailure()
    {
        Error[] errors =
        [
            new ()
            {
                Message = "Name required",
                Target = "name",
                Category = ErrorCategory.Validation
            },
            new ()
            {
                Message = "Name too short",
                Target = "name",
                Code = "MinLength",
                Category = ErrorCategory.Validation
            }
        ];

        var result = Result<ContactDto>.Fail(errors);
        return result.ToMinimalApiResult();
    }

    private static IResult GetHeaderAliasPayload(HttpContext context)
    {
        context.Response.Headers.Append("X-TraceId", "first");
        context.Response.Headers.Append("X-Correlation-Id", "second");

        var metadata = MetadataObject.Create(("note", MetadataValue.FromString("ok")));
        var result = Result.Ok(metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static IResult GetPrimitiveHeaderPayload(HttpContext context)
    {
        context.Response.Headers.Append("X-Bool", "true");
        context.Response.Headers.Append("X-Int", "123");
        context.Response.Headers.Append("X-Double", "3.5");
        context.Response.Headers.Append("X-Text", "alpha");
        context.Response.Headers.Append("X-Ids", "1");
        context.Response.Headers.Append("X-Ids", "2");

        var result = Result<int>.Ok(42);
        return result.ToMinimalApiResult();
    }

    private static IResult GetContextSuccess()
    {
        const string json =
            """
            {
                "value": {
                    "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
                    "name": "Contact A"
                },
                "metadata": {
                    "source": "context"
                }
            }
            """;

        return Results.Text(json, contentType: "application/json");
    }

    private static IResult GetContextFailure()
    {
        const string json =
            """
            {
                "type": "https://example.org/problems/validation",
                "title": "Validation failed",
                "status": 400,
                "errors": [
                    {
                        "message": "Name is required",
                        "code": "NameRequired",
                        "target": "name",
                        "category": "Validation"
                    }
                ],
                "metadata": {
                    "source": "context"
                }
            }
            """;

        return Results.Text(
            json,
            contentType: "application/problem+json",
            statusCode: StatusCodes.Status400BadRequest
        );
    }
}
