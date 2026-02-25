using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class MetadataObjectDataCoverageTests
{
    private static readonly Type MetadataObjectDataType =
        typeof(MetadataObject).Assembly.GetType(
            "Light.PortableResults.Metadata.MetadataObjectData",
            throwOnError: true
        )!;

    [Fact]
    public void Constructor_WithNullEntries_ShouldThrowArgumentNullException()
    {
        var constructor = MetadataObjectDataType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(KeyValuePair<string, MetadataValue>[]), typeof(IEqualityComparer<string>)],
            modifiers: null
        );

        constructor.Should().NotBeNull();

        var act = () => constructor.Invoke([null!, null]);

        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>()
           .Which.ParamName.Should().Be("entries");
    }

    [Fact]
    public void EqualsTyped_WithNullOther_ShouldReturnFalse()
    {
        var instance = CreateMetadataObjectData(("key", 1));
        var equalsMethod = MetadataObjectDataType.GetMethod("Equals", [MetadataObjectDataType]);

        equalsMethod.Should().NotBeNull();

        var result = (bool) equalsMethod!.Invoke(instance, [null!])!;

        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_WithNullKey_ShouldThrowArgumentNullException()
    {
        var instance = CreateMetadataObjectData(("key", 1));
        var tryGetValueMethod = MetadataObjectDataType.GetMethod(
            "TryGetValue",
            [typeof(string), typeof(MetadataValue).MakeByRefType()]
        );

        tryGetValueMethod.Should().NotBeNull();

        var args = new object?[] { null, null };
        var act = () => _ = tryGetValueMethod!.Invoke(instance, args);

        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>()
           .Which.ParamName.Should().Be("key");
    }

    [Fact]
    public void EqualsObject_WithNonMetadataObjectData_ShouldReturnFalse()
    {
        var instance = CreateMetadataObjectData(("key", 1));

        var result = instance.Equals("not metadata object data");

        result.Should().BeFalse();
    }

    private static object CreateMetadataObjectData(params (string Key, MetadataValue Value)[] values)
    {
        var entries = new KeyValuePair<string, MetadataValue>[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            entries[i] = new KeyValuePair<string, MetadataValue>(values[i].Key, values[i].Value);
        }

        var constructor = MetadataObjectDataType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(KeyValuePair<string, MetadataValue>[]), typeof(IEqualityComparer<string>)],
            modifiers: null
        )!;

        return constructor.Invoke([entries, null]);
    }
}
