using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Reading.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Light.PortableResults.Tests.SourceGeneratedSerialization;
using Xunit;

namespace Light.PortableResults.Tests.SharedJsonSerialization;

public sealed class PortableResultsJsonContractsTests
{
    [Fact]
    public void CreateMissingTypeInfoMessageShouldRejectNullType()
    {
        var act = () => PortableResultsJsonContracts.CreateMissingTypeInfoMessage(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("type");
    }

    [Fact]
    public void CreateMissingLibraryContractMessageShouldRejectNullType()
    {
        var act = () => PortableResultsJsonContracts.CreateMissingLibraryContractMessage(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("type");
    }

    [Fact]
    public void EnsureTypeInfoResolverShouldRejectNullOptions()
    {
        var act = () => PortableResultsJsonContracts.EnsureTypeInfoResolver(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void GetRequiredTypeInfoShouldRejectNullOptions()
    {
        var act = () => PortableResultsJsonContracts.GetRequiredTypeInfo<MovieDto>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void GetLibraryTypeInfoShouldRejectNullOptions()
    {
        var act = () => PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void WriteLibraryValueShouldRejectNullWriter()
    {
        var act = () => PortableResultsJsonContracts.WriteLibraryValue(
            null!,
            new CloudEventsSuccessPayload(null),
            new JsonSerializerOptions()
        );

        act.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void WriteLibraryValueShouldRejectNullOptions()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var act = () => PortableResultsJsonContracts.WriteLibraryValue(
            writer,
            new CloudEventsSuccessPayload(null),
            null!
        );

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void EnsureTypeInfoResolverShouldMaterializeTheReflectionResolver()
    {
        var options = PortableResultsCloudEventsReadingModule.CreateDefaultSerializerOptions();
        options.TypeInfoResolver.Should().BeNull();

        PortableResultsJsonContracts.EnsureTypeInfoResolver(options);

        options.TypeInfoResolver.Should().NotBeNull();
    }

    [Fact]
    public void EnsureTypeInfoResolverShouldKeepAConfiguredResolver()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions();
        options.AddDefaultPortableResultsCloudEventsReadJsonConverters();

        PortableResultsJsonContracts.EnsureTypeInfoResolver(options);

        options.TypeInfoResolver.Should().BeSameAs(MovieJsonContext.Default);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldCreateTheContractAfterTheOptionsBecameReadOnly()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions();
        options.AddDefaultPortableResultsCloudEventsReadJsonConverters();
        options.MakeReadOnly();

        var typeInfo = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);

        typeInfo.Should().NotBeNull();
        typeInfo.Type.Should().Be(typeof(CloudEventsSuccessPayload));
    }

    [Fact]
    public void GetLibraryTypeInfoShouldCreateTheContractOnlyOncePerOptionsInstance()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions();
        options.AddDefaultPortableResultsCloudEventsReadJsonConverters();
        options.MakeReadOnly();

        var first = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);
        var second = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldCreateTheContractOnlyOnceForOptionsTheCallerNeverFroze()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        options.Converters.Add(new FixedSuccessPayloadConverter());

        var first = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);
        var second = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldCreateSeparateContractsPerOptionsInstance()
    {
        var firstOptions = SourceGeneratedOptions.CreateSerializerOptions();
        firstOptions.AddDefaultPortableResultsCloudEventsReadJsonConverters();
        firstOptions.MakeReadOnly();
        var secondOptions = SourceGeneratedOptions.CreateSerializerOptions();
        secondOptions.AddDefaultPortableResultsCloudEventsReadJsonConverters();
        secondOptions.MakeReadOnly();

        var first = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(firstOptions);
        var second = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(secondOptions);

        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldPreferTheContractOfTheConfiguredResolver()
    {
        var options = PortableResultsCloudEventsReadingModule.CreateDefaultSerializerOptions();

        var typeInfo = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);

        typeInfo.Should().BeSameAs(options.GetTypeInfo(typeof(CloudEventsSuccessPayload)));
    }

    [Fact]
    public void GetLibraryTypeInfoShouldUseTheConverterRegisteredBeforeTheDefaults()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        options.Converters.Add(new FixedSuccessPayloadConverter());
        options.AddDefaultPortableResultsCloudEventsReadJsonConverters();

        var typeInfo = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);
        var payload = JsonSerializer.Deserialize("{}"u8, typeInfo);

        payload.Metadata.Should().Be(FixedSuccessPayloadConverter.Metadata);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldFallBackToTheLibraryConverterWhenNoneIsRegistered()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);

        var typeInfo = PortableResultsJsonContracts.GetLibraryTypeInfo(
            options,
            static () => new CloudEventsSuccessPayloadJsonConverter()
        );
        var payload = JsonSerializer.Deserialize("""{"metadata":{"note":"body"}}"""u8, typeInfo);

        payload.Metadata.Should().Be(MetadataObject.Create(("note", MetadataValue.FromString("body"))));
    }

    [Fact]
    public void GetLibraryTypeInfoShouldSkipConvertersThatCannotProduceAMatchingConverter()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        options.Converters.Add(new NullReturningConverterFactory());
        options.Converters.Add(new UntypedGreedyConverter());
        options.Converters.Add(new FixedSuccessPayloadConverter());

        var typeInfo = PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);
        var payload = JsonSerializer.Deserialize("{}"u8, typeInfo);

        payload.Metadata.Should().Be(FixedSuccessPayloadConverter.Metadata);
    }

    [Fact]
    public void GetLibraryTypeInfoShouldThrowWhenNeitherContractNorConverterIsAvailable()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);

        var act = () => PortableResultsJsonContracts.GetLibraryTypeInfo<CloudEventsSuccessPayload>(options);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(
                PortableResultsJsonContracts.CreateMissingLibraryContractMessage(typeof(CloudEventsSuccessPayload))
            );
    }

    [Fact]
    public void GetRequiredTypeInfoShouldThrowWhenTheConsumerTypeIsNotRegistered()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);

        var act = () => PortableResultsJsonContracts.GetRequiredTypeInfo<MovieDto>(options);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(PortableResultsJsonContracts.CreateMissingTypeInfoMessage(typeof(MovieDto)));
    }

    [Fact]
    public void WriteLibraryValueShouldUseTheConverterRegisteredBeforeTheDefaults()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        options.Converters.Add(new FixedSuccessPayloadConverter());
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            PortableResultsJsonContracts.WriteLibraryValue(writer, new CloudEventsSuccessPayload(null), options);
        }

        Encoding.UTF8.GetString(stream.ToArray()).Should().Be(FixedSuccessPayloadConverter.WrittenJson);
    }

    [Fact]
    public void WriteLibraryValueShouldUseTheContractOfTheConfiguredResolver()
    {
        var options = PortableResultsCloudEventsReadingModule.CreateDefaultSerializerOptions();
        options.Converters.Insert(0, new FixedSuccessPayloadConverter());
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            PortableResultsJsonContracts.WriteLibraryValue(writer, new CloudEventsSuccessPayload(null), options);
        }

        options.TypeInfoResolver.Should().NotBeNull();
        Encoding.UTF8.GetString(stream.ToArray()).Should().Be(FixedSuccessPayloadConverter.WrittenJson);
    }

    [Fact]
    public void WriteLibraryValueShouldReuseTheResolvedConverter()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        var converter = new CountingSuccessPayloadConverter();
        options.Converters.Add(converter);
        options.MakeReadOnly();

        WriteSuccessPayload(options);
        WriteSuccessPayload(options);

        converter.CanConvertCallCount.Should().Be(1);
    }

    [Fact]
    public void WriteLibraryValueShouldReuseTheResolvedConverterForOptionsTheCallerNeverFroze()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        var converter = new CountingSuccessPayloadConverter();
        options.Converters.Add(converter);

        WriteSuccessPayload(options);
        WriteSuccessPayload(options);

        converter.CanConvertCallCount.Should().Be(1);
    }

    [Fact]
    public void WriteLibraryValueShouldThrowWhenNeitherContractNorConverterIsAvailable()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var act = () => PortableResultsJsonContracts.WriteLibraryValue(
            writer,
            new CloudEventsSuccessPayload(null),
            options
        );

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(
                PortableResultsJsonContracts.CreateMissingLibraryContractMessage(typeof(CloudEventsSuccessPayload))
            );
    }

    private static void WriteSuccessPayload(JsonSerializerOptions options)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        PortableResultsJsonContracts.WriteLibraryValue(writer, new CloudEventsSuccessPayload(null), options);
    }

    private sealed class FixedSuccessPayloadConverter : JsonConverter<CloudEventsSuccessPayload>
    {
        public const string WrittenJson = """{"fixed":true}""";

        public static MetadataObject Metadata { get; } =
            MetadataObject.Create(("origin", MetadataValue.FromString("custom-converter")));

        public override CloudEventsSuccessPayload Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            reader.Skip();
            return new CloudEventsSuccessPayload(Metadata);
        }

        public override void Write(
            Utf8JsonWriter writer,
            CloudEventsSuccessPayload value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteBoolean("fixed", true);
            writer.WriteEndObject();
        }
    }

    private sealed class NullReturningConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(CloudEventsSuccessPayload);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => null;
    }

    private sealed class UntypedGreedyConverter : JsonConverter<string>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(CloudEventsSuccessPayload);

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }

    private sealed class CountingSuccessPayloadConverter : JsonConverter<CloudEventsSuccessPayload>
    {
        public int CanConvertCallCount { get; private set; }

        public override bool CanConvert(Type typeToConvert)
        {
            CanConvertCallCount++;
            return base.CanConvert(typeToConvert);
        }

        public override CloudEventsSuccessPayload Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) =>
            throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter writer,
            CloudEventsSuccessPayload value,
            JsonSerializerOptions options
        ) =>
            writer.WriteNullValue();
    }
}
