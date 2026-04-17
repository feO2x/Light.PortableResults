# Light.PortableResults

*A high-performant, enterprise-grade .NET library implementing the Result Pattern where each result is serializable and deserializable. Comes with integrations for ASP.NET Core Minimal APIs and MVC, `HttpResponseMessage`, and CloudEvents JSON format, as well as a validation framework.*

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-0.4.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages?q=Light.PortableResults)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/releases)

## ✨ Key Features

- 🧱 **Clear Result Pattern** — `Result` / `Result<T>` is either a success value or one or more structured errors. No exceptions for expected failures.
- 📝 **Rich, machine-readable errors** — every `Error` carries a human-readable `Message`, stable `Code`, input `Target`, and `Category` — ready for API contracts and frontend mapping.
- 🗂️ **Serialization-safe metadata** — metadata uses a dedicated JSON-like type system instead of `Dictionary<string, object>`, so results serialize reliably across any protocol.
- 🔁 **Full functional operator suite** — `Map`, `Bind`, `Match`, `Ensure`, `Tap`, `Switch`, and their `Async` variants let you build clean, chainable pipelines.
- ☁️ **Cloud-Native** — Light.PortableResults contains System.Text.Json serialization support for HTTP responses, including RFC-9457 Problem Details compatibility, and CloudEvents Spec 1.0 JSON payloads for asynchronous messaging. Full round-trip included.
- 🧩 **ASP.NET Core ready** — Minimal APIs and MVC packages translate `Result` and `Result<T>` directly to `IResult` / `IActionResult` with automatic HTTP status mapping and RFC-9457 Problem Details support.
- 🛡️ **Validation framework** — Light.PortableResults.Validation allows you to easily validate DTOs and any values. Use transforming validators to write efficient Anti-Corruption Layers. At least 5x faster than FluentValidation 12.1.1 while having less than 9% of FluentValidation's memory footprint.
- 🛠️ **Microsoft.Extensions.Configuration**: validate options with your custom `Validator<T>` implementations.
- ⚡ **Allocation-minimal by design** — pooled buffers, struct-friendly internals, smart caching, and fast paths keep GC pressure near zero even at high throughput.
- 🧊 **.NET Native AOT** — The base, validation, and Minimal APIs packages are designed to work seamlessly with .NET Native AOT, ensuring minimal runtime overhead and efficient memory usage.

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

ASP.NET Core Minimal APIs integration with support for Dependency Injection and `IResult`:

```bash
dotnet add package Light.PortableResults.AspNetCore.MinimalApis
```

ASP.NET Core MVC integration with support for Dependency Injection and `IActionResult`:

```bash
dotnet add package Light.PortableResults.AspNetCore.Mvc
```

If you only need the Result Pattern itself, `Light.PortableResults` is the most lightweight dependency.

## 🤓 Basic Usage

If you are new to the Result Pattern, think of it like this:

- A method can either succeed or fail.
- Instead of throwing exceptions for expected failures (validation, not found, conflicts), the method returns a value that explicitly describes the outcome.
- Callers must handle both paths on purpose, which makes Control Flow easier to read and test.

This is covered by the following types in Light.PortableResults:

- `Result<T>` means: either a success value of type `T`, or one or more errors.
- `Result` (non-generic) means: success/failure without a return value (corresponds to `void`).
- Each `Error` can carry machine-readable details such as `Code`, `Target`, `Category`, and `Metadata`.

You can then build business logic around these types:

```csharp
using Light.PortableResults;

// Use Result<T> or Result as response types in your methods
static Result<int> ParsePositiveInteger(string input)
{
    if (int.TryParse(input, out var value) && value > 0)
    {
        // If everything is fine, then use Result<T>.Ok() to return a success value
        return Result<int>.Ok(value);
    }

    // If an error occurred, use Result<T>.Fail() to indicate an issue.
    // Here we create a single error, but you can also return multiple errors.
    return Result<int>.Fail(new Error
    {
        Message = "Value must be a positive integer",
        Code = "parse.invalid_positive_int",
        Target = "input",
        Category = ErrorCategory.Validation
    });
}
```

You can then examine results in two ways: with implicit if-else Control Flow...

```csharp
var input = Console.ReadLine();
Result<int> result = ParsePositiveInteger();

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

var input = Console.ReadLine();
string message = ParsePositiveInteger(input).Match(
    onSuccess: value => $"Success: {value}",
    onError: errors => $"Error {errors.First.Code}: {errors.First.Message}"
);
Console.WriteLine(message);
```

See [Functional Operators](#functional-operators) for more details on the available operators.

> The core idea is that you avoid throwing exceptions as part of the contract between a method and its caller. Instead, you return a `Result<T>` or `Result` instance that explicitly indicates success or failure.

## ℹ️ Metadata

In Light.PortableResults, metadata is not just a `Dictionary<string, object>` as with many other Result Pattern implementations. Instead, it uses a type system pretty similar to JSON which allows each result instance to be serialized and deserialized.

Metadata can be attached to `Result<T>/Result` instances as well as to `Error` instances.

```csharp
using Light.PortableResults;
using Light.PortableResults.Metadata;

// Create metadata using primitive types (bool, long, double, string, decimal)
// or nested objects and arrays. MetadataObject uses implicit conversions
// from these types for easy construction.
var metadata = MetadataObject.Create(
    ("requestId", "550e8400-e29b-41d4-a716-446655440000"),
    ("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
    ("cacheHit", false),
    ("attemptCount", 3)
);

// Attach metadata to a successful result
Result<Order> result = Result<Order>.Ok(
    new Order { Id = Guid.NewGuid(), Total = 99.99m },
    metadata
);

// Or attach metadata to an error for additional context
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

// Access metadata from a result or error
if (result.Metadata?.TryGetString("requestId", out var requestId) == true)
{
    Console.WriteLine($"Request: {requestId}");
}
```

## 🛫️ Validation Quick Start

Instead of creating `Error` instances manually, you can reference the `Light.PortableResults.Validation` package and use its rich assertions and support for validators, similar to FluentValidation. Here is an example:

```csharp
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
    // Inject any service you need for validation into the constructor.
    // The IValidationContextFactory is used to obtain a ValidationContext instance
    // and must always be injected.
    public MovieRatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<MovieRatingDto> PerformValidation(
        ValidationContext context, // Collects errors during validation
        ValidationCheckpoint checkpoint, // Used to determine if errors occurred in this method
        MovieRatingDto dto // The value to validate
    )
    {
        // Use the ValidationContext.Check method to create Check<T> instances.
        // These offer various extension methods which attach errors to the context
        // if validation fails. The Check call will also smartly obtain a value for
        // Error.Target depending on your argument (CallerArgumentExpression).
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();

        // Instead of only examining values, ValidationContext.Check normalizes values.
        // By default, strings are processed in the following way:
        // - Null -> Empty string (avoids NullReferenceException)
        // - Not-Null -> Trimmed string
        // You can write these normalized string values back to ensure safe processing
        // after validation finished. See ValidationContextOptions.ValueNormalizer.
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();

        context.Check(dto.Rating).IsInBetween(1, 5);

        // Use the checkpoint to determine if validation errors were attached to
        // to the ValidationContext during this method call. The checkpoint will
        // automatically return a corresponding ValidatedValue<T> instance for you.
        return checkpoint.ToValidatedValue(dto);
    }
}

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

        // Do something useful with the validated DTO. In the end
        // a MovieRating domain object is created and returned.
        var movieRating = new MovieRating(...);
        return Result<MovieRating>.Ok(movieRating);
    }
}
```

Light.PortableResults is significantly faster and leaner than FluentValidation — see the [benchmark results](#composing-validators) in Validation In Depth.

See [Validation In Depth](#-validation-in-depth) for composing validators, async validation, domain object mapping, sharing state between validators, custom assertions, and configuration options.

## 🚀 HTTP Quick Start

Given the classes in the previous Validation Quick Start section, you can easily integrate Light.PortableResults into ASP.NET Core.

### Minimal APIs

```csharp
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;

var builder = WebApplication.CreateBuilder(args);
builder
   .Services
   .AddPortableResultsForMinimalApis()
   .AddValidationForPortableResults()
   .Configure<PortableResultsHttpWriteOptions>(
       // We highly recommend using the Rich serialization format for HTTP responses.
       // If you do not adjust this value, the default value of
       // ValidationProblemSerializationFormat.AspNetCoreCompatible is used which
       // writes the Problem Details errors in the same way as ASP.NET Core does.
       x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
    )
   .AddSingleton<MovieRatingValidator>() // Register validators as singletons by default
   .AddScoped<AddMovieRatingService>();

var app = builder.Build();

app.MapPut("/api/movieRatings", async (MovieRatingDto dto, AddMovieRatingService service) =>
{
	var result = await service.AddMovieRatingAsync(dto);
    // Any Result<T>/Result instance can be easily converted to
    // Minimal API's IResult. Under the covers, we use an
    // optimized LightResult<T>/LightResult type.
	return result.ToMinimalApiResult();
});

app.Run();
```

### MVC

```csharp
using Light.PortableResults;
using Light.PortableResults.AspNetCore.Mvc;

builder.Services.AddControllers();
builder
    .Services
    .AddPortableResultsForMvc()
    .AddValidationForPortableResults()
    .AddSingleton<MovieRatingValidator>()
    .AddScoped<AddMovieRatingService>();

var app = builder.Build();
app.MapControllers();
app.Run();

[ApiController]
[Route("moveRatings")]
public sealed class AddMovieRatingsController : ControllerBase
{
    public AddMovieRatingsController(AddMovieRatingsService service)
    {
        _service = service;
    }

    [HttpPut]
    public async Task<LightActionResult<MovieRating>> AddMovieRating(AddMovieRatingDto dto)
    {
        var result = await _service.AddMovieRatingAsync(dto);
        return result.ToMvcActionResult();
    }
}
```

### HTTP Response On the Wire

For both examples above (Minimal APIs and MVC), the HTTP response shape is the same.

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
      "code": "LengthIn",
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
      "code": "IsInBetween",
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

### Deserializing Result<T> back from HttpResponseMessage

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using Light.PortableResults;
using Light.PortableResults.Http.Reading;

using var httpClient = new HttpClient
{
	BaseAddress = new Uri("https://localhost:5000")
};

var requestDto = new MovieRatingDto
{
    Id = Guid.CreateVersion7(),
    MovieId = matrixMovie.Id,
    UserName = "Trinity",
    Comment = "The Answer Is Out There, Neo. It's Looking for You.",
    Rating = 5
};

using var response = await httpClient.PutAsJsonAsync(
	"/api/movieRatings",
	requestDto
);

Result<MovieRatingDto> result = await response.ReadResultAsync<MovieRatingDto>();

if (result.IsValid)
{
    Console.WriteLine($"Added movie rating");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Target}: {error.Message}");
    }
}
```

## ☁️ CloudEvents Quick Start

The following example uses `RabbitMQ.Client` to publish and consume a CloudEvents JSON message carrying `Result<UserDto>`.

### Publish to RabbitMQ

```csharp
using System;
using Light.PortableResults;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.CloudEvents.Writing;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "users.updated", durable: true, exclusive: false, autoDelete: false);

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

### Consume from RabbitMQ

```csharp
using Light.PortableResults;
using Light.PortableResults.CloudEvents.Reading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "users.updated", durable: true, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
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

await channel.BasicConsumeAsync(queue: "users.updated", autoAck: false, consumer: consumer);
```

## When to Use Result vs. Exceptions

Use `Result` / `Result<T>` for expected business outcomes:

- validation failed
- resource not found
- user is not authorized
- domain rule was violated

Use exceptions for truly unexpected failures:

- database/network outage
- misconfiguration
- programming bugs and invariant violations (detected via Guard Clauses)

This keeps exceptions exceptional and business outcomes explicit.

## Use non-generic `Result` for command-style operations

```csharp
using Light.PortableResults;

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

## Functional Operators

Supported functional operators:

| Category                | Operators                                  | What they are used for                                                         |
|-------------------------|--------------------------------------------|--------------------------------------------------------------------------------|
| Transform success value | `Map`, `Bind`                              | Convert successful values or chain operations that already return `Result<T>`. |
| Transform errors        | `MapError`                                 | Normalize or translate errors (for example domain -> transport layer).         |
| Add validation rules    | `Ensure`, `FailIf`                         | Keep fluent pipelines while adding business or guard conditions.               |
| Handle outcomes         | `Match`, `MatchFirst`, `Else`              | Turn a result into a value/fallback without manually branching every time.     |
| Side effects            | `Tap`, `TapError`, `Switch`, `SwitchFirst` | Perform logging/metrics/notifications on success or failure paths.             |

All operators also provide async variants with the `Async` suffix (for example `BindAsync`, `MatchAsync`, `TapErrorAsync`).

Example pipeline:

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

## Keep error payloads useful for clients

As a rule of thumb:

- `Message`: human-readable explanation
- `Code`: stable machine-readable identifier (great for frontend/API contracts)
- `Target`: which input field/header/value failed
- `Category`: determines transport mapping (for example, HTTP status)
- `Metadata`: additional information (for example, header values or comparative values)

Using a consistent error shape early will make your APIs and message consumers easier to evolve.

There is an `Error.Exception` property which you can also set, but it is never serialized and thus never exposed to calling processes.

## 🔬 Validation In Depth

### Composing Validators

Use child validators when your DTO contains nested objects or collections that each have their own validation rules. Validators compose by sharing a single `ValidationContext` — errors from all levels accumulate in one pass, and all of them are reported at once.

```csharp
public sealed record PurchaseOrderDto
{
    public required Guid OrderId { get; set; }
    public required string CustomerEmail { get; set; } = string.Empty;
    public required ShippingAddressDto ShippingAddress { get; set; }
    public required List<string> Tags { get; set; }
    public required List<OrderItemDto> Items { get; set; }
}

public sealed record ShippingAddressDto
{
    public required string RecipientName { get; set; } = string.Empty;
    public required string Street { get; set; } = string.Empty;
    public required string PostalCode { get; set; } = string.Empty;
    public required string CountryCode { get; set; } = string.Empty;
}

public sealed record OrderItemDto
{
    public required string Sku { get; set; } = string.Empty;
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
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
        context.Check(dto.OrderId).IsNotEmpty();
        dto.CustomerEmail = context.Check(dto.CustomerEmail).IsEmail();

        // ValidateChild delegates to the child validator, including its automatic null check.
        // If dto.ShippingAddress is null, the child validator produces a validation error
        // automatically — no explicit IsNotNull() call needed here.
        context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);

        // ValidateItems automatically handles a null collection by emitting the standard
        // NotNull error via the active AutomaticNullErrorProvider — no explicit IsNotNull()
        // is needed under default configuration. Guard explicitly only when the provider is
        // configured to skip automatic null errors (e.g. NoOpAutomaticNullErrorProvider).
        // The default string normalization already trims whitespace for individual string items,
        // so no IsNotNullOrWhiteSpace() is needed inside the lambda.
        context.Check(dto.Tags).ValidateItems(
            static (Check<string> tag) => tag.HasLengthIn(2, 30)
        );

        // ValidateItems with a Validator<TItem> delegates null-item handling to the item
        // validator's automatic null check, just like ValidateChild does for single objects.
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

> **What is a `ValidatedValue<T>`?**
>
> `ValidatedValue<T>` is the internal handshake type between a validator and its callers within a single validation pipeline run. Especially in composition scenarios, a validator's `PerformValidation` method gets passed a shared `ValidationContext` and errors accumulate in that context. Rather than being surfaced immediately as a `Result<T>`, `ValidatedValue<T>` carries the signal back: either a successfully validated (and potentially normalized) value via `ValidatedValue<T>.Success(value)`, or `ValidatedValue<T>.NoValue` when errors were added. `checkpoint.ToValidatedValue(dto)` chooses the right outcome for you based on whether any errors were added since the checkpoint was created. You can use this method in `Validator<T>` where the return type is not different from the input type. You never need to construct `ValidatedValue<T>` directly except in transforming validators — see [Mapping to Domain Objects](#mapping-to-domain-objects).

If you don't inject scoped dependencies into them, register all validators as singletons — they should be designed stateless and safe to share:

```csharp
services
    .AddValidationForPortableResults()
    .AddSingleton<ShippingAddressValidator>()
    .AddSingleton<OrderItemValidator>()
    .AddSingleton<PurchaseOrderValidator>();
```

### Automatic Null Checking

The validation framework handles `null` values automatically so you rarely need an explicit `IsNotNull()` guard. The active `AutomaticNullErrorProvider` (configurable via `ValidationContextOptions`) decides what error to produce; under the default configuration it emits a `NotNull` validation error.

- **Validators (`Validator<T>`, `Validator<TSource, TValidated>`, `AsyncValidator<T>`, `AsyncValidator<TSource, TValidated>`)** — when the source value passed to `Validate` / `ValidateAsync` is `null`, the validator adds the automatic null error and returns a failed `Result` without calling `PerformValidation(Async)`. The `isAutomaticNullCheckingEnabled` constructor parameter (default `true`) controls this per validator class.

- **Child validation (`ValidateChild`, `ValidateChildAsync`)** — when a nested collection value is `null`, the child validator's own automatic null check fires, adds the error for the child target, and returns `ValidatedValue<T>.NoValue`. The parent continues collecting other errors in the same pass. `Check<T>` instances that are short-circuited are ignored (no automatic null error will be added to the context, `PerformValidation(Async)` will not be called).

- **Collection item validation (`ValidateItems`, `ValidateItemsAsync`)** — when a `null` collection is passed on a non-short-circuited check, the same automatic null-error pipeline runs for the collection target and the method returns `ValidatedValue<...>.NoValue` without invoking any item validators or delegates. Regarding the items in the collection: item validators as well as delegates have automatic null checking of items, too.

Guard explicitly with `IsNotNull()` when `NoOpAutomaticNullErrorProvider` was set on `ValidationContextOptions.AutomaticNullErrorProvider` (which means that automatic null errors are disabled).

```csharp
// Default configuration — automatic null handling, no explicit guard needed
context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);
context.Check(dto.Tags).ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));

// Explicit guard — required when the active provider does not emit automatic null errors,
// or when you want to short-circuit further checks on null
context.Check(dto.Tags).IsNotNull().ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));
```

Short-circuited checks are always a no-op: validation methods return `ValidatedValue<...>.NoValue` immediately without adding any error or invoking validators or delegates.

### Validation Benchmarks

Below are benchmark results comparing Light.PortableResults to FluentValidation using flat and composite DTOs (see the `benchmarks/Benchmarks` project for details). The `PurchaseOrderDto` example above corresponds to the Complex DTO scenario.

All benchmarks were run on the handware and software versions:

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
```

#### Flat DTO Becnhmark Setup

```csharp
public sealed record MovieRatingDto
{
    public required Guid Id { get; set; }
    public required string Comment { get; set; } = string.Empty;
    public required int Rating { get; set; }
}

// FluentValidation Validator
public sealed class FluentValidationMovieRatingDtoValidator : AbstractValidator<MovieRatingDto>
{
    public FluentValidationMovieRatingDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().Length(10, 1000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

// Light.PortableResults Validator
public sealed class LightPortableResultsMovieRatingDtoValidator : Validator<MovieRatingDto>
{
    public LightPortableResultsMovieRatingDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<MovieRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        MovieRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        dto.Comment = context.Check(dto.Comment).IsNotNullOrWhiteSpace().HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsInBetween(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}
```

The details can be found [here](https://github.com/feO2x/Light.PortableResults/blob/main/benchmarks/Benchmarks/FlatDtoValidationBenchmarks.cs).

#### Valid Flat DTO Benchmarks

No errors on all three properties.

| Method                            | Mean        | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 1,324.57 ns | 8.570 ns | 7.156 ns |  1.00 | 0.8316 | 0.0076 |    6984 B |        1.00 |
| FluentValidationSingleton         |   105.84 ns | 0.246 ns | 0.205 ns |  0.08 | 0.0755 | 0.0001 |     632 B |        0.09 |
| LightPortableResults              |    50.49 ns | 0.091 ns | 0.076 ns |  0.04 | 0.0124 |      - |     104 B |        0.01 |

#### Invalid Flat DTO Benchmarks

All three properties are invalid.

| Method                            | Mean       | Error    | StdDev  | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|--------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,145.2 ns | 10.38 ns | 9.71 ns |  1.00 | 1.7509 | 0.0267 |   14672 B |        1.00 |
| FluentValidationSingleton         | 1,793.6 ns |  3.93 ns | 3.48 ns |  0.57 | 0.9937 | 0.0095 |    8320 B |        0.57 |
| LightPortableResults              |   289.6 ns |  0.57 ns | 0.51 ns |  0.09 | 0.0820 |      - |     688 B |        0.05 |

#### Complex DTO Benchmark Setup

This is the DTO with one nested object, one nested collection with primitive items (strings), and one nested collection with complex items.

```csharp
public sealed record PurchaseOrderDto
{
    public required Guid OrderId { get; set; }
    public required string CustomerEmail { get; set; } = string.Empty;
    public required ShippingAddressDto ShippingAddress { get; set; }
    public required List<string> Tags { get; set; }
    public required List<OrderItemDto> Items { get; set; }
}

public sealed record ShippingAddressDto
{
    public required string RecipientName { get; set; } = string.Empty;
    public required string Street { get; set; } = string.Empty;
    public required string PostalCode { get; set; } = string.Empty;
    public required string CountryCode { get; set; } = string.Empty;
}

public sealed record OrderItemDto
{
    public required string Sku { get; set; } = string.Empty;
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
}
```

The details can be found [here](https://github.com/feO2x/Light.PortableResults/blob/main/benchmarks/Benchmarks/ComplexDtoValidationBenchmarks.cs).

#### Valid Complex DTO Benchmarks

No errors in the DTO object graph.

| Method                            | Mean       | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 8,318.7 ns | 78.34 ns | 69.45 ns |  1.00 | 4.1504 | 0.1221 |  33.94 KB |        1.00 |
| FluentValidationSingleton         | 1,685.9 ns |  5.01 ns |  4.69 ns |  0.20 | 0.7057 | 0.0019 |   5.77 KB |        0.17 |
| LightPortableResults              |   742.2 ns |  7.40 ns |  6.93 ns |  0.09 | 0.1554 |      - |   1.27 KB |        0.04 |

#### Invalid Complex DTO Benchmarks

Nine errors overall in the object graph.

| Method                            |      Mean |     Error |    StdDev | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.985 μs | 0.0705 μs | 0.0625 μs |  1.00 | 6.5308 | 0.3052 |  53.45 KB |        1.00 |
| FluentValidationSingleton         |  6.755 μs | 0.0410 μs | 0.0343 μs |  0.48 | 3.1128 | 0.0763 |  25.47 KB |        0.48 |
| LightPortableResults              |  1.507 μs | 0.0019 μs | 0.0018 μs |  0.11 | 0.2422 |      - |   1.99 KB |        0.04 |

### Mapping to Domain Objects

Use `Validator<TSource, TValidated>` when validation must also produce a *different* output type — typically a mutable DTO in, an immutable domain object out. This pattern implements an Anti-Corruption Layer: the domain model is never exposed to raw, unvalidated input.

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

    // PerformValidation returns ValidatedValue<Movie>, not ValidatedValue<CreateMovieDto>.
    // The domain object is only constructed when all checks pass.
    protected override ValidatedValue<Movie> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        CreateMovieDto dto
    )
    {
        var title = context.Check(dto.Title).IsNotNullOrWhiteSpace();
        context.Check(dto.ReleaseYear).IsInBetween(1888, DateTime.UtcNow.Year);
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

The caller receives a `Result<Movie>` — the `CreateMovieDto` type never escapes the validator boundary:

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

> `Validator<T>` (single type parameter) can also normalize field values — as shown in the Quick Start where strings are trimmed and written back. The difference with `Validator<TSource, TValidated>` is purely at the type level: when the validated output must be a structurally different type, use the two-parameter form.

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
        // Synchronous checks run first — cheap and allocation-free
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsInBetween(1, 5);

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
    .AddScoped<AddMovieRatingValidator>(); // scoped: depends on a scoped repository
```

### Using ValidationContext Directly

You do not need to write a validator class for every case. Inject `IValidationContextFactory` and use `ValidationContext` directly anywhere inline validation is more practical than a dedicated class.

```csharp
public sealed class MovieSearchService
{
    private readonly IValidationContextFactory _contextFactory;
    private readonly IMovieRepository _movieRepository;

    public MovieSearchService(
        IValidationContextFactory contextFactory,
        IMovieRepository movieRepository
    )
    {
        _contextFactory = contextFactory;
        _movieRepository = movieRepository;
    }

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
        context.Check(pageSize).IsInBetween(1, 100);

        if (context.HasErrors)
        {
            return Result<IReadOnlyList<Movie>>.Fail(context.ToFailureResult().Errors);
        }

        return Result<IReadOnlyList<Movie>>.Ok(
            await _movieRepository.SearchAsync(normalizedQuery, page, pageSize, cancellationToken)
        );
    }
}
```

`IValidationContextFactory` is registered automatically by `AddValidationForPortableResults()`.

### Sharing State Between Validators

When a child validator needs data that was loaded or computed by the parent, use `ValidationContext.SetItem` and `GetRequiredItem` with a typed key. This avoids loading the same data twice and keeps child validators free from infrastructure dependencies.

```csharp
// Define the key once — store it as a static field near the validators that use it
public static class MovieConstants
{
    public static readonly ValidationContextKey<Movie> MovieKey = new("movie");
}

// Parent async validator loads the movie and shares it via the context
public sealed class AddMovieRatingValidator : AsyncValidator<MovieRatingDto>
{
    private readonly IMovieClient _movieClient;
    private readonly MovieQuotaValidator _quotaValidator;

    public AddMovieRatingValidator(
        IValidationContextFactory validationContextFactory,
        IMovieClient movieClient,
        MovieQuotaValidator quotaValidator
    ) : base(validationContextFactory)
    {
        _movieClient = movieClient;
        _quotaValidator = quotaValidator;
    }

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
        context.Check(dto.Rating).IsInBetween(1, 5);

        if (!checkpoint.HasNewErrors)
        {
            var movie = await movieClient.GetAsync(dto.MovieId, cancellationToken);
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
                // Store the loaded entity so the child validator can access it
                context.SetItem(MovieConstants.MovieKey, movie);
                context.Check(dto).ValidateChild(_quotaValidator);
            }
        }

        return checkpoint.ToValidatedValue(dto);
    }
}

// Child validator retrieves the pre-loaded entity without hitting the database again
public sealed class MovieQuotaValidator : Validator<MovieRatingDto>
{
    public MovieQuotaValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

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
}
```

`ValidationContextKey<T>` is a typed key that ensures you cannot accidentally retrieve the wrong type from the context. Use `TryGetItem` instead of `GetRequiredItem` when the item may not have been set.

### Custom Assertions

For domain-specific rules that do not map to any built-in assertion, you have two options.

**Ad-hoc predicate** — use `Must` for a one-off check with a custom message:

```csharp
context.Check(dto.ReleaseYear).Must(
    year => year >= 1888 && year <= DateTime.UtcNow.Year
);
```

**Reusable definition** — for rules used across multiple validators, create a `ValidationErrorDefinition` subclass and expose it as a fluent extension method. This participates in the library's message caching and achieves the same performance as built-in assertions.

```csharp
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;

// 1. Define the rule — a static singleton so the cache key is stable
public sealed class MustBeValidMovieYearDefinition : ValidationErrorDefinition
{
    public static readonly MustBeValidMovieYearDefinition Instance = new();

    private MustBeValidMovieYearDefinition() : base(code: "MustBeValidMovieYear") { }

    // IsMessageStable = true tells the framework that the message only depends on
    // DisplayName and can be cached across calls — no dynamic parameters.
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
        new ($"{context.DisplayName} must be a valid movie release year (1888 or later, not in the future)");
}

// 2. Expose it as a fluent extension method on Check<int>
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

`ValidationContextOptions` controls how a `ValidationContext` behaves. All properties are `init`-only, so the record is immutable once created.

| Property                     | Default                                      | What it controls                                                                                                                                                 |
|------------------------------|----------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValueNormalizer`            | `TrimStringNormalizer.Instance`              | How values are normalized before checks see them. The default trims strings and converts `null` to `""`. Replace with `NoOpValueNormalizer.Instance` to disable. |
| `TargetNormalizer`           | `ValidationTargets.DefaultNormalizer`        | How caller-expression targets (e.g., `dto.ShippingAddress`) are converted to error target strings.                                                               |
| `CultureInfo`                | `CultureInfo.InvariantCulture`               | Culture used to format number parameters in error messages. Change for locale-aware output.                                                                      |
| `AutomaticNullErrorProvider` | `DefaultAutomaticNullErrorProvider.Instance` | Produces the error when a validator receives a null source value. Replace to customize the null error shape.                                                     |
| `ErrorTemplates`             | `ValidationErrorTemplates.Default`           | The full set of built-in message templates. Replace individual templates to customize wording globally.                                                          |
| `ErrorDefinitionCache`       | `ValidationErrorDefinitionCache.Default`     | Shared cache for reusable definition instances. The default is a process-wide singleton.                                                                         |

To use custom options in a dependency-injection host, register a customized `IValidationContextFactory` before calling `AddValidationForPortableResults()`:

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

Without a DI host, create the factory directly:

```csharp
var factory = DefaultValidationContextFactory.Create(new ValidationContextOptions
{
    CultureInfo = CultureInfo.GetCultureInfo("de-DE")
});
var validator = new CreateMovieValidator(factory);
```

### Validate `Microsoft.Extensions.Configuration` Options

You can write your custom `Validator<T>` implementations to validate options bound from configuration through `IValidateOptions<T>`. Use the `ValidateWithPortableResults<TOptions, TValidator>()` extension method to integrate with the standard options validation pipeline.

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
        context.Check(options.Port).IsInBetween(1, 65535);
        context.Check(options.ApiKey).IsNotNullOrWhiteSpace();
        return checkpoint.ToValidatedValue(options);
    }
}

IServiceCollection services = new ServiceCollection();

services
    .AddOptions<EmailSenderOptions>()
    .BindConfiguration("EmailSender")
    .ValidateWithPortableResults<EmailSenderOptions, EmailSenderOptionsValidator>()
    .ValidateOnStart(); // Not required, but usually what you want
```

`ValidateWithPortableResults` integrates with the standard options validation pipeline, supports named options, and forwards the current options name to the `ValidationContext`. Use `ValidationContext.TryGetItem(ConfigurationConstants.OptionsNameKey, out var optionsName);` to access the options name in your validator.

## ⚙️ Configuration for HTTP and CloudEvents

### HTTP write options (`PortableResultsHttpWriteOptions`)

| Option                                 | Default                | Description                                                                                                                                                                                               |
|----------------------------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValidationProblemSerializationFormat` | `AspNetCoreCompatible` | Controls how validation errors are serialized for HTTP 400/422 responses. Defaults to `AspNetCoreCompatible` for backwards-compatibility, we encourage you to use `Rich`.                                 |
| `MetadataSerializationMode`            | `ErrorsOnly`           | Controls whether metadata is serialized in response bodies (`ErrorsOnly` or `Always`).                                                                                                                    |
| `CreateProblemDetailsInfo`             | `null`                 | Optional custom factory for generating Problem Details fields (`type`, `title`, `detail`, etc.).                                                                                                          |
| `FirstErrorCategoryIsLeadingCategory`  | `true`                 | If `true`, the first error category decides the HTTP status code for failures. If `false`, Light.PortableResults checks if all errors have the same category and chooses `Unclassified` when they differ. |

### HTTP read options (`PortableResultsHttpReadOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `HeaderParsingService` | `ParseNoHttpHeadersService.Instance` | Controls how HTTP headers are converted into metadata (default: skip all headers). |
| `MergeStrategy` | `AddOrReplace` | Strategy used when merging metadata with the same key from headers and body. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `TreatProblemDetailsAsFailure` | `true` | If `true`, `application/problem+json` is treated as failure even for 2xx status codes. |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |

### CloudEvents write options (`PortableResultsCloudEventsWriteOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `Source` | `null` | Default CloudEvents `source` URI reference if not set per call. |
| `MetadataSerializationMode` | `Always` | Controls whether metadata is serialized into CloudEvents `data`. |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |
| `ConversionService` | `DefaultCloudEventsAttributeConversionService.Instance` | Converts metadata entries into CloudEvents extension attributes. |
| `SuccessType` | `null` | Default CloudEvents `type` for successful results. |
| `FailureType` | `null` | Default CloudEvents `type` for failed results. |
| `Subject` | `null` | Default CloudEvents `subject`. |
| `DataSchema` | `null` | Default CloudEvents `dataschema` URI. |
| `Time` | `null` | Default CloudEvents `time` value (`UTC now` is used when omitted). |
| `IdResolver` | `null` | Optional function used to generate CloudEvents `id` values. |
| `ArrayPool` | `ArrayPool<byte>.Shared` | Buffer pool used for CloudEvents serialization. |
| `PooledArrayInitialCapacity` | `RentedArrayBufferWriter.DefaultInitialCapacity` | Initial buffer size used for pooled serialization, which is 2048 bytes. |

### CloudEvents read options (`PortableResultsCloudEventsReadOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `IsFailureType` | `null` | Optional fallback classifier to decide failure based on CloudEvents `type`. |
| `ParsingService` | `null` | Optional parser for mapping extension attributes to metadata. |
| `MergeStrategy` | `AddOrReplace` | Strategy used when merging envelope extension attributes and payload metadata. |

### Configure HTTP behavior

```csharp
using Light.PortableResults.Http.Writing;
using Light.PortableResults.SharedJsonSerialization;

builder.Services.Configure<PortableResultsHttpWriteOptions>(options =>
{
	options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich;
	options.MetadataSerializationMode = MetadataSerializationMode.Always;
	options.FirstErrorCategoryIsLeadingCategory = false;
});
```

```csharp
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Reading.Json;

var readOptions = new PortableResultsHttpReadOptions
{
	HeaderParsingService = new DefaultHttpHeaderParsingService(new AllHeadersSelectionStrategy()),
	PreferSuccessPayload = PreferSuccessPayload.Auto,
	TreatProblemDetailsAsFailure = true
};

Result<UserDto> result = await response.ReadResultAsync<UserDto>(readOptions);
```

### Configure CloudEvents behavior

```csharp
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.SharedJsonSerialization;

builder.Services.Configure<PortableResultsCloudEventsWriteOptions>(options =>
{
	options.Source = "urn:light-portable-results:sample:user-service";
	options.SuccessType = "users.updated";
	options.FailureType = "users.update.failed";
	options.MetadataSerializationMode = MetadataSerializationMode.Always;
});
```

```csharp
using System;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.Http.Reading.Json;

var cloudReadOptions = new PortableResultsCloudEventsReadOptions
{
	IsFailureType = eventType => eventType.EndsWith(".failed", StringComparison.Ordinal),
	PreferSuccessPayload = PreferSuccessPayload.Auto
};

Result<UserDto> result = messageBody.ReadResult<UserDto>(cloudReadOptions);
```

### Supported Error Categories

| `ErrorCategory` | HTTP Status Code |
| --- | --- |
| `Unclassified` | `500` |
| `Validation` | `400` |
| `Unauthorized` | `401` |
| `PaymentRequired` | `402` |
| `Forbidden` | `403` |
| `NotFound` | `404` |
| `MethodNotAllowed` | `405` |
| `NotAcceptable` | `406` |
| `Timeout` | `408` |
| `Conflict` | `409` |
| `Gone` | `410` |
| `LengthRequired` | `411` |
| `PreconditionFailed` | `412` |
| `ContentTooLarge` | `413` |
| `UriTooLong` | `414` |
| `UnsupportedMediaType` | `415` |
| `RequestedRangeNotSatisfiable` | `416` |
| `ExpectationFailed` | `417` |
| `MisdirectedRequest` | `421` |
| `UnprocessableContent` | `422` |
| `Locked` | `423` |
| `FailedDependency` | `424` |
| `UpgradeRequired` | `426` |
| `PreconditionRequired` | `428` |
| `TooManyRequests` | `429` |
| `RequestHeaderFieldsTooLarge` | `431` |
| `UnavailableForLegalReasons` | `451` |
| `InternalError` | `500` |
| `NotImplemented` | `501` |
| `BadGateway` | `502` |
| `ServiceUnavailable` | `503` |
| `GatewayTimeout` | `504` |
| `InsufficientStorage` | `507` |
