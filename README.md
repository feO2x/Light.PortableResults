# Light.PortableResults

*The Result Pattern for .NET that travels. Every `Result<T>` serializes reliably over HTTP (with RFC-9457 Problem Details support), CloudEvents, and back — with a validation framework that is at least 5x faster and uses less than 9% of the memory of FluentValidation.*

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-0.7.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages?q=Light.PortableResults)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/releases)

Most Result Pattern libraries stop at the application boundary. Light.PortableResults does not: a `Result<T>` can be written as an HTTP response (including RFC-9457 Problem Details support), published as a CloudEvents JSON message, read back from both protocols on the other side, and arrive as a fully-typed `Result<T>` — without losing errors, metadata, or structure. If you also need validation, the built-in framework lets you write FluentValidation-style rules with a fraction of the allocations. Plus: Roslyn Source Generators write OpenAPI error schemas and examples for you.

## Contents

- [Key Features](#key-features)
- [Installation](#installation)
- [When to Use Result vs. Exceptions](#when-to-use-result-vs-exceptions)
- [Basic Usage](#basic-usage)
- [Functional Operators](#functional-operators)
- [Metadata](#metadata)
- [Validation Quick Start](#validation-quick-start)
- [Validation Performance](#validation-performance)
- [HTTP Quick Start](#http-quick-start)
- [CloudEvents Quick Start](#cloudevents-quick-start)
- [Validation In Depth](#validation-in-depth)
- [OpenAPI Support](#openapi-support)
- [Native AOT and Trimming](#native-aot-and-trimming)
- [Configuration Reference](#configuration-reference)

<a id="key-features"></a>

## ✨ Key Features

- **Clear Result Pattern** — `Result` / `Result<T>` is either a success value or one or more structured errors. No exceptions for expected failures.
- **Rich, machine-readable errors** — every `Error` carries a human-readable `Message`, stable `Code`, input `Target`, and `Category` — ready for API contracts and frontend mapping.
- **Serialization-safe metadata** — metadata uses a dedicated JSON-like type system instead of `Dictionary<string, object>`, so results serialize reliably across any protocol.
- **Full functional operator suite** — `Map`, `Bind`, `Match`, `Ensure`, `Tap`, `Switch`, and their `Async` variants let you build clean, chainable pipelines.
- **Cloud-native round-trip** — write results as RFC-9457 HTTP responses or CloudEvents Spec 1.0 JSON payloads, and deserialize them back on any consumer.
- **ASP.NET Core ready** — Minimal APIs and MVC packages translate `Result` and `Result<T>` directly to `IResult` / `IActionResult` with automatic HTTP status mapping.
- **High-performance validation** — at least 5x faster than FluentValidation 12.1.1, using less than 9% of its memory footprint. Compose validators, map DTOs to domain objects, and share state — all with full async support.
- **Microsoft.AspNetCore.OpenAPI integration** — write validators and generate accurate OpenAPI schemas and examples via source generation.
- **.NET Native AOT** — the base, validation, and Minimal APIs packages are compatible with .NET Native AOT.

<a id="installation"></a>

## 📦 Installation

Install the packages you need for your scenario.

Core Result Pattern, Metadata, Functional Operators, and serialization support for HTTP and CloudEvents:

```bash
dotnet add package Light.PortableResults
```

Validation context, checks, and synchronous/asynchronous validators:

```bash
dotnet add package Light.PortableResults.Validation
```

ASP.NET Core Minimal APIs integration:

```bash
dotnet add package Light.PortableResults.AspNetCore.MinimalApis
```

ASP.NET Core MVC integration:

```bash
dotnet add package Light.PortableResults.AspNetCore.Mvc
```

OpenAPI integration:

```bash
dotnet add package Light.PortableResults.AspNetCore.OpenApi
```

Built-in validation error contracts for OpenAPI:

```bash
dotnet add package Light.PortableResults.Validation.OpenApi
```

If you only need the Result Pattern itself, `Light.PortableResults` is the most lightweight dependency.

<a id="when-to-use-result-vs-exceptions"></a>

## ↔️ When to Use Result vs. Exceptions

Use `Result` / `Result<T>` for **expected business outcomes**:

- validation failed
- resource not found
- user is not authorized
- domain rule was violated

Use **exceptions** for truly unexpected failures:

- database/network outage
- misconfiguration
- programming bugs and invariant violations (detected via guard clauses)

This keeps exceptions exceptional and business outcomes explicit.

<a id="basic-usage"></a>

## 🤓 Basic Usage

```csharp
using Light.PortableResults;

static Result<int> ParsePositiveInteger(string input)
{
    if (int.TryParse(input, out var value) && value > 0)
    {
        return Result<int>.Ok(value);
    }

    return Result<int>.Fail(new Error
    {
        Message = "Value must be a positive integer",
        Code = "parse.invalid_positive_int",
        Target = "input",
        Category = ErrorCategory.Validation
    });
}
```

Examine the result with an if-else...

```csharp
var input = Console.ReadLine();
Result<int> result = ParsePositiveInteger(input);

if (result.IsValid)
{
    Console.WriteLine($"Success: {result.Value}");
}
else
{
    var error = result.Errors.First;
    Console.WriteLine($"Error {error.Code}: {error.Message}");
}
```

...or in a functional style:

```csharp
using Light.PortableResults.FunctionalExtensions;

string message = ParsePositiveInteger(input).Match(
    onSuccess: value => $"Success: {value}",
    onError: errors => $"Error {errors.First.Code}: {errors.First.Message}"
);
Console.WriteLine(message);
```

Use the non-generic `Result` for command-style operations that do not return a value:

```csharp
static Result DeleteUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return Result.Fail(new Error
        {
            Message = "User id must not be empty",
            Code = "user.invalid_id",
            Target = "id",
            Category = ErrorCategory.Validation
        });
    }

    return Result.Ok();
}
```

### Designing useful error payloads

Consistent error shapes make APIs and message consumers easier to evolve. As a rule of thumb:

- `Message`: human-readable explanation
- `Code`: stable machine-readable identifier (great for frontend/API contracts)
- `Target`: which input field, header, or value failed
- `Category`: determines transport mapping (for example, HTTP status code)
- `Metadata`: additional context (for example, boundary values or comparative amounts)

`Error.Exception` can be set for local diagnostics, but it is never serialized and is never exposed to calling processes.

<a id="functional-operators"></a>

## 🔁 Functional Operators

| Category                | Operators                                  | What they are used for                                                         |
|-------------------------|--------------------------------------------|--------------------------------------------------------------------------------|
| Transform success value | `Map`, `Bind`                              | Convert successful values or chain operations that already return `Result<T>`. |
| Transform errors        | `MapError`                                 | Normalize or translate errors (for example domain → transport layer).          |
| Add validation rules    | `Ensure`, `FailIf`                         | Keep fluent pipelines while adding business or guard conditions.               |
| Handle outcomes         | `Match`, `MatchFirst`, `Else`              | Turn a result into a value or fallback without manually branching every time.  |
| Side effects            | `Tap`, `TapError`, `Switch`, `SwitchFirst` | Perform logging, metrics, or notifications on success or failure paths.        |

All operators provide async variants with the `Async` suffix (for example `BindAsync`, `MatchAsync`, `TapErrorAsync`).

```csharp
using Light.PortableResults;
using Light.PortableResults.FunctionalExtensions;

Result<string> message = GetUser(userId)
    .Ensure(user => user.IsActive, new Error
    {
        Message = "User is not active",
        Code = "user.inactive",
        Category = ErrorCategory.Forbidden
    })
    .Map(user => user.Email)
    .Match(
        onSuccess: email => $"User email: {email}",
        onError: errors => $"Failed: {errors.First.Message}"
    );
```

<a id="metadata"></a>

## ℹ️ Metadata

Metadata is not a `Dictionary<string, object>`. Instead it uses a dedicated JSON-like type system so every result serializes and deserializes correctly across any protocol — HTTP, CloudEvents, or otherwise.

Metadata can be attached to `Result<T>` / `Result` instances as well as to individual `Error` instances.

```csharp
using Light.PortableResults;
using Light.PortableResults.Metadata;

// MetadataObject uses implicit conversions from bool, long, double, string, decimal,
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

<a id="validation-quick-start"></a>

## 🛡️ Validation Quick Start

Instead of constructing `Error` instances manually, reference `Light.PortableResults.Validation` and write a typed validator:

```csharp
using Light.PortableResults.Validation;

public sealed record MovieRatingDto
{
    public required Guid Id { get; init; }
    public required Guid MovieId { get; init; }
    public required string UserName { get; set; } = string.Empty;
    public required string Comment { get; set; } = string.Empty;
    public required int Rating { get; init; }
}

public sealed class MovieRatingValidator : Validator<MovieRatingDto>
{
    public MovieRatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<MovieRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        MovieRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();

        // Check() normalizes strings by default (null → "", non-null → trimmed).
        // Assign the return value back to persist the normalized string.
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();

        context.Check(dto.Rating).IsInRange(1, 5);

        return checkpoint.ToValidatedValue(dto);
    }
}
```

Call the validator from a service using `CheckForErrors` to avoid the `if (!result.IsValid)` ceremony:

```csharp
public sealed class AddMovieRatingService
{
    private readonly MovieRatingValidator _validator;

    public AddMovieRatingService(MovieRatingValidator validator) => _validator = validator;

    public async Task<Result<MovieRating>> AddMovieRatingAsync(
        MovieRatingDto dto,
        CancellationToken cancellationToken = default
    )
    {
        if (_validator.CheckForErrors(dto, out var errorResult))
        {
            return Result<MovieRating>.Fail(errorResult.Errors);
        }

        var movieRating = new MovieRating(...);
        return Result<MovieRating>.Ok(movieRating);
    }
}
```

Register validators as singletons when they have no scoped dependencies — they are stateless by design:

```csharp
services
    .AddValidationForPortableResults()
    .AddSingleton<MovieRatingValidator>();
```

See [Validation In Depth](#validation-in-depth) for composing validators, async validation, domain object mapping, sharing state between validators, custom assertions, and configuration options.

<a id="validation-performance"></a>

## ⚡ Validation Performance

Light.PortableResults Validation is significantly faster and leaner than FluentValidation. All benchmarks ran on:

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
```

### Flat DTO — valid (no errors)

| Method                            | Mean        | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 1,324.57 ns |  1.00 |    6984 B |        1.00 |
| FluentValidationSingleton         |   105.84 ns |  0.08 |     632 B |        0.09 |
| LightPortableResults              |    50.49 ns |  0.04 |     104 B |        0.01 |

### Flat DTO — invalid (all three properties fail)

| Method                            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,145.2 ns |  1.00 |   14672 B |        1.00 |
| FluentValidationSingleton         | 1,793.6 ns |  0.57 |    8320 B |        0.57 |
| LightPortableResults              |   289.6 ns |  0.09 |     688 B |        0.05 |

### Complex DTO — valid (one nested object, two nested collections, no errors)

| Method                            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 8,318.7 ns |  1.00 |  33.94 KB |        1.00 |
| FluentValidationSingleton         | 1,685.9 ns |  0.20 |   5.77 KB |        0.17 |
| LightPortableResults              |   742.2 ns |  0.09 |   1.27 KB |        0.04 |

### Complex DTO — invalid (nine errors overall)

| Method                            |      Mean | Ratio | Allocated | Alloc Ratio |
|-----------------------------------|----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.985 μs |  1.00 |  53.45 KB |        1.00 |
| FluentValidationSingleton         |  6.755 μs |  0.48 |  25.47 KB |        0.48 |
| LightPortableResults              |  1.507 μs |  0.11 |   1.99 KB |        0.04 |

See the `benchmarks/Benchmarks` project for the full benchmark source.

<a id="http-quick-start"></a>

## 🚀 HTTP Quick Start

Given the classes from the Validation Quick Start above, you can easily integrate Light.PortableResults with ASP.NET Core in a few lines.

### Minimal APIs

```csharp
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Http.Writing;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddPortableResultsForMinimalApis()
    .AddValidationForPortableResults()
    .Configure<PortableResultsHttpWriteOptions>(
        // Rich format is recommended — it serializes errors Code/Target/Category/Metadata in one object.
        x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
    )
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

> To auto-generate accurate OpenAPI schemas and examples from your validators, add `Light.PortableResults.Validation.OpenApi`, annotate your validator with `[GeneratePortableValidationOpenApi]`, and replace `.ProducesPortableValidationProblem(...)` with `.ProducesPortableValidationProblemFor<TValidator>(...)` on the endpoint. See [OpenAPI Support](#openapi-support).

### MVC

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

> To auto-generate accurate OpenAPI schemas and examples from your validators, add `Light.PortableResults.Validation.OpenApi`, annotate your validator with `[GeneratePortableValidationOpenApi]`, and replace `[ProducesPortableValidationProblem]` with `[ProducesPortableValidationProblemFor<TValidator>]` on the action. See [OpenAPI Support](#openapi-support).

### HTTP Responses on the Wire

Successful update (`200 OK`):

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

Validation failure (`400 Bad Request`):

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": [
    {
      "message": "comment must be between 10 and 1000 characters long",
      "code": "LengthInRange",
      "target": "comment",
      "category": "Validation",
      "metadata": {
        "minLength": 10,
        "maxLength": 1000
      }
    },
    {
      "message": "userName must not be empty or whitespace",
      "code": "NotNullOrWhiteSpace",
      "target": "userName",
      "category": "Validation"
    },
    {
      "message": "rating must be between 1 and 5",
      "code": "InRange",
      "target": "rating",
      "category": "Validation",
      "metadata": {
        "lowerBoundary": 1,
        "upperBoundary": 5
      }
    }
  ]
}
```

### Deserializing `Result<T>` from `HttpResponseMessage`

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

<a id="cloudevents-quick-start"></a>

## ☁️ CloudEvents Quick Start

Light.PortableResults can serialize a `Result<T>` as a CloudEvents Spec 1.0 JSON payload and deserialize it on any consumer. The key API calls are `result.ToCloudEvent(...)` and `ReadResult<T>()`.

### Publish to RabbitMQ

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

### Extension Attribute Encoding

CloudEvents extension attributes use the JSON Event Format context-attribute mapping, not each
`MetadataKind`'s natural JSON shape:

| Metadata value | Extension-attribute JSON |
|---|---|
| `Null` | Omitted (unset) |
| `Boolean` | JSON boolean |
| `Int64` from `-2147483648` through `2147483647` | JSON number |
| Every other non-null primitive, including larger `Int64`, `Double`, `Single`, and `Decimal` | JSON string containing canonical invariant text |
| `Array` or `Object` | Rejected |

CloudEvents `String` values are validated without normalization: controls, Unicode noncharacters, and
unpaired surrogates are rejected. This mapping affects only extension attributes; metadata inside `data`
and `problem+json` keeps its normal JSON representation.

The `Int64` rule is value-dependent. For example, one extension name can be emitted as `2147483647` in
one event and as `"2147483648"` in another. This is a deliberate deviation from CloudEvents' stable-type
recommendation so all in-range CloudEvents integers retain their natural JSON representation while the
full `long` domain remains lossless. If a key requires a stable string shape, convert it to
`MetadataKind.String` with a `CloudEventsAttributeConverter`:

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

On read-back, a JSON boolean becomes `MetadataKind.Boolean`, an integer-number becomes
`MetadataKind.Int64`, and every string-mapped value initially becomes `MetadataKind.String`. Canonical
out-of-range integer text remains accessible through `MetadataValue.TryGetInt64`. Register a
`CloudEventsAttributeParser` when a particular attribute must be restored to another original kind.
Inbound `null` means unset and is not added to extension metadata.

### Consume from RabbitMQ

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

<a id="validation-in-depth"></a>

## 🔬 Validation In Depth

### Composing Validators

Use child validators when your DTO contains nested objects or collections that each have their own validation rules. Validators compose by sharing a single `ValidationContext` — errors from all levels accumulate in one pass.

```csharp
public sealed record PurchaseOrderDto
{
    public required Guid OrderId { get; set; }
    public required DateTime PlacedAt { get; set; }
    public required string CustomerEmail { get; set; } = string.Empty;
    public required ShippingAddressDto ShippingAddress { get; set; }
    public required List<string> Tags { get; set; }
    public required List<OrderItemDto> Items { get; set; }
}

public sealed class PurchaseOrderValidator : Validator<PurchaseOrderDto>
{
    private readonly ShippingAddressValidator _addressValidator;
    private readonly OrderItemValidator _itemValidator;

    public PurchaseOrderValidator(
        IValidationContextFactory validationContextFactory,
        ShippingAddressValidator addressValidator,
        OrderItemValidator itemValidator
    ) : base(validationContextFactory)
    {
        _addressValidator = addressValidator;
        _itemValidator = itemValidator;
    }

    protected override ValidatedValue<PurchaseOrderDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        PurchaseOrderDto dto
    )
    {
        // The client mints the order ID, so require a UUIDv7 — its leading timestamp keeps
        // client-generated keys roughly sortable and index-friendly. Guid.Empty and v4 GUIDs fail.
        context.Check(dto.OrderId).IsUuidV7();

        // Timestamps must be unambiguous, so require UTC. With the default System.Text.Json
        // converter this means the payload has to carry a trailing "Z".
        context.Check(dto.PlacedAt).IsUtc();

        dto.CustomerEmail = context.Check(dto.CustomerEmail).IsEmail();

        // If dto.ShippingAddress is null the child validator emits a null error automatically.
        context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);

        // If dto.Tags is null the framework emits a NotNull error automatically.
        context.Check(dto.Tags).ValidateItems(
            static (Check<string> tag) => tag.HasLengthIn(2, 30)
        );

        context.Check(dto.Items).ValidateItems(_itemValidator);

        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class ShippingAddressValidator : Validator<ShippingAddressDto>
{
    public ShippingAddressValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<ShippingAddressDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        ShippingAddressDto dto
    )
    {
        dto.RecipientName = context.Check(dto.RecipientName).IsNotNullOrWhiteSpace();
        dto.Street = context.Check(dto.Street).IsNotNullOrWhiteSpace();
        dto.PostalCode = context.Check(dto.PostalCode).HasLengthIn(4, 12);
        dto.CountryCode = context.Check(dto.CountryCode).HasLengthIn(2, 2);
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class OrderItemValidator : Validator<OrderItemDto>
{
    public OrderItemValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<OrderItemDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        OrderItemDto dto
    )
    {
        dto.Sku = context.Check(dto.Sku).IsNotNullOrWhiteSpace();
        context.Check(dto.Quantity).IsGreaterThanOrEqualTo(1);
        context.Check(dto.UnitPrice).IsGreaterThan(0m);
        return checkpoint.ToValidatedValue(dto);
    }
}
```

`IsUuidV7` fails with the `UuidV7` error code unless the GUID's RFC 9562 version field is `7` **and** its variant bits are the RFC variant. The same invariant is available standalone as `guid.IsUuidV7()` (`GuidExtensions`) when a repository or message handler needs to guard it outside a check chain.

`IsUtc`, `IsLocal`, and `IsUnspecified` assert the `DateTime.Kind` of the checked value and fail with the `Utc`, `Local`, and `Unspecified` error codes. They partition `DateTimeKind`: every `DateTime` is accepted by exactly one of them. The contract is only the kind of the value the check sees, independent of its origin — `DateTime.UtcNow`, `DateTime.SpecifyKind`, a custom converter, and non-JSON transports can all produce `Utc` just as a JSON payload with a trailing `Z` does.

That matters for JSON requests, because the default `System.Text.Json` converter maps the ISO 8601 forms to kinds like this:

| Wire value | `DateTime.Kind` | Accepted by |
| --- | --- | --- |
| `2026-08-02T10:00:00Z` | `Utc` | `IsUtc` |
| `2026-08-02T10:00:00+00:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00+02:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00` | `Unspecified` | `IsUnspecified` |

So `IsUtc` effectively requires a trailing `Z` on the wire: an explicit numeric offset is converted to server-local time and deserializes as `Local`, and that includes `+00:00` even though it denotes the same instant as `Z`. Document the `Z` requirement for your clients — a `+00:00` payload is rejected. Conversely, the two `Local` values above only agree on the instant; their wall-clock value depends on the server's time zone, which is rarely what a portable API wants.

`DateTime` values reach these assertions after the per-check normalizer or `ValidationContextOptions.ValueNormalizer` has run — the default `TrimStringNormalizer` passes non-strings through unchanged. Do not install a normalizer that coerces `Unspecified` to `Utc`: it destroys exactly the signal being validated, and `IsUtc` would then pass for every request. Convert to UTC in your mapping code, after validation. Note also that `default(DateTime)` is `Unspecified`, so `IsUnspecified` accepts a missing value; pair it with a range check when that matters.

> **What is `ValidatedValue<T>`?**
>
> `ValidatedValue<T>` is the handshake type between a validator and its callers within a single validation pipeline run. Rather than surfacing errors immediately as `Result<T>`, it carries the signal back: either a successfully validated value via `ValidatedValue<T>.Success(value)`, or `ValidatedValue<T>.NoValue` when errors were added. `checkpoint.ToValidatedValue(dto)` chooses the right outcome based on whether any errors were added since the checkpoint was created. You never need to construct `ValidatedValue<T>` directly unless you are writing a transforming validator — see [Mapping to Domain Objects](#mapping-to-domain-objects).

Register all validators as singletons when they have no scoped dependencies:

```csharp
services
    .AddValidationForPortableResults()
    .AddSingleton<ShippingAddressValidator>()
    .AddSingleton<OrderItemValidator>()
    .AddSingleton<PurchaseOrderValidator>();
```

### Automatic Null Checking

The validation framework handles `null` values automatically so you rarely need an explicit `IsNotNull()` guard. The active `AutomaticNullErrorProvider` (configurable via `ValidationContextOptions`) decides what error to produce; the default emits a `NotNull` validation error.

- **Validators** — when the source value passed to `Validate` / `ValidateAsync` is `null`, the validator adds the automatic null error and returns a failed `Result` without calling `PerformValidation`. The `isAutomaticNullCheckingEnabled` constructor parameter (default `true`) controls this per validator class.
- **Child validation (`ValidateChild`, `ValidateChildAsync`)** — when the nested value is `null`, the child validator's null check fires for that target and the parent continues collecting other errors.
- **Collection item validation (`ValidateItems`, `ValidateItemsAsync`)** — when a `null` collection is passed, the null error is added for the collection target and item validators are skipped. Individual item validators also handle `null` items automatically.

Guard explicitly with `IsNotNull()` only when `NoOpAutomaticNullErrorProvider` is configured (automatic null errors disabled), or when you need to short-circuit further checks:

```csharp
// Default configuration — no explicit guard needed
context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);
context.Check(dto.Tags).ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));

// Explicit guard — short-circuits any further checks on this value
context.Check(dto.Tags).IsNotNull().ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));
```

### Mapping to Domain Objects

Use `Validator<TSource, TValidated>` when validation must produce a *different* output type — typically a mutable DTO in, an immutable domain object out. This pattern implements an Anti-Corruption Layer.

```csharp
// Mutable DTO received from the API
public sealed record CreateMovieDto
{
    public required string Title { get; set; } = string.Empty;
    public required int ReleaseYear { get; set; }
    public required string DirectorName { get; set; } = string.Empty;
}

// Immutable domain entity — no public setters
public sealed record Movie
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int ReleaseYear { get; init; }
    public required string DirectorName { get; init; }
}

public sealed class CreateMovieValidator : Validator<CreateMovieDto, Movie>
{
    public CreateMovieValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    // PerformValidation returns ValidatedValue<Movie>.
    // The domain object is only constructed when all checks pass.
    protected override ValidatedValue<Movie> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        CreateMovieDto dto
    )
    {
        var title = context.Check(dto.Title).IsNotNullOrWhiteSpace();
        context.Check(dto.ReleaseYear).IsInRange(1888, DateTime.UtcNow.Year);
        var directorName = context.Check(dto.DirectorName).IsNotNullOrWhiteSpace();

        if (checkpoint.HasNewErrors)
        {
            return ValidatedValue<Movie>.NoValue;
        }

        return ValidatedValue<Movie>.Success(new Movie
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            ReleaseYear = dto.ReleaseYear,
            DirectorName = directorName
        });
    }
}
```

The caller receives `Result<Movie>` — `CreateMovieDto` never escapes the validator boundary:

```csharp
public async Task<Result<Movie>> CreateMovieAsync(CreateMovieDto dto)
{
    Result<Movie> result = _validator.Validate(dto);
    if (!result.IsValid)
    {
        return result;
    }

    await _movieRepository.AddAsync(result.Value);
    return result;
}
```

### Async Validators

Use `AsyncValidator<T>` (or `AsyncValidator<TSource, TValidated>`) when any validation step requires an async operation such as a database look-up or an external API call.

```csharp
public sealed class AddMovieRatingValidator : AsyncValidator<MovieRatingDto>
{
    private readonly IMovieRepository _movieRepository;

    public AddMovieRatingValidator(
        IValidationContextFactory validationContextFactory,
        IMovieRepository movieRepository
    ) : base(validationContextFactory)
    {
        _movieRepository = movieRepository;
    }

    protected override async ValueTask<ValidatedValue<MovieRatingDto>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        MovieRatingDto dto,
        CancellationToken cancellationToken
    )
    {
        // Synchronous checks first — cheap and allocation-free
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsInRange(1, 5);

        // Only hit the database if the synchronous checks passed
        if (!checkpoint.HasNewErrors)
        {
            var movieExists = await _movieRepository.ExistsAsync(dto.MovieId, cancellationToken);
            if (!movieExists)
            {
                context.Check(dto.MovieId).AddError(new Error
                {
                    Message = "The specified movie does not exist",
                    Code = "movie.notFound",
                    Target = "movieId",
                    Category = ErrorCategory.NotFound
                });
            }
        }

        return checkpoint.ToValidatedValue(dto);
    }
}
```

Call `ValidateAsync` from the service layer:

```csharp
public async Task<Result<MovieRating>> AddMovieRatingAsync(
    MovieRatingDto dto,
    CancellationToken cancellationToken = default
)
{
    Result<MovieRatingDto> validationResult = await _validator.ValidateAsync(dto, cancellationToken);
    if (!validationResult.IsValid)
    {
        return Result<MovieRating>.Fail(validationResult.Errors);
    }

    var movieRating = new MovieRating(...);
    return Result<MovieRating>.Ok(movieRating);
}
```

Register async validators that depend on scoped services as scoped themselves:

```csharp
builder.Services
    .AddValidationForPortableResults()
    .AddScoped<IMovieRepository, MovieRepository>()
    .AddScoped<AddMovieRatingValidator>(); // scoped because it depends on a scoped repository
```

### Using `ValidationContext` Directly

You do not need a validator class for every case. Inject `IValidationContextFactory` and use `ValidationContext` directly for inline validation:

```csharp
public async Task<Result<IReadOnlyList<Movie>>> SearchMoviesAsync(
    string? query,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default
)
{
    var context = _contextFactory.CreateValidationContext();
    var normalizedQuery = context.Check(query).IsNotNullOrWhiteSpace();
    context.Check(page).IsGreaterThanOrEqualTo(1);
    context.Check(pageSize).IsInRange(1, 100);

    if (context.HasErrors)
    {
        return Result<IReadOnlyList<Movie>>.Fail(context.ToFailureResult().Errors);
    }

    return Result<IReadOnlyList<Movie>>.Ok(
        await _movieRepository.SearchAsync(normalizedQuery, page, pageSize, cancellationToken)
    );
}
```

`IValidationContextFactory` is registered automatically by `AddValidationForPortableResults()`.

### Sharing State Between Validators

When a child validator needs data loaded by the parent, use `ValidationContext.SetItem` and `GetRequiredItem` with a typed key. This avoids loading the same data twice and keeps child validators free from infrastructure dependencies.

```csharp
// Define the key once — store it as a static field near the validators that use it
public static class MovieConstants
{
    public static readonly ValidationContextKey<Movie> MovieKey = new("movie");
}

// Parent loads the movie and stores it in the context
protected override async ValueTask<ValidatedValue<MovieRatingDto>> PerformValidationAsync(
    ValidationContext context,
    ValidationCheckpoint checkpoint,
    MovieRatingDto dto,
    CancellationToken cancellationToken
)
{
    context.Check(dto.Id).IsNotEmpty();
    context.Check(dto.MovieId).IsNotEmpty(shortCircuitOnError: true);
    dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();
    dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
    context.Check(dto.Rating).IsInRange(1, 5);

    if (!checkpoint.HasNewErrors)
    {
        var movie = await _movieClient.GetAsync(dto.MovieId, cancellationToken);
        if (movie is null)
        {
            context.Check(dto.MovieId).AddError(new Error
            {
                Message = "The specified movie does not exist",
                Code = "movie.notFound",
                Target = "movieId",
                Category = ErrorCategory.NotFound
            });
        }
        else
        {
            context.SetItem(MovieConstants.MovieKey, movie);
            context.Check(dto).ValidateChild(_quotaValidator);
        }
    }

    return checkpoint.ToValidatedValue(dto);
}

// Child retrieves the pre-loaded entity without touching the database
protected override ValidatedValue<MovieRatingDto> PerformValidation(
    ValidationContext context,
    ValidationCheckpoint checkpoint,
    MovieRatingDto dto
)
{
    var movie = context.GetRequiredItem(MovieConstants.MovieKey);

    if (movie.MaxRatingsPerUser > 0 && movie.CurrentRatingCount >= movie.MaxRatingsPerUser)
    {
        context.Check(dto.MovieId).AddError(new Error
        {
            Message = "Rating quota for this movie has been reached",
            Code = "movie.quotaExceeded",
            Target = "movieId",
            Category = ErrorCategory.Conflict
        });
    }

    return checkpoint.ToValidatedValue(dto);
}
```

`ValidationContextKey<T>` is typed so you cannot accidentally retrieve the wrong type. Use `TryGetItem` instead of `GetRequiredItem` when the item may not have been set.

### Custom Assertions

**Ad-hoc predicate** — use `Must` for a one-off check:

```csharp
context.Check(dto.ReleaseYear).Must(
    year => year >= 1888 && year <= DateTime.UtcNow.Year
);
```

**Reusable definition** — for rules used across multiple validators, create a `ValidationErrorDefinition` subclass and expose it as a fluent extension method. This participates in the library's message caching and has the same performance as built-in assertions.

```csharp
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;

public sealed class MustBeValidMovieYearDefinition : ValidationErrorDefinition
{
    public static readonly MustBeValidMovieYearDefinition Instance = new();

    private MustBeValidMovieYearDefinition() : base(code: "MustBeValidMovieYear") { }

    public override bool IsMessageStable => true;

    public override bool TryGetStableMessageProvider(
        ReadOnlyValidationContext context,
        out object? provider
    )
    {
        provider = this;
        return true;
    }

    public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
        new($"{context.DisplayName} must be a valid movie release year (1888 or later, not in the future)");
}

public static class MovieValidationExtensions
{
    public static Check<int> MustBeValidMovieYear(this Check<int> check, bool shortCircuitOnError = false)
    {
        if (check.IsShortCircuited)
            return check;

        var year = check.Value;
        if (year >= 1888 && year <= DateTime.UtcNow.Year)
            return check;

        check = check.AddError(MustBeValidMovieYearDefinition.Instance);
        return shortCircuitOnError ? check.ShortCircuit() : check;
    }
}
```

Use it exactly like any built-in assertion:

```csharp
context.Check(dto.ReleaseYear).MustBeValidMovieYear();
```

### Configuring Validation Behavior

`ValidationContextOptions` controls how a `ValidationContext` behaves. All properties are `init`-only.

| Property                     | Default                                      | What it controls                                                                                                                                                  |
|------------------------------|----------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValueNormalizer`            | `TrimStringNormalizer.Instance`              | How values are normalized before checks see them. The default trims strings and converts `null` to `""`. Replace with `NoOpValueNormalizer.Instance` to disable.  |
| `TargetNormalizer`           | `ValidationTargets.DefaultNormalizer`        | How caller-expression targets (e.g. `dto.ShippingAddress`) are converted to error target strings.                                                                 |
| `CultureInfo`                | `CultureInfo.InvariantCulture`               | Culture used to format number parameters in error messages.                                                                                                       |
| `AutomaticNullErrorProvider` | `DefaultAutomaticNullErrorProvider.Instance` | Produces the error when a validator receives a null source value.                                                                                                 |
| `ErrorTemplates`             | `ValidationErrorTemplates.Default`           | The full set of built-in message templates. Replace individual templates to customize wording globally.                                                           |
| `ErrorDefinitionCache`       | `ValidationErrorDefinitionCache.Default`     | Shared cache for reusable definition instances. The default is a process-wide singleton.                                                                          |

Register a customized factory before calling `AddValidationForPortableResults()`:

```csharp
using System.Globalization;
using Light.PortableResults.Validation;

builder.Services.AddSingleton<IValidationContextFactory>(
    _ => DefaultValidationContextFactory.Create(new ValidationContextOptions
    {
        CultureInfo = CultureInfo.GetCultureInfo("de-DE")
    })
);
builder.Services.AddValidationForPortableResults();
```

Without a DI host:

```csharp
var factory = DefaultValidationContextFactory.Create(new ValidationContextOptions
{
    CultureInfo = CultureInfo.GetCultureInfo("de-DE")
});
var validator = new CreateMovieValidator(factory);
```

### Validate `Microsoft.Extensions.Configuration` Options

Use `ValidateWithPortableResults<TOptions, TValidator>()` to integrate your `Validator<T>` implementations with the standard options validation pipeline:

```csharp
public sealed class EmailSenderOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class EmailSenderOptionsValidator : Validator<EmailSenderOptions>
{
    public EmailSenderOptionsValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<EmailSenderOptions> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        EmailSenderOptions options
    )
    {
        context.Check(options.Host).IsNotNullOrWhiteSpace();
        context.Check(options.Port).IsInRange(1, 65535);
        context.Check(options.ApiKey).IsNotNullOrWhiteSpace();
        return checkpoint.ToValidatedValue(options);
    }
}

services
    .AddOptions<EmailSenderOptions>()
    .BindConfiguration("EmailSender")
    .ValidateWithPortableResults<EmailSenderOptions, EmailSenderOptionsValidator>()
    .ValidateOnStart();
```

`ValidateWithPortableResults` supports named options and forwards the current options name to the `ValidationContext`. Use `ValidationContext.TryGetItem(ConfigurationConstants.OptionsNameKey, out var optionsName)` to access it in your validator.

<a id="openapi-support"></a>

## 🌐 OpenAPI Support

OpenAPI support lives in the dedicated `Light.PortableResults.AspNetCore.OpenApi` package and is opt-in — it does not change runtime serialization. The package contributes endpoint metadata and a document transformer that understands `LightResult<T>` / `LightActionResult<T>`.

### Registration

```csharp
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation.OpenApi;

builder.Services
       .AddOpenApi()
       .AddPortableResultsForMinimalApis()
       .AddPortableResultsOpenApi(contracts => contracts.RegisterBuiltInValidationErrors());
```

Use `AddPortableResultsForMvc()` for MVC applications. `RegisterBuiltInValidationErrors()` registers schemas for all built-in validation error codes from `Light.PortableResults.Validation`.

### Documenting Endpoints

Minimal APIs expose three helpers:

- `ProducesPortableSuccessResponse<TValue>(...)` — documents the success response (bare `TValue` or `{ value, metadata }` depending on `MetadataSerializationMode`).
- `ProducesPortableProblem(...)` — documents a non-validation failure response.
- `ProducesPortableValidationProblem(...)` — documents a validation failure (400/422), selecting the rich or ASP.NET Core-compatible envelope shape automatically.

MVC exposes matching attributes: `[ProducesPortableSuccessResponse<TValue>]`, `[ProducesPortableProblem]`, and `[ProducesPortableValidationProblem]`.

```csharp
app.MapPut("/api/movieRatings", async (MovieRatingDto dto, AddMovieRatingService service) =>
    {
        var result = await service.AddMovieRatingAsync(dto);
        return result.ToMinimalApiResult();
    })
   .ProducesPortableSuccessResponse<MovieRating>()
   .ProducesPortableValidationProblem(
        configure: x =>
            x.UseFormat(ValidationProblemSerializationFormat.Rich)
             .WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange)
             .WithInRangeError<int>()
    )
   .ProducesPortableProblem();
```

### Narrowing Error Schemas

`WithErrorCodes(...)`, `WithErrorMetadata<TMetadata>(code)`, and typed helpers like `WithInRangeError<T>()` narrow error items exhaustively once you document at least one code. The generated schema becomes a `oneOf` discriminated by `code`.

If an endpoint can emit additional codes that cannot be enumerated at build time, opt out with `AllowUnknownErrorCodes()` (Minimal APIs) or `AllowUnknownErrorCodes = true` (MVC attributes). The schema then falls back to a non-exhaustive `anyOf` shape while still documenting the known variants.

### Source Generation

Mark a synchronous `Validator<T>` with `[GeneratePortableValidationOpenApi]` and make it `partial` to let the source generator produce an OpenAPI contract automatically from the validator's check calls:

```csharp
[GeneratePortableValidationOpenApi]
public sealed partial class AddMovieRatingValidator : Validator<MovieRatingDto>
{
    protected override ValidatedValue<MovieRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        MovieRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsInRange(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}

app.MapPut("/api/movieRatings", AddMovieRating)
   .ProducesPortableValidationProblemFor<AddMovieRatingValidator>(
        configure: x => x.UseFormat(ValidationProblemSerializationFormat.Rich)
    );
```

The generator analyzes top-level `context.Check(...).Rule(...)` chains and produces response schemas and examples from compile-time constants (for example, `HasLengthIn(10, 1000)` or `IsInRange(1, 5)`). It also reconstructs deterministic `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Guid`, and `Uri` boundaries written with supported constructors, factories, well-known static values, or `static readonly` fields declared in the validator's file. Reconstructed message values and example metadata use the same canonical text.

Runtime-computed or unsupported boundary expressions still generate valid schemas, but the affected example omits its message and metadata and reports `LPRSG0015` at the argument. `DateTimeKind.Local`, culture-sensitive parsing, arithmetic or chained calls, and invalid constructor or factory arguments follow this degraded path. A `static readonly` field declared in another file or assembly is deliberately not followed and reports `LPRSG0016`; write the value inline or move its declaration into the validator's file when the complete example is required.

Use `[PortableValidationOpenApiErrorHint]` to annotate codes the generator cannot infer (for example, from `Must(...)`, `Custom(...)`, or child validators):

```csharp
[GeneratePortableValidationOpenApi]
[PortableValidationOpenApiErrorHint("MovieAlreadyRated")]
public sealed partial class AddMovieRatingValidator : Validator<MovieRatingDto> { ... }
```

### Reusable Error Code Contracts

Register per-error-code metadata contracts once in DI, then opt specific endpoints into them:

```csharp
builder.Services.AddPortableResultsOpenApi(contracts =>
{
    contracts.ForCode<VersionMismatchMetadata>("VersionMismatch");
    contracts.ForCode<InsufficientFundsMetadata>("InsufficientFunds");
});

app.MapPut("/api/movieRatings", handler)
   .ProducesPortableValidationProblem(
        configure: x => x.WithErrorCodes("VersionMismatch")
    );
```

<a id="native-aot-and-trimming"></a>

## 🛩️ Native AOT and Trimming

`Light.PortableResults` and `Light.PortableResults.Validation` ship `net10.0` assets that are built with
`IsAotCompatible`, and CI publishes a Native AOT sample so that trim and AOT regressions fail the build.

### What you have to register

You only register **your own result value types** with a `JsonSerializerContext`. Every envelope and payload type
that Light.PortableResults owns is resolved by the library itself, so you never declare closed library wrappers such
as `CloudEventsEnvelopeForWriting<MovieRating>` or `HttpReadAutoSuccessResultPayload<MovieRating>`. Combining
source-generated resolvers cannot synthesize those closed generics from separate contracts anyway.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieRating))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
```

Compose the options by setting the resolver and then adding the Light.PortableResults converters:

```csharp
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Http.Reading;

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = MovieRatingJsonContext.Default
};
serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();

var writeOptions = new PortableResultsCloudEventsWriteOptions
{
    SerializerOptions = serializerOptions,
    Source = "urn:movies:rating-service"
};

byte[] cloudEvent = result.ToCloudEvent(successType: "movie.rated", options: writeOptions);
```

The same composition applies to `AddDefaultPortableResultsCloudEventsReadJsonConverters` and
`AddDefaultPortableResultsHttpReadJsonConverters`. Non-generic `Result` values require **no** consumer registration
at all: every type involved in writing and reading them belongs to Light.PortableResults.

### How a contract is resolved

For each library-owned type, `PortableResultsJsonContracts` resolves in this order:

1. When the options carry no resolver and reflection-based serialization is enabled, the reflection resolver is
   materialized so that the shared default options keep working exactly as before.
2. A contract supplied by the configured `TypeInfoResolver` wins. Declaring a library type in your own context
   therefore still overrides the library.
3. Otherwise the converter that the options select for the type is used, following the normal
   `JsonSerializerOptions` precedence — the first entry in `Converters` that can convert the type. A converter you
   insert **before** the Light.PortableResults defaults keeps its precedence; the library converter is only reached
   when nothing else matches. Contracts created this way are cached per `JsonSerializerOptions` instance and can be
   created after the options became read-only.

If a value type is missing from your context, the affected entry point throws an `InvalidOperationException` that
names the unresolved type and tells you to register it, instead of failing deep inside `System.Text.Json`.

### HTTP writing with ASP.NET Core

The HTTP write path is the exception: `LightResult`/`LightActionResult` serialize `HttpResultForWriting` and
`HttpResultForWriting<T>` through the configured resolver, so those wrappers still have to be declared in your
context — see the `samples/NativeAotMovieRating` project. `Light.PortableResults.AspNetCore.Mvc` is not Native AOT
compatible.

<a id="configuration-reference"></a>

## ⚙️ Configuration Reference

### HTTP write options (`PortableResultsHttpWriteOptions`)

| Option                                 | Default                | Description                                                                                                                                                                                               |
|----------------------------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValidationProblemSerializationFormat` | `AspNetCoreCompatible` | Controls how validation errors are serialized for HTTP 400/422 responses. We encourage using `Rich`.                                                                                                      |
| `MetadataSerializationMode`            | `ErrorsOnly`           | Controls whether metadata is serialized in response bodies (`ErrorsOnly` or `Always`).                                                                                                                    |
| `CreateProblemDetailsInfo`             | `null`                 | Optional custom factory for generating Problem Details fields (`type`, `title`, `detail`, etc.).                                                                                                          |
| `FirstErrorCategoryIsLeadingCategory`  | `true`                 | If `true`, the first error category decides the HTTP status code. If `false`, all errors must share the same category; otherwise `Unclassified` (500) is used.                                            |

```csharp
builder.Services.Configure<PortableResultsHttpWriteOptions>(options =>
{
    options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich;
    options.MetadataSerializationMode = MetadataSerializationMode.Always;
    options.FirstErrorCategoryIsLeadingCategory = false;
});
```

### HTTP read options (`PortableResultsHttpReadOptions`)

| Option                       | Default                              | Description                                                                              |
|------------------------------|--------------------------------------|------------------------------------------------------------------------------------------|
| `HeaderParsingService`       | `ParseNoHttpHeadersService.Instance` | Controls how HTTP headers are converted into metadata.                                   |
| `MergeStrategy`              | `AddOrReplace`                       | Strategy when merging metadata with the same key from headers and body.                  |
| `PreferSuccessPayload`       | `Auto`                               | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`).              |
| `TreatProblemDetailsAsFailure` | `true`                             | If `true`, `application/problem+json` is treated as failure even for 2xx status codes.  |
| `SerializerOptions`          | `Module.DefaultSerializerOptions`    | System.Text.JSON serializer options used for deserialization.                            |

```csharp
var readOptions = new PortableResultsHttpReadOptions
{
    HeaderParsingService = new DefaultHttpHeaderParsingService(new AllHeadersSelectionStrategy()),
    PreferSuccessPayload = PreferSuccessPayload.Auto,
    TreatProblemDetailsAsFailure = true
};

Result<UserDto> result = await response.ReadResultAsync<UserDto>(readOptions);
```

### CloudEvents write options (`PortableResultsCloudEventsWriteOptions`)

| Option                       | Default                                                   | Description                                                                              |
|------------------------------|-----------------------------------------------------------|------------------------------------------------------------------------------------------|
| `Source`                     | `null`                                                    | Default CloudEvents `source` URI if not set per call.                                    |
| `MetadataSerializationMode`  | `Always`                                                  | Controls whether metadata is serialized into CloudEvents `data`.                         |
| `SerializerOptions`          | `Module.DefaultSerializerOptions`                         | System.Text.JSON serializer options.                                                     |
| `ConversionService`          | `DefaultCloudEventsAttributeConversionService.Instance`   | Converts metadata entries into CloudEvents extension attributes.                         |
| `SuccessType`                | `null`                                                    | Default CloudEvents `type` for successful results.                                       |
| `FailureType`                | `null`                                                    | Default CloudEvents `type` for failed results.                                           |
| `Subject`                    | `null`                                                    | Default CloudEvents `subject`.                                                           |
| `DataSchema`                 | `null`                                                    | Default CloudEvents `dataschema` URI.                                                    |
| `Time`                       | `null`                                                    | Default `time` value (`UTC now` used when omitted).                                      |
| `IdResolver`                 | `null`                                                    | Optional function used to generate CloudEvents `id` values.                              |
| `ArrayPool`                  | `ArrayPool<byte>.Shared`                                  | Buffer pool used for serialization.                                                      |
| `PooledArrayInitialCapacity` | `RentedArrayBufferWriter.DefaultInitialCapacity` (2048 B) | Initial buffer size for pooled serialization.                                            |

```csharp
builder.Services.Configure<PortableResultsCloudEventsWriteOptions>(options =>
{
    options.Source = "urn:light-portable-results:sample:user-service";
    options.SuccessType = "users.updated";
    options.FailureType = "users.update.failed";
    options.MetadataSerializationMode = MetadataSerializationMode.Always;
});
```

### CloudEvents read options (`PortableResultsCloudEventsReadOptions`)

| Option               | Default                           | Description                                                                   |
|----------------------|-----------------------------------|-------------------------------------------------------------------------------|
| `SerializerOptions`  | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options.                                          |
| `PreferSuccessPayload` | `Auto`                          | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`).   |
| `IsFailureType`      | `null`                            | Optional fallback classifier to decide failure based on CloudEvents `type`.   |
| `ParsingService`     | `null`                            | Optional parser for mapping extension attributes to metadata.                 |
| `MergeStrategy`      | `AddOrReplace`                    | Strategy when merging extension attributes and payload metadata.              |

```csharp
var cloudReadOptions = new PortableResultsCloudEventsReadOptions
{
    IsFailureType = eventType => eventType.EndsWith(".failed", StringComparison.Ordinal),
    PreferSuccessPayload = PreferSuccessPayload.Auto
};

Result<UserDto> result = messageBody.ReadResult<UserDto>(cloudReadOptions);
```

### Supported Error Categories

| `ErrorCategory`                  | HTTP Status |
|----------------------------------|-------------|
| `Unclassified`                   | 500         |
| `Validation`                     | 400         |
| `Unauthorized`                   | 401         |
| `PaymentRequired`                | 402         |
| `Forbidden`                      | 403         |
| `NotFound`                       | 404         |
| `MethodNotAllowed`               | 405         |
| `NotAcceptable`                  | 406         |
| `Timeout`                        | 408         |
| `Conflict`                       | 409         |
| `Gone`                           | 410         |
| `LengthRequired`                 | 411         |
| `PreconditionFailed`             | 412         |
| `ContentTooLarge`                | 413         |
| `UriTooLong`                     | 414         |
| `UnsupportedMediaType`           | 415         |
| `RequestedRangeNotSatisfiable`   | 416         |
| `ExpectationFailed`              | 417         |
| `MisdirectedRequest`             | 421         |
| `UnprocessableContent`           | 422         |
| `Locked`                         | 423         |
| `FailedDependency`               | 424         |
| `UpgradeRequired`                | 426         |
| `PreconditionRequired`           | 428         |
| `TooManyRequests`                | 429         |
| `RequestHeaderFieldsTooLarge`    | 431         |
| `UnavailableForLegalReasons`     | 451         |
| `InternalError`                  | 500         |
| `NotImplemented`                 | 501         |
| `BadGateway`                     | 502         |
| `ServiceUnavailable`             | 503         |
| `GatewayTimeout`                 | 504         |
| `InsufficientStorage`            | 507         |
