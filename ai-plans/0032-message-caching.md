# Validation Error Message Caching

## Rationale

Validation error messages are currently formatted every time an assertion fails: the template concatenates the display name with fixed text fragments and formatted parameters, allocating a new string on every `AddError` call. For a given `ValidationErrorDefinition` instance and display name, the resulting message is deterministic -- the definition fixes the template and parameter, and the display name is the only variable input. Since definitions are long-lived cached singletons, caching the produced `ValidationErrorMessage` by `(definition, displayName, cultureInfo)` would let the cache stabilize after the first validation run and serve all subsequent runs allocation-free on the error path.

## Acceptance Criteria

- [ ] Templates indicate whether their output is stable (depends only on display name and fixed parameters, not on the validated value) via a new `IsMessageStable` property on all four template interfaces.
- [ ] All built-in template implementations return `true` for `IsMessageStable`.
- [ ] `ValidationErrorDefinition` exposes a `virtual bool IsMessageStable` property that defaults to `false`. `TemplateValidationErrorDefinition` and `TemplateValidationErrorDefinition<TParameter>` override it to delegate to their template's `IsMessageStable`.
- [ ] A new `IValidationErrorMessageCache` interface provides pluggable message cache storage.
- [ ] A `ValidationErrorMessageCacheKey` readonly record struct encapsulates the cache key components: the message provider reference, display name, and culture.
- [ ] A default implementation backed by concurrent dictionaries is provided, keyed by `ValidationErrorMessageCacheKey`.
- [ ] `ValidationErrorTemplates` exposes the message cache via a new property. The default templates instance uses the default cache implementation. This couples the cache to the templates that determine message content, ensuring consistency and preventing stale messages when templates are customized.
- [ ] All `Check<T>.AddError` overloads that produce messages check `IsMessageStable` and delegate to the cache's `TryGet`/`Store` when caching is applicable, falling back to direct `ProvideMessage` otherwise.
- [ ] Built-in definition classes that override `ProvideMessage` directly (instead of delegating to a stored template) also set `IsMessageStable` appropriately.
- [ ] Automated tests cover cache hit/miss behavior, stable vs. unstable templates, culture-aware keying, disabled cache path, and custom `ErrorTemplates` using a separate cache.

## Technical Details

### Template stability indicator

Add `bool IsMessageStable { get; }` to the four existing template interfaces:

- `IValidationErrorMessageTemplate`
- `IValidationErrorMessageTemplate<TParameter>`
- `IComparableValidationErrorMessageTemplate`
- `IRangeValidationErrorMessageTemplate`

A stable message depends only on the display name and fixed parameters baked into the definition -- it does not use `context.Value`. All built-in template implementations (`DisplayNameValidationErrorMessageTemplate`, `DisplayNameWithComparableValidationErrorMessageTemplate`, `DisplayNameWithRangeValidationErrorMessageTemplate`, `DisplayNameWithParameterValidationErrorMessageTemplate`, `DisplayNameWithPrecisionScaleValidationErrorMessageTemplate`, `ConstantValidationErrorMessageTemplate`, `IgnoreParameterValidationErrorMessageTemplate`) return `true`.

On `ValidationErrorDefinition`, add `public virtual bool IsMessageStable => false`. This is the safe default for custom definitions whose `ProvideMessage` override may use the validated value. `TemplateValidationErrorDefinition` overrides with `public override bool IsMessageStable => Template.IsMessageStable`. Apply the same pattern to `TemplateValidationErrorDefinition<TParameter>`.

The built-in definition classes nested in `BuiltInValidationErrorDefinitions` override `ProvideMessage` directly and delegate to `context.ValidationContext.ErrorTemplates.XXX`. These should override `IsMessageStable` to return `true`, since all built-in error templates produce stable messages.

### Cache key

Introduce a `ValidationErrorMessageCacheKey` readonly record struct that encapsulates the three components that identify a cached message:

```
public readonly record struct ValidationErrorMessageCacheKey
{
    /// The object whose reference identity distinguishes one rule from another.
    /// For definition-based assertions, this is the ValidationErrorDefinition instance.
    /// For template-based assertions, this is the IValidationErrorMessageTemplate instance.
    /// Both are typically long-lived singletons with stable reference identity.
    public object Provider { get; }

    /// The human-readable display name of the validated field (e.g. "email", "address.zipCode").
    /// Together with the provider and culture, this fully determines the message text
    /// for stable message providers.
    public string DisplayName { get; }

    /// The culture used for formatting parameters in the message.
    /// CultureInfo instances are interned by the runtime, so reference equality
    /// works correctly as part of the key.
    public CultureInfo Culture { get; }
}
```

The struct uses reference equality for `Provider` and `Culture` (both are singleton-like references) and ordinal equality for `DisplayName`. This keeps the key allocation-free and the dictionary lookup cheap.

### Cache interface and default implementation

The cache interface uses a `TryGet`/`Store` pair. This keeps the cache agnostic of how messages are produced -- it never needs to know about parameter counts, template types, or `ProvideMessage` signatures. Callers own the production logic and the `IsMessageStable` decision; the cache is purely a key-value store.

```
public interface IValidationErrorMessageCache
{
    bool TryGet(ValidationErrorMessageCacheKey key, out ValidationErrorMessage message);
    void Store(ValidationErrorMessageCacheKey key, ValidationErrorMessage message);
}
```

The default implementation uses a `ConcurrentDictionary<ValidationErrorMessageCacheKey, ValidationErrorMessage>`. Since the key struct implements `IEquatable` via the record, the dictionary uses its `Equals`/`GetHashCode` directly without boxing.

Provide a static `Default` singleton on the default implementation class so that `ValidationErrorTemplates.Default` can reference it without allocating a new cache per templates instance.

### Integration into ValidationErrorTemplates

Add a nullable `IValidationErrorMessageCache?` property to `ValidationErrorTemplates` rather than `ValidationContextOptions`. The templates determine message content, and the cache stores those messages -- coupling them ensures consistency. When `null`, caching is disabled entirely. The default value should be the default cache implementation's singleton, so caching is enabled out of the box.

This means `ValidationErrorTemplates.Default` holds the default singleton cache (maximum reuse for the common case), custom `ValidationErrorTemplates` instances get their own cache unless explicitly shared, and `options with { ErrorTemplates = customTemplates }` automatically uses the custom templates' cache. The design makes it impossible to serve stale messages from a mismatched templates/cache combination.

### Integration into Check\<T\>

All `AddError` overloads that produce messages follow the same pattern after normalization:

1. Determine the message provider (`definition` or `template`) and check its `IsMessageStable` property.
2. If stable and a cache is configured, construct a `ValidationErrorMessageCacheKey` from the provider, the display name, and the culture from the context options. Call `cache.TryGet`. On a hit, use the cached message.
3. On a miss, call `ProvideMessage` as before to produce the message, then call `cache.Store` with the same key.
4. If not stable or no cache is configured, call `ProvideMessage` directly (current behavior).

This applies to:

- `AddError(ValidationErrorDefinition, ...)` -- provider is the definition. Any baked-in parameters are captured by the definition's reference identity, so different parameter values produce different cache entries automatically.
- `AddError(IValidationErrorMessageTemplate, ...)` -- provider is the template.
- `AddError<TParameter>(IValidationErrorMessageTemplate<TParameter>, TParameter, ...)` -- this overload bypasses the cache and always calls `ProvideMessage` directly. The parameter is not part of the cache key, and a single template instance can produce different messages for different parameter values. Callers who want caching for parameterized messages should use the definition-based path, where the parameter is captured by the definition's reference identity.
- `AddError(ValidationErrorMessage, ...)` and `AddError(string, ...)` -- these overloads take a pre-built message from the caller. There is no template or definition to cache through, so caching does not apply.

### Thread safety

Concurrent `TryGet` calls are naturally safe with `ConcurrentDictionary`. Two threads can both miss on `TryGet` and both call `Store` with equivalent values -- the second write is a benign overwrite. No additional locking is needed beyond what `ConcurrentDictionary` provides.
