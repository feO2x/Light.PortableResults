using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Writing;

public sealed class CloudEventResultExtensionsTests
{
    [Fact]
    public void ToCloudEvent_ForGenericSuccess_ShouldWriteRequiredEnvelopeAndBareData()
    {
        var result = Result<int>.Ok(42);
        var time = new DateTimeOffset(2026, 2, 14, 12, 30, 0, TimeSpan.Zero);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-1",
            source: "urn:test:source",
            time: time,
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("specversion").GetString().Should().Be("1.0");
        root.GetProperty("type").GetString().Should().Be("app.success");
        root.GetProperty("source").GetString().Should().Be("urn:test:source");
        root.GetProperty("id").GetString().Should().Be("evt-1");
        root.GetProperty("lroutcome").GetString().Should().Be("success");
        root.GetProperty("datacontenttype").GetString().Should().Be("application/json");
        root.GetProperty("data").GetInt32().Should().Be(42);
        DateTimeOffset.Parse(root.GetProperty("time").GetString()!).Should().Be(time);
    }

    [Fact]
    public void ToCloudEvent_ForNonGenericSuccessWithoutDataMetadata_ShouldOmitDataAndDataContentType()
    {
        var result = Result.Ok();
        var options = new LightResultsCloudEventWriteOptions
        {
            Source = "urn:test:source",
            MetadataSerializationMode = MetadataSerializationMode.ErrorsOnly
        };

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-2",
            options: options
        );

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("data", out _).Should().BeFalse();
        root.TryGetProperty("datacontenttype", out _).Should().BeFalse();
        root.GetProperty("lroutcome").GetString().Should().Be("success");
    }

    [Fact]
    public void ToCloudEvent_ForNonGenericSuccessWithCloudEventDataMetadata_ShouldWriteMetadataObjectAsData()
    {
        var metadata = MetadataObject.Create(
            (
                "traceId",
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventData)
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-3",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("datacontenttype").GetString().Should().Be("application/json");
        root.GetProperty("data").GetProperty("metadata").GetProperty("traceId").GetString().Should().Be("abc");
    }

    [Fact]
    public void ToCloudEvent_ForGenericSuccessWithCloudEventDataMetadata_ShouldWriteWrappedValueAndMetadata()
    {
        var metadata = MetadataObject.Create(
            (
                "traceId",
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventData)
            )
        );
        var result = Result<string>.Ok("payload", metadata);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-4",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");

        data.GetProperty("value").GetString().Should().Be("payload");
        data.GetProperty("metadata").GetProperty("traceId").GetString().Should().Be("abc");
    }

    [Fact]
    public void ToCloudEvent_ForFailure_ShouldWriteFailureOutcomeAndLightResultsErrorPayload()
    {
        var errors = new[]
        {
            new Error
            {
                Message = "failed",
                Code = "FAIL",
                Target = "field",
                Category = ErrorCategory.Validation
            }
        };
        var metadata = MetadataObject.Create(
            (
                "traceId",
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventData)
            )
        );
        var result = Result<int>.Fail(errors, metadata);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-5",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("lroutcome").GetString().Should().Be("failure");
        root.GetProperty("type").GetString().Should().Be("app.failure");
        root.GetProperty("data").GetProperty("errors")[0].GetProperty("message").GetString().Should().Be("failed");
        root.GetProperty("data").GetProperty("metadata").GetProperty("traceId").GetString().Should().Be("abc");
    }

    [Fact]
    public void ToCloudEvent_ShouldUseMetadataAttributesForRequiredValues_WhenExplicitParametersAreMissing()
    {
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromString(
                    "app.success.from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            )
        );
        var result = Result<int>.Ok(5, metadata);

        var json = result.ToCloudEvent(options: CreateWriteOptions());

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("type").GetString().Should().Be("app.success.from-metadata");
        root.GetProperty("source").GetString().Should().Be("urn:source:from-metadata");
        root.GetProperty("id").GetString().Should().Be("evt-from-metadata");
    }

    [Fact]
    public void ToCloudEvent_ShouldPreferExplicitParametersOverMetadata()
    {
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromString(
                    "app.success.from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            )
        );
        var result = Result<int>.Ok(5, metadata);

        var json = result.ToCloudEvent(
            successType: "app.success.explicit",
            failureType: "app.failure.explicit",
            id: "evt-explicit",
            source: "urn:source:explicit",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("type").GetString().Should().Be("app.success.explicit");
        root.GetProperty("source").GetString().Should().Be("urn:source:explicit");
        root.GetProperty("id").GetString().Should().Be("evt-explicit");
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenRequiredAttributesCannotBeResolved()
    {
        var result = Result<int>.Ok(5);

        var act = () => result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-6",
            options: CreateWriteOptions(source: null)
        );

        act.Should().Throw<InvalidOperationException>().WithMessage("*source*");
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenSourceIsInvalid()
    {
        var result = Result<int>.Ok(5);

        var act = () => result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-7",
            source: "http://[invalid",
            options: CreateWriteOptions()
        );

        act.Should().Throw<ArgumentException>().WithMessage("*source*");
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenDataSchemaIsNotAbsoluteUri()
    {
        var result = Result<int>.Ok(5);

        var act = () => result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-8",
            source: "urn:test:source",
            dataschema: "relative/path",
            options: CreateWriteOptions()
        );

        act.Should().Throw<ArgumentException>().WithMessage("*dataschema*");
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenMetadataAttemptsToMapReservedAttribute()
    {
        var metadata = MetadataObject.Create(
            (
                "data",
                MetadataValue.FromString(
                    "forbidden",
                    MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
                )
            )
        );
        var result = Result.Ok(metadata);

        var act = () => result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-9",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        act.Should().Throw<ArgumentException>().WithMessage("*reserved*");
    }

    private static LightResultsCloudEventWriteOptions CreateWriteOptions(string? source = "urn:test:source")
    {
        return new LightResultsCloudEventWriteOptions
        {
            Source = source,
            SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            }
        };
    }
}
