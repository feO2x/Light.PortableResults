# Child Validation Refactoring Completion

## Rationale

The recent child-validation refactoring moved composition from `BaseValidator<TSource>` into `Check<T>`, which makes validator implementations easier to read and aligns better with the fluent validation style of the package. However, the refactoring is not complete yet: target-composition ownership is still ambiguous in the new helpers, collection validation does not cover primitive or transforming item scenarios well, and the async surface is missing entirely. This plan completes the refactoring so that child validation, collection validation, and async validation follow one consistent design that preserves the package's flat target semantics and low-allocation goals.

## Acceptance Criteria

- [ ] `Check<T>` exposes child-context helpers that preserve target ownership and target-normalization state for nested validation scenarios.
- [ ] The fluent child-validation APIs use those helpers instead of composing child scopes manually, so already-normalized or already-composed targets are not normalized or prefixed a second time.
- [ ] `ValidateItems` no longer performs automatic null-error creation; callers must guard nullable collections explicitly, and the method only tolerates `null` when the check has already been short-circuited.
- [ ] Collection validation supports delegate-based item validation for primitive and ad-hoc validation scenarios, including a normalization-capable overload that can return `ValidatedValue<TItem>`.
- [ ] Collection validation supports transforming item validators for `T[]`, `List<T>`, and `ImmutableArray<T>` in addition to same-type item validators.
- [ ] Async parity is added for child and collection validation, including async validator overloads and async delegate-based item validation.
- [ ] The completed API preserves deterministic error ordering, target composition, and existing short-circuit behavior across synchronous and asynchronous flows.
- [ ] Unsupported collection shapes are intentionally left to user-defined child validators, and this scope boundary is documented in the public API guidance.
- [ ] Automated tests cover target-normalization ownership, short-circuited null collections, delegate-based item validation, transforming item validation, async child validation, async collection validation, and representative root/member/index target paths.
- [ ] XML documentation and representative usage examples in README.md are updated for the completed child-validation and collection-validation API surface.

## Technical Details

Keep `ValidationState` unchanged. The target-composition problem is path-local metadata, not shared run-level state, so the ownership boundary should stay on `Check<T>` and `ValidationContext`.

Add explicit child-scope helpers to `Check<T>`:

- `CreateChildContext()` should create a child context for the current check target while preserving `IsTargetNormalized`.
- `CreateChildContextForMember(string memberName, bool isNormalized = false)` should compose an additional member segment under the current check target.
- `CreateChildContextForIndex(int index)` should append an index segment under the current check target.

These helpers should become the single fluent entry point for nested scope creation from a check. `CheckExtensions` should stop calling `check.Context.ForMember(check.Target)` directly and should instead use the new helpers so that raw caller-expression targets are normalized once and already-normalized targets are treated as absolute within the current validation scope.

`ValidateChild` and all future child-validation overloads should be refactored around `check.CreateChildContext()`. The same ownership rule must be applied to `ValidateItems`, including indexed child scopes created for collection items.

Change collection null semantics deliberately. `ValidateItems` should assume that the collection has already passed through an explicit null guard such as `IsNotNull`. If the incoming check is short-circuited, the method should return the appropriate "no validated value" result without touching the collection. If the incoming check is not short-circuited and the collection value is `null`, the method should throw an `InvalidOperationException` with a clear message that callers must guard nullable collections before validating items. This keeps null semantics aligned with the rest of the fluent assertion model and removes duplicate automatic-null handling logic from the collection helper.

Extend `CheckExtensions` with delegate-based item-validation overloads for collections of primitives or ad-hoc rules. The plan should support both of the following shapes explicitly:

- `Action<Check<TItem>>` for pure-validation scenarios that only add errors to the shared context
- `Func<Check<TItem>, ValidatedValue<TItem>>` for normalization-capable scenarios

The delegate-based overloads should reuse the same child-scope creation logic as validator-based overloads so that target paths and short-circuit semantics remain identical.

Collection support should be split by capability so that mutability requirements stay explicit:

| Capability | Intended behavior | Supported collection shapes |
| --- | --- | --- |
| Validation-only item checks | Iterate items, add errors, do not replace items | `IReadOnlyList<T>` |
| Same-type normalization | Validate and replace items in place | Mutable indexed collections only (generic with `IList<T>` constraint) |
| Transforming item validation | Validate items and materialize a new collection of a different item type | `T[]`, `List<T>`, and `ImmutableArray<T>` only |

`ImmutableArray<T>` therefore participates in validation-only and transforming flows, but not in-place same-type normalization. The implementation should keep these capability boundaries visible in the overload set instead of relying on runtime failures.

Complete collection support for transforming validators as well, but keep the built-in scope intentionally narrow. Same-type item validation can continue to normalize in place for mutable `IList<TItem>` collections. Transforming item validation should only be provided for linear indexed collection types with straightforward, deterministic materialization semantics:

- `T[]`
- `List<T>`
- `ImmutableArray<T>`

These overloads should materialize the corresponding target collection type directly with predictable allocation behavior rather than trying to preserve arbitrary source collection shapes. Because extensibility is less important than performance in this library, do not introduce generic collection-factory abstractions for transforming validation. Collection types outside this built-in set, including custom collections and non-linear collections such as dictionaries or sets, should be handled by user-defined child validators.

Add async parity in `CheckExtensions` rather than reintroducing special-case helper methods on `BaseValidator<TSource>`. Introduce `ValidateChildAsync` overloads for `AsyncValidator<T>` and `AsyncValidator<TSource, TValidated>`, and introduce `ValidateItemsAsync` overloads for async validators and async delegate-based item validation. The async delegate overloads should be explicit and follow the synchronous design closely:

- `Func<Check<TItem>, CancellationToken, ValueTask>` for pure-validation async item checks
- `Func<Check<TItem>, CancellationToken, ValueTask<ValidatedValue<TItem>>>` for normalization-capable async item checks

Use `ValueTask` and `CancellationToken` throughout the async surface to stay aligned with the existing validator model and to avoid unnecessary allocations. Item validation should remain sequential by contract so that error ordering and collection normalization stay deterministic.

The automated tests should be written through the public API and should cover at least the following scenarios:

- validating a child from a raw check target
- validating a child from an already-normalized check target
- validating items under member and indexed paths without duplicate prefixes
- calling `ValidateItems` on a short-circuited null collection
- calling `ValidateItems` on a non-short-circuited null collection
- delegate-based validation of primitive items such as `string` and `int`
- delegate-based normalization of collection items
- transforming collection items through validator-based overloads for arrays, `List<T>`, and `ImmutableArray<T>`
- `ValidateChildAsync` with same-type and transforming async validators
- `ValidateItemsAsync` with validator-based and delegate-based item validation

XML documentation and representative examples should be updated together with the API changes so that callers can discover the intended patterns for nullable collections, primitive item validation, transforming item validation, and async item validation without having to infer them from tests alone.

If existing benchmarks exercise the affected validation hot paths, update them only as needed to keep the benchmark project compiling and representative. A dedicated benchmark expansion is not required unless implementation choices indicate a measurable regression risk.
