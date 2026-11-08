# Result basics

`Result<T>` holds either a success value of `T` or one or more errors, and `Result` does the same for operations that return no value. This page shows how to create and examine results, how `Error` and `Errors` are structured, how to design errors that survive a trip across a process boundary, and why you must never pass on the default instance of `Result<T>`.

**Package:** `Light.PortableResults` (namespace `Light.PortableResults`)

## Creating results

Create a successful result with `Ok` and a failed one with one of the `Fail` overloads:

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

`Fail` accepts a single `Error`, an `Errors` instance, or a `ReadOnlyMemory<Error>` (so an `Error[]` works as well). Every factory takes optional result-level metadata as its last parameter — see [Metadata](metadata.md). `Ok` throws an `ArgumentNullException` when the value is `null`, and `Fail` throws an `ArgumentException` when it receives no error at all.

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

`Result` wraps a `Result<Unit>`; `Unit` is the value-less placeholder type of this library.

## Examining results

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

The most important members of `Result<T>`:

| Member | Description |
| --- | --- |
| `IsValid` | `true` when the result holds a value and no errors. |
| `Value` | The success value. Throws an `InvalidOperationException` on a failed result. |
| `TryGetValue(out T value)` | Returns the value without throwing. |
| `Errors` | The errors; empty on success. |
| `FirstError` | The first error. Throws an `InvalidOperationException` on a successful result. |
| `Metadata` | The optional result-level `MetadataObject`. |
| `ReplaceMetadata(metadata)` | Returns a copy with other metadata; pass `null` to clear it. |

Results are immutable structs with value equality: `Equals` compares errors, value, and metadata. The overload `Equals(other, compareMetadata, valueComparer)` lets you ignore metadata or supply an `IEqualityComparer<T>` for the value, and `GetHashCode(includeMetadata, equalityComparer)` mirrors it.

The [functional operators](functional-operators.md) build pipelines on top of these members.

## `Error`

`Error` is an immutable struct:

| Property | Description |
| --- | --- |
| `Message` | Required human-readable explanation. Must not be `null`, empty, or whitespace. |
| `Code` | Optional, stable, machine-readable identifier. Assign every kind of error its own code so that clients can rely on it. |
| `Target` | Optional input field, header, or value that caused the error. |
| `Category` | The `ErrorCategory`, which decides the transport mapping such as the HTTP status code. Defaults to `Unclassified`. |
| `Metadata` | Optional `MetadataObject` with additional context. |
| `Exception` | Optional exception for local diagnostics. |

`Error.Exception` can be set for local diagnostics, but it is never serialized and is never exposed to calling processes. `IsDefaultInstance` tells you whether an `Error` was created with `default`; such errors cannot be added to `Errors`. `Equals(other, compareMetadata)` and `GetHashCode(includeMetadata)` let you compare errors without their metadata.

`ErrorCategory` has one member per HTTP client and server error status code, such as `Validation` (400), `NotFound` (404), `Conflict` (409), `UnprocessableContent` (422), and `ServiceUnavailable` (503). The numeric value of each member is its status code; `Unclassified` maps to 500. See the [complete map](configuration-reference.md#errorcategory-to-http-status-code) in the configuration reference.

<a id="designing-useful-errors"></a>

## Designing useful errors

Consistent error shapes make APIs and message consumers easier to evolve. As a rule of thumb:

- `Message`: human-readable explanation
- `Code`: stable machine-readable identifier (great for frontend and API contracts)
- `Target`: which input field, header, or value failed
- `Category`: determines the transport mapping (for example, the HTTP status code)
- `Metadata`: additional context (for example, boundary values or comparative amounts)

Errors produced by the [validation framework](validation/getting-started.md) follow these rules automatically: their codes come from `ValidationErrorCodes`, their targets are derived from the checked expression, and their metadata contains the rule's parameters.

HTTP 400 and 422 responses require a `Target` on every error (use `""` for the root object); see [HTTP](http.md#validation-responses-require-targets).

## Working with `Errors`

`Errors` is a read-only list of `Error` values with a small-buffer optimization: a single error is stored inline without allocating an array.

```csharp
Errors errors = result.Errors;

int count = errors.Count;
Error first = errors.First;   // throws when empty
Error second = errors[1];
bool none = errors.IsEmpty;   // true on success and for the default instance

foreach (var error in errors) // uses a struct enumerator and does not allocate
{
    Console.WriteLine($"{error.Target}: {error.Message}");
}
```

Create an instance with `new Errors(error)` or `new Errors(errorArray)`. Both constructors reject default `Error` instances, and the array constructor rejects empty arrays. Two `Errors` instances are equal when they contain equal errors in the same order; `Equals(other, compareMetadata: false)` ignores metadata.

`GetLeadingCategory(firstCategoryIsLeadingCategory)` determines the category that represents all errors, which the HTTP integration uses to select the status code. With `true`, the category of the first error wins. With `false` (the default of this method), the shared category is returned when all errors agree, and `Unclassified` otherwise. See [status code selection](http.md#status-code-selection).

## The default instance of `Result<T>`

Do not use `default(Result<T>)`. When `T` is a reference type or a nullable value type, the default instance is neither a success nor a failure: `IsValid` is `false`, but `Errors` is empty. Such an instance carries no information, and every write boundary of this library — `ToMinimalApiResult`, `ToMvcActionResult`, `ToCloudEvent`, the JSON converters, and the ASP.NET Core helpers — rejects it with an `ArgumentException`.

`default(Result<T>)` with a non-nullable value type `T`, as well as `default(Result)`, are successes. Prefer `Ok` and `Fail` anyway, because they state the intent at the call site.

If you write your own boundary, call `MustNotBeDefaultInstance()` to apply the same guard:

```csharp
public void Publish(Result<Order> result)
{
    result.MustNotBeDefaultInstance(); // throws ArgumentException for default(Result<Order>)
    // ...
}
```

The `failure` result of `Validator<T>.CheckForErrors` is only meaningful when the method returns `true`; on success, it is the default `Result` and must not be passed on as a result of its own. See [Validation](validation/getting-started.md#checkforerrors).
