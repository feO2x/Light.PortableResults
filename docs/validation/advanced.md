# Advanced validation

This page shows how validators compose for nested objects and collections, how `null` values are handled automatically, how a validator maps a DTO to a domain object, how to validate asynchronously, how to validate without a validator class, how validators share state, and how to write custom assertions and add errors by hand.

**Package:** `Light.PortableResults.Validation` (namespaces `Light.PortableResults.Validation`, `Light.PortableResults.Validation.Definitions`, and `Light.PortableResults.Validation.Messaging`)

<a id="composing-validators"></a>

## Composing validators

Use child validators when your DTO contains nested objects or collections that each have their own validation rules. Validators compose by sharing a single `ValidationContext` — errors from all levels accumulate in one pass, and their targets are prefixed with the path of the nested value.

```csharp
public sealed record PurchaseOrderDto
{
    public required Guid OrderId { get; set; }
    public required DateTime PlacedAt { get; set; }
    public required string CustomerEmail { get; set; }
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

Register all validators as singletons when they have no scoped dependencies:

```csharp
services
    .AddValidationForPortableResults()
    .AddSingleton<ShippingAddressValidator>()
    .AddSingleton<OrderItemValidator>()
    .AddSingleton<PurchaseOrderValidator>();
```

The composition methods return a `ValidatedValue<T>` that holds the (possibly normalized) child value when it is valid:

- `ValidateChild(validator)` and `ValidateChildAsync(validator, cancellationToken)` validate one nested value with a `Validator<T>`, `Validator<TSource, TValidated>`, or their async counterparts.
- `ValidateItems(validator)` validates every item with a validator; `ValidateItems(static (Check<TItem> item) => ...)` validates every item with a delegate, which suits primitive items. Item targets get an index, for example `items[2].quantity`.
- For mutable indexed collections such as `List<T>` and arrays, normalized items are written back in place. `ImmutableArray<T>` and read-only lists are validated without replacing items.
- Transforming item validators (`Validator<TSource, TValidated>`) materialize a new `TValidated[]`, `List<TValidated>`, or `ImmutableArray<TValidated>` for arrays, lists, and immutable arrays. For other collection shapes, write a dedicated child validator for the collection itself.
- `ValidateItemsAsync` provides the same variants for async validators and async delegates.

<a id="automatic-null-checking"></a>

## Automatic null checking

The validation framework handles `null` values automatically so you rarely need an explicit `IsNotNull()` guard. The active `AutomaticNullErrorProvider` (configurable via `ValidationContextOptions`) decides what error to produce; the default emits a `NotNull` validation error.

- **Validators** — when the source value passed to `Validate` / `ValidateAsync` is `null`, the validator adds the automatic null error and returns a failed `Result` without calling `PerformValidation`. The `isAutomaticNullCheckingEnabled` constructor parameter (default `true`) controls this per validator class.
- **Child validation (`ValidateChild`, `ValidateChildAsync`)** — when the nested value is `null`, the child validator's null check fires for that target and the parent continues collecting other errors.
- **Collection item validation (`ValidateItems`, `ValidateItemsAsync`)** — when a `null` collection is passed, the null error is added for the collection target and item validators are skipped. Individual item validators also handle `null` items automatically.

Guard explicitly with `IsNotNull()` only when `NoOpAutomaticNullErrorProvider` is configured (automatic null errors disabled), or when you need to short-circuit further checks. With a provider that declines to create an error, item validation of a `null` collection throws an `InvalidOperationException`, so guard nullable collections explicitly in that configuration:

```csharp
// Default configuration — no explicit guard needed
context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);
context.Check(dto.Tags).ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));

// Explicit guard — short-circuits any further checks on this value
context.Check(dto.Tags).IsNotNull().ValidateItems(static (Check<string> tag) => tag.HasLengthIn(2, 30));
```

<a id="mapping-to-domain-objects"></a>

## Mapping to domain objects

Use `Validator<TSource, TValidated>` when validation must produce a *different* output type — typically a mutable DTO in, an immutable domain object out. This pattern implements an Anti-Corruption Layer.

```csharp
// Mutable DTO received from the API
public sealed record CreateMovieDto
{
    public required string Title { get; set; }
    public required int ReleaseYear { get; set; }
    public required string DirectorName { get; set; }
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

<a id="async-validators"></a>

## Async validators

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

    var movieRating = new MovieRating(dto.Id, dto.MovieId, dto.UserName, dto.Comment, dto.Rating);
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

Async validators have no `CheckForErrors`; use `ValidateAsync` and inspect `IsValid`. The OpenAPI source generator supports synchronous validators only.

<a id="using-validationcontext-directly"></a>

## Using `ValidationContext` directly

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

`IValidationContextFactory` is registered automatically by `AddValidationForPortableResults()`. `HasErrors`, `Errors`, `TryGetErrors(out Errors)`, and `ToFailureResult()` read the collected errors. A context can also be passed to `Validate`, `CheckForErrors`, or `ValidateAsync` so that a validator adds its errors to it — useful when a service adds further errors after validation:

```csharp
var validationContext = _validator.ValidationContextFactory.CreateValidationContext();
if (_validator.CheckForErrors(dto, validationContext, out var errorResult))
{
    return Result<MovieRating>.Fail(errorResult.Errors);
}

var movie = await _session.GetMovieAsync(dto.MovieId, cancellationToken);
if (movie is null)
{
    validationContext
       .Check(dto.MovieId)
       .AddError(
            "There is no movie with the specified ID",
            code: "MovieNotFound",
            MetadataObject.Create(("movieId", dto.MovieId.ToString()))
        );
    return Result<MovieRating>.Fail(validationContext.Errors);
}
```

<a id="sharing-state-between-validators"></a>

## Sharing state between validators

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

`ValidationContextKey<T>` is typed so you cannot accidentally retrieve the wrong type. Use `TryGetItem` instead of `GetRequiredItem` when the item may not have been set, and `RemoveItem` to delete it. Items belong to the validation run: child contexts share them with their parent.

<a id="adding-errors-by-hand"></a>

## Adding errors by hand

When no assertion fits, add errors directly. `Check<T>` offers three `AddError` overloads, which return the check for chaining:

| Overload | Behavior |
| --- | --- |
| `AddError(Error error, respectShortCircuit = true)` | Adds the error. A `null` target is replaced with the target of the check. |
| `AddError(string message, code, metadata, target, category = Validation, respectShortCircuit = true)` | Creates the error from its parts. The target defaults to the target of the check. |
| `AddError(ValidationErrorDefinition definition, code, metadata, target, category, respectShortCircuit = true)` | Creates the error from a reusable definition and optionally overrides its code, metadata, target, and category. |

With `respectShortCircuit: true`, nothing is added to a short-circuited check. An explicit `target` is a `ValidationTarget`: relative targets are composed with the current scope, absolute targets are used unchanged, and caller-expression targets are normalized first.

`ValidationContext` itself has `AddError(Error)`, which adds the error unchanged, `AddError(string message, code, target, metadata)`, and `AddError(ValidationErrorMessage message, code, target, metadata)`; the last two create `Validation` errors whose target defaults to the scope of the context.

<a id="custom-assertions"></a>

## Custom assertions

**Ad-hoc predicate** — use `Must` for a one-off check:

```csharp
context.Check(dto.ReleaseYear).Must(
    year => year >= 1888 && year <= DateTime.UtcNow.Year
);
```

`Must` produces a `Predicate` error with the message `{displayName} is invalid`. Pass `ErrorOverrides` (or just a message string), a message template with a code, or a `ValidationErrorDefinition` to make the error more specific. The overload whose predicate receives a `ReadOnlyValidationContext` enables cross-field checks.

**Imperative logic** — use `Custom` when one rule may add several errors or needs the full context:

```csharp
context.Check(dto.Period).Custom(static (scopedContext, period) =>
{
    if (period.End < period.Start)
    {
        scopedContext.AddError("end must not be before start", code: "PeriodInverted");
    }

    if (period.End - period.Start > TimeSpan.FromDays(366))
    {
        scopedContext.AddError("the period must not exceed one year", code: "PeriodTooLong");
    }
});
```

The context passed to `Custom` is scoped to the checked value, so errors without an explicit target get the target of the check (`period`).

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

A definition carries a default `Code`, `Metadata`, `Target`, and `Category` (constructor parameters). `IsMessageStable` and `TryGetStableMessageProvider` tell the message cache whether a message depends only on the display name and fixed parameters; return `false` (the default) when the message depends on the checked value. `TemplateValidationErrorDefinition` and `TemplateValidationErrorDefinition<TParameter>` combine a [message template](configuration.md#message-templates-and-localization) with a code, and `ValidationErrorDefinition<TParameter>` adds a typed `Parameter`.

To make a custom assertion visible to the OpenAPI source generator, annotate it with `[ValidationRule]`; see [Documenting custom checks](../openapi.md#documenting-custom-checks).
