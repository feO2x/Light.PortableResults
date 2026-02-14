using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Serialization;
using Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;
using Light.Results.AspNetCore.Shared.Enrichment;
using Light.Results.Http;
using Light.Results.Http.Writing;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: AssemblyFixture(typeof(ExtendedMinimalApiApp))]

namespace Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class ExtendedMinimalApiApp : IAsyncLifetime
{
    private static readonly LightResultsHttpWriteOptions AlwaysSerializeMetadataOptions = new ()
    {
        MetadataSerializationMode = MetadataSerializationMode.Always
    };

    public ExtendedMinimalApiApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLightResultsForMinimalApis();
        builder.Services.Configure<JsonOptions>(
            options =>
            {
                ConfigureTypeInfoResolver(options.SerializerOptions);
            }
        );
        builder.Services.AddSingleton<IHttpResultEnricher, StaticMetadataEnricher>();

        App = builder.Build();
        App.MapGet("/api/extended/metadata", GetContactWithMetadata);
        App.MapGet("/api/extended/value-metadata", GetContactWithValueAndMetadata);
        App.MapGet("/api/extended/non-generic-metadata", GetNonGenericMetadata);
        App.MapGet("/api/extended/validation-rich", GetValidationErrorsRich);
        App.MapGet("/api/extended/validation-fallback", GetValidationErrorsFallback);
        App.MapGet("/api/extended/custom-converter", GetCustomSerializedResult);
        App.MapGet("/api/extended/custom-converter-generic", GetCustomSerializedGenericResult);
        App.MapGet("/api/extended/enriched-error", GetEnrichedError);
        App.MapGet("/api/extended/non-generic-header-only-metadata", GetNonGenericHeaderOnlyMetadata);
        App.MapGet("/api/extended/generic-header-only-metadata", GetGenericHeaderOnlyMetadata);
        App.MapGet("/api/extended/non-generic-null-header", GetNonGenericNullHeaderMetadata);
        App.MapGet("/api/extended/generic-header-annotation-flags", GetGenericHeaderAnnotationFlagsMetadata);
    }

    public WebApplication App { get; }

    public async ValueTask InitializeAsync() => await App.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }

    public HttpClient CreateHttpClient() => App.GetTestClient();

    private static LightResult GetContactWithMetadata()
    {
        var result = Result.Ok(CreateRichMetadata());
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult<string> GetContactWithValueAndMetadata()
    {
        const string contact = "contact-42";
        var metadata = MetadataObject.Create(("source", MetadataValue.FromString("value-metadata")));

        var result = Result<string>.Ok(contact, metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult GetNonGenericMetadata()
    {
        var metadata = MetadataObject.Create(
            ("note", MetadataValue.FromString("non-generic")),
            ("count", MetadataValue.FromInt64(3))
        );

        var result = Result.Ok(metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult<ContactDto> GetValidationErrorsRich()
    {
        var errorMetadata = MetadataObject.Create(("minLength", MetadataValue.FromInt64(1)));
        Error[] errors =
        [
            new ()
            {
                Message = "Name is required",
                Code = "NameRequired",
                Target = " ",
                Category = ErrorCategory.Validation,
                Metadata = errorMetadata
            },
            new ()
            {
                Message = "Email is required",
                Target = "email",
                Category = ErrorCategory.Validation
            }
        ];

        var metadata = MetadataObject.Create(("source", MetadataValue.FromString("rich")));
        var result = Result<ContactDto>.Fail(errors, metadata);
        return result.ToMinimalApiResult(overrideOptions: CreateRichValidationOptions());
    }

    private static LightResult<ContactDto> GetValidationErrorsFallback()
    {
        var errors = new Error[11];
        for (var i = 0; i < errors.Length; i++)
        {
            errors[i] = new Error
            {
                Message = $"Validation error {i}",
                Code = $"VAL{i}",
                Target = $"field{i}",
                Category = ErrorCategory.Validation
            };
        }

        errors[5] = new Error
        {
            Message = "Validation error 5",
            Code = "VAL5",
            Target = "field5",
            Category = ErrorCategory.Validation,
            Metadata = MetadataObject.Create(("index", MetadataValue.FromInt64(5)))
        };

        var result = Result<ContactDto>.Fail(errors);
        return result.ToMinimalApiResult();
    }

    private static LightResult GetCustomSerializedResult()
    {
        var result = Result.Ok(MetadataObject.Create(("custom", MetadataValue.FromString("true"))));
        var serializerOptions = CreateCustomSerializerOptions();
        return result.ToMinimalApiResult(
            overrideOptions: AlwaysSerializeMetadataOptions,
            serializerOptions: serializerOptions
        );
    }

    private static LightResult<ContactDto> GetCustomSerializedGenericResult()
    {
        var contact = new ContactDto
        {
            Id = new Guid("C5C6B3F9-4E09-4E0D-9D9B-9B2B364EC5B8"),
            Name = "Custom"
        };

        var result = Result<ContactDto>.Ok(contact);
        var serializerOptions = CreateCustomGenericSerializerOptions();
        return result.ToMinimalApiResult(serializerOptions: serializerOptions);
    }

    private static LightResult<ContactDto> GetEnrichedError()
    {
        var error = new Error
        {
            Message = "Contact was not found",
            Code = "ContactNotFound",
            Target = "contactId",
            Category = ErrorCategory.NotFound
        };

        var result = Result<ContactDto>.Fail(error);
        return result.ToMinimalApiResult();
    }

    private static LightResult GetNonGenericHeaderOnlyMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-TraceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result.Ok(metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult<string> GetGenericHeaderOnlyMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-Null", default),
            ("X-TraceId", MetadataValue.FromString("trace-43", MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result<string>.Ok("ok", metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult GetNonGenericNullHeaderMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-Null", MetadataValue.Null),
            ("note", MetadataValue.FromString("non-generic"))
        );

        var result = Result.Ok(metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResult<string> GetGenericHeaderAnnotationFlagsMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-HeaderAndBody", MetadataValue.FromString("both", MetadataValueAnnotation.SerializeInHttpHeaderAndBody)),
            ("X-None", MetadataValue.FromString("none", MetadataValueAnnotation.None))
        );
        var result = Result<string>.Ok("ok", metadata);
        return result.ToMinimalApiResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    private static LightResultsHttpWriteOptions CreateRichValidationOptions() =>
        new ()
        {
            ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich,
            CreateProblemDetailsInfo = (_, _) => new ProblemDetailsInfo
            {
                Type = "https://example.org/problems/validation",
                Title = "Validation failed",
                Status = HttpStatusCode.BadRequest,
                Detail = "",
                Instance = "/instances/validation-rich"
            }
        };

    private static MetadataObject CreateRichMetadata()
    {
        var nested = MetadataObject.Create(("inner", MetadataValue.FromString("nested")));
        var array = MetadataArray.Create(
            MetadataValue.FromBoolean(true),
            MetadataValue.FromInt64(7),
            MetadataValue.FromDouble(1.5),
            MetadataValue.FromString("alpha"),
            MetadataValue.FromObject(nested)
        );

        return MetadataObject.Create(
            ("nullValue", MetadataValue.Null),
            ("boolValue", MetadataValue.FromBoolean(true)),
            ("intValue", MetadataValue.FromInt64(42)),
            ("doubleValue", MetadataValue.FromDouble(12.5)),
            ("stringValue", MetadataValue.FromString("sample")),
            ("arrayValue", MetadataValue.FromArray(array)),
            ("objectValue", MetadataValue.FromObject(nested))
        );
    }

    private static JsonSerializerOptions CreateCustomSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        ConfigureTypeInfoResolver(options);
        options.Converters.Add(new CustomResultJsonConverter());
        return options;
    }

    private static JsonSerializerOptions CreateCustomGenericSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        ConfigureTypeInfoResolver(options);
        options.Converters.Add(new CustomResultJsonConverter<ContactDto>());
        return options;
    }

    private static void ConfigureTypeInfoResolver(JsonSerializerOptions options)
    {
        options.TypeInfoResolverChain.Insert(0, ExtendedMinimalApiJsonContext.Default);
        options.TypeInfoResolverChain.Insert(1, LightResultsMinimalApiJsonContext.Default);
    }

    private sealed class StaticMetadataEnricher : IHttpResultEnricher
    {
        public TResult Enrich<TResult>(TResult result, HttpContext httpContext)
            where TResult : struct, IResultObject, ICanReplaceMetadata<TResult>
        {
            if (result.Metadata is not null)
            {
                return result;
            }

            var metadata = MetadataObject.Create(("enriched", MetadataValue.FromString("true")));
            return result.ReplaceMetadata(metadata);
        }
    }

    private sealed class CustomResultJsonConverter : JsonConverter<Result>
    {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("ok", value.IsValid);
            writer.WriteEndObject();
        }
    }

    private sealed class CustomResultJsonConverter<T> : JsonConverter<Result<T>>
    {
        public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("ok", value.IsValid);
            if (value.IsValid)
            {
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, value.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}
