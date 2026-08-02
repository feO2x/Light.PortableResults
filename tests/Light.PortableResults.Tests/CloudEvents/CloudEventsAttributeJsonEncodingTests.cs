using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.Metadata;
using Light.PortableResults.Tests.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents;

public sealed class CloudEventsAttributeJsonEncodingTests
{
    public static TheoryData<MetadataValue, CloudEventsAttributeJsonEncoding> PrimitiveValues =>
        new ()
        {
            { MetadataValue.Null, CloudEventsAttributeJsonEncoding.Null },
            { MetadataValue.FromBoolean(true), CloudEventsAttributeJsonEncoding.Boolean },
            { MetadataValue.FromInt64(int.MinValue), CloudEventsAttributeJsonEncoding.Integer },
            { MetadataValue.FromInt64(int.MaxValue), CloudEventsAttributeJsonEncoding.Integer },
            { MetadataValue.FromInt64((long) int.MinValue - 1), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromInt64((long) int.MaxValue + 1), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromDouble(5), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromString("text"), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromDecimal(19.50m), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromUInt64(ulong.MaxValue), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromSingle(0.1f), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromChar('x'), CloudEventsAttributeJsonEncoding.String },
            {
                MetadataValue.FromDateTime(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc)),
                CloudEventsAttributeJsonEncoding.String
            },
            {
                MetadataValue.FromDateTimeOffset(
                    new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2))
                ),
                CloudEventsAttributeJsonEncoding.String
            },
#if !TESTING_NETSTANDARD_ASSET
            { MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26)), CloudEventsAttributeJsonEncoding.String },
            { MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30)), CloudEventsAttributeJsonEncoding.String },
#endif
            { MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5)), CloudEventsAttributeJsonEncoding.String },
            {
                MetadataValue.FromGuid(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")),
                CloudEventsAttributeJsonEncoding.String
            },
            {
                MetadataValue.FromUri(new Uri("https://example.com/items/42")),
                CloudEventsAttributeJsonEncoding.String
            }
        };

    [Theory]
    [MemberData(nameof(PrimitiveValues))]
    public void GetCloudEventsAttributeJsonEncodingShouldClassifyEveryPrimitive(
        MetadataValue value,
        CloudEventsAttributeJsonEncoding expected
    )
    {
        value.GetCloudEventsAttributeJsonEncoding().Should().Be(expected);
    }

    [Fact]
    public void GetCloudEventsAttributeJsonEncodingShouldNameComplexKindsInFailures()
    {
        var array = MetadataValue.FromArray(MetadataArray.Empty);
        var @object = MetadataValue.FromObject(MetadataObject.Empty);

        Action arrayAct = () => array.GetCloudEventsAttributeJsonEncoding();
        Action objectAct = () => @object.GetCloudEventsAttributeJsonEncoding();

        arrayAct.Should().Throw<InvalidOperationException>().WithMessage("*Array*");
        objectAct.Should().Throw<InvalidOperationException>().WithMessage("*Object*");
    }

    [Fact]
    public void GetCloudEventsAttributeJsonEncodingShouldThrowForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        Action act = () => value.GetCloudEventsAttributeJsonEncoding();

#if TESTING_NETSTANDARD_ASSET
        act.Should().Throw<InvalidOperationException>();
#else
        act.Should().Throw<SwitchExpressionException>();
#endif
    }
}
