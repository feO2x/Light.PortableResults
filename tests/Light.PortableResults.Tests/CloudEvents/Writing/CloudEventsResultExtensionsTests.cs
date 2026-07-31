using System;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Writing;

public sealed class CloudEventsResultExtensionsTests
{
    [Fact]
    public void ToCloudEvent_ForGenericSuccess_ShouldWriteRequiredEnvelopeAndWrappedData()
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
        root.GetProperty("lproutcome").GetString().Should().Be("success");
        root.GetProperty("datacontenttype").GetString().Should().Be("application/json");
        root.GetProperty("data").GetProperty("value").GetInt32().Should().Be(42);
        DateTimeOffset.Parse(root.GetProperty("time").GetString()!).Should().Be(time);
    }

    [Fact]
    public void ToCloudEvent_ForNonGenericSuccessWithoutDataMetadata_ShouldOmitDataAndDataContentType()
    {
        var result = Result.Ok();
        var options = new PortableResultsCloudEventsWriteOptions
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

        root.TryGetProperty("datacontenttype", out _).Should().BeFalse();
        root.TryGetProperty("data", out _).Should().BeFalse();
        root.GetProperty("lproutcome").GetString().Should().Be("success");
    }

    [Fact]
    public void ToCloudEvent_ForNonGenericSuccessWithCloudEventDataMetadata_ShouldWriteMetadataObjectAsData()
    {
        var metadata = MetadataObject.Create(
            (
                "traceId",
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventsData)
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
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventsData)
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
    public void ToCloudEvent_ForFailure_ShouldWriteFailureOutcomeAndPortableResultsErrorPayload()
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
                MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventsData)
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

        root.GetProperty("lproutcome").GetString().Should().Be("failure");
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
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
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
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
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
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
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

    [Fact]
    public void ToCloudEvent_ShouldSerializeAllMetadataKinds_WhenWrittenToDataPayload()
    {
        var metadata = MetadataObject.Create(
            ("nullValue", MetadataValue.FromNull(MetadataValueAnnotation.SerializeInCloudEventsData)),
            ("boolValue", MetadataValue.FromBoolean(true, MetadataValueAnnotation.SerializeInCloudEventsData)),
            ("intValue", MetadataValue.FromInt64(42, MetadataValueAnnotation.SerializeInCloudEventsData)),
            ("doubleValue", MetadataValue.FromDouble(12.5, MetadataValueAnnotation.SerializeInCloudEventsData)),
            ("stringValue", MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInCloudEventsData)),
            (
                "arrayValue",
                MetadataValue.FromArray(
                    MetadataArray.Create(MetadataValue.FromInt64(1), MetadataValue.FromString("x")),
                    MetadataValueAnnotation.SerializeInCloudEventsData
                )
            ),
            (
                "objectValue",
                MetadataValue.FromObject(
                    MetadataObject.Create(("nested", MetadataValue.FromBoolean(true))),
                    MetadataValueAnnotation.SerializeInCloudEventsData
                )
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-10",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var metadataElement = document.RootElement.GetProperty("data").GetProperty("metadata");

        metadataElement.GetProperty("nullValue").ValueKind.Should().Be(JsonValueKind.Null);
        metadataElement.GetProperty("boolValue").GetBoolean().Should().BeTrue();
        metadataElement.GetProperty("intValue").GetInt64().Should().Be(42);
        metadataElement.GetProperty("doubleValue").GetDouble().Should().Be(12.5);
        metadataElement.GetProperty("stringValue").GetString().Should().Be("abc");
        metadataElement.GetProperty("arrayValue")[0].GetInt64().Should().Be(1);
        metadataElement.GetProperty("arrayValue")[1].GetString().Should().Be("x");
        metadataElement.GetProperty("objectValue").GetProperty("nested").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ToCloudEvent_ShouldResolveAttributes_FromNonStringMetadataValues()
    {
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromBoolean(true, MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
            ),
            (
                "source",
                MetadataValue.FromInt64(42, MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
            ),
            (
                "id",
                MetadataValue.FromDouble(12.5, MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(options: CreateWriteOptions(source: null));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("type").GetString().Should().Be("true");
        root.GetProperty("source").GetString().Should().Be("42");
        root.GetProperty("id").GetString().Should().Be("12.5");
    }

    [Fact]
    public void ToCloudEvent_ShouldResolveCoreStringAttributes_FromDecimalMetadataValues()
    {
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromString(
                    "app.success",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:test:source",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "subject",
                MetadataValue.FromDecimal(19.50m, MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(options: CreateWriteOptions(source: null));

        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("subject").GetString().Should().Be("19.50");
    }

    [Fact]
    public void ToCloudEvent_ShouldResolveCoreStringAttributeFromEveryPrimitiveKind()
    {
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes;
        // Null is deliberately absent here - it resolves to no attribute at all, see the dedicated test below.
        var values = new (MetadataValue Value, string Expected)[]
        {
            (MetadataValue.FromBoolean(true, annotation), "true"),
            (MetadataValue.FromInt64(42, annotation), "42"),
            (MetadataValue.FromDouble(5, annotation), "5.0"),
            (MetadataValue.FromString("plain text", annotation), "plain text"),
            (MetadataValue.FromDecimal(19.50m, annotation), "19.50"),
            (MetadataValue.FromUInt64(ulong.MaxValue, annotation), "18446744073709551615"),
            (MetadataValue.FromSingle(0.1f, annotation), "0.1"),
            (MetadataValue.FromChar('x', annotation), "x"),
            (
                MetadataValue.FromDateTime(
                    new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc),
                    annotation
                ),
                "2026-07-26T13:45:30Z"
            ),
            (
                MetadataValue.FromDateTimeOffset(
                    new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2)),
                    annotation
                ),
                "2026-07-26T13:45:30+02:00"
            ),
#if !TESTING_NETSTANDARD_ASSET
            (MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26), annotation), "2026-07-26"),
            (MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30), annotation), "13:45:30"),
#endif
            (MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5), annotation), "PT5S"),
            (
                MetadataValue.FromGuid(
                    new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    annotation
                ),
                "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            ),
            (
                MetadataValue.FromUri(new Uri("https://example.com/items/42"), annotation),
                "https://example.com/items/42"
            )
        };

        foreach (var (value, expected) in values)
        {
            var metadata = MetadataObject.Create(
                (
                    "type",
                    MetadataValue.FromString("app.success", annotation)
                ),
                (
                    "source",
                    MetadataValue.FromString("urn:test:source", annotation)
                ),
                ("subject", value)
            );

            var json = Result.Ok(metadata).ToCloudEvent(options: CreateWriteOptions(source: null));

            using var document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("subject").GetString().Should().Be(expected);
        }
    }

    [Fact]
    public void ToCloudEvent_ShouldTreatNullCoreAttribute_AsAbsent()
    {
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes;
        var metadata = MetadataObject.Create(
            ("type", MetadataValue.FromString("app.success", annotation)),
            ("source", MetadataValue.FromString("urn:test:source", annotation)),
            ("subject", MetadataValue.FromNull(annotation)),
            ("dataschema", MetadataValue.FromNull(annotation))
        );

        var json = Result.Ok(metadata).ToCloudEvent(options: CreateWriteOptions(source: null));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("subject", out _).Should().BeFalse();
        root.TryGetProperty("dataschema", out _).Should().BeFalse();
    }

    [Fact]
    public void ToCloudEvent_ShouldFallBackToDefaultSource_WhenSourceAttributeIsNull()
    {
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes;
        var metadata = MetadataObject.Create(
            ("type", MetadataValue.FromString("app.success", annotation)),
            ("source", MetadataValue.FromNull(annotation))
        );

        var json = Result.Ok(metadata).ToCloudEvent(options: CreateWriteOptions());

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("source").GetString().Should().Be("urn:test:source");
    }

    [Fact]
    public void ToCloudEvent_ShouldFallBackToCurrentTimestamp_WhenTimeAttributeIsNull()
    {
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes;
        var before = DateTimeOffset.UtcNow;
        var metadata = MetadataObject.Create(
            ("type", MetadataValue.FromString("app.success", annotation)),
            ("source", MetadataValue.FromString("urn:test:source", annotation)),
            ("time", MetadataValue.FromNull(annotation))
        );

        var json = Result.Ok(metadata).ToCloudEvent(options: CreateWriteOptions(source: null));

        using var document = JsonDocument.Parse(json);
        var time = DateTimeOffset.Parse(document.RootElement.GetProperty("time").GetString()!);
        time.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ToCloudEvent_ShouldWriteDecimalExtensionAttribute_AsUnquotedNumber()
    {
        var metadata = MetadataObject.Create(
            (
                "price",
                MetadataValue.FromDecimal(19.99m, MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(
            successType: "app.success",
            failureType: "app.failure",
            id: "evt-decimal",
            options: CreateWriteOptions()
        );

        using var document = JsonDocument.Parse(json);
        var price = document.RootElement.GetProperty("price");

        price.ValueKind.Should().Be(JsonValueKind.Number);
        price.GetDecimal().Should().Be(19.99m);
    }

    [Fact]
    public void ToCloudEvent_ShouldResolveTimeFromMetadata_WhenProvidedAsExtensionAttribute()
    {
        var expectedTime = new DateTimeOffset(2026, 2, 14, 18, 45, 0, TimeSpan.Zero);
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromString(
                    "app.success.from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "time",
                MetadataValue.FromString(
                    expectedTime.ToString("O"),
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            )
        );
        var result = Result.Ok(metadata);

        var json = result.ToCloudEvent(options: CreateWriteOptions(source: null));

        using var document = JsonDocument.Parse(json);
        var actualTime = DateTimeOffset.Parse(document.RootElement.GetProperty("time").GetString()!);

        actualTime.Should().Be(expectedTime);
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenTimeMetadataIsInvalid()
    {
        var metadata = MetadataObject.Create(
            (
                "type",
                MetadataValue.FromString(
                    "app.success.from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "source",
                MetadataValue.FromString(
                    "urn:source:from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "time",
                MetadataValue.FromString(
                    "not-a-time",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            )
        );
        var result = Result.Ok(metadata);

        var act = () => result.ToCloudEvent(options: CreateWriteOptions(source: null));

        act.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "time");
    }

    [Fact]
    public void ToCloudEvent_ShouldThrow_WhenTimeMetadataHasNoUtcOffset()
    {
        // The canonical text of a DateTimeKind.Unspecified value carries no designator. Resolving it against
        // the serializing machine's time zone would make the same metadata produce different instants on
        // different hosts, so it is rejected with a pointer towards UTC.
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes;
        var metadata = MetadataObject.Create(
            ("type", MetadataValue.FromString("app.success", annotation)),
            ("source", MetadataValue.FromString("urn:test:source", annotation)),
            (
                "time",
                MetadataValue.FromDateTime(
                    new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified),
                    annotation
                )
            )
        );
        var result = Result.Ok(metadata);

        var act = () => result.ToCloudEvent(options: CreateWriteOptions(source: null));

        act.Should().Throw<ArgumentException>()
           .Where(exception => exception.ParamName == "time")
           .WithMessage("*without a UTC offset*DateTimeKind.Utc*");
    }

    [Fact]
    public void ToCloudEventsEnvelopeForWriting_ShouldCreateEnvelopeWithFrozenOptionsAndConvertedExtensionAttributes()
    {
        var metadata = MetadataObject.Create(
            (
                "traceid",
                MetadataValue.FromString(
                    "abc",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            ),
            (
                "id",
                MetadataValue.FromString(
                    "evt-from-metadata",
                    MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
                )
            )
        );
        var result = Result<int>.Ok(42, metadata);

        var options = CreateWriteOptions(source: "urn:default:source");
        options.SuccessType = "app.success";
        options.FailureType = "app.failure";
        options.MetadataSerializationMode = MetadataSerializationMode.ErrorsOnly;

        var envelope = result.ToCloudEventsEnvelopeForWriting(options: options);

        envelope.Type.Should().Be("app.success");
        envelope.Source.Should().Be("urn:default:source");
        envelope.Id.Should().Be("evt-from-metadata");
        envelope.ResolvedOptions.MetadataSerializationMode.Should().Be(MetadataSerializationMode.ErrorsOnly);
        envelope.ExtensionAttributes.Should().NotBeNull();
        envelope.ExtensionAttributes!.Value.ContainsKey("traceid").Should().BeTrue();
    }

    [Fact]
    public void ToCloudEventsEnvelopeForWriting_ShouldGenerateIdWhenNoneIsProvided()
    {
        var result = Result.Ok();

        var envelope = result.ToCloudEventsEnvelopeForWriting(
            successType: "app.success",
            failureType: "app.failure",
            source: "urn:test:source",
            options: CreateWriteOptions()
        );

        envelope.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CloudEventsEnvelopeForWritingConverter_ShouldThrowOnRead()
    {
        const string json =
            "{\"specversion\":\"1.0\",\"type\":\"app.success\",\"source\":\"urn:test:source\",\"id\":\"evt-1\",\"data\":null}";

        Action act = () => JsonSerializer.Deserialize<CloudEventsEnvelopeForWriting>(
            json,
            PortableResultsCloudEventsWriteOptions.Default.SerializerOptions
        );

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void CloudEventsEnvelopeForWritingConverterGeneric_ShouldThrowOnRead()
    {
        const string json =
            "{\"specversion\":\"1.0\",\"type\":\"app.success\",\"source\":\"urn:test:source\",\"id\":\"evt-1\",\"data\":null}";

        Action act = () => JsonSerializer.Deserialize<CloudEventsEnvelopeForWriting<int>>(
            json,
            PortableResultsCloudEventsWriteOptions.Default.SerializerOptions
        );

        act.Should().Throw<NotSupportedException>();
    }

    private static PortableResultsCloudEventsWriteOptions CreateWriteOptions(string? source = "urn:test:source")
    {
        return new PortableResultsCloudEventsWriteOptions
        {
            Source = source
        };
    }
}
