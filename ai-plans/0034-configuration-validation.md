# Configuration Validation via IValidateOptions\<T>

## Rationale

Microsoft.Extensions.Options provides `IValidateOptions<T>` as the standard hook for validating `IOptions<T>` instances at resolution time. Libraries like DataAnnotations already integrate through this mechanism. With the Light.PortableResults.Validation library now available, callers should be able to reuse their existing `Validator<T>` implementations to validate configuration/options objects through the same Microsoft mechanism, without writing manual adapter code each time.

This plan adds a generic bridge adapter and a fluent registration extension so that any synchronous `Validator<TOptions>` can participate in the `IValidateOptions<TOptions>` pipeline with a single line of DI configuration.

## Acceptance Criteria

- [ ] A public `PortableResultsValidateOptions<TOptions>` class implements `IValidateOptions<TOptions>` by delegating to a `Validator<TOptions>` instance. On success, it returns `ValidateOptionsResult.Success`; on failure it maps each `Error.Message` to the `ValidateOptionsResult.Fail(IEnumerable<string>)` overload.
- [ ] The adapter supports named options filtering. When constructed with an options name, it returns `ValidateOptionsResult.Skip` for non-matching names, consistent with Microsoft's `DataAnnotationValidateOptions<T>` behavior.
- [ ] The options name received by `IValidateOptions<TOptions>.Validate` is forwarded into the `ValidationContext` via a well-known `ValidationContextKey<string?>` so that validators can read it if needed but are not forced to.
- [ ] A public extension method on `OptionsBuilder<TOptions>` allows fluent registration: `builder.ValidateWithPortableResults<TOptions, TValidator>()`. The method captures `OptionsBuilder<TOptions>.Name` and passes it to the adapter for named options filtering. It registers the validator and the adapter into the DI container and returns the `OptionsBuilder<TOptions>` for further chaining.
- [ ] Only synchronous `Validator<T>` (and `Validator<TSource, TValidated>` where `TSource` equals `TOptions`) is supported. `AsyncValidator<T>` is intentionally excluded because `IValidateOptions<T>` is synchronous.
- [ ] The adapter uses `ValidationTarget.Absolute("")` as the root target so that error targets reflect clean property paths (e.g. `ConnectionString`) instead of caller-expression artifacts.
- [ ] Automated tests cover the adapter, the extension method, named-options filtering and forwarding, `ValidateOptionsResult.Skip` behavior, and failure mapping.

## Technical Details

### Bridge Adapter

Add a `PortableResultsValidateOptions<TOptions>` class inside the existing `Light.PortableResults.Validation` project and namespace. It implements `Microsoft.Extensions.Options.IValidateOptions<TOptions>` with a `where TOptions : class` constraint (required by the interface). The class receives the `Validator<TOptions>` and an optional `string? name` through its constructor.

The `Validate(string? name, TOptions options)` method should:

1. If the adapter was constructed with a non-null name and the incoming `name` does not match, return `ValidateOptionsResult.Skip`. This follows the same pattern as Microsoft's `DataAnnotationValidateOptions<T>`.
2. Create a fresh `ValidationContext` from the validator's `ValidationContextFactory`.
3. Store the `name` parameter on the context via `context.SetItem(OptionsNameKey, name)` using a well-known `ValidationContextKey<string?>`. Expose this key as a public static field so that validator implementations can read the options name when they need to distinguish named options.
4. Call `_validator.CheckForErrors(options, context, out var failure, ValidationTarget.Absolute(""))`.
5. If validation succeeds, return `ValidateOptionsResult.Success`.
6. If validation fails, iterate `failure.Errors`, collect each `error.Message` into a list, and return `ValidateOptionsResult.Fail(messages)`.

### Registration Extension

Add a static class (e.g. `OptionsBuilderExtensions`) with a `ValidateWithPortableResults` extension method on `OptionsBuilder<TOptions>`. The method should:

1. Call `services.AddValidationForPortableResults()` to ensure the validation infrastructure is registered (this call is idempotent via the existing `TryAdd` semantics — verify this or make `AddValidationForPortableResults` idempotent if it isn't yet).
2. Register `TValidator` as a singleton via `TryAddSingleton<TValidator>()` so that callers who pre-register the validator (e.g. with a different lifetime or custom instance) are not overridden.
3. Capture `OptionsBuilder<TOptions>.Name` and pass it to the `PortableResultsValidateOptions<TOptions>` constructor so the adapter filters on the correct named options instance.
4. Register `IValidateOptions<TOptions>` by resolving `TValidator` from the container and wrapping it in `PortableResultsValidateOptions<TOptions>`.
5. Return the `OptionsBuilder<TOptions>` for method chaining.

Callers will then write:

```csharp
services
    .AddOptions<MyDatabaseOptions>()
    .BindConfiguration("Database")
    .ValidateWithPortableResults<MyDatabaseOptions, MyDatabaseOptionsValidator>();
```

### Idempotency of AddValidationForPortableResults

Check whether `AddValidationForPortableResults` currently uses `TryAddSingleton` or plain `AddSingleton`. If it uses `AddSingleton`, switch to `TryAddSingleton` so that calling it multiple times (once per options type) does not create duplicate registrations. The extension method must be safe to call repeatedly.

### Scope

- Only `Validator<TOptions>` is supported. `AsyncValidator<T>` support is out of scope because `IValidateOptions<T>.Validate` is synchronous.
- `Validator<TSource, TValidated>` (transforming validators) are not relevant here because options validation does not transform the options object — it only checks for errors. We can add support for this later if needed but it would only need `CheckForErrors`, discarding the transformed output.
- No benchmarks are required for this feature — the code is a thin adapter layer without performance-sensitive paths.
