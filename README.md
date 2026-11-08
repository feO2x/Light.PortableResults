# Light.PortableResults

*The Result Pattern for .NET that travels. Every `Result<T>` serializes reliably to RFC 9457 Problem Details over HTTP, to CloudEvents, and back again — with a validation framework that is 2–6x faster than FluentValidation 12.1.1 with 8–22% of its allocations (compared with singleton-registered FluentValidation validators).*

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Light.PortableResults.svg?style=for-the-badge)](https://www.nuget.org/packages?q=Light.PortableResults)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/releases)

Most Result Pattern libraries stop at the application boundary. Light.PortableResults does not: a `Result<T>` can be written as an HTTP response or a CloudEvents message, read back on the other side, and arrive as a fully typed `Result<T>` — without losing errors, metadata, or structure.

<a id="key-features"></a>

## ✨ Key Features

- **Clear Result Pattern** — `Result` and `Result<T>` hold either a success value or one or more structured errors. No exceptions for expected failures. See [Result basics](https://github.com/feO2x/Light.PortableResults/blob/main/docs/result-basics.md).
- **Rich, machine-readable errors** — every `Error` carries a human-readable `Message`, a stable `Code`, an input `Target`, a `Category`, and optional metadata. See [Result basics](https://github.com/feO2x/Light.PortableResults/blob/main/docs/result-basics.md#designing-useful-errors).
- **Serialization-safe metadata** — a dedicated JSON-like type system instead of `Dictionary<string, object>`, with per-value control over where a value is written. See [Metadata](https://github.com/feO2x/Light.PortableResults/blob/main/docs/metadata.md).
- **Functional operators** — `Map`, `Bind`, `Match`, `Ensure`, `Tap`, `Switch`, and their `Async` variants. See [Functional operators](https://github.com/feO2x/Light.PortableResults/blob/main/docs/functional-operators.md).
- **HTTP round trip** — Minimal APIs and MVC write RFC 9457 Problem Details with automatic status codes, and `HttpResponseMessage.ReadResultAsync<T>()` reads them back. See [HTTP](https://github.com/feO2x/Light.PortableResults/blob/main/docs/http.md).
- **CloudEvents round trip** — write and read CloudEvents Spec 1.0 JSON envelopes for any message broker. See [CloudEvents](https://github.com/feO2x/Light.PortableResults/blob/main/docs/cloudevents.md).
- **High-performance validation** — 2–6x faster than FluentValidation 12.1.1 with 8–22% of its allocations when both use singleton validators; compose validators, map DTOs to domain objects, and validate asynchronously. See [Validation](https://github.com/feO2x/Light.PortableResults/blob/main/docs/validation/getting-started.md) and [Benchmarks](https://github.com/feO2x/Light.PortableResults/blob/main/docs/benchmarks.md).
- **OpenAPI from your validators** — a Roslyn source generator turns validators into accurate error schemas and examples for Microsoft.AspNetCore.OpenApi. See [OpenAPI](https://github.com/feO2x/Light.PortableResults/blob/main/docs/openapi.md).
- **Native AOT** — every package except the MVC integration is trimming- and AOT-compatible. See [Native AOT](https://github.com/feO2x/Light.PortableResults/blob/main/docs/native-aot.md).

<a id="installation"></a>

## 📦 Installation

Install the packages you need for your scenario:

| Package | Contents | Target frameworks |
| --- | --- | --- |
| `Light.PortableResults` | `Result`, `Error`, metadata, functional operators, HTTP and CloudEvents serialization | `netstandard2.0`, `net10.0` |
| `Light.PortableResults.Validation` | Validators, validation context, built-in checks | `netstandard2.0`, `net10.0` |
| `Light.PortableResults.AspNetCore.MinimalApis` | `IResult` integration for Minimal APIs | `net10.0` |
| `Light.PortableResults.AspNetCore.Mvc` | `IActionResult` integration for MVC | `net10.0` |
| `Light.PortableResults.AspNetCore.OpenApi` | OpenAPI response documentation | `net10.0` |
| `Light.PortableResults.Validation.OpenApi` | Built-in validation error contracts and the OpenAPI source generator | `net10.0` |

```bash
dotnet add package Light.PortableResults.AspNetCore.MinimalApis
dotnet add package Light.PortableResults.Validation
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

<a id="walkthrough"></a>

## 🚀 Walkthrough

The following four steps take one movie rating from a service through an HTTP endpoint to a typed client. The code is compiled and executed by the test suite of this repository.

### 1. Return `Result<MovieRating>` from a service

A service returns either the stored rating or the errors that explain why it could not be stored. `CheckForErrors` runs the validator from step 2 and hands back a failed `Result` when there is something to report.

```csharp
public sealed record MovieRating(Guid Id, Guid MovieId, string UserName, string Comment, int Rating);

public sealed class AddMovieRatingService
{
    private readonly MovieRatingValidator _validator;

    public AddMovieRatingService(MovieRatingValidator validator) => _validator = validator;

    public Result<MovieRating> AddMovieRating(MovieRatingDto dto)
    {
        if (_validator.CheckForErrors(dto, out var failure))
        {
            return Result<MovieRating>.Fail(failure.Errors);
        }

        var rating = new MovieRating(dto.Id, dto.MovieId, dto.UserName, dto.Comment, dto.Rating);
        return Result<MovieRating>.Ok(rating);
    }
}
```

### 2. Validate the DTO with `Validator<MovieRatingDto>`

Validators use the `Light.PortableResults.Validation` namespace. Every failed check adds an `Error` with message, code, target, category, and metadata — you never construct validation errors by hand.

```csharp
public sealed record MovieRatingDto
{
    public required Guid Id { get; init; }
    public required Guid MovieId { get; init; }
    public required string UserName { get; set; }
    public required string Comment { get; set; }
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

        // Strings are trimmed by default - assign the result to keep the normalized value.
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();

        context.Check(dto.Rating).IsInRange(1, 5);

        return checkpoint.ToValidatedValue(dto);
    }
}
```

### 3. Return `result.ToMinimalApiResult()` from an endpoint

Register the services. The `Rich` format writes every error as one object with all its details; the default is the ASP.NET Core-compatible format.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddPortableResultsForMinimalApis()
    .AddValidationForPortableResults()
    .Configure<PortableResultsHttpWriteOptions>(
        x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
    )
    .AddSingleton<MovieRatingValidator>()
    .AddSingleton<AddMovieRatingService>();
```

Then map the endpoint and call `app.Run()`. `ToMinimalApiResult()` writes the value with `200 OK` on success and derives the status code of a failure from the error category.

```csharp
var app = builder.Build();
app.MapPost("/api/movieRatings", (MovieRatingDto dto, AddMovieRatingService service) =>
{
    Result<MovieRating> result = service.AddMovieRating(dto);
    return result.ToMinimalApiResult();
});
```

Posting a rating with a short comment, a blank user name, and a rating of 7 produces this response:

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

### 4. Read the response back with `ReadResultAsync<MovieRating>()`

On the client, `ReadResultAsync<T>()` from `Light.PortableResults.Http.Reading` turns the response back into a `Result<MovieRating>` with the same errors, codes, targets, categories, and metadata that the server produced.

```csharp
using var response = await httpClient.PostAsJsonAsync("/api/movieRatings", dto);
Result<MovieRating> result = await response.ReadResultAsync<MovieRating>();

if (result.IsValid)
{
    Console.WriteLine($"Stored rating {result.Value.Id}");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Target}: {error.Message} ({error.Code})");
    }
}
```

The [HTTP documentation](https://github.com/feO2x/Light.PortableResults/blob/main/docs/http.md) covers MVC, response headers, metadata in success responses, and all read options.

<a id="not-just-http"></a>

## 🌐 Not Just HTTP

### CloudEvents

`ToCloudEvent` serializes a result into a CloudEvents Spec 1.0 JSON envelope that you can publish with any broker client, and `ReadResult<T>` turns a received envelope back into a `Result<T>`.

```csharp
// Producer: serialize the result and publish the bytes with your broker client.
byte[] cloudEvent = result.ToCloudEvent(
    successType: "movie-ratings.added",
    failureType: "movie-ratings.add-failed",
    source: "urn:movies:rating-service"
);

// Consumer: read the received UTF-8 bytes back into a typed result.
ReadOnlyMemory<byte> body = cloudEvent;
Result<MovieRating> received = body.ReadResult<MovieRating>();
```

See [CloudEvents](https://github.com/feO2x/Light.PortableResults/blob/main/docs/cloudevents.md) for the wire format, extension attributes, and options.

### OpenAPI

Mark a validator with `[GeneratePortableValidationOpenApi]` and make it `partial`: a source generator derives the error codes, metadata schemas, and examples from its checks. `ProducesPortableValidationProblemFor<TValidator>` puts them into the OpenAPI document of the endpoint.

```csharp
[GeneratePortableValidationOpenApi]
public sealed partial class MovieRatingValidator : Validator<MovieRatingDto> { /* as in step 2 */ }

app.MapPost("/api/movieRatings", AddMovieRating)
   .ProducesPortableSuccessResponse<MovieRating>()
   .ProducesPortableValidationProblemFor<MovieRatingValidator>();
```

See [OpenAPI](https://github.com/feO2x/Light.PortableResults/blob/main/docs/openapi.md) for registration, MVC attributes, and the diagnostics reference.

### Native AOT

`Light.PortableResults`, `Light.PortableResults.Validation`, `Light.PortableResults.AspNetCore.MinimalApis`, `Light.PortableResults.AspNetCore.OpenApi`, and `Light.PortableResults.Validation.OpenApi` are trimming- and AOT-compatible; `Light.PortableResults.AspNetCore.Mvc` is not. CI publishes a Native AOT sample so that regressions fail the build. See [Native AOT](https://github.com/feO2x/Light.PortableResults/blob/main/docs/native-aot.md) for the `JsonSerializerContext` you need to declare.

<a id="documentation"></a>

## 📚 Documentation

- [Result basics](https://github.com/feO2x/Light.PortableResults/blob/main/docs/result-basics.md) — `Result`, `Result<T>`, `Error`, `Errors`, and error design
- [Functional operators](https://github.com/feO2x/Light.PortableResults/blob/main/docs/functional-operators.md) — `Map`, `Bind`, `Match`, `Ensure`, `Tap`, and friends
- [Metadata](https://github.com/feO2x/Light.PortableResults/blob/main/docs/metadata.md) — the metadata type system, annotations, builders, merging, and tracing
- [HTTP](https://github.com/feO2x/Light.PortableResults/blob/main/docs/http.md) — Minimal APIs, MVC, wire formats, headers, enrichers, and reading responses
- [CloudEvents](https://github.com/feO2x/Light.PortableResults/blob/main/docs/cloudevents.md) — wire format, extension attributes, converters, and parsers
- Validation
  - [Getting started](https://github.com/feO2x/Light.PortableResults/blob/main/docs/validation/getting-started.md) — validators, `CheckForErrors`, and registration
  - [Built-in checks](https://github.com/feO2x/Light.PortableResults/blob/main/docs/validation/built-in-checks.md) — every check with its error code and metadata keys
  - [Advanced validation](https://github.com/feO2x/Light.PortableResults/blob/main/docs/validation/advanced.md) — composition, null handling, mapping, async validation, shared state, and custom checks
  - [Validation configuration](https://github.com/feO2x/Light.PortableResults/blob/main/docs/validation/configuration.md) — context factory, message templates, localization, normalization, and options validation
- [OpenAPI](https://github.com/feO2x/Light.PortableResults/blob/main/docs/openapi.md) — registration, endpoint documentation, source generation, and diagnostics
- [Native AOT](https://github.com/feO2x/Light.PortableResults/blob/main/docs/native-aot.md) — what to register and how contracts are resolved
- [Configuration reference](https://github.com/feO2x/Light.PortableResults/blob/main/docs/configuration-reference.md) — every options type and the `ErrorCategory` to status code map
- [Benchmarks](https://github.com/feO2x/Light.PortableResults/blob/main/docs/benchmarks.md) — validation performance compared with FluentValidation
