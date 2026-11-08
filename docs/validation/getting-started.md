# Getting started with validation

Instead of constructing `Error` instances by hand, write a typed validator: each failed check adds an error with a message, a stable code, a target derived from the checked expression, the `Validation` category, and metadata with the rule's parameters. Validators collect all errors in one pass and return a `Result<T>`, so they plug directly into the HTTP and CloudEvents integrations. This page covers writing and calling validators, `CheckForErrors`, registration, targets, and short-circuiting.

**Package:** `Light.PortableResults.Validation` (namespace `Light.PortableResults.Validation`)

## Writing a validator

Derive from `Validator<T>` and override `PerformValidation`:

```csharp
using Light.PortableResults.Validation;

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

        // Check() normalizes strings by default (null → "", non-null → trimmed).
        // Assign the return value back to persist the normalized string.
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();

        context.Check(dto.Rating).IsInRange(1, 5);

        return checkpoint.ToValidatedValue(dto);
    }
}
```

`context.Check(value)` creates a `Check<T>` — a small struct that carries the value, its target, and the context. Every assertion returns the check, so assertions chain (`context.Check(dto.Code).IsNotNullOrWhiteSpace().HasMaxLength(20)`), and a check converts implicitly to its value. The value is normalized when the check is created; see [Value normalization](configuration.md#value-normalization). [Built-in checks](built-in-checks.md) lists every assertion, and [Advanced validation](advanced.md) shows how to write your own.

A validator is stateless: all per-run state lives in the `ValidationContext`, which the validator creates through the injected `IValidationContextFactory` (or receives from a parent validator).

<a id="what-is-validatedvaluet"></a>

> **What is `ValidatedValue<T>`?**
>
> `ValidatedValue<T>` is the handshake type between a validator and its callers within a single validation pipeline run. Rather than surfacing errors immediately as `Result<T>`, it carries the signal back: either a successfully validated value via `ValidatedValue<T>.Success(value)`, or `ValidatedValue<T>.NoValue` when errors were added. `checkpoint.ToValidatedValue(dto)` chooses the right outcome based on whether any errors were added since the checkpoint was created. You never need to construct `ValidatedValue<T>` directly unless you are writing a transforming validator — see [Mapping to domain objects](advanced.md#mapping-to-domain-objects).

`ValidationCheckpoint` remembers the error count at the start of `PerformValidation`. `HasNewErrors`, `NewErrorCount`, and `TryGetNewErrors(out Errors)` tell you what was added since then.

## Calling a validator

`Validate(value)` returns a `Result<T>`: the (normalized) value on success, all collected errors on failure.

```csharp
Result<MovieRatingDto> result = _validator.Validate(dto);
```

<a id="checkforerrors"></a>

### `CheckForErrors`

When the validated value is only an intermediate step, `CheckForErrors` avoids the `if (!result.IsValid)` ceremony:

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

        var movieRating = new MovieRating(dto.Id, dto.MovieId, dto.UserName, dto.Comment, dto.Rating);
        await _repository.AddAsync(movieRating, cancellationToken);
        return Result<MovieRating>.Ok(movieRating);
    }
}
```

`CheckForErrors` returns `true` when validation failed and hands back the errors as a non-generic `Result`. That `Result` is only meaningful when the method returns `true`; on success it is the default instance, which carries no information about the validated value and must not be passed on to a caller as a result of its own.

### Overloads

`Validate` and `CheckForErrors` have overloads that accept:

- an existing `ValidationContext`, so that several validators or additional checks share one context and one error list (see [Using `ValidationContext` directly](advanced.md#using-validationcontext-directly));
- a `ValidationTarget` and a display name for the root value, which control the target and message of the automatic null error.

Validating `null` does not call `PerformValidation`; the validator adds a `NotNull` error instead. See [Automatic null checking](advanced.md#automatic-null-checking).

## Registration

`AddValidationForPortableResults()` registers `IValidationContextFactory` and `ValidationContextOptions` as singletons, unless they are registered already. Register validators as singletons when they have no scoped dependencies — they are stateless by design:

```csharp
services
    .AddValidationForPortableResults()
    .AddSingleton<MovieRatingValidator>();
```

Validators that depend on scoped services, such as a repository used by an [async validator](advanced.md#async-validators), must be scoped themselves. Without a DI container, create the factory with `DefaultValidationContextFactory.Create()`:

```csharp
var validator = new MovieRatingValidator(DefaultValidationContextFactory.Create());
```

To customize culture, templates, normalization, or targets, see [Validation configuration](configuration.md).

<a id="targets-and-display-names"></a>

## Targets and display names

`Check` captures the expression you pass with `[CallerArgumentExpression]` and turns it into the error target:

- The first segment up to and including the first `.` is removed, so `dto.ShippingAddress.Street` becomes `shippingAddress.street`. An expression without a dot, such as `query`, is used as it is.
- Each member segment is converted to camel case, a leading `@` of a verbatim identifier is removed, and indexers are kept: `dto.Items[0].Sku` becomes `items[0].sku`.
- Child validators and item validation prefix their targets with the parent target, for example `shippingAddress.postalCode` or `items[2].quantity`.

The casing is configurable through `ValidationTargetCasing` (`CamelCase`, `PascalCase`, or `Preserve`); see [Targets](configuration.md#targets).

The display name is the first word of every built-in message. It defaults to the resolved target (`comment must be between 10 and 1000 characters long`). Pass `displayName` or call `WithDisplayName` to change it without changing the target:

```csharp
context.Check(dto.Email, displayName: "E-mail address").IsEmail();
context.Check(dto.Rating).WithDisplayName("Your rating").IsInRange(1, 5);
```

Items of a collection use the display name of the collection followed by the index, for example `tags[1]`.

When an expression does not describe the target you want, pass a `ValidationTarget` explicitly:

- `ValidationTarget.Relative("address.city")` is composed with the current scope.
- `ValidationTarget.Absolute("customer.address.city")` is used unchanged.
- `ValidationTarget.CallerExpression("dto.City")` is normalized like a captured expression.

```csharp
context.Check(dto.City, ValidationTarget.Relative("address.city")).IsNotNullOrWhiteSpace();
```

`ValidationContext.ForMember`, `ForIndex`, `ForRelative`, `ForAbsolute`, and `For(child)` create scoped child contexts whose checks are prefixed accordingly.

<a id="short-circuiting"></a>

## Short-circuiting

All assertions of a check run by default, so one value can produce several errors. Pass `shortCircuitOnError: true` to stop the chain after the first failure — later assertions on the same check are skipped:

```csharp
context.Check(dto.Code)
   .IsNotNullOrWhiteSpace(shortCircuitOnError: true)
   .Matches("^[A-Z]{3}-[0-9]{4}$");
```

`IsNotNull()` and `IsNull()` short-circuit by default, because the following assertions usually cannot handle the other case. `check.ShortCircuit()` short-circuits unconditionally, and `check.IsShortCircuited` tells custom assertions whether to skip their work. Short-circuiting affects only the current chain; checks on other values continue to run.
