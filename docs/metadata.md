# Metadata

Metadata is additional, machine-readable context that you attach to a result or to a single error: a correlation ID, the boundaries of a failed range check, or an ETag that should become a response header. Metadata is not a `Dictionary<string, object>`. It uses a dedicated JSON-like type system so that every result serializes and deserializes reliably across HTTP, CloudEvents, or any other protocol, and each value carries an annotation that decides where it is written.

**Package:** `Light.PortableResults` (namespace `Light.PortableResults.Metadata`)

## Attaching and reading metadata

Metadata can be attached to `Result<T>` and `Result` instances as well as to individual `Error` instances.

```csharp
using Light.PortableResults;
using Light.PortableResults.Metadata;

// MetadataObject uses implicit conversions from the supported primitive types,
// nested objects, and arrays.
var metadata = MetadataObject.Create(
    ("requestId", "550e8400-e29b-41d4-a716-446655440000"),
    ("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
    ("cacheHit", false),
    ("attemptCount", 3)
);

Result<Order> result = Result<Order>.Ok(
    new Order { Id = Guid.NewGuid(), Total = 99.99m },
    metadata
);

// Attach metadata to an error for additional context
var error = new Error
{
    Message = "Order exceeds account limit",
    Code = "order.limit_exceeded",
    Target = "total",
    Category = ErrorCategory.Validation,
    Metadata = MetadataObject.Create(
        ("accountLimit", 500.00m),
        ("requestedAmount", 599.99m),
        ("currency", "USD")
    )
};

// Read metadata from a result
if (result.Metadata?.TryGetString("requestId", out var requestId) == true)
{
    Console.WriteLine($"Request: {requestId}");
}
```

The type system consists of three immutable structs:

- `MetadataObject` — a read-only dictionary of `string` keys and `MetadataValue` values that preserves insertion order. Create it with `MetadataObject.Create(...)`; `MetadataObject.Empty` is the empty instance. `TryGetValue`, `TryGetBoolean`, `TryGetInt64`, `TryGetDouble`, `TryGetString`, `TryGetDecimal`, `TryGetArray`, and `TryGetObject` read entries.
- `MetadataArray` — a read-only list of `MetadataValue` items. Create it with `MetadataArray.Create(...)`; `MetadataArray.Empty` is the empty instance and `AsSpan()` gives zero-allocation access.
- `MetadataValue` — a discriminated union whose `Kind` determines the payload.

Equality of all three types is structural and ignores annotations.

## Value kinds

`MetadataValue` has a `From...` factory and an implicit conversion for every supported .NET type. `MetadataKind` describes what a value holds, and `JsonShape` (`MetadataJsonShape`) and `Kind.GetNumberEncoding()` (`MetadataNumberEncoding`) describe how it is written to JSON.

| `MetadataKind` | Created from | JSON shape | Notes |
| --- | --- | --- | --- |
| `Null` | `MetadataValue.Null`, `FromNull`, a `null` string or URI | `null` | |
| `Boolean` | `bool` | boolean | |
| `Int64` | `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long` | number | |
| `UInt64` | `ulong` | string | Written as canonical text so that JSON consumers do not lose precision. |
| `Single` | `float` | number | Must be finite. |
| `Double` | `double` | number | Must be finite. |
| `Decimal` | `decimal` | number | The scale is preserved. |
| `String` | `string` | string | |
| `Char` | `char` | string | |
| `DateTime` | `DateTime` | string | `Local` values are converted to UTC. `Unspecified` values are written without a UTC designator, which is valid ISO 8601 but not RFC 3339; prefer `Utc` or `DateTimeOffset` at API boundaries. |
| `DateTimeOffset` | `DateTimeOffset` | string | RFC 3339 with an explicit offset; a zero offset is written as `+00:00`. |
| `DateOnly` | `DateOnly` (`net10.0` only) | string | RFC 3339 full-date, `yyyy-MM-dd`. |
| `TimeOnly` | `TimeOnly` (`net10.0` only) | string | RFC 3339 full-time. |
| `TimeSpan` | `TimeSpan` | string | ISO 8601 / XML Schema duration, for example `PT1H30M`. |
| `Guid` | `Guid` | string | Lowercase `D` format. |
| `Uri` | `Uri` | string | The original string of the URI. |
| `Array` | `MetadataArray` | array | |
| `Object` | `MetadataObject` | object | |

`MetadataKind.IsPrimitive()` is `true` for every kind except `Array` and `Object`.

### Canonical text

Every primitive value has a culture-independent canonical text encoding, which is used for JSON strings, HTTP headers, CloudEvents extension attributes, and validation messages. `ToCanonicalString()` returns it; `TryFormatCanonical(Span<char>, out int)` and `TryFormatCanonicalUtf8(Span<byte>, out int)` write it without allocating. `MetadataValue.MaximumPrimitiveCanonicalLength` is a destination size that fits every primitive kind except the unbounded `String` and `Uri`.

### What survives a round trip

A JSON number or string carries no .NET type, so reading metadata from JSON cannot restore every kind:

- JSON numbers become `Int64` when they fit into a 64-bit integer and `Double` otherwise. They never become `Decimal`: preferring decimals would make the resulting kind depend on the magnitude of the value and would break round-tripping in the opposite direction, where a `Double` would read back as a decimal.
- JSON strings become `String` — including the canonical text of `UInt64`, `Char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, and `Uri` values.

The typed accessors therefore also parse canonical text and convert compatible numbers. `TryGetGuid`, `TryGetDateTimeOffset`, `TryGetTimeSpan`, and their siblings succeed on a `String` value that holds the corresponding canonical text, `TryGetDouble` widens `Single` values, and `TryGetDecimal` converts integers, floating-point numbers, and invariant numeric strings. Two details matter for date and time values:

- `TryGetDateTime` accepts UTC and unspecified text and rejects text with a numeric offset, because resolving it would depend on the time zone of the reading machine.
- `TryGetDateTimeOffset` accepts both `+00:00` and `Z` for a zero offset and rejects text without an offset.

## Annotations: where a value is written

Each `MetadataValue` has an `Annotation` (`MetadataValueAnnotation`, a flags enum) that decides which parts of a message contain it. Pass the annotation as the second argument of a `From...` factory; values created by implicit conversion use `MetadataValue.DefaultAnnotation`, which is `SerializeInBodies`.

| Annotation | Written to |
| --- | --- |
| `None` | Nowhere — the value stays in the process. |
| `SerializeInHttpResponseBody` | The HTTP response body. |
| `SerializeInHttpHeader` | An HTTP response header. Only primitives and arrays of primitives. |
| `SerializeInHttpHeaderAndBody` | Both of the above. |
| `SerializeInCloudEventsData` | The CloudEvents `data` payload. |
| `SerializeInCloudEventsExtensionAttributes` | A CloudEvents extension attribute. Only primitives. |
| `SerializeInCloudEventsExtensionAttributesAndData` | Both of the above. |
| `SerializeInBodies` (default) | The HTTP response body and the CloudEvents `data` payload. |

```csharp
var metadata = MetadataObject.Create(
    ("etag", MetadataValue.FromString("\"33a64df5\"", MetadataValueAnnotation.SerializeInHttpHeader)),
    ("tenant", MetadataValue.FromString("contoso", MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)),
    ("internalTrace", MetadataValue.FromString(traceId, MetadataValueAnnotation.None))
);
```

The factories reject impossible combinations with an `ArgumentException`: objects can be neither headers nor extension attributes, arrays cannot be extension attributes, and arrays that contain nested arrays or objects cannot be headers. `value.HasAnnotation(annotation)` and `metadata.HasAnyValuesWithAnnotation(annotation)` query annotations.

Annotations are honored per value, including values nested in arrays and objects. A few rules apply on top of them:

- Headers and CloudEvents extension attributes are created only from result-level metadata, never from error metadata.
- Headers are always written, regardless of `MetadataSerializationMode`. Result-level metadata in a successful HTTP body is written only with `MetadataSerializationMode.Always`; see [HTTP](http.md#metadata-in-response-bodies).
- When metadata is read back, values get the annotation of the place they came from, for example `SerializeInHttpResponseBody` for body metadata and `SerializeInHttpHeader` for header metadata.

## Building metadata efficiently

`MetadataObjectBuilder` and `MetadataArrayBuilder` collect entries in pooled buffers and are the cheapest way to create metadata with many or dynamically determined entries. They are mutable structs: keep them in a local variable, do not copy them, and dispose them.

```csharp
using var builder = MetadataObjectBuilder.Create(capacity: 4);
builder.Add("orderId", order.Id);
builder.Add("itemCount", order.Items.Count);
builder.AddOrReplace("itemCount", order.Items.Count + 1);
MetadataObject metadata = builder.Build();

using var arrayBuilder = MetadataArrayBuilder.Create();
foreach (var tag in order.Tags)
{
    arrayBuilder.Add(tag);
}

MetadataArray tags = arrayBuilder.Build();
```

`MetadataObjectBuilder.From(existing)` starts with the entries of an existing object. `Add` throws an `ArgumentException` for a duplicate key, `Replace` throws a `KeyNotFoundException` for a missing key, and `AddOrReplace` does either. `TryGetValue`, `ContainsKey`, and `Count` inspect the collected entries, and `SetKeyComparer` changes key comparison (ordinal by default). A builder can build only once; afterwards every call throws an `InvalidOperationException`.

## Merging metadata

`metadata.With(key, value)` and `metadata.With(params (string Key, MetadataValue Value)[])` return a copy with added or replaced entries. `original.Merge(incoming, strategy)` combines two objects according to a `MetadataMergeStrategy`:

| `MetadataMergeStrategy` | Behavior for keys present in both objects |
| --- | --- |
| `AddOrReplace` (default) | The incoming value wins. Nested objects are merged recursively; arrays are replaced wholesale. |
| `PreserveExisting` | The original value is kept. |
| `FailOnConflict` | An `InvalidOperationException` is thrown. |

`MetadataObjectExtensions.MergeIfNeeded(existing, incoming, strategy)` works with nullable objects and returns an existing instance without allocating when one side is empty or both are equal.

Results and their metadata are immutable, so the result extensions return new instances:

```csharp
result = result.MergeMetadata(("tenant", "contoso"), ("region", "eu-west"));
result = result.MergeMetadata(otherMetadata, MetadataMergeStrategy.PreserveExisting);
result = result.ClearMetadata();
result = result.ReplaceMetadata(metadata);
```

`Bind` merges the metadata of both results, and the HTTP and CloudEvents readers merge header or extension-attribute metadata with body metadata; all of them accept a `MetadataMergeStrategy`.

## Tracing

The `Tracing` extensions in `Light.PortableResults.MetadataExtensions` store a source and a correlation ID under the well-known keys `Tracing.SourceKey` (`"source"`) and `Tracing.CorrelationIdKey` (`"correlationId"`):

```csharp
using Light.PortableResults.MetadataExtensions;

result = result.WithTracing(source: "rating-service", correlationId: Activity.Current?.TraceId.ToString() ?? "");

if (result.TryGetCorrelationId(out var correlationId))
{
    logger.LogInformation("Correlation ID {CorrelationId}", correlationId);
}
```

`WithSource` and `WithCorrelationId` set one value each, and `TryGetSource` reads the source back. The values use the default annotation, so they travel in HTTP bodies and CloudEvents `data`. To add them to every HTTP response automatically, use an [enricher](http.md#enriching-every-response).
