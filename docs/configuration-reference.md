# Configuration reference

This page lists every options type of Light.PortableResults with its defaults, and the map from `ErrorCategory` to HTTP status code. The topic pages explain the behavior behind each option and link here instead of repeating the tables.

**Packages:** `Light.PortableResults` for the HTTP and CloudEvents options and `ErrorCategory`; `Light.PortableResults.Validation` for `ValidationContextOptions`.

<a id="http-write-options-portableresultshttpwriteoptions"></a>

## HTTP write options (`PortableResultsHttpWriteOptions`)

Namespace `Light.PortableResults.Http.Writing`. Registered with the options pattern by `AddPortableResultsForMinimalApis()` and `AddPortableResultsForMvc()`; every `ToMinimalApiResult` and `ToMvcActionResult` call can pass an override instance. See [HTTP](http.md).

| Option | Default | Description |
| --- | --- | --- |
| `ValidationProblemSerializationFormat` | `AspNetCoreCompatible` | Controls how validation errors are serialized for HTTP 400 and 422 responses (`AspNetCoreCompatible` or `Rich`). All other failures always use the rich format. We encourage using `Rich`. |
| `MetadataSerializationMode` | `ErrorsOnly` | Controls whether result-level metadata is serialized in success response bodies (`ErrorsOnly` or `Always`). Does not affect headers. |
| `CreateProblemDetailsInfo` | `null` | Optional custom factory for generating Problem Details fields (`type`, `title`, `status`, `detail`, `instance`) from the errors and metadata. `null` uses `ProblemDetailsInfo.CreateDefault`. |
| `FirstErrorCategoryIsLeadingCategory` | `true` | If `true`, the first error category decides the HTTP status code. If `false`, all errors must share the same category; otherwise `Unclassified` (500) is used. |

```csharp
builder.Services.Configure<PortableResultsHttpWriteOptions>(options =>
{
    options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich;
    options.MetadataSerializationMode = MetadataSerializationMode.Always;
    options.FirstErrorCategoryIsLeadingCategory = false;
});
```

<a id="http-read-options-portableresultshttpreadoptions"></a>

## HTTP read options (`PortableResultsHttpReadOptions`)

Namespace `Light.PortableResults.Http.Reading`. Passed to `ReadResultAsync`; `PortableResultsHttpReadOptions.Default` is used when you pass none. `AddPortableResultsHttpReadOptions()` registers it with the options pattern. See [Reading responses](http.md#reading-responses).

| Option | Default | Description |
| --- | --- | --- |
| `HeaderParsingService` | `ParseNoHttpHeadersService.Instance` | Controls how HTTP headers are converted into metadata. The default reads no headers. |
| `MergeStrategy` | `AddOrReplace` | Strategy when merging metadata with the same key from headers and body. Headers are read first, the body second. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `TreatProblemDetailsAsFailure` | `true` | If `true`, `application/problem+json` is treated as failure even for 2xx status codes. |
| `SerializerOptions` | `PortableResultsHttpReadingModule.DefaultSerializerOptions` | `System.Text.Json` serializer options used for deserialization. |

```csharp
var readOptions = new PortableResultsHttpReadOptions
{
    HeaderParsingService = new DefaultHttpHeaderParsingService(new AllHeadersSelectionStrategy()),
    PreferSuccessPayload = PreferSuccessPayload.Auto,
    TreatProblemDetailsAsFailure = true
};

Result<UserDto> result = await response.ReadResultAsync<UserDto>(readOptions);
```

<a id="cloudevents-write-options-portableresultscloudeventswriteoptions"></a>

## CloudEvents write options (`PortableResultsCloudEventsWriteOptions`)

Namespace `Light.PortableResults.CloudEvents.Writing`. Passed to `ToCloudEvent` and its siblings; `PortableResultsCloudEventsWriteOptions.Default` is used when you pass none. `AddPortableResultsCloudEventsWriteOptions()` registers it with the options pattern. See [CloudEvents](cloudevents.md).

| Option | Default | Description |
| --- | --- | --- |
| `Source` | `null` | Default CloudEvents `source` URI reference if not set per call or by metadata. |
| `MetadataSerializationMode` | `Always` | Controls whether result-level metadata is serialized into CloudEvents `data` of successful results. |
| `SerializerOptions` | `PortableResultsCloudEventsWritingModule.DefaultSerializerOptions` | `System.Text.Json` serializer options. |
| `ConversionService` | `DefaultCloudEventsAttributeConversionService.Instance` | Converts metadata entries into CloudEvents extension attributes. The default has no custom converters. |
| `SuccessType` | `null` | Default CloudEvents `type` for successful results. |
| `FailureType` | `null` | Default CloudEvents `type` for failed results. |
| `Subject` | `null` | Default CloudEvents `subject`. |
| `DataSchema` | `null` | Default CloudEvents `dataschema` URI. |
| `Time` | `null` | Default `time` value (UTC now is used when omitted). |
| `IdResolver` | `null` | Optional function used to generate CloudEvents `id` values. When `null` and no ID is passed, a new GUID string is generated for every event. |
| `ArrayPool` | `ArrayPool<byte>.Shared` | Buffer pool used for serialization. |
| `PooledArrayInitialCapacity` | `RentedArrayBufferWriter.DefaultInitialCapacity` (2048 bytes) | Initial buffer size for pooled serialization. |

```csharp
builder.Services
    .AddPortableResultsCloudEventsWriteOptions()
    .Configure<PortableResultsCloudEventsWriteOptions>(options =>
    {
        options.Source = "urn:light-portable-results:sample:user-service";
        options.SuccessType = "users.updated";
        options.FailureType = "users.update.failed";
        options.MetadataSerializationMode = MetadataSerializationMode.Always;
    });
```

<a id="cloudevents-read-options-portableresultscloudeventsreadoptions"></a>

## CloudEvents read options (`PortableResultsCloudEventsReadOptions`)

Namespace `Light.PortableResults.CloudEvents.Reading`. Passed to `ReadResult` and `ReadResultWithCloudEventsEnvelope`; `PortableResultsCloudEventsReadOptions.Default` is used when you pass none. `AddPortableResultsCloudEventsReadOptions()` registers it with the options pattern. See [Consuming](cloudevents.md#consuming).

| Option | Default | Description |
| --- | --- | --- |
| `SerializerOptions` | `PortableResultsCloudEventsReadingModule.DefaultSerializerOptions` | `System.Text.Json` serializer options. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `IsFailureType` | `null` | Optional fallback classifier to decide failure based on the CloudEvents `type` when the `lproutcome` extension attribute is missing. |
| `ParsingService` | `null` | Optional parser for mapping extension attributes to metadata. When `null`, extension attributes are not added to the result metadata. |
| `MergeStrategy` | `AddOrReplace` | Strategy when merging extension attributes and payload metadata. Extension attributes are read first, `data` second. |

```csharp
var cloudReadOptions = new PortableResultsCloudEventsReadOptions
{
    IsFailureType = eventType => eventType.EndsWith(".failed", StringComparison.Ordinal),
    PreferSuccessPayload = PreferSuccessPayload.Auto
};

Result<UserDto> result = messageBody.ReadResult<UserDto>(cloudReadOptions);
```

<a id="validationcontextoptions"></a>

## `ValidationContextOptions`

Namespace `Light.PortableResults.Validation`. Registered as a singleton by `AddValidationForPortableResults()` and used by `DefaultValidationContextFactory`. All properties are `init`-only and reject `null`. See [Validation configuration](validation/configuration.md).

| Property | Default | What it controls |
| --- | --- | --- |
| `ValueNormalizer` | `TrimStringNormalizer.Instance` | How values are normalized before checks see them. The default trims strings and converts `null` to `""`. Replace with `NoOpValueNormalizer.Instance` to disable. |
| `TargetNormalizer` | `ValidationTargets.DefaultNormalizer` | How caller-expression targets (e.g. `dto.ShippingAddress`) are converted to error target strings. The default produces camel case. |
| `CultureInfo` | `CultureInfo.InvariantCulture` | Culture used to format parameters in error messages and by localization-aware templates. |
| `AutomaticNullErrorProvider` | `DefaultAutomaticNullErrorProvider.Instance` | Produces the error when a validator receives a null source value. |
| `ErrorTemplates` | `ValidationErrorTemplates.Default` | The full set of built-in message templates. Replace individual templates to customize wording globally. |
| `ErrorDefinitionCache` | `ValidationErrorDefinitionCache.Default` | Shared cache for reusable definition instances. The default is a process-wide singleton. |

<a id="errorcategory-to-http-status-code"></a>

## `ErrorCategory` to HTTP status code

Namespace `Light.PortableResults`. The numeric value of every member is its status code, except for `Unclassified`. `ErrorCategoryExtensionsForHttp.ToHttpStatusCode()` performs the mapping; see [Status code selection](http.md#status-code-selection).

| `ErrorCategory` | HTTP Status |
| --- | --- |
| `Unclassified` | 500 |
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `PaymentRequired` | 402 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `MethodNotAllowed` | 405 |
| `NotAcceptable` | 406 |
| `Timeout` | 408 |
| `Conflict` | 409 |
| `Gone` | 410 |
| `LengthRequired` | 411 |
| `PreconditionFailed` | 412 |
| `ContentTooLarge` | 413 |
| `UriTooLong` | 414 |
| `UnsupportedMediaType` | 415 |
| `RequestedRangeNotSatisfiable` | 416 |
| `ExpectationFailed` | 417 |
| `ImATeapot` | 418 |
| `MisdirectedRequest` | 421 |
| `UnprocessableContent` | 422 |
| `Locked` | 423 |
| `FailedDependency` | 424 |
| `UpgradeRequired` | 426 |
| `PreconditionRequired` | 428 |
| `TooManyRequests` | 429 |
| `RequestHeaderFieldsTooLarge` | 431 |
| `UnavailableForLegalReasons` | 451 |
| `InternalError` | 500 |
| `NotImplemented` | 501 |
| `BadGateway` | 502 |
| `ServiceUnavailable` | 503 |
| `GatewayTimeout` | 504 |
| `InsufficientStorage` | 507 |
