using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Http;
using Light.Results.Http.Writing;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
using Microsoft.AspNetCore.Mvc;

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

[ApiController]
[Route("api/extended")]
public sealed class ExtendedMvcController : ControllerBase
{
    private static readonly LightResultsHttpWriteOptions AlwaysSerializeMetadataOptions = new ()
    {
        MetadataSerializationMode = MetadataSerializationMode.Always
    };

    [HttpGet("metadata")]
    public LightActionResult GetContactWithMetadata()
    {
        var result = Result.Ok(CreateRichMetadata());
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("value-metadata")]
    public LightActionResult<string> GetContactWithValueAndMetadata()
    {
        const string contact = "contact-42";
        var metadata = MetadataObject.Create(("source", MetadataValue.FromString("value-metadata")));

        var result = Result<string>.Ok(contact, metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("non-generic-metadata")]
    public LightActionResult GetNonGenericMetadata()
    {
        var metadata = MetadataObject.Create(
            ("note", MetadataValue.FromString("non-generic")),
            ("count", MetadataValue.FromInt64(3))
        );

        var result = Result.Ok(metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("validation-rich")]
    public LightActionResult<ContactDto> GetValidationErrorsRich()
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
        return result.ToMvcActionResult(overrideOptions: CreateRichValidationOptions());
    }

    [HttpGet("validation-fallback")]
    public LightActionResult<ContactDto> GetValidationErrorsFallback()
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
        return result.ToMvcActionResult();
    }

    [HttpGet("custom-converter")]
    public LightActionResult GetCustomSerializedResult()
    {
        var result = Result.Ok(MetadataObject.Create(("custom", MetadataValue.FromString("true"))));
        var serializerOptions = CreateCustomSerializerOptions();
        return result.ToMvcActionResult(
            overrideOptions: AlwaysSerializeMetadataOptions,
            serializerOptions: serializerOptions
        );
    }

    [HttpGet("custom-converter-generic")]
    public LightActionResult<ContactDto> GetCustomSerializedGenericResult()
    {
        var contact = new ContactDto
        {
            Id = new Guid("C5C6B3F9-4E09-4E0D-9D9B-9B2B364EC5B8"),
            Name = "Custom"
        };

        var result = Result<ContactDto>.Ok(contact);
        var serializerOptions = CreateCustomGenericSerializerOptions();
        return result.ToMvcActionResult(serializerOptions: serializerOptions);
    }

    [HttpGet("enriched-error")]
    public LightActionResult<ContactDto> GetEnrichedError()
    {
        var error = new Error
        {
            Message = "Contact was not found",
            Code = "ContactNotFound",
            Target = "contactId",
            Category = ErrorCategory.NotFound
        };

        var result = Result<ContactDto>.Fail(error);
        return result.ToMvcActionResult();
    }

    [HttpGet("non-generic-header-only-metadata")]
    public LightActionResult GetNonGenericHeaderOnlyMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-TraceId", MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result.Ok(metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("generic-header-only-metadata")]
    public LightActionResult<string> GetGenericHeaderOnlyMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-Null", default),
            ("X-TraceId", MetadataValue.FromString("trace-43", MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result<string>.Ok("ok", metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("non-generic-null-header")]
    public LightActionResult GetNonGenericNullHeaderMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-Null", MetadataValue.Null),
            ("note", MetadataValue.FromString("non-generic"))
        );

        var result = Result.Ok(metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
    }

    [HttpGet("generic-header-annotation-flags")]
    public LightActionResult<string> GetGenericHeaderAnnotationFlagsMetadata()
    {
        var metadata = MetadataObject.Create(
            ("X-HeaderAndBody", MetadataValue.FromString("both", MetadataValueAnnotation.SerializeInHttpHeaderAndBody)),
            ("X-None", MetadataValue.FromString("none", MetadataValueAnnotation.None))
        );
        var result = Result<string>.Ok("ok", metadata);
        return result.ToMvcActionResult(overrideOptions: AlwaysSerializeMetadataOptions);
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        options.Converters.Add(new CustomResultJsonConverter());
        return options;
    }

    private static JsonSerializerOptions CreateCustomGenericSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        options.Converters.Add(new CustomResultJsonConverter<ContactDto>());
        return options;
    }

    public sealed class CustomResultJsonConverter : JsonConverter<Result>
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

    public sealed class CustomResultJsonConverter<T> : JsonConverter<Result<T>>
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
