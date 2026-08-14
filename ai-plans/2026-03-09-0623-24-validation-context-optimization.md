# Validation Context Optimization

## Rationale

The validation package is already competitive on the simple endpoint benchmark, but the complex endpoint benchmark shows that nested validation still pays for avoidable overhead in scoped-context creation, target-prefix plumbing, and failure-state materialization. This plan redesigns validation around a single per-run mutable state object and lightweight scoped contexts so that nested validation no longer allocates a new context object per child object or collection item. At the same time, the error accumulation model should be aligned more tightly with `Errors`: keep the single-error fast path allocation-free, allocate a small backing array only when a second error arrives, and materialize `Errors` by slicing the used portion of that array rather than copying into a second exact-sized array.

## Acceptance Criteria

- [x] `ValidationContext` is redesigned as a lightweight scoped value type for validation runs rather than a dedicated heap object per scope.
- [x] A single internal `ValidationState` reference type owns the mutable per-run validation state, including options, templates, error accumulation, and any shared root-level services needed by all scoped contexts.
- [x] `IValidationContextFactory` and `ValidationContextFactory` are simplified to create root validation contexts only; child validation scopes are created directly from `ValidationContext` without round-tripping through the factory.
- [x] `ValidationContext` exposes explicit scope/prefix creation APIs for nested validation scenarios, including member and index-based composition, so nested validators can avoid unnecessary caller-expression normalization work.
- [x] The previous sink-based error accumulator is removed and merged into `ValidationState`.
- [x] `ValidationState` keeps the first error inline and allocates an owned `Error[]` with an initial capacity of 10 when the second error arrives, allowing the common 2-to-10-error case to avoid further storage allocations.
- [x] When more than 10 errors occur, `ValidationState` grows the owned array in a predictable way while preserving the existing error order.
- [x] `ValidationContext.ToErrors()` / `TryGetErrors(...)` materialize `Errors` directly from the owned array via `ReadOnlyMemory<Error>` slicing when multiple errors are present, avoiding a second copy for the common failure path.
- [x] The redesign preserves the current flat target semantics for nested validation, including root targets, member paths such as `address.zipCode`, and indexed paths such as `addresses[0].zipCode`.
- [x] The redesign does not regress the success path: successful validation should allocate only the root per-run state, while nested scopes should not allocate additional objects.
- [x] Automated tests cover root and nested scope creation, error accumulation across multiple scopes, first-error and multi-error materialization, direct wrapping of the owned multi-error buffer into `Errors`, and overflow behavior when more than 10 errors are produced.
- [x] Benchmarks are added or updated to measure the validation hot paths affected by this redesign, including at least nested validation with child scopes and failure accumulation for 1, 2, 10, and more than 10 errors.

## Technical Details

Redesign `ValidationContext` in `Light.PortableResults.Validation` as a `readonly struct` that represents a scoped view over a single validation run. The struct should hold only the shared `ValidationState` reference and the current target prefix for that scope. This keeps copies cheap enough for normal method passing while eliminating the current heap allocation that occurs whenever a nested child scope is created. Do not use `ref struct` here because validators must continue to compose with asynchronous flows and ordinary method calls.

Introduce an internal sealed `ValidationState` type that replaces both the current mutable `ValidationContext` internals and `ValidationErrorSink`. `ValidationState` should own the immutable dependencies for the validation run (`ValidationContextOptions`, `ValidationErrorTemplates`, and any future run-level services) plus the mutable error storage. This state object is created exactly once per root validation run by `ValidationContextFactory`, and every scoped `ValidationContext` struct created from that root context points back to the same state instance.

Simplify `IValidationContextFactory` to root context creation only. Child-scope creation should move onto `ValidationContext` itself because nested scopes no longer need a separate factory-owned object graph; they only need a different target prefix while sharing the same `ValidationState`. Replace the current `CreateChildValidationContext(...)` API with scope-oriented APIs on `ValidationContext`, for example:

- `For<TChild>(TChild child, [CallerArgumentExpression("child")] string target = "")`
- `ForMember(string memberName, bool isNormalized = false)`
- `ForIndex(int index)`
- an optional low-level `WithPrefix(string prefix, bool isNormalized = false)` escape hatch

These APIs should allow nested validators and collection validators to avoid unnecessary target normalization when they already know the normalized path segment. For example, a collection validator should be able to normalize the member name once, then append indexes with `ForIndex(i)` without rebuilding the full prefix each time through the factory.

Merge the current sink logic directly into `ValidationState`. The storage model should be optimized to match `Errors`:

- zero errors: no allocated error storage
- one error: store the first `Error` inline in a dedicated field
- second error: allocate an owned `Error[]` with a capacity of 10, copy the first error into slot 0, store the second
  error into slot 1, and continue using that array
- third through tenth error: append into the same array without further allocation
- eleventh and later errors: grow the array with a predictable strategy such as doubling or `Math.Max(length * 2, count + 1)`

The exact capacity of 10 is deliberate because endpoint validation failures rarely exceed that number in practice, and the goal is to optimize the common failure shape rather than unbounded validation graphs. This design keeps the single-error path allocation-free while ensuring that the common multi-error path usually performs a single allocation total. The state object should track the current error count separately from the array length so the array can be wrapped without trimming.

Materialization must take advantage of the existing `Errors(ReadOnlyMemory<Error>)` constructor in the core library. For multiple errors, `ValidationState` should create `Errors` by slicing the owned array to the used count via `array.AsMemory(0, count)` rather than allocating a new exact-sized array. This is a key part of the optimization: with the current `Errors` representation, there is no need to introduce an additional carrier type such as `ErrorsData`, and there is no need to copy the accumulated errors a second time as long as the owned array is not shared elsewhere.

Do not use `ArrayPool<Error>` as the primary storage model for this redesign. Pooled arrays are awkward with the current `Errors` ownership semantics because the array cannot be returned to the pool while `Errors` still references it through `ReadOnlyMemory<Error>`. A normal owned array is the correct fit here, especially because the chosen design allocates that array only on the second error and then usually reuses it for the rest of the validation run.

Keep the flat target semantics exactly as they work today. The scoped `ValidationContext` struct should compose the current prefix with normalized or pre-normalized child targets, but it must not duplicate already-composed targets. Root null-validation failures must continue to use the empty target string `""`, while child validators must continue to produce paths such as `address.zipCode` and `addresses[0].zipCode`. The redesign should therefore preserve the current target-composition rules in `ValidationTargets` while moving more of the nested-scope work away from factory calls and object allocation.

Update `Check<T>`, `Validator<T>`, `Validator<TSource, TValidated>`, `AsyncValidator<T>`, and `AsyncValidator<TSource, TValidated>` to operate on the new scoped `ValidationContext` struct. This should be mostly a mechanical change, but pay close attention to helper methods that currently assume `ValidationContext` is a reference type. The public API surface should remain ergonomic for callers, while the internal implementation should avoid unnecessary copies and child-state construction.

Test this redesign via the public API of the Validation project using sociable unit tests. Avoid exposing internals to the test project.

Finally, add focused microbenchmarks alongside the existing endpoint benchmarks so the effect of this redesign is
directly measurable. In addition to rerunning the simple and complex endpoint comparisons, add internal benchmarks for:

- creating nested child scopes repeatedly
- accumulating exactly 1 error
- accumulating exactly 2 errors
- accumulating exactly 10 errors
- accumulating more than 10 errors
- materializing `Errors` from each of those cases

These benchmarks should confirm that nested validation no longer allocates per child scope and that the common failure path usually performs at most one owned-array allocation total.
