# Message Cache Key Optimization

## Rationale

The message caching introduced in 0032-message-caching.md eliminates repeated message formatting on the error path. However, every `AddError` overload still calls `NormalizeTargetIfNecessary()` before the cache lookup. For nested validation scopes, this triggers `ValidationTargets.Compose(prefix, normalizedTarget)` which allocates a new string on every invocation. Since `Compose` is deterministic for a given `(TargetDescriptor, TargetPrefix)` pair, the composed result can be cached alongside the message by restructuring the cache key to use raw, stable inputs.

This optimization also cleans up two design issues that complicate the current caching model: `Check<T>.DisplayName` mutates after normalization (making cache keys unstable across a check's lifetime), and `TryGetStableMessageProvider` takes a full `ValidationErrorMessageContext<T>` even though it only reads `ErrorTemplates`. Both are addressed as prerequisites to make the cache key restructuring clean.

## Acceptance Criteria

- [ ] `Check<T>.DisplayName` becomes nullable. `null` means "derive from target at message-formatting time"; a non-null value means "caller explicitly chose this display name". `ValidationContext.Check()` no longer defaults `displayName` to `TargetDescriptor.Input`.
- [ ] `NormalizeTargetIfNecessary()` stops updating `DisplayName`. The property is invariant across the check's lifetime.
- [ ] `ValidationErrorDefinition.TryGetStableMessageProvider` changes its signature from `TryGetStableMessageProvider<T>(in ValidationErrorMessageContext<T>, out object)` to `TryGetStableMessageProvider(ReadOnlyValidationContext, out object)`. All overrides are updated accordingly.
- [ ] `ValidationErrorMessageCacheKey` is restructured to key by `(Provider, ValidationTarget, TargetPrefix, DisplayName, CultureInfo)` instead of `(Provider, DisplayName, CultureInfo)`.
- [ ] The cache value is expanded from `ValidationErrorMessage` to a readonly struct that also carries the resolved absolute target string.
- [ ] `IValidationErrorMessageCache` and its default implementation are updated for the expanded value type.
- [ ] The definition-based and parameterless-template-based `AddError` overloads in `Check<T>` skip `NormalizeTargetIfNecessary()` entirely on cache hits. On cache miss, they normalize, produce the message, and store both the message and resolved target.
- [ ] The parameterless-template-based `AddError` overload builds the `Error` directly and adds it to the context on a cache hit instead of delegating to the `AddError(ValidationErrorMessage, ...)` overload.
- [ ] The `Check<T>` returned from cache-hit `AddError` calls carries the cached resolved target in `_resolvedAbsoluteTarget` so that subsequent fluent assertions and child-context creation work correctly.
- [ ] Automated tests cover cache-hit target resolution, custom display name cache behavior, nested validation scope cache behavior, the interaction with definition-target and override-target scenarios, and the nullable `DisplayName` semantics.
- [ ] A micro benchmark compares the allocation profile of the optimized path against the previous implementation for a representative nested validation scenario.

## Technical Details

### Nullable DisplayName on Check\<T\>

`Check<T>.DisplayName` becomes `string?`. The `ValidationContext.Check()` factory no longer defaults `displayName` to `validatedTarget.Input` — it passes the caller's value through, which is `null` by default. `WithDisplayName(string)` keeps requiring a non-null argument.

`NormalizeTargetIfNecessary()` stops its conditional display-name rewrite entirely. It only sets `_resolvedAbsoluteTarget`:

```
public Check<T> NormalizeTargetIfNecessary()
{
    if (_resolvedAbsoluteTarget is not null) return this;

    var resolvedTarget = Context.ResolveTarget(TargetDescriptor);
    return new Check<T>(Context, TargetDescriptor, DisplayName, Value, resolvedTarget, IsShortCircuited);
}
```

The effective display name (the "subject" of the error message) is derived on demand when constructing a `ValidationErrorMessageContext<T>`:

```
string effectiveDisplayName = DisplayName ?? _resolvedAbsoluteTarget ?? TargetDescriptor.Input;
```

- `DisplayName` is non-null only when the caller explicitly set it.
- `_resolvedAbsoluteTarget` is the composed, normalized target, available after normalization  (e.g. `"address.firstName"`).
- `TargetDescriptor.Input` is the raw compiler-captured expression (e.g. `"dto.FirstName"`), used as a last resort before normalization has run.

On the cache-hit fast path, `_resolvedAbsoluteTarget` may not be set yet. In that case, the effective display name is derived from the cached resolved target instead:

```
string effectiveDisplayName = DisplayName ?? cachedEntry.ResolvedTarget;
```

### TryGetStableMessageProvider signature change

Change the virtual method from:

```
public virtual bool TryGetStableMessageProvider<T>(
    in ValidationErrorMessageContext<T> context, out object provider)
```

to:

```
public virtual bool TryGetStableMessageProvider(
    ReadOnlyValidationContext context, out object provider)
```

Every existing override only reads `context.ValidationContext.ErrorTemplates` to retrieve a template reference and check `IsMessageStable`. The generic type parameter `T`, the display name, the target, and the value are never accessed. The new signature removes the dependency on post-normalization state, making the method callable before normalization. `ReadOnlyValidationContext` is a `readonly struct` that wraps the existing `ValidationState` and target prefix — constructing it via `Context.AsReadOnly()` is zero-allocation. It gives custom definition authors access to `ErrorTemplates`, `Options`, and the shared items store, which prevents a future breaking change if they need to pick a provider dynamically based on context items.

Built-in overrides change from `context.ValidationContext.ErrorTemplates.Foo` to `context.ErrorTemplates.Foo`. Update the static helper `TryGetStableProvider` in `BuiltInValidationErrorDefinitions` to take `IValidationErrorMessageTemplate` directly (the caller extracts it from `context.ErrorTemplates`), keeping each override a one-liner.

### Cache key restructuring

Replace the three-field key `(Provider, DisplayName, CultureInfo)` with `(Provider, ValidationTarget, TargetPrefix, DisplayName, CultureInfo)`. The key remains a `readonly record struct` with hand-written `Equals`/`GetHashCode`:

- `Provider` and `CultureInfo` keep reference equality via `ReferenceEquals` / `RuntimeHelpers.GetHashCode`, same as today.
- `ValidationTarget` is a `readonly record struct`. Use its auto-generated `Equals` and `GetHashCode`.
- `TargetPrefix` uses ordinal string equality. For root contexts this is the interned empty string; for child contexts it is the parent scope's resolved target.
- `DisplayName` is nullable. `null` in the common case means the null check is a single branch in `Equals` and contributes zero to `GetHashCode`. For non-null custom display names, ordinal equality distinguishes entries that produce different messages from the same target.

Because `DisplayName` is now invariant and all other key fields are stable across a check's lifetime, the cache key is the same regardless of whether the check has been normalized. This eliminates the dual-key problem where the first assertion (pre-normalization) and the second assertion (post-normalization) would produce different keys.

### Expanded cache value

Introduce a `readonly record struct` as the cache value:

- `ValidationErrorMessage Message` — the cached message.
- `string ResolvedTarget` — the result of `Compose(TargetPrefix, Normalize(TargetDescriptor))`. Used to stamp on the `Error` and to populate `_resolvedAbsoluteTarget` on the returned check.

`IValidationErrorMessageCache` changes its value type from `ValidationErrorMessage` to this new struct. This is a breaking change to the cache interface, acceptable since the library is not published yet.

### AddError fast path

The definition-based `AddError` overload follows this flow:

1. Call `definition.TryGetStableMessageProvider(Context.AsReadOnly(), out var provider)`. This requires no normalization.
2. If unstable or no cache is configured, fall back to the existing normalize-then-produce path.
3. Construct the key from `(provider, TargetDescriptor, Context.TargetPrefix, DisplayName, culture)` — all invariant fields, no normalization needed.
4. **Cache hit**: read `entry.Message` and `entry.ResolvedTarget`. Resolve the definition target: if no override target and no definition target, use `entry.ResolvedTarget`; otherwise, call `Context.ResolveTarget(...)` for the override or definition target as before. Derive the effective display name from `DisplayName ?? entry.ResolvedTarget`. Build the `Error` directly and add it to the context. Return a `Check<T>` with `_resolvedAbsoluteTarget` set to `entry.ResolvedTarget`.
5. **Cache miss**: call `NormalizeTargetIfNecessary()`, produce the message via `ProvideMessage`, store `(message, resolvedTarget)` under the key.

The parameterless-template-based `AddError` overload uses the template itself as the provider (templates don't have `TryGetStableMessageProvider` — the template reference is the provider identity when `template.IsMessageStable` is true):

1. Check `template.IsMessageStable`. If unstable or no cache is configured, fall back to normalizing first.
2. Construct the key from `(template, TargetDescriptor, Context.TargetPrefix, DisplayName, culture)`.
3. **Cache hit**: same as the definition path — read `entry.Message` and `entry.ResolvedTarget`, resolve target, build the `Error` directly and add it to the context. This avoids delegating to `AddError(ValidationErrorMessage, ...)`, which would redundantly call `NormalizeTargetIfNecessary()`.
4. **Cache miss**: normalize, produce the message, store `(message, resolvedTarget)`.

The existing private `GetMessage(ValidationErrorDefinition, ...)` and `GetMessage(IValidationErrorMessageTemplate, ...)` helper methods are removed. Their cache-lookup logic is folded into the respective `AddError` overloads, since the fast path needs to skip normalization and build the `Error` directly — responsibilities that span beyond message retrieval.

The `AddError(Error, ...)`, `AddError(ValidationErrorMessage, ...)`, `AddError(string, ...)`, and parameterized-template `AddError<TParameter>(...)` overloads are not affected. They continue to call `NormalizeTargetIfNecessary()` as before.

### Override targets and definition targets

When `AddError(definition, ...)` includes a non-null override target or the definition carries its own target, `ResolveDefinitionTarget` resolves those via `Context.ResolveTarget(...)`. This resolution is independent of the check's own target and is not covered by this optimization. The cached resolved target only serves as the fallback when both are null — the common case for all built-in assertions.

### Thread safety

Same model as today. `ConcurrentDictionary` provides safe concurrent reads. Two threads that both miss and both store produce a benign overwrite. No additional locking is needed.
