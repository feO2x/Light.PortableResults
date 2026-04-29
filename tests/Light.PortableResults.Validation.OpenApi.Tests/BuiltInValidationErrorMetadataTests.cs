using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Light.PortableResults.Validation.Definitions;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class BuiltInValidationErrorMetadataTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static TheoryData<object, string[]> MetadataCases =>
        new ()
        {
            { new EqualToMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            { new NotEqualToMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            { new GreaterThanMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            { new GreaterThanOrEqualToMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            { new LessThanMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            { new LessThanOrEqualToMetadata<int>(42), [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                new InRangeMetadata<DateTime>(
                    new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc),
                    new DateTime(2024, 2, 4, 4, 5, 6, DateTimeKind.Utc)
                ),
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                new NotInRangeMetadata<int>(1, 10),
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                new ExclusiveRangeMetadata<int>(1, 10),
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            }
        };

    [Theory]
    [MemberData(nameof(MetadataCases))]
    public void MetadataRecords_ShouldSerializeExpectedProperties(object metadata, string[] expectedProperties)
    {
        var payload = JsonNode.Parse(JsonSerializer.Serialize(metadata, SerializerOptions))
           .Should()
           .BeOfType<JsonObject>()
           .Subject;

        payload.Select(static pair => pair.Key).Should().BeEquivalentTo(expectedProperties);
        foreach (var property in expectedProperties)
        {
            payload[property].Should().NotBeNull();
        }
    }
}
