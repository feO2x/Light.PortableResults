# Validation Checkpoints

## Rationale

`ValidatedValue<T>` currently composes well only at the outermost validator boundary because the final public
`Result<T>` is decided by `BaseValidator.FinalizeValidation(...)` against the shared `ValidationContext`. Inside
`PerformValidation(...)`, however, validator implementations still have to decide whether they can safely return a
successful `ValidatedValue<T>`. The current fallback is `context.HasErrors`, but that is a run-global signal and not a
local boundary for the current validator invocation. As soon as sibling checks, earlier collection items, or parent
validators have already added errors, same-run child validation can no longer distinguish "this validator caused
errors" from "some other validator in the run already caused errors".

Introduce explicit validation checkpoints to model that local success boundary directly. A checkpoint has fields for the
shared `ValidationState` together with the starting error count of one validator invocation and can later determine
whether new errors were appended during that invocation. This keeps the validation error model append-only, remains
allocation-free on the success path, and gives validator authors a deterministic way to return `ValidatedValue<T>`
without correlating checks to concrete `Error` instances or materializing intermediate `Result<T>` objects.

The design should deliberately center on validator-level checkpoints, not per-check error flags. `Check<T>` should not
gain a `CausedErrors` property in this plan. A per-check flag would be narrower, easier to misuse on a `readonly
struct`, and would still not represent errors added through sibling checks, imperative `context.AddError(...)`,
`Custom(...)`, child validators, or collection validation helpers. The checkpoint is the correct abstraction because it
matches the real semantic question: did this validator invocation add new errors since it started?

## Acceptance Criteria

- [ ] A public `ValidationCheckpoint` value type is introduced in `Light.PortableResults.Validation` so it can appear in the protected extensibility points of the public validator base classes.
- [ ] `ValidationCheckpoint` captures the shared `ValidationState` and the starting error count for one validator invocation, and exposes at least `HasNewErrors`, `NewErrorCount`, and `TryGetNewErrors(out Errors errors)` without allocating on the success path.
- [ ] `ValidationCheckpoint` exposes a success-finalization helper such as `ToValidatedValue<T>(T value)` that returns `ValidatedValue<T>.NoValue` when new errors were added and `ValidatedValue.Success(value)` otherwise.
- [ ] `Validator<T>`, `Validator<TSource, TValidated>`, `AsyncValidator<T>`, and `AsyncValidator<TSource, TValidated>` create a checkpoint automatically before calling `PerformValidation(...)` / `PerformValidationAsync(...)` and pass it into those protected methods.
- [ ] The protected validator extensibility points are updated so validator implementations can finalize from the supplied checkpoint instead of consulting `ValidationContext.HasErrors` directly.
- [ ] Child-validation and collection-validation helpers are updated to decide their `ValidatedValue<T>` results from a checkpoint local to the helper invocation instead of the shared `ValidationContext.HasErrors`, so valid later children/items are not suppressed by unrelated earlier errors in the same run.
- [ ] `Check<T>` does not gain a `CausedErrors` or `HasErrors` property in this plan; validator and helper composition relies exclusively on checkpoint semantics.
- [ ] Public exposure of newly added errors uses `Errors`-based APIs rather than a public `ReadOnlySpan<Error>` surface. Any lower-level span-based slicing that becomes useful for implementation or benchmarking remains internal.
- [ ] Automated tests cover top-level validators, nested child validators, collection item validation, transforming validators, async validators, `TryGetNewErrors(...)`, and the invariant that a validator without newly added errors can still return a successful `ValidatedValue<T>` even when the shared run already contains unrelated earlier failures.
- [ ] README examples, validator tests, and the existing validation benchmarks are updated to use checkpoint finalization. No new benchmark suite is introduced in this plan.

## Technical Details

Introduce a new `public readonly struct ValidationCheckpoint`. It should store only the shared `ValidationState` reference
plus the `ErrorCount` observed when the checkpoint was created. This mirrors the current append-only validation model:
errors are never removed or rewritten, so "did this scope add errors?" can be answered by comparing the current error
count with the starting count. The struct should not cache or copy errors.

The public surface of `ValidationCheckpoint` should stay narrow and aligned with the rest of Light.PortableResults:

- `bool HasNewErrors`
- `int NewErrorCount`
- `bool TryGetNewErrors(out Errors errors)`
- `ValidatedValue<T> ToValidatedValue<T>(T value)`

Do not expose a public `ReadOnlySpan<Error>` API. `Errors` is already the library's established
error carrier and is easier to use safely across ordinary method boundaries.

Please note that validators that depend on transforming child validators or transforming collection helpers cannot safely construct the final validated output by reading `ValidatedValue<T>.Value` from intermediate
instances before it is known that the current checkpoint has no new errors. In other words, this
pattern is not sufficient:

```csharp
var address = context.Check(value.Address).ValidateChild(_addressValidator);
return checkpoint.ToValidatedValue(new CreateOrderCommand(address.Value));
```

because `address.Value` may throw before `checkpoint.ToValidatedValue(...)` gets a chance to short-circuit.

Instead, this simple branching pattern should be used:

```csharp
var address = context.Check(value.Address).ValidateChild(_addressValidator);

return checkpoint.HasNewErrors ?
    ValidatedValue<CreateOrderCommand>.NoValue :
    ValidatedValue.Success(new CreateOrderCommand(address.Value));
```

This works because only the selected branch of the conditional operator is evaluated. If `checkpoint.HasNewErrors` is
`true`, `address.Value` is not accessed. The same pattern should be used when a transforming validator depends on
multiple validated child results or transforming collection helpers: first branch on `checkpoint.HasNewErrors`, then
read the validated child values only in the success branch.

No new combinator or LINQ-like API is required for this plan. The implementation should instead document and test this
explicit branching pattern. The normal invariant still applies: when a child validator or collection helper returns
`ValidatedValue<T>.NoValue` without adding new errors to its local checkpoint, that represents a broken implementation
and should still fail when `.Value` is accessed.

`ValidationState` already tracks `ErrorCount` and stores the first error inline plus later errors in an owned array.
Extend it only as much as necessary so a checkpoint can slice the errors added after a given starting count:

- zero new errors => empty / `false`
- one new error => return a single-error `Errors`
- multiple new errors => wrap the used segment of the existing backing storage without copying when possible

Preserve the current append-only semantics. This plan should not introduce any API for removing or mutating existing
errors.

Update the validator pipeline so checkpoints are created automatically by the framework, not manually by validator
authors. The rule should be explicit: every framework-owned operation that returns a `ValidatedValue<T>` from shared
validation state must establish its own local checkpoint boundary. `ValidateChildValue(...)` /
`ValidateChildValueAsync(...)` should therefore create a checkpoint immediately before dispatching into
`PerformValidation(...)` / `PerformValidationAsync(...)`, and helper methods such as child-validation and
collection-validation helpers must do the same when they synthesize `ValidatedValue<T>` results directly. The
protected abstract signatures then become:

```csharp
protected abstract ValidatedValue<T> PerformValidation(
    ValidationContext context,
    ValidationCheckpoint checkpoint,
    T value
);
```

and the corresponding async variants receive the same checkpoint plus `CancellationToken`.

This is an intentional breaking change to the protected extensibility surface. It is acceptable because the library is
not published in a stable version yet, and it makes the intended finalization pattern explicit in every validator
implementation.

Validator implementations should now finalize from the checkpoint instead of `context.HasErrors`:

```csharp
protected override ValidatedValue<CreatePersonCommand> PerformValidation(
    ValidationContext context,
    ValidationCheckpoint checkpoint,
    RegistrationDto value
)
{
    var firstName = context.Check(value.FirstName).IsNotNullOrWhiteSpace();
    var email = context.Check(value.Email).IsEmail();

    return checkpoint.ToValidatedValue(
        new CreatePersonCommand(firstName.Value, email.Value)
    );
}
```

This pattern should be used throughout the package itself, README examples, tests, and benchmarks. The benchmark
validators that currently return `ValidatedValue.Success(...)` unconditionally are one of the motivating examples for
this plan and should be rewritten accordingly.

Update `CheckExtensions` so child-validation and collection-validation helper methods also use local checkpoints
internally. Today helpers such as `ValidateChild(...)` and `ValidateItems(...)` can derive their `ValidatedValue<T>`
from the shared `ValidationContext.HasErrors`, which suppresses successful later children/items after unrelated earlier
failures in the same run. Each helper invocation should instead create its own checkpoint before invoking the child
validator or iterating collection items, then finalize from that checkpoint. This preserves the intended semantics:

- a child validator returns `NoValue` only when that child invocation added errors
- a collection helper returns `NoValue` only when that helper invocation added errors
- already-existing errors elsewhere in the run do not erase valid local transformation results

Keep short-circuit semantics unchanged. If a check is already short-circuited, child and collection helpers should
still return `ValidatedValue<T>.NoValue` immediately without creating additional errors or invoking user code.

Do not add any new state to `Check<T>` for this feature. `Check<T>` remains a lightweight value that carries the
current value, target information, display name, and short-circuit state. The checkpoint is the single source of truth
for validator-local failure detection.

Automated tests should stay public-API-driven and specifically lock down the differences between global and local
error scopes. Important scenarios include:

- a parent validator adds an early error, then a later valid child validator still returns a successful
  `ValidatedValue<TChild>` to its caller
- an invalid child validator returns `NoValue` and contributes the expected flat error targets
- collection helpers still normalize or transform valid later items even when earlier items failed
- top-level validation still produces the same final `Result<T>` failures and error ordering as today
- async validators and async collection helpers preserve the same local checkpoint semantics
- `TryGetNewErrors(out Errors errors)` exposes only the errors added since the checkpoint was created

The benchmark update does not need a brand-new benchmark suite if the existing validation benchmarks remain
representative. It is sufficient to update the affected benchmark validators to the checkpoint-based pattern and keep
the existing benchmark coverage compiling and representative so regressions in runtime or allocations remain visible.
