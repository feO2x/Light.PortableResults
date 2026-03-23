# Validation Target Redesign

## Rationale

The current validation target model overloads plain strings with too many meanings. A check target can start as a raw caller expression, become a normalized relative path, or effectively behave like an absolute path inside a nested validation scope. Because this meaning is inferred from string comparisons against `ValidationContext.TargetPrefix`, child-context creation and target normalization need defensive prefix-detection logic that is harder to understand than the underlying rules. This plan redesigns target handling around explicit target semantics so that target ownership is encoded in data instead of inferred from path text. The goal is to make validation targets easier to reason about while preserving the library's flat-path semantics and avoiding unnecessary allocations when no errors are produced.

## Acceptance Criteria

- [ ] A dedicated validation-target type is introduced that explicitly distinguishes caller-expression targets, relative targets, and absolute targets instead of encoding these semantics implicitly in plain strings.
- [ ] The redesign keeps lazy target resolution so that normalized absolute targets are not allocated unless a validation flow actually needs them.
- [ ] `Check<T>` no longer decides target ownership by comparing target strings with `ValidationContext.TargetPrefix`; target ownership comes from the explicit `ValidationTarget` semantics and the current prefix-detection logic is removed or reduced to trivial assertions.
- [ ] `ValidationContext` exposes explicit APIs for creating checks from caller-expression, relative, and absolute targets, and for creating child scopes from resolved absolute targets without ambiguous prefixing rules.
- [ ] Caller-expression normalization is separated from validation-path normalization so that direct relative paths are not treated like caller expressions.
- [ ] The public check-creation API remains ergonomic for the common case while also exposing explicit advanced entry points for relative and absolute targets.
- [ ] Child validation, collection validation, error creation, and message-context creation all use the new target model consistently across synchronous and asynchronous flows.
- [ ] The redesign preserves deterministic target composition and does not regress the current flat-path behavior for representative root, member, and indexed targets.
- [ ] Automated tests cover the new target semantics, including caller-expression, relative, and absolute targets, plus representative child-validation and collection-validation scenarios.
- [ ] XML documentation and README examples are updated to describe the explicit target semantics and the recommended APIs for advanced manual target control.

## Technical Details

Introduce a dedicated target descriptor instead of passing raw strings through the validation pipeline without context.
The target descriptor should encode two concerns explicitly:

- how the input text must be interpreted
- whether the input text has already been normalized

Do not use a flags enum for the interpretation itself. Caller-expression, relative, and absolute targets are mutually exclusive semantics, so a closed enum or equivalent representation should enforce that only one of them can be active at a time. Normalization state can be tracked separately. The target descriptor should remain a lightweight value type so that it fits the library's low-allocation design while still behaving like a value object in tests and other comparison scenarios. The plan should use the following structure unless implementation details uncover a strong reason to deviate:

```csharp
public readonly record struct ValidationTarget
{
    public ValidationTarget(
        string input,
        ValidationTargetSemantics semantics,
        bool isNormalized = false
    )
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        if (!IsDefined(semantics))
        {
            throw new ArgumentOutOfRangeException(nameof(semantics));
        }

        Semantics = semantics;
        IsNormalized = isNormalized;
    }

    public string Input { get; }
    public ValidationTargetSemantics Semantics { get; }
    public bool IsNormalized { get; }

    private static bool IsDefined(ValidationTargetSemantics semantics) =>
        semantics == ValidationTargetSemantics.CallerExpression ||
        semantics == ValidationTargetSemantics.Relative ||
        semantics == ValidationTargetSemantics.Absolute;
}

public enum ValidationTargetSemantics
{
    CallerExpression,
    Relative,
    Absolute
}
```

Because `Light.PortableResults.Validation` targets .NET Standard 2.0, the undefined-enum-value check should not depend on newer generic `Enum.IsDefined` APIs. Use a dedicated helper or an equivalent explicit comparison that validates the supported `ValidationTargetSemantics` values without relying on unavailable framework overloads.

The target descriptor represents caller intent only. It should not also store the resolved absolute target path. The resolved absolute target remains runtime state on `Check<T>` so that it can stay lazy.

`Check<T>` should move away from the current `Target` plus `IsTargetNormalized` model. Instead, it should store the incoming target descriptor together with an optional resolved absolute target. The resolved absolute target should be filled lazily when the check needs an actual path, for example when creating an error, creating a message context, or creating a child scope. This keeps the common success path allocation-light while still making absolute-target ownership explicit once it matters.

`ValidationContext` should gain explicit entry points for each target semantic instead of forcing all callers through a single caller-expression-oriented path. The common `Check(value, ...)` overload should stay optimized for `CallerArgumentExpression`, and its `target` parameter should continue to mean "caller-expression-style target" even when the caller sets that parameter explicitly. Advanced callers should opt into relative or absolute semantics through the `ValidationTarget` overload rather than relying on string-shape inference. Prefer keeping the current ergonomic overload and adding a target-descriptor overload instead of adding more optional semantic arguments to the existing signature. The intended API shape is:

```csharp
public Check<T> Check<T>(
    T value,
    IStringValueNormalizer? stringValueNormalizer = null,
    [CallerArgumentExpression("value")] string? target = null,
    string? displayName = null
);

public Check<T> Check<T>(
    T value,
    ValidationTarget target,
    IStringValueNormalizer? stringValueNormalizer = null,
    string? displayName = null
);
```

The `ValidationTarget` overload should be treated as the canonical advanced API. This design should cover at least these conceptual operations:

- create a check from a caller expression via the existing ergonomic overload
- create a check from a relative target via `new ValidationTarget(..., ValidationTargetSemantics.Relative, ...)`
- create a check from an absolute target via `new ValidationTarget(..., ValidationTargetSemantics.Absolute, ...)`
- create a child scope from a resolved absolute target

The `ValidationTarget.Input` contract should be explicit:

- `CallerExpression`: `Input` is a caller-expression-style string that must be interpreted by the caller-expression normalization rules unless `IsNormalized` is already `true`
- `Relative`: `Input` is a path relative to the current validation scope and must never be treated as a caller expression
- `Absolute`: `Input` is a path that is already rooted for the full validation run and must never be prefixed with the current validation scope

When `IsNormalized` is `true`, the input should already be normalized according to the rules for the specified semantics and must not be normalized again.

The normalizer responsibilities should be split accordingly. A caller-expression target normalizer can continue to trim parameter names and normalize member/index syntax. Direct relative and absolute validation paths should not be routed through the same logic when that logic would reinterpret the first path segment as an object-root identifier. If this requires separate normalizer abstractions or a single abstraction with explicit target semantics, prefer the option that keeps the rules easiest to understand at call sites and in the implementation.

Once the target descriptor is in place, child-scope creation should become straightforward. `Check<T>` should resolve an absolute target once and then hand that absolute target to `ValidationContext` through an explicit absolute-scope API. This should remove the need for prefix/equality/separator heuristics whose only job is to guess whether a target string already includes the current scope prefix.

The redesign should be applied consistently across the validation surface:

- `Check<T>.NormalizeTargetIfNecessary`
- child-context helpers on `Check<T>`
- error creation on `Check<T>` and `ValidationContext`
- message-context creation
- child validation and collection validation in `CheckExtensions`
- async child and collection validation paths

Because the library is still pre-stable, breaking changes are acceptable. However, the common validation experience should remain concise. Most callers should still be able to write validators without thinking about target semantics explicitly, while advanced users should have direct, efficient APIs for manual relative and absolute targets when they need them.

The automated tests should verify both semantics and ergonomics. Cover at least the following:

- caller-expression targets resolving to expected flat paths
- explicitly passing the `target` parameter to the ergonomic `Check(value, ...)` overload still using caller-expression semantics
- manually specified relative targets being prefixed exactly once
- manually specified absolute targets not being prefixed again
- child validation starting from each target semantic
- collection validation under member and index paths starting from each target semantic
- sync and async error creation using explicit target semantics
- message-context creation for caller-expression, relative, and absolute targets
