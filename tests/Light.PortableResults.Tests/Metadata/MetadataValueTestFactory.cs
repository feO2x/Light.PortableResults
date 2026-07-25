using System;
using System.Reflection;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Tests.Metadata;

/// <summary>
/// Creates metadata values whose kind and payload do not match what the factory methods on
/// <see cref="MetadataValue" /> produce - a kind that is not declared on the enum, or a declared kind whose payload
/// is empty. No dispatch site on <see cref="MetadataKind" /> fails to compile when a new kind is added, thus the
/// fallback arms are the only safety net left and they have to be pinned by tests.
/// </summary>
internal static class MetadataValueTestFactory
{
    public static MetadataValue CreateWithUndeclaredKind(MetadataKind kind = (MetadataKind) byte.MaxValue) =>
        CreateWithEmptyPayload(kind);

    public static MetadataValue CreateWithEmptyPayload(MetadataKind kind)
    {
        var metadataValueType = typeof(MetadataValue);
        var metadataPayloadType = metadataValueType.Assembly.GetType(
            "Light.PortableResults.Metadata.MetadataPayload",
            throwOnError: true
        )!;

        var constructor = metadataValueType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(MetadataKind), metadataPayloadType, typeof(MetadataValueAnnotation)],
            modifiers: null
        )!;

        var payload = Activator.CreateInstance(metadataPayloadType)!;

        return (MetadataValue) constructor.Invoke([kind, payload, MetadataValue.DefaultAnnotation]);
    }
}
