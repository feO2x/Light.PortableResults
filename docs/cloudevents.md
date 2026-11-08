# CloudEvents

Light.PortableResults serializes `Result` and `Result<T>` as [CloudEvents Spec 1.0](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md) JSON envelopes and deserializes them on any consumer. The library produces and consumes UTF-8 bytes, so it works with every message broker client: RabbitMQ, Azure Service Bus, Kafka, or plain HTTP. This page covers publishing, the wire format, how consumers detect failures, extension attributes, and configuration.

**Package:** `Light.PortableResults` (namespaces `Light.PortableResults.CloudEvents.Writing` and `Light.PortableResults.CloudEvents.Reading`)

All options and their defaults are listed in the [configuration reference](configuration-reference.md#cloudevents-write-options-portableresultscloudeventswriteoptions).

<a id="publishing"></a>

## Publishing

`result.ToCloudEvent(...)` returns the envelope as a new byte array. Publishing it with RabbitMQ:

```csharp
using Light.PortableResults;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.CloudEvents.Writing;
using RabbitMQ.Client;

var result = Result<UserDto>.Ok(new UserDto
{
    Id = Guid.Parse("6b8a4dca-779d-4f36-8274-487fe3e86b5a"),
    Email = "ada@example.com"
});

byte[] cloudEvent = result.ToCloudEvent(
    successType: "users.updated",
    failureType: "users.update.failed",
    source: "urn:light-portable-results:sample:user-service",
    subject: "users/6b8a4dca-779d-4f36-8274-487fe3e86b5a"
);

var properties = new BasicProperties();
properties.ContentType = CloudEventsConstants.CloudEventsJsonContentType;

await channel.BasicPublishAsync(
    exchange: "",
    routingKey: "users.updated",
    mandatory: false,
    basicProperties: properties,
    body: cloudEvent
);
```

`CloudEventsConstants.CloudEventsJsonContentType` is `application/cloudevents+json`. Three alternatives avoid the final array copy:

- `ToCloudEventPooled(...)` returns an `IRentedArray` whose `Memory` is backed by a pooled buffer. Dispose it after the message was sent.
- `WriteCloudEvent(writer, ...)` writes to a `Utf8JsonWriter` you own.
- `ToCloudEventsEnvelopeForWriting(...)` resolves all attributes up front and returns a `CloudEventsEnvelopeForWriting(<T>)` that you can serialize later with `System.Text.Json`.

### Resolving the context attributes

All methods accept `successType`, `failureType`, `id`, `source`, `subject`, `dataschema`, `time`, and `options`. Each attribute is resolved from the first available of these sources:

| Attribute | Resolution order | Notes |
| --- | --- | --- |
| `type` | argument (`successType` or `failureType`, depending on the outcome), `SuccessType`/`FailureType` option, metadata attribute `type` | Required. |
| `source` | `source` argument, metadata attribute `source`, `Source` option | Required. Must be a valid URI reference. |
| `id` | `id` argument, `IdResolver` option, metadata attribute `id`, a newly generated GUID | |
| `subject` | `subject` argument, `Subject` option, metadata attribute `subject` | Optional. |
| `dataschema` | `dataschema` argument, `DataSchema` option, metadata attribute `dataschema` | Optional. Must be an absolute URI. |
| `time` | `time` argument, `Time` option, metadata attribute `time`, `DateTimeOffset.UtcNow` | A metadata timestamp must be RFC 3339 with an offset. |

"Metadata attribute" means a result-level metadata value annotated with `SerializeInCloudEventsExtensionAttributes` whose (converted) name is the attribute name — see [Extension attributes](#extension-attributes). An `InvalidOperationException` is thrown when `type` or `source` cannot be resolved, and an `ArgumentException` when `source`, `dataschema`, or `time` has an invalid format.

Set `id` explicitly for idempotent processing: otherwise, every call generates a new ID. Set `subject` for routing and observability.

## Wire format

Every envelope contains `specversion`, `type`, `source`, `id`, and `time`, followed by `subject` and `dataschema` when set. The library adds the extension attribute `lproutcome` with the value `success` or `failure`, so that consumers can tell the outcome without inspecting the type. `datacontenttype` is `application/json` whenever `data` is present.

A successful `Result<T>` with two metadata values — `tenant` annotated for extension attributes and `sequence` for extension attributes and data:

```json
{
  "specversion": "1.0",
  "type": "movie-ratings.added",
  "source": "urn:movies:rating-service",
  "subject": "movie-ratings/b507182e-f9ff-48d7-8a78-bcdc15cb4d0a",
  "id": "0193f1d2-7a4e-7c1a-9b2f-4d5e6f708192",
  "time": "2026-11-08T12:30:00.0000000+00:00",
  "lproutcome": "success",
  "datacontenttype": "application/json",
  "tenant": "contoso",
  "sequence": 42,
  "data": {
    "value": {
      "id": "b507182e-f9ff-48d7-8a78-bcdc15cb4d0a",
      "movieId": "5c200e1d-4a16-4572-b884-e3a3957771fc",
      "userName": "Trinity",
      "comment": "The Answer Is Out There, Neo.",
      "rating": 5
    },
    "metadata": {
      "sequence": 42
    }
  }
}
```

A failure writes the errors in the rich format, the same format as HTTP — but targets are optional, because there is no validation response rule:

```json
{
  "specversion": "1.0",
  "type": "movie-ratings.add-failed",
  "source": "urn:movies:rating-service",
  "id": "0193f1d2-7a4e-7c1a-9b2f-4d5e6f708193",
  "time": "2026-11-08T12:30:00.0000000+00:00",
  "lproutcome": "failure",
  "datacontenttype": "application/json",
  "data": {
    "errors": [
      {
        "message": "There is no movie with ID '5c200e1d-4a16-4572-b884-e3a3957771fc'",
        "code": "MovieNotFound",
        "target": "movieId",
        "category": "NotFound",
        "metadata": {
          "movieId": "5c200e1d-4a16-4572-b884-e3a3957771fc"
        }
      }
    ]
  }
}
```

A successful non-generic `Result` has no `data` and no `datacontenttype` unless it carries metadata for `data`. `MetadataSerializationMode` defaults to `Always` for CloudEvents, so result-level metadata annotated with `SerializeInCloudEventsData` is part of `data` for successes and failures; with `ErrorsOnly`, it is written for failures only.

<a id="consuming"></a>

## Consuming

`ReadResult<T>()` and `ReadResult()` are extension methods on `ReadOnlyMemory<byte>`. Consuming the event from above with RabbitMQ:

```csharp
using Light.PortableResults;
using Light.PortableResults.CloudEvents.Reading;
using RabbitMQ.Client.Events;

consumer.ReceivedAsync += async (_, eventArgs) =>
{
    Result<UserDto> result = eventArgs.Body.ReadResult<UserDto>();

    if (result.IsValid)
    {
        Console.WriteLine($"Updated user: {result.Value.Email}");
    }
    else
    {
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"{error.Target}: {error.Message}");
        }
    }

    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
};
```

### How failures are detected

1. When the envelope has an `lproutcome` extension attribute, its value decides: `success` or `failure` (case-insensitive). Any other value causes a `JsonException`.
2. Otherwise, the `IsFailureType` callback of `PortableResultsCloudEventsReadOptions` classifies the CloudEvents `type`, for example `eventType => eventType.EndsWith(".failed", StringComparison.Ordinal)`. This lets you consume events from producers that do not use this library.
3. Otherwise, an `InvalidOperationException` is thrown.

A `Result<T>` always requires non-null `data`, and so does a non-generic failure; a non-generic success may omit it. `PreferSuccessPayload` decides whether `data` of a `Result<T>` success is read as a `{ "value": ..., "metadata": ... }` wrapper (`Auto` detects it, `WrappedValue` requires it) or as the bare value (`BareValue`).

### Reading the envelope

`ReadResultWithCloudEventsEnvelope<T>()` and `ReadResultWithCloudEventsEnvelope()` return a `CloudEventsEnvelope<T>` or `CloudEventsEnvelope` with the context attributes (`Type`, `Source`, `Id`, `Subject`, `Time`, `DataContentType`, `DataSchema`), all `ExtensionAttributes` including `lproutcome`, and the deserialized result as `Data`.

`ReadResult` merges extension attributes into the result metadata only when you configure a `ParsingService`; see [Parsing extension attributes](#parsing-extension-attributes). Without it, extension attributes are available only through the envelope.

<a id="extension-attributes"></a>

## Extension attributes

Result-level metadata values annotated with `SerializeInCloudEventsExtensionAttributes` (or `SerializeInCloudEventsExtensionAttributesAndData`) become extension attributes. Their names must consist of lowercase ASCII letters and digits only, and `data`, `data_base64`, and `lproutcome` are reserved; violations cause an `ArgumentException`. Names of standard context attributes such as `type` or `subject` are not written as extensions — they feed the [attribute resolution](#resolving-the-context-attributes) instead.

### Encoding

CloudEvents extension attributes use the JSON Event Format context-attribute mapping, not each `MetadataKind`'s natural JSON shape:

| Metadata value | Extension-attribute JSON |
| --- | --- |
| `Null` | Omitted (unset) |
| `Boolean` | JSON boolean |
| `Int64` from `-2147483648` through `2147483647` | JSON number |
| Every other non-null primitive, including larger `Int64`, `Double`, `Single`, and `Decimal` | JSON string containing canonical invariant text |
| `Array` or `Object` | Rejected |

CloudEvents `String` values are validated without normalization: controls, Unicode noncharacters, and unpaired surrogates are rejected. This mapping affects only extension attributes; metadata inside `data` and `problem+json` keeps its normal JSON representation.

The `Int64` rule is value-dependent. For example, one extension name can be emitted as `2147483647` in one event and as `"2147483648"` in another. This is a deliberate deviation from CloudEvents' stable-type recommendation so all in-range CloudEvents integers retain their natural JSON representation while the full `long` domain remains lossless.

### Converters

A `CloudEventsAttributeConverter` changes the name or value of an attribute before it is written. If a key requires a stable string shape, convert it to `MetadataKind.String`:

```csharp
public sealed class StableSequenceConverter : CloudEventsAttributeConverter
{
    public StableSequenceConverter() : base(["sequence"]) { }

    public override KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
        string metadataKey,
        MetadataValue value
    ) =>
        new (
            metadataKey,
            MetadataValue.FromString(value.ToCanonicalString(), value.Annotation)
        );
}
```

Converters are applied by `DefaultCloudEventsAttributeConversionService`, which validates the converted name and value. Pass it through the `ConversionService` option:

```csharp
var converters = new Dictionary<string, CloudEventsAttributeConverter>
{
    ["sequence"] = new StableSequenceConverter()
}.ToFrozenDictionary();

var writeOptions = new PortableResultsCloudEventsWriteOptions
{
    Source = "urn:movies:rating-service",
    ConversionService = new DefaultCloudEventsAttributeConversionService(converters)
};
```

With dependency injection, register converters as `CloudEventsAttributeConverter` services and call `AddPortableResultsCloudEventsAttributeConversionService(metadataKeyComparer)`, which builds the dictionary from all registered converters (two converters for the same key cause an `InvalidOperationException`) and registers `ICloudEventsAttributeConversionService`. Assign that service to the `ConversionService` option, as shown in [Configuration with dependency injection](#configuration-with-dependency-injection). Implement `ICloudEventsAttributeConversionService` yourself for full control.

<a id="parsing-extension-attributes"></a>

### Parsing extension attributes

On read-back, a JSON boolean becomes `MetadataKind.Boolean`, an integer number becomes `MetadataKind.Int64`, and every string-mapped value initially becomes `MetadataKind.String`. Canonical out-of-range integer text remains accessible through `MetadataValue.TryGetInt64`. Inbound `null` means unset and is not added to extension metadata.

To add extension attributes to the result metadata, set `ParsingService` to a `DefaultCloudEventsAttributeParsingService`. Register a `CloudEventsAttributeParser` when a particular attribute must be restored to another original kind or stored under another metadata key:

```csharp
public sealed class SequenceAttributeParser : CloudEventsAttributeParser
{
    public SequenceAttributeParser() : base("sequence", ["sequence"]) { }

    public override MetadataValue ParseAttribute(
        string attributeName,
        MetadataValue value,
        MetadataValueAnnotation annotation
    ) =>
        value.TryGetInt64(out var sequence) ? MetadataValue.FromInt64(sequence, annotation) : value;
}

var readOptions = new PortableResultsCloudEventsReadOptions
{
    ParsingService = new DefaultCloudEventsAttributeParsingService(
        CloudEventsAttributeParserRegistry.Create([new SequenceAttributeParser()]),
        CloudEventsAttributeConflictStrategy.Throw
    ),
    MergeStrategy = MetadataMergeStrategy.AddOrReplace
};

Result<MovieRating> result = body.ReadResult<MovieRating>(readOptions);
```

`lproutcome` is never added to the metadata. Extension metadata is merged first and `data` metadata second, combined with `MergeStrategy`. Parsed values are annotated with `SerializeInCloudEventsExtensionAttributes` by default (third constructor parameter); `CloudEventsAttributeConflictStrategy` decides between `Throw` (default) and `LastWriteWins` when two attributes map to the same metadata key. `CloudEventsAttributeParserRegistry.Create` compares attribute names ordinally unless you pass another comparer and throws when two parsers claim the same attribute. With dependency injection, register parsers as `CloudEventsAttributeParser` services and call `AddPortableResultsCloudEventsAttributeParsingService(attributeNameComparer)` to register `ICloudEventsAttributeParsingService`.

<a id="configuration-with-dependency-injection"></a>

## Configuration with dependency injection

`ToCloudEvent` and `ReadResult` do not resolve anything from a service provider: they use `PortableResultsCloudEventsWriteOptions.Default` and `PortableResultsCloudEventsReadOptions.Default` unless you pass options. `AddPortableResultsCloudEventsWriteOptions()` and `AddPortableResultsCloudEventsReadOptions()` register the options with the options pattern and as singletons, so that you can configure them once and inject them where you publish or consume:

```csharp
builder.Services
    .AddPortableResultsCloudEventsWriteOptions()
    .AddPortableResultsCloudEventsAttributeConversionService()
    .AddSingleton<CloudEventsAttributeConverter, StableSequenceConverter>();

builder.Services
    .AddOptions<PortableResultsCloudEventsWriteOptions>()
    .Configure<ICloudEventsAttributeConversionService>((options, conversionService) =>
    {
        options.Source = "urn:light-portable-results:sample:user-service";
        options.SuccessType = "users.updated";
        options.FailureType = "users.update.failed";
        options.IdResolver = static () => Guid.CreateVersion7().ToString();
        options.ConversionService = conversionService;
    });

public sealed class UserEventPublisher(PortableResultsCloudEventsWriteOptions options, IChannel channel)
{
    public async Task PublishAsync(Result<UserDto> result) =>
        await channel.BasicPublishAsync("", "users", result.ToCloudEvent(options: options));
}
```

The read side works the same way with `PortableResultsCloudEventsReadOptions`.

## JSON serializer options

The options use `PortableResultsCloudEventsWritingModule.DefaultSerializerOptions` and `PortableResultsCloudEventsReadingModule.DefaultSerializerOptions`, which are `JsonSerializerDefaults.Web` options with the library's converters. When you supply your own `SerializerOptions`, call `AddDefaultPortableResultsCloudEventsWriteJsonConverters()` or `AddDefaultPortableResultsCloudEventsReadJsonConverters()` on them. For Native AOT, see [Native AOT](native-aot.md).
