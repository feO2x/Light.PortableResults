# ASP.NET Core Minimal API Serialization for Light.Results

## Goals & Constraints

1. **New project**: Create `Light.Results.AspNetCore` in `./src/` targeting .NET 10 (not .NET Standard 2.0).
2. **RFC 7807/9457 compliance**: Serialize failed `Result<T>` and `Result` instances to Problem Details format.
3. **Minimal API focus**: This plan covers ASP.NET Core Minimal APIs only; MVC API Controllers will be handled in a separate plan.
4. **Metadata preservation**: Transport Light.Results metadata through the `extensions` property of Problem Details.
5. **Performance**: Minimize allocations; avoid unnecessary object creation.
6. **Deserialization**: Response deserialization from `HttpResponseMessage` will be handled in a future plan.

## Current State Analysis

### Light.Results Types

| Type | Description |
|------|-------------|
| `Result<T>` | Success with value `T` or one or more `Error` instances |
| `Result` | Success (void) or one or more `Error` instances |
| `Error` | Message (required), Code, Target, Category, Metadata |
| `Errors` | Collection of `Error` with small-buffer optimization |
| `ErrorCategory` | Enum mapping to HTTP status codes (e.g., `Validation = 400`) |
| `MetadataObject` | Immutable key-value store for arbitrary metadata |

### ErrorCategory → HTTP Status Code Mapping

`ErrorCategory` values are already aligned with HTTP status codes:

| ErrorCategory | HTTP Status |
|---------------|-------------|
| `Unclassified` (0) | 500 (fallback) |
| `Validation` (400) | 400 Bad Request |
| `Unauthorized` (401) | 401 Unauthorized |
| `Forbidden` (403) | 403 Forbidden |
| `NotFound` (404) | 404 Not Found |
| `Timeout` (408) | 408 Request Timeout |
| `Conflict` (409) | 409 Conflict |
| `Gone` (410) | 410 Gone |
| `PreconditionFailed` (412) | 412 Precondition Failed |
| `ContentTooLarge` (413) | 413 Content Too Large |
| `UriTooLong` (414) | 414 URI Too Long |
| `UnsupportedMediaType` (415) | 415 Unsupported Media Type |
| `UnprocessableEntity` (422) | 422 Unprocessable Entity |
| `RateLimited` (429) | 429 Too Many Requests |
| `UnavailableForLegalReasons` (451) | 451 Unavailable For Legal Reasons |
| `InternalError` (500) | 500 Internal Server Error |
| `NotImplemented` (501) | 501 Not Implemented |
| `BadGateway` (502) | 502 Bad Gateway |
| `ServiceUnavailable` (503) | 503 Service Unavailable |
| `GatewayTimeout` (504) | 504 Gateway Timeout |

## RFC 7807/9457 Problem Details Structure

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/orders/123",
  "errors": [
    {
      "message": "Name is required",
      "code": "VALIDATION_REQUIRED",
      "target": "name"
    }
  ],
  "traceId": "abc123",
  "customKey": "customValue"
}
```

**Note**: This example shows the **rich format** (`ErrorSerializationFormat.Rich`). The default **ASP.NET Core-compatible format** uses `errors` as a `Dictionary<string, string[]>` grouped by target—see Design Decision #2 for details.

Per RFC 9457, extension members (like `traceId`, `customKey`, `errors`) appear at the root level of the Problem Details object, not nested inside an `extensions` wrapper. The `errors` array is a Light.Results extension to the standard.

## Design Decisions

### 1. Bypassing ASP.NET Core's `ProblemDetails` (Hybrid Approach)

**Decision**: Create a custom `LightProblemDetails` class that implements `Microsoft.AspNetCore.Http.IResult` directly, bypassing ASP.NET Core's `ProblemDetails` class.

**Rationale**:
- ASP.NET Core's `ProblemDetails.Extensions` is `IDictionary<string, object?>`, which requires dictionary allocations and boxing for value types—undermining Light.Results' `MetadataObject` design with less allocations.
- Direct `IResult` implementation gives full control over JSON serialization.
- Using `System.Text.Json` source generators enables zero-allocation serialization paths.
- The serialized JSON output must be fully RFC 7807/9457 compliant.

**Why not `ProblemDetails`?**
- `ProblemDetails` integrates with `IProblemDetailsService` and `UseExceptionHandler` middleware, but these only apply to **unhandled exceptions**.
- Light.Results handles **expected error conditions** via `Result<T>` / `Result` — no exceptions are thrown, so `UseExceptionHandler` is never involved.
- Callers who need ASP.NET Core `ProblemDetails` interop can use the optional `ToProblemDetails()` method.

**Approach**:
- `LightProblemDetails` holds `Errors` and `MetadataObject?` directly—no intermediate dictionary conversion.
- Implements `IResult.ExecuteAsync()` to write RFC 7807/9457-compliant JSON directly to the response.
- Uses source-generated `JsonSerializerContext` for optimal serialization performance.
- Separate `ToProblemDetails()` extension methods on `Result<T>` / `Result` provide interop when callers need the ASP.NET Core type (see Design Decision #6).

### 2. Error Serialization Format

Two serialization formats are supported, controlled by the `ErrorSerializationFormat` enum:

#### Default: ASP.NET Core-Compatible Format

By default, errors are serialized in a format compatible with ASP.NET Core's `ValidationProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "name": ["Name is required", "Name must be at least 2 characters"],
    "email": ["Invalid email format"]
  },
  "errorDetails": [
    { "target": "name", "index": 0, "code": "REQUIRED", "category": "Validation" },
    { "target": "name", "index": 1, "code": "MIN_LENGTH", "metadata": { "minLength": 2 } },
    { "target": "email", "index": 0, "code": "INVALID_FORMAT" }
  ],
  "traceId": "abc123"
}
```

- **`errors`**: `Dictionary<string, string[]>` grouped by `Error.Target` — compatible with clients expecting `ValidationProblemDetails`.
- **`errorDetails`**: Array with additional error details (code, category, metadata). Only included when errors have properties beyond message/target. The `index` field indicates which message in the `errors` array this detail corresponds to.
- Errors without a `Target` are grouped under the key `""` (empty string), matching ASP.NET Core's behavior for model-level errors.

#### Opt-in: Rich Format

For clients that can consume the full error structure:

```json
{
  "type": "...",
  "title": "...",
  "status": 400,
  "detail": "...",
  "errors": [
    {
      "message": "Name is required",
      "code": "REQUIRED",
      "target": "name",
      "category": "Validation",
      "metadata": { "attemptedValue": "" }
    }
  ]
}
```

This format preserves all error information inline but is not compatible with ASP.NET Core's `ValidationProblemDetails` structure.

#### Format Selection

```csharp
public enum ErrorSerializationFormat
{
    /// <summary>
    /// ASP.NET Core-compatible: errors as Dictionary&lt;string, string[]&gt; grouped by target.
    /// Additional error details in separate "errorDetails" array when present.
    /// </summary>
    AspNetCoreCompatible,

    /// <summary>
    /// Rich format: errors as array of objects with all properties inline.
    /// </summary>
    Rich
}
```

**Rationale**: Defaulting to ASP.NET Core-compatible format maximizes interop with existing clients and tooling (e.g., Blazor's `EditContext`, JavaScript clients expecting `errors` as a dictionary). The rich format is available for clients that need full error fidelity.

### 3. Leading Error Category Algorithm

When an `Errors` collection contains multiple errors with different categories, we need to determine which category drives the HTTP status code.

**Method signature**:
```csharp
public static ErrorCategory GetLeadingCategory(
    this Errors errors,
    bool firstCategoryIsLeadingCategory = false)
```

**Algorithm**:
1. If `firstCategoryIsLeadingCategory` is `true`: Return `errors.First.Category`.
2. Otherwise:
   - Iterate through all errors.
   - If all errors have the same category, return that category.
   - If categories differ, return `ErrorCategory.Unclassified`.

**Rationale for `Unclassified` fallback**: When errors span multiple categories (e.g., `Validation` and `NotFound`), there's no single correct HTTP status. `Unclassified` maps to 500, signaling an unexpected mixed-error state. Callers who want different behavior can use `firstCategoryIsLeadingCategory = true`.

### 4. Metadata Handling

**Result-level metadata**: Serialized as extension properties at the root level of the Problem Details JSON (per RFC 9457, extension members are allowed at the top level).

**Error-level metadata**: Placed in each error object's `metadata` property.

**Example**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid email",
  "errors": [
    {
      "message": "Invalid email",
      "code": "INVALID_EMAIL",
      "target": "email",
      "metadata": {
        "attemptedValue": "not-an-email"
      }
    }
  ],
  "traceId": "abc123",
  "correlationId": "xyz789"
}
```

**Note**: `MetadataObject` and `MetadataValue` are serialized directly without conversion to `Dictionary<string, object?>`—preserving the allocation-free design.

### 5. Success Response Handling

For successful results:
- `Result<T>`: Return `Results.Ok(value)` (HTTP 200) by default.
- `Result`: Return `Results.NoContent()` (HTTP 204) by default.

The `ToMinimalApiResult` method will have overloads accepting a `Func<T, IResult>` or `Func<IResult>` to customize success responses (e.g., `Results.Created()`).

### 6. ASP.NET Core Interop

For callers who need ASP.NET Core's `ProblemDetails` type (e.g., for `IProblemDetailsService` customization or third-party library compatibility), provide extension methods directly on `Result<T>` and `Result`:

```csharp
/// <summary>
/// Converts a failed Result to ASP.NET Core's ProblemDetails.
/// Note: This allocates a Dictionary for the Extensions property.
/// </summary>
public static ProblemDetails ToProblemDetails(
    this Result result,
    bool firstCategoryIsLeadingCategory = false);

/// <summary>
/// Converts a failed Result&lt;T&gt; to ASP.NET Core's ProblemDetails.
/// Note: This allocates a Dictionary for the Extensions property.
/// </summary>
public static ProblemDetails ToProblemDetails<T>(
    this Result<T> result,
    bool firstCategoryIsLeadingCategory = false);
```

**Why not on `LightProblemDetails`?**
- Placing `ToProblemDetails()` on `LightProblemDetails` would force callers to allocate an intermediate `LightProblemDetails` instance just to convert it immediately to `ProblemDetails`.
- Extension methods on `Result<T>` / `Result` create `ProblemDetails` directly—no intermediate allocation.

This is an **opt-in** escape hatch, not the default path.

### 7. Result Enrichment Service

For cross-cutting concerns like adding trace IDs, correlation IDs, or other metadata to all error responses, provide an optional service abstraction:

```csharp
/// <summary>
/// Service for enriching results with additional metadata before conversion to LightProblemDetails.
/// Register in DI to add traceId, correlationId, or other cross-cutting metadata.
/// </summary>
public interface ILightProblemDetailsEnricher
{
    /// <summary>
    /// Enriches a result with additional metadata before conversion to LightProblemDetails.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
    Result<T> Enrich<T>(Result<T> result, HttpContext httpContext);

    /// <summary>
    /// Enriches a void result with additional metadata before conversion to LightProblemDetails.
    /// </summary>
    /// <param name="result">The original result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
    Result Enrich(Result result, HttpContext httpContext);
}
```

**Design notes**:
- Two methods: generic `Enrich<T>` for `Result<T>` and non-generic `Enrich` for `Result`.
- Only invoked for failed results (success responses don't need Problem Details enrichment).
- Returns a new `Result<T>` with additional metadata rather than mutating the original.
- Optional: If no enricher is registered, the original result is used unchanged.

**Example implementation**:
```csharp
public class TracingProblemDetailsEnricher : ILightProblemDetailsEnricher
{
    public Result<T> Enrich<T>(Result<T> result, HttpContext httpContext)
    {
        if (result.IsValid) return result;

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return result.WithMetadata("traceId", traceId);
    }

    public Result Enrich(Result result, HttpContext httpContext)
    {
        if (result.IsValid) return result;

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return result.WithMetadata("traceId", traceId);
    }
}

// Registration
builder.Services.AddSingleton<ILightProblemDetailsEnricher, TracingProblemDetailsEnricher>();
```

**Usage in extension methods**:
```csharp
public static IResult ToMinimalApiResult<T>(
    this Result<T> result,
    HttpContext httpContext,  // Required when enricher might be registered
    bool firstCategoryIsLeadingCategory = false,
    string? instance = null,
    ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
{
    var enricher = httpContext.RequestServices.GetService<ILightProblemDetailsEnricher>();
    var enrichedResult = enricher?.Enrich(result, httpContext) ?? result;

    // ... rest of implementation
}
```

## API Design

### LightProblemDetails Class

```csharp
namespace Light.Results.AspNetCore;

/// <summary>
/// RFC 7807/9457-compliant Problem Details response that implements IResult directly.
/// Avoids the allocation overhead of ASP.NET Core's ProblemDetails.Extensions dictionary.
/// </summary>
/// <remarks>
/// This is a class (not a struct) because it will be boxed when returned as IResult anyway.
/// Using a class avoids the additional boxing allocation.
/// </remarks>
public sealed class LightProblemDetails : Microsoft.AspNetCore.Http.IResult
{
    /// <summary>RFC 9110 section URI for the status code.</summary>
    public string Type { get; }

    /// <summary>HTTP status phrase.</summary>
    public string Title { get; }

    /// <summary>HTTP status code.</summary>
    public int Status { get; }

    /// <summary>Human-readable explanation (first error message).</summary>
    public string Detail { get; }

    /// <summary>URI reference identifying the specific occurrence (optional).</summary>
    public string? Instance { get; }

    /// <summary>The errors from the failed result.</summary>
    public Errors Errors { get; }

    /// <summary>Result-level metadata (serialized as extension properties).</summary>
    public MetadataObject? Metadata { get; }

    /// <summary>The serialization format for errors.</summary>
    public ErrorSerializationFormat ErrorFormat { get; }

    /// <summary>
    /// Writes the Problem Details JSON directly to the HTTP response.
    /// Uses source-generated serialization for optimal performance.
    /// </summary>
    public Task ExecuteAsync(HttpContext httpContext);
}
```

### Primary Extension Methods

```csharp
namespace Light.Results.AspNetCore;

public static class MinimalApiResultExtensions
{
    /// <summary>
    /// Converts a Result&lt;T&gt; to an ASP.NET Core Minimal API IResult.
    /// On success, returns the value with HTTP 200.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">HTTP context for enricher resolution (optional if no enricher registered).</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format (default: ASP.NET Core-compatible).</param>
    public static Microsoft.AspNetCore.Http.IResult ToMinimalApiResult<T>(
        this Result<T> result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible);

    /// <summary>
    /// Converts a Result&lt;T&gt; to an ASP.NET Core Minimal API IResult.
    /// On success, invokes the success factory to create the response.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToMinimalApiResult<T>(
        this Result<T> result,
        Func<T, Microsoft.AspNetCore.Http.IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible);

    /// <summary>
    /// Converts a Result to an ASP.NET Core Minimal API IResult.
    /// On success, returns HTTP 204 No Content.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToMinimalApiResult(
        this Result result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible);

    /// <summary>
    /// Converts a Result to an ASP.NET Core Minimal API IResult.
    /// On success, invokes the success factory to create the response.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToMinimalApiResult(
        this Result result,
        Func<Microsoft.AspNetCore.Http.IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible);
}
```

### Leading Category Extension

```csharp
namespace Light.Results.AspNetCore;

public static class ErrorCategoryExtensions
{
    /// <summary>
    /// Determines the leading error category from a collection of errors.
    /// </summary>
    /// <param name="errors">The errors collection (must contain at least one error).</param>
    /// <param name="firstCategoryIsLeadingCategory">
    /// If true, returns the category of the first error.
    /// If false, returns the common category if all errors share it, otherwise Unclassified.
    /// </param>
    /// <returns>The leading error category.</returns>
    /// <exception cref="InvalidOperationException">Thrown when errors is empty.</exception>
    public static ErrorCategory GetLeadingCategory(
        this Errors errors,
        bool firstCategoryIsLeadingCategory = false);

    /// <summary>
    /// Converts an ErrorCategory to its corresponding HTTP status code.
    /// </summary>
    public static int ToHttpStatusCode(this ErrorCategory category);
}
```

## Project Structure

```
src/
├── Light.Results/                          # Existing (netstandard2.0)
└── Light.Results.AspNetCore/               # New (net10.0)
    ├── Light.Results.AspNetCore.csproj
    ├── LightProblemDetails.cs              # IResult implementation for error responses
    ├── LightProblemDetailsJsonContext.cs   # Source-generated JSON serializer
    ├── MinimalApiResultExtensions.cs       # ToMinimalApiResult and ToProblemDetails methods
    ├── ErrorCategoryExtensions.cs          # GetLeadingCategory, ToHttpStatusCode
    ├── HttpStatusCodeInfo.cs               # RFC 9110 type URIs and status titles
    ├── ErrorSerializationFormat.cs         # Enum for ASP.NET Core-compatible vs rich format
    └── ILightProblemDetailsEnricher.cs     # Optional service for enriching results

tests/
├── Light.Results.Tests/                    # Existing
└── Light.Results.AspNetCore.Tests/         # New (net10.0)
    ├── Light.Results.AspNetCore.Tests.csproj
    ├── LightProblemDetailsTests.cs
    ├── MinimalApiResultExtensionsTests.cs
    ├── ToProblemDetailsTests.cs
    ├── ErrorCategoryExtensionsTests.cs
    ├── GetLeadingCategoryTests.cs
    ├── ErrorSerializationFormatTests.cs    # Tests for both serialization formats
    └── LightProblemDetailsEnricherTests.cs # Tests for enricher integration
```

## Implementation Steps

### Phase 1: Project Setup

1. Create `src/Light.Results.AspNetCore/Light.Results.AspNetCore.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
     </PropertyGroup>

     <ItemGroup>
       <FrameworkReference Include="Microsoft.AspNetCore.App" />
       <ProjectReference Include="..\Light.Results\Light.Results.csproj" />
     </ItemGroup>
   </Project>
   ```

2. Create `tests/Light.Results.AspNetCore.Tests/Light.Results.AspNetCore.Tests.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
       <OutputType>Exe</OutputType>
       <RootNamespace>Light.Results.AspNetCore.Tests</RootNamespace>
     </PropertyGroup>

     <ItemGroup>
       <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
     </ItemGroup>

     <ItemGroup>
       <Using Include="Xunit" />
     </ItemGroup>

     <ItemGroup>
       <PackageReference Include="FluentAssertions" />
       <PackageReference Include="Microsoft.NET.Test.Sdk" />
       <PackageReference Include="xunit.v3" />
       <PackageReference Include="xunit.runner.visualstudio" />
       <PackageReference Include="coverlet.collector" />
     </ItemGroup>

     <ItemGroup>
       <ProjectReference Include="..\..\src\Light.Results.AspNetCore\Light.Results.AspNetCore.csproj" />
     </ItemGroup>
   </Project>
   ```

3. Add both projects to `Light.Results.slnx`.

### Phase 2: ErrorCategoryExtensions

1. Implement `GetLeadingCategory`:
   ```csharp
   public static ErrorCategory GetLeadingCategory(
       this Errors errors,
       bool firstCategoryIsLeadingCategory = false)
   {
       if (errors.IsDefaultInstance)
       {
           throw new InvalidOperationException("Errors collection must contain at least one error.");
       }

       if (firstCategoryIsLeadingCategory)
       {
           return errors.First.Category;
       }

       var firstCategory = errors.First.Category;
       foreach (var error in errors)
       {
           if (error.Category != firstCategory)
           {
               return ErrorCategory.Unclassified;
           }
       }

       return firstCategory;
   }
   ```

2. Implement `ToHttpStatusCode`:
   ```csharp
   public static int ToHttpStatusCode(this ErrorCategory category)
   {
       // ErrorCategory values are already HTTP status codes (except Unclassified = 0)
       return category == ErrorCategory.Unclassified ? 500 : (int)category;
   }
   ```

3. Write unit tests for both methods.

### Phase 3: LightProblemDetails

Implement the custom `IResult` class:

```csharp
namespace Light.Results.AspNetCore;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// RFC 7807/9457-compliant Problem Details response that implements IResult directly.
/// Avoids the allocation overhead of ASP.NET Core's ProblemDetails.Extensions dictionary.
/// </summary>
/// <remarks>
/// This is a class (not a struct) because it will be boxed when returned as IResult anyway.
/// Using a class avoids the additional boxing allocation.
/// </remarks>
public sealed class LightProblemDetails : IResult
{
    public string Type { get; }
    public string Title { get; }
    public int Status { get; }
    public string Detail { get; }
    public string? Instance { get; }
    public Errors Errors { get; }
    public MetadataObject? Metadata { get; }

    public LightProblemDetails(
        Errors errors,
        MetadataObject? metadata,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
    {
        var leadingCategory = errors.GetLeadingCategory(firstCategoryIsLeadingCategory);
        Status = leadingCategory.ToHttpStatusCode();
        Type = HttpStatusCodeInfo.GetTypeUri(Status);
        Title = HttpStatusCodeInfo.GetTitle(Status);
        Detail = errors.First.Message;
        Instance = instance;
        Errors = errors;
        Metadata = metadata;
        ErrorFormat = errorFormat;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = Status;
        httpContext.Response.ContentType = "application/problem+json";
        return JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            this,
            LightProblemDetailsJsonContext.Default.LightProblemDetails,
            httpContext.RequestAborted);
    }
}
```

### Phase 4: Source-Generated JSON Serializer

Create a `JsonSerializerContext` for optimal serialization:

```csharp
namespace Light.Results.AspNetCore;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LightProblemDetails))]
internal partial class LightProblemDetailsJsonContext : JsonSerializerContext
{
}
```

**Note**: Custom `JsonConverter<T>` implementations will be needed for `Errors`, `Error`, `MetadataObject`, and `MetadataValue` to serialize them correctly without intermediate allocations.

### Phase 5: HttpStatusCodeInfo Helper

Create a helper for RFC 9110 type URIs and status titles:

```csharp
namespace Light.Results.AspNetCore;

internal static class HttpStatusCodeInfo
{
    public static string GetTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        // ... other status codes
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1" // 500 fallback
    };

    public static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        // ... other status codes
        _ => "Internal Server Error"
    };
}
```

### Phase 6: ErrorSerializationFormat and ILightProblemDetailsEnricher

1. Create `ErrorSerializationFormat.cs`:
   ```csharp
   namespace Light.Results.AspNetCore;

   /// <summary>
   /// Specifies how errors are serialized in Problem Details responses.
   /// </summary>
   public enum ErrorSerializationFormat
   {
       /// <summary>
       /// ASP.NET Core-compatible: errors as Dictionary&lt;string, string[]&gt; grouped by target.
       /// Additional error details in separate "errorDetails" array when present.
       /// </summary>
       AspNetCoreCompatible,

       /// <summary>
       /// Rich format: errors as array of objects with all properties inline.
       /// </summary>
       Rich
   }
   ```

2. Create `ILightProblemDetailsEnricher.cs`:
   ```csharp
   namespace Light.Results.AspNetCore;

   using Microsoft.AspNetCore.Http;

   /// <summary>
   /// Service for enriching results with additional metadata before conversion to LightProblemDetails.
   /// Register in DI to add traceId, correlationId, or other cross-cutting metadata.
   /// </summary>
   public interface ILightProblemDetailsEnricher
   {
       /// <summary>
       /// Enriches a result with additional metadata before conversion to LightProblemDetails.
       /// </summary>
       /// <typeparam name="T">The result value type.</typeparam>
       /// <param name="result">The original result.</param>
       /// <param name="httpContext">The current HTTP context.</param>
       /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
       Result<T> Enrich<T>(Result<T> result, HttpContext httpContext);

       /// <summary>
       /// Enriches a void result with additional metadata before conversion to LightProblemDetails.
       /// </summary>
       /// <param name="result">The original result.</param>
       /// <param name="httpContext">The current HTTP context.</param>
       /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
       Result Enrich(Result result, HttpContext httpContext);
   }
   ```

### Phase 7: MinimalApiResultExtensions

Implement the main extension methods (including `ToProblemDetails` for interop):

```csharp
namespace Light.Results.AspNetCore;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public static class MinimalApiResultExtensions
{
    public static IResult ToMinimalApiResult<T>(
        this Result<T> result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
    {
        if (result.IsValid)
        {
            return TypedResults.Ok(result.Value);
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat);
    }

    public static IResult ToMinimalApiResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsValid)
        {
            return onSuccess(result.Value);
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat);
    }

    public static IResult ToMinimalApiResult(
        this Result result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
    {
        if (result.IsValid)
        {
            return TypedResults.NoContent();
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat);
    }

    public static IResult ToMinimalApiResult(
        this Result result,
        Func<IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsValid)
        {
            return onSuccess();
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat);
    }

    private static Result<T> EnrichIfRegistered<T>(Result<T> result, HttpContext? httpContext)
    {
        if (httpContext is null) return result;

        var enricher = httpContext.RequestServices.GetService<ILightProblemDetailsEnricher>();
        return enricher?.Enrich(result, httpContext) ?? result;
    }

    private static Result EnrichIfRegistered(Result result, HttpContext? httpContext)
    {
        if (httpContext is null) return result;

        var enricher = httpContext.RequestServices.GetService<ILightProblemDetailsEnricher>();
        return enricher?.Enrich(result, httpContext) ?? result;
    }

    // --- ASP.NET Core ProblemDetails Interop ---

    /// <summary>
    /// Converts a failed Result to ASP.NET Core's ProblemDetails.
    /// Throws if the result is valid.
    /// </summary>
    public static ProblemDetails ToProblemDetails(
        this Result result,
        bool firstCategoryIsLeadingCategory = false)
    {
        if (result.IsValid)
        {
            throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");
        }

        return CreateProblemDetails(
            result.Errors,
            result.Metadata,
            firstCategoryIsLeadingCategory);
    }

    /// <summary>
    /// Converts a failed Result&lt;T&gt; to ASP.NET Core's ProblemDetails.
    /// Throws if the result is valid.
    /// </summary>
    public static ProblemDetails ToProblemDetails<T>(
        this Result<T> result,
        bool firstCategoryIsLeadingCategory = false)
    {
        if (result.IsValid)
        {
            throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");
        }

        return CreateProblemDetails(
            result.Errors,
            result.Metadata,
            firstCategoryIsLeadingCategory);
    }

    private static ProblemDetails CreateProblemDetails(
        Errors errors,
        MetadataObject? metadata,
        bool firstCategoryIsLeadingCategory)
    {
        var leadingCategory = errors.GetLeadingCategory(firstCategoryIsLeadingCategory);
        var statusCode = leadingCategory.ToHttpStatusCode();

        var pd = new ProblemDetails
        {
            Type = HttpStatusCodeInfo.GetTypeUri(statusCode),
            Title = HttpStatusCodeInfo.GetTitle(statusCode),
            Status = statusCode,
            Detail = errors.First.Message
        };

        // Add errors array
        var errorList = new List<object>(errors.Count);
        foreach (var error in errors)
        {
            errorList.Add(new
            {
                message = error.Message,
                code = error.Code,
                target = error.Target,
                category = error.Category == ErrorCategory.Unclassified ? null : error.Category.ToString(),
                metadata = error.Metadata.HasValue ? ConvertMetadataToDict(error.Metadata.Value) : null
            });
        }
        pd.Extensions["errors"] = errorList;

        // Add result-level metadata
        if (metadata.HasValue)
        {
            foreach (var kvp in metadata.Value)
            {
                pd.Extensions[kvp.Key] = ConvertMetadataValue(kvp.Value);
            }
        }

        return pd;
    }

    private static Dictionary<string, object?> ConvertMetadataToDict(MetadataObject metadata) { /* ... */ }
    private static object? ConvertMetadataValue(MetadataValue value) { /* ... */ }
}
```

### Phase 8: Testing

1. **GetLeadingCategoryTests**:
   - Single error returns its category
   - Multiple errors with same category returns that category
   - Multiple errors with different categories returns `Unclassified`
   - `firstCategoryIsLeadingCategory = true` returns first error's category regardless
   - Empty errors throws `InvalidOperationException`

2. **ToHttpStatusCodeTests**:
   - Each `ErrorCategory` value maps to correct HTTP status
   - `Unclassified` maps to 500

3. **LightProblemDetailsTests**:
   - Correct `type` URI for each status code
   - Correct `title` for each status code
   - `ExecuteAsync` writes correct JSON to response body
   - `ExecuteAsync` sets correct `Content-Type` header
   - Errors array is serialized correctly
   - Result metadata appears as extension properties
   - Error metadata appears in error objects

4. **MinimalApiResultExtensionsTests**:
   - Success `Result<T>` returns 200 with value
   - Success `Result` returns 204
   - Failed result returns `LightProblemDetails` with correct status
   - Custom success factory is invoked
   - Enricher is invoked when registered and httpContext provided
   - Enricher is skipped when httpContext is null

5. **ToProblemDetailsTests**:
   - Failed `Result` produces correct `ProblemDetails`
   - Failed `Result<T>` produces correct `ProblemDetails`
   - Success result throws `InvalidOperationException`
   - Errors array is populated correctly in `Extensions`
   - Result metadata appears in `Extensions`
   - Error metadata appears in error objects

6. **ErrorSerializationFormatTests**:
   - `AspNetCoreCompatible` format groups errors by target as `Dictionary<string, string[]>`
   - `AspNetCoreCompatible` format includes `errorDetails` array when errors have code/category/metadata
   - `AspNetCoreCompatible` format omits `errorDetails` when errors only have message/target
   - Errors without target are grouped under empty string key
   - `Rich` format serializes errors as array of objects with all properties
   - Both formats produce valid RFC 7807/9457 JSON

7. **LightProblemDetailsEnricherTests**:
   - Enricher adds metadata to failed results
   - Enricher is not called for successful results
   - Multiple enrichers can be chained (if supported)
   - Enricher receives correct HttpContext

## Usage Examples

### Basic Usage

```csharp
app.MapGet("/orders/{id}", (int id, IOrderService service) =>
{
    Result<Order> result = service.GetOrder(id);
    return result.ToMinimalApiResult();
});
```

### Custom Success Response

```csharp
app.MapPost("/orders", (CreateOrderRequest request, IOrderService service) =>
{
    Result<Order> result = service.CreateOrder(request);
    return result.ToMinimalApiResult(
        order => Results.Created($"/orders/{order.Id}", order));
});
```

### Void Operations

```csharp
app.MapDelete("/orders/{id}", (int id, IOrderService service) =>
{
    Result result = service.DeleteOrder(id);
    return result.ToMinimalApiResult();
});
```

### With First Category Strategy

```csharp
app.MapPut("/orders/{id}", (int id, UpdateOrderRequest request, IOrderService service) =>
{
    Result<Order> result = service.UpdateOrder(id, request);
    return result.ToMinimalApiResult(firstCategoryIsLeadingCategory: true);
});
```

### With Instance URI

```csharp
app.MapGet("/orders/{id}", (int id, HttpContext httpContext, IOrderService service) =>
{
    Result<Order> result = service.GetOrder(id);
    return result.ToMinimalApiResult(instance: httpContext.Request.Path);
});
```

### Using ToProblemDetails for Interop

```csharp
// When you need ASP.NET Core's ProblemDetails type (e.g., for IProblemDetailsService)
app.MapGet("/orders/{id}", (int id, IOrderService service) =>
{
    Result<Order> result = service.GetOrder(id);
    if (result.IsValid)
    {
        return Results.Ok(result.Value);
    }

    // Use ToProblemDetails when you need the ASP.NET Core type
    ProblemDetails pd = result.ToProblemDetails();
    // Customize or pass to other middleware...
    return Results.Problem(pd);
});
```

### Using Rich Error Format

```csharp
// For clients that need full error details inline
app.MapPost("/orders", (CreateOrderRequest request, IOrderService service) =>
{
    Result<Order> result = service.CreateOrder(request);
    return result.ToMinimalApiResult(
        errorFormat: ErrorSerializationFormat.Rich);
});
```

### With Enricher for Trace IDs

```csharp
// Program.cs - Register the enricher
builder.Services.AddSingleton<ILightProblemDetailsEnricher, TracingProblemDetailsEnricher>();

// TracingProblemDetailsEnricher.cs
public class TracingProblemDetailsEnricher : ILightProblemDetailsEnricher
{
    public Result<T> Enrich<T>(Result<T> result, HttpContext httpContext)
    {
        if (result.IsValid) return result;

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return result.WithMetadata("traceId", traceId);
    }

    public Result Enrich(Result result, HttpContext httpContext)
    {
        if (result.IsValid) return result;

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return result.WithMetadata("traceId", traceId);
    }
}

// Endpoint - pass HttpContext to enable enrichment
app.MapGet("/orders/{id}", (int id, HttpContext httpContext, IOrderService service) =>
{
    Result<Order> result = service.GetOrder(id);
    return result.ToMinimalApiResult(httpContext: httpContext);
});
```

## Future Considerations

1. **Deserialization**: A future plan will cover deserializing `HttpResponseMessage` back to `Result<T>` / `Result`.
2. **MVC Controllers**: A separate plan will cover `IActionResult` conversion for MVC API Controllers.
3. **gRPC**: A separate plan will cover gRPC status code mapping.
4. **OpenAPI/Swagger**: Consider adding attributes or conventions for OpenAPI documentation.
5. **Localization**: Consider supporting localized error messages via `IStringLocalizer`.

## Testing Strategy

1. **Unit tests**: Cover all extension methods and edge cases.
2. **Integration tests**: Use `WebApplicationFactory` to test actual HTTP responses.
3. **Serialization tests**: Verify JSON output matches RFC 7807/9457 format.
4. **Metadata round-trip tests**: Ensure metadata survives serialization (prep for deserialization plan).

## Performance Considerations

### Why This Approach is Faster

| Aspect | ASP.NET Core `ProblemDetails` | `LightProblemDetails` |
|--------|-------------------------------|------------------------|
| Extensions storage | `Dictionary<string, object?>` allocation | Direct `MetadataObject?` reference |
| Value types in metadata | Boxing required | No boxing (MetadataValue union) |
| JSON serialization | Reflection-based (default) | Source-generated |
| Error collection | Converted to `List<object>` | Direct `Errors` struct reference |
| IResult boxing | N/A (already a class) | No boxing (class, not struct) |

### Benchmarking

Add benchmarks comparing:
1. `LightProblemDetails.ExecuteAsync()` vs `Results.Problem(problemDetails)`
2. Memory allocations for both approaches
3. Serialization throughput

### JSON Property Ordering

RFC 7807 recommends the following property order: `type`, `title`, `status`, `detail`, `instance`. The `LightProblemDetails` class declares properties in this order, and `System.Text.Json` source generators preserve declaration order during serialization.

### Use `TypedResults` for Default Responses

When returning default ASP.NET Core Minimal API results (e.g., `Ok`, `NoContent`, `Created`), use `TypedResults` instead of `Results` to ensure the return types are concrete and public:

```csharp
// Use TypedResults (returns concrete types like Ok<T>, NoContent, etc.)
return TypedResults.Ok(result.Value);
return TypedResults.NoContent();

// NOT Results (returns IResult, harder to downcast in tests)
return Results.Ok(result.Value);
```

**Rationale**: `TypedResults` returns concrete, public types (e.g., `Ok<T>`, `NoContent`) that can be easily downcast in unit tests. `Results` returns `IResult`, which requires reflection or internal type knowledge to inspect.
