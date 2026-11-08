# HTTP

Light.PortableResults writes `Result` and `Result<T>` as HTTP responses — the value on success and RFC 9457 Problem Details on failure — and reads such responses back into fully typed results on the client. This page covers the Minimal APIs and MVC integrations, the wire format, status code selection, response headers, enrichers, and reading responses with `HttpClient`.

**Packages:** `Light.PortableResults.AspNetCore.MinimalApis` or `Light.PortableResults.AspNetCore.Mvc` for writing responses; `Light.PortableResults` for reading them (namespace `Light.PortableResults.Http.Reading`).

All options and their defaults are listed in the [configuration reference](configuration-reference.md#http-write-options-portableresultshttpwriteoptions).

## Minimal APIs

Register the services once and convert results with `ToMinimalApiResult()`:

```csharp
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Http.Writing;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddPortableResultsForMinimalApis()
    .Configure<PortableResultsHttpWriteOptions>(
        // Rich format is recommended — it serializes Code, Target, Category, and Metadata of an error in one object.
        x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
    )
    .AddValidationForPortableResults()
    .AddSingleton<MovieRatingValidator>()
    .AddScoped<AddMovieRatingService>();

var app = builder.Build();

app.MapPut("/api/movieRatings", async (MovieRatingDto dto, AddMovieRatingService service) =>
{
    var result = await service.AddMovieRatingAsync(dto);
    return result.ToMinimalApiResult();
});

app.Run();
```

`AddPortableResultsForMinimalApis()` registers `PortableResultsHttpWriteOptions` with the options pattern, the header conversion service, and the JSON converters on the Minimal API `JsonOptions`.

| Method | Success status | Returns |
| --- | --- | --- |
| `result.ToMinimalApiResult()` | `200 OK`, or `successStatusCode` | `LightResult` / `LightResult<T>` |
| `result.ToHttp201CreatedMinimalApiResult(location)` | `201 Created` | `LightResult` / `LightResult<T>` |

Both methods accept optional parameters: `location` sets the `Location` header, `overrideOptions` replaces the registered `PortableResultsHttpWriteOptions` for this response, and `serializerOptions` replaces the `JsonSerializerOptions` from `JsonOptions`. The status code of a failure is always derived from its errors (see [Status code selection](#status-code-selection)).

To document the endpoint in OpenAPI and generate accurate schemas and examples from your validators, add `Light.PortableResults.Validation.OpenApi`, annotate your validator with `[GeneratePortableValidationOpenApi]`, and use `.ProducesPortableValidationProblemFor<TValidator>(...)` on the endpoint; see [OpenAPI](openapi.md#source-generation).

`LightResult` and `LightResult<T>` implement `IResult` and derive from `BaseLightResult<TResult>`, whose `SetHeaders` and `WriteBodyAsync` methods are virtual and protected so that you can customize them in a subclass.

## MVC

```csharp
using Light.PortableResults;
using Light.PortableResults.AspNetCore.Mvc;

builder.Services.AddControllers();
builder.Services
    .AddPortableResultsForMvc()
    .AddValidationForPortableResults()
    .AddSingleton<MovieRatingValidator>()
    .AddScoped<AddMovieRatingService>();

var app = builder.Build();
app.MapControllers();
app.Run();

[ApiController]
[Route("api/movieRatings")]
public sealed class AddMovieRatingsController : ControllerBase
{
    private readonly AddMovieRatingService _service;

    public AddMovieRatingsController(AddMovieRatingService service) => _service = service;

    [HttpPut]
    public async Task<LightActionResult<MovieRating>> AddMovieRating(MovieRatingDto dto)
    {
        var result = await _service.AddMovieRatingAsync(dto);
        return result.ToMvcActionResult();
    }
}
```

`AddPortableResultsForMvc()` registers the same services as the Minimal APIs integration and configures the MVC `JsonOptions`. `ToMvcActionResult()` and `ToHttp201CreatedMvcActionResult(location)` mirror their Minimal API counterparts and return `LightActionResult` / `LightActionResult<T>`, which implement `IActionResult`. The MVC integration is not Native AOT compatible; see [Native AOT](native-aot.md).

To generate OpenAPI schemas and examples from your validators, annotate the validator with `[GeneratePortableValidationOpenApi]` and put `[ProducesPortableValidationProblemFor<TValidator>]` on the action instead of `[ProducesPortableValidationProblem]`; see [OpenAPI](openapi.md#mvc).

<a id="wire-format"></a>

## Wire format

### Success

A successful `Result<T>` writes its value as `application/json` with `200 OK` (or the success status code you chose):

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "comment": "The Answer Is Out There, Neo. It's Looking for You.",
  "movieId": "5c200e1d-4a16-4572-b884-e3a3957771fc",
  "userName": "Trinity",
  "rating": 5,
  "id": "b507182e-f9ff-48d7-8a78-bcdc15cb4d0a"
}
```

A successful non-generic `Result` writes no body and no `Content-Type`.

<a id="metadata-in-response-bodies"></a>

### Metadata in response bodies

`MetadataSerializationMode` decides whether result-level metadata is part of successful responses. With the default `ErrorsOnly`, metadata is written for failures only. With `Always`, a successful `Result<T>` is wrapped in an object with a `value` and an optional `metadata` property, and a successful `Result` with body metadata writes `{ "metadata": { ... } }`:

```csharp
builder.Services.Configure<PortableResultsHttpWriteOptions>(
    x => x.MetadataSerializationMode = MetadataSerializationMode.Always
);
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "value": "contact-42",
  "metadata": {
    "source": "value-metadata"
  }
}
```

Only metadata values annotated with `SerializeInHttpResponseBody` (included in the default `SerializeInBodies`) appear in bodies; see [annotations](metadata.md#annotations-where-a-value-is-written). Response headers are independent of this setting.

### Failure

A failed result writes Problem Details as `application/problem+json`. The `type`, `title`, `status`, and `detail` members are derived from the leading error category; `instance` is written when your [problem details factory](#customizing-problem-details) sets it. The `errors` member follows, and result-level metadata is written as `metadata`. A `404` problem:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "errors": [
    {
      "message": "Contact with id 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' was not found",
      "code": "ContactNotFound",
      "category": "NotFound"
    }
  ]
}
```

Every error object contains `message` and, when set, `code`, `target`, `category` (omitted for `Unclassified`), and `metadata`.

### Validation responses

For `400 Bad Request` and `422 Unprocessable Content`, `ValidationProblemSerializationFormat` selects the shape of `errors`. All other failures always use the rich format.

`Rich` writes the same array of error objects as above. The [README walkthrough](../README.md#walkthrough) shows a complete example; this format is recommended.

`AspNetCoreCompatible` (the default, for backwards compatibility) writes `errors` as an object that maps each target to its messages, like ASP.NET Core's `ValidationProblemDetails`. Codes, categories, and metadata move into a separate `errorDetails` array, which is omitted when no error has any of them. `index` is the position of the message in the array of its target:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "comment": [
      "comment must be between 10 and 1000 characters long"
    ],
    "userName": [
      "userName must not be empty or whitespace"
    ],
    "rating": [
      "rating must be between 1 and 5"
    ]
  },
  "errorDetails": [
    {
      "target": "comment",
      "index": 0,
      "code": "LengthInRange",
      "category": "Validation",
      "metadata": {
        "minLength": 10,
        "maxLength": 1000
      }
    },
    {
      "target": "userName",
      "index": 0,
      "code": "NotNullOrWhiteSpace",
      "category": "Validation"
    },
    {
      "target": "rating",
      "index": 0,
      "code": "InRange",
      "category": "Validation",
      "metadata": {
        "lowerBoundary": 1,
        "upperBoundary": 5
      }
    }
  ]
}
```

<a id="validation-responses-require-targets"></a>

In both formats, every error of a `400` or `422` response must have a `Target`; writing such a response throws an `InvalidOperationException` otherwise. Use an empty string to refer to the root object — whitespace-only targets are written as `""` as well. Errors created by the [validation framework](validation/getting-started.md) always have a target.

<a id="status-code-selection"></a>

## Status code selection

The status code of a failure comes from the `ErrorCategory` of its errors: every category maps to the status code with the same numeric value, and `Unclassified` maps to `500`. The [configuration reference](configuration-reference.md#errorcategory-to-http-status-code) lists all categories.

When the errors of one result have different categories, `FirstErrorCategoryIsLeadingCategory` decides:

- `true` (default): the category of the first error determines the status code and the Problem Details members.
- `false`: all errors must share the same category; otherwise, `Unclassified` and thus `500 Internal Server Error` is used.

For example, a result with a `Validation` error followed by a `NotFound` error produces a `400` by default and a `500` with `FirstErrorCategoryIsLeadingCategory = false`. Order errors deliberately or keep categories homogeneous when you return several errors.

<a id="customizing-problem-details"></a>

## Customizing problem details

`CreateProblemDetailsInfo` replaces the default factory for the `type`, `title`, `status`, `detail`, and `instance` members. It receives the errors and the result-level metadata:

```csharp
using Light.PortableResults.Http;

builder.Services.Configure<PortableResultsHttpWriteOptions>(options =>
{
    options.CreateProblemDetailsInfo = (errors, metadata) =>
    {
        var defaultInfo = ProblemDetailsInfo.CreateDefault(errors, firstCategoryIsLeadingCategory: true);
        return defaultInfo with
        {
            Type = $"https://api.example.org/problems/{errors.First.Code ?? "unknown"}",
            Instance = metadata?.TryGetString("requestPath", out var path) == true ? path : null
        };
    };
});
```

`ProblemDetailsInfo.CreateDefault` uses the extension methods of `ErrorCategoryExtensionsForHttp`: `GetTypeUri()` (the RFC section of the status code), `GetTitle()`, `GetDetail()`, and `ToHttpStatusCode()`. The `Status` you return also decides whether the response counts as a validation response (`400` or `422`). Keep it consistent with the response status code, which is still determined by [status code selection](#status-code-selection).

## Response headers

Result-level metadata values annotated with `SerializeInHttpHeader` (or `SerializeInHttpHeaderAndBody`) become response headers — for successes and failures, and regardless of `MetadataSerializationMode`. By default, the metadata key is the header name:

```csharp
var metadata = MetadataObject.Create(
    ("X-Total-Count", MetadataValue.FromInt64(totalCount, MetadataValueAnnotation.SerializeInHttpHeader))
);
return Result<List<MovieRating>>.Ok(ratings, metadata).ToMinimalApiResult();
```

`HttpHeaderValueFormatter.Format` turns a value into header values: primitives become their [canonical text](metadata.md#canonical-text), and arrays of primitives become multiple values of the same header. A top-level `null` and an empty array produce no header. A single-item array is indistinguishable from a scalar after parsing, an empty array cannot be reconstructed because no header is emitted, and `null` items inside arrays are written as the text `null`. Objects and arrays with complex items cannot be headers.

To rename a header or format its value differently, derive from `HttpHeaderConverter` and register it in DI. `DefaultHttpHeaderConversionService` uses the converter registered for a metadata key and falls back to `HttpHeaderValueFormatter` for all other keys:

```csharp
using Light.PortableResults.Http.Writing.Headers;
using Microsoft.Extensions.Primitives;

public sealed class ETagHeaderConverter : HttpHeaderConverter
{
    public ETagHeaderConverter() : base(["etag"]) { }

    public override KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value) =>
        new ("ETag", $"\"{value.ToCanonicalString()}\"");
}

builder.Services.AddSingleton<HttpHeaderConverter, ETagHeaderConverter>();
```

`AddPortableResultsForMinimalApis()` and `AddPortableResultsForMvc()` call `AddPortableResultsHttpHeaderConversionService()`, which collects all registered converters into a frozen dictionary keyed by metadata key; two converters for the same key cause an `InvalidOperationException`. Call `AddPortableResultsHttpHeaderConversionService(metadataKeyComparer)` before the integration method to use a different key comparer, or register your own `IHttpHeaderConversionService`.

<a id="enriching-every-response"></a>

## Enriching every response

Register an `IHttpResultEnricher` (namespace `Light.PortableResults.AspNetCore.Shared.Enrichment`) to add cross-cutting metadata such as trace IDs to every `LightResult` and `LightActionResult`. The enricher runs before headers and body are written, so the metadata it adds follows the usual annotation and serialization rules:

```csharp
public sealed class TraceIdEnricher : IHttpResultEnricher
{
    public TResult Enrich<TResult>(TResult result, HttpContext httpContext)
        where TResult : struct, IResultObject, ICanReplaceMetadata<TResult> =>
        result.MergeMetadata(
            ("X-Trace-Id", MetadataValue.FromString(httpContext.TraceIdentifier, MetadataValueAnnotation.SerializeInHttpHeader))
        );
}

builder.Services.AddSingleton<IHttpResultEnricher, TraceIdEnricher>();
```

The enricher is resolved from the request services, so it may also be scoped.

## Lower-level building blocks

The integrations are built from public helpers that you can use for custom response types: `HttpExtensions` (namespace `Light.PortableResults.AspNetCore.Shared`) provides `SetStatusCodeFromResult`, `SetContentTypeFromResult`, `SetMetadataValuesAsHeadersIfNecessary`, and `ResolvePortableResultsHttpWriteOptions`. In `Light.PortableResults`, `result.ToHttpResultForWriting(options)` wraps a result for serialization through `System.Text.Json`, and `serializerOptions.AddDefaultPortableResultsHttpWriteJsonConverters()` registers the converters it needs.

<a id="reading-responses"></a>

## Reading responses

`ReadResultAsync<T>()` and `ReadResultAsync()` read an `HttpResponseMessage` into a result:

```csharp
using System.Net.Http.Json;
using Light.PortableResults;
using Light.PortableResults.Http.Reading;

using var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5000") };

using var response = await httpClient.PutAsJsonAsync("/api/movieRatings", requestDto);

Result<MovieRating> result = await response.ReadResultAsync<MovieRating>();

if (result.IsValid)
{
    Console.WriteLine($"Added movie rating with id {result.Value.Id}");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Target}: {error.Message}");
    }
}
```

How a response is interpreted:

- **Failure detection:** a response is a failure when its status code is not a success code, or when its content type is `application/problem+json` and `TreatProblemDetailsAsFailure` is `true` (default).
- **Failures** must contain Problem Details. Both the rich and the ASP.NET Core-compatible `errors` shapes are read, including `errorDetails`. Errors read from the ASP.NET Core-compatible shape get the `Validation` category unless `errorDetails` supplies another one. When a Problem Details payload has no `errors`, a single error is created from `detail` (or `title`) with the category whose value matches `status` (or `Unclassified`).
- **Successes of `Result<T>`** must contain a body. `PreferSuccessPayload` decides how it is read: `Auto` (default) treats the body as a `{ "value": ..., "metadata": ... }` wrapper only when its root object contains nothing but these two properties, `BareValue` always reads the body as the value, and `WrappedValue` always expects the wrapper.
- **Successes of `Result`** may have an empty body; a body may contain `metadata`.
- Bodies that violate these rules cause a `JsonException` or an `InvalidOperationException`.

Pass a `PortableResultsHttpReadOptions` instance to change the behavior. `AddPortableResultsHttpReadOptions()` registers it with the options pattern if you want to configure it in DI. When you supply your own `SerializerOptions`, add the reading converters with `AddDefaultPortableResultsHttpReadJsonConverters()`.

### Reading headers into metadata

By default, no headers are read (`ParseNoHttpHeadersService`). `DefaultHttpHeaderParsingService` reads response and content headers into metadata, which is then merged with the body metadata — headers first, body second, combined with `MergeStrategy`:

```csharp
using Light.PortableResults.Http.Reading.Headers;

var readOptions = new PortableResultsHttpReadOptions
{
    HeaderParsingService = new DefaultHttpHeaderParsingService(
        new AllowListHeaderSelectionStrategy("ETag", "X-Total-Count"),
        HttpHeaderParserRegistry.Create([new ETagHeaderParser()]),
        HeaderConflictStrategy.LastWriteWins,
        headerValueParsingMode: HeaderValueParsingMode.Primitive
    )
};

Result<List<MovieRating>> result = await response.ReadResultAsync<List<MovieRating>>(readOptions);
```

The parts of `DefaultHttpHeaderParsingService`:

- **Selection strategy** (`IHttpHeaderSelectionStrategy`) — decides which headers are read: `AllHeadersSelectionStrategy`, `NoHeadersSelectionStrategy`, `AllowListHeaderSelectionStrategy`, or `DenyListHeaderSelectionStrategy`. The list strategies compare header names case-insensitively unless you pass another comparer.
- **Parsers** (`HttpHeaderParser`) — map selected headers to a metadata key and value. `HttpHeaderParserRegistry.Create(parsers, headerNameComparer)` builds the registry (case-insensitive by default) and throws when two parsers claim the same header.
- **`HeaderValueParsingMode`** — how headers without a parser are read: `Primitive` (default) parses `bool`, 64-bit integers, and finite floating-point numbers and keeps everything else as a string, while `StringOnly` keeps all values as strings. Headers with several values become arrays.
- **`HeaderConflictStrategy`** — `Throw` (default) or `LastWriteWins` when two headers map to the same metadata key.
- **Annotation** — the annotation of the created values, `SerializeInHttpHeader` by default.

A parser that restores an `ETag` header as metadata:

```csharp
public sealed class ETagHeaderParser : HttpHeaderParser
{
    public ETagHeaderParser() : base("etag", ["ETag"]) { }

    public override MetadataValue ParseHeader(
        string headerName,
        IReadOnlyList<string> values,
        MetadataValueAnnotation annotation
    ) =>
        MetadataValue.FromString(values[0].Trim('"'), annotation);
}
```

Implement `IHttpHeaderParsingService` yourself for full control.
