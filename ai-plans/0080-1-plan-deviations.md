# Plan Deviations for the Guard Against Default Result Instances

## Referenced Plans

- `0080-0-result-default-guard.md` introduced `MustNotBeDefaultInstance` next to `IResultObject` and applied it at
  the CloudEvents, HTTP body, and HTTP header write boundaries, so that a result which is neither a success nor a
  failure can never reach a transport.

## Deviations

### `default(Result)` is a success, not a defective instance

The Rationale states that `default(Result<T>)` **and** `default(Result)` are neither a success nor a failure. That
holds only for `Result<T>` whose `default(T)` is null.

`Result` encapsulates a `Result<Unit>`, and `Unit` is a struct. `Result<T>.IsValid` is `_value is not null &&
_errors.Count is 0`, and for a value type `T` the first operand is always true. Measured on the current code:

| Expression | `IsValid` | Equal to |
| --- | --- | --- |
| `default(Result)` | `true` | `Result.Ok()` |
| `default(Result<int>)` | `true` | `Result<int>.Ok(0)` |
| `default(Result<string>)` | `false` | — (the defective instance) |
| `default(Result<int?>)` | `false` | — (the defective instance) |

So `default(Result)` is bit-for-bit an ordinary success without metadata, and writes and round-trips like one.
The corruption the plan describes — `{"lproutcome":"failure", ..., "data":{"errors":[]}}` — is reachable only
through `Result<T>` with a reference type or a nullable value type, which is the common case for typed results and
the case the plan's own example uses.

This changes nothing about the guard: `!IsValid && Errors.Count == 0` still identifies the defective instance
exactly, and it remains the only condition needed. It changes what the guard can be observed to reject, and what
the documentation may claim.

### The guards on the `Result`-typed sites are unreachable and were kept anyway

Because no `Result` value can be invalid while carrying no errors, four guarded sites can never throw:

- `CloudEventsResultExtensions.ToCloudEventsEnvelopeForWriting(this Result, ...)`
- `JsonCloudEventsExtensions.WriteCloudEvents(this Utf8JsonWriter, CloudEventsEnvelopeForWriting, ...)`
- `HttpResultForWritingExtensions.ToHttpResultForWriting(this Result, ...)` (both overloads)
- `HttpResultForWritingJsonConverter.Write`

They are kept for uniformity: the invariant lives in one place, the inlined guard costs two field reads on a path
that immediately serializes JSON, and the sites become live if `Result` ever stops encapsulating a `Result<Unit>`.
Expect their throw branches to show up as surviving mutants and as uncovered branches; that is a property of the
`Result` representation, not a missing test. The generic counterparts of all four are covered.

The three `HttpExtensions` guards are a different case. They are constrained to `TResult : struct, IResultObject`,
and `IResultObject` is public, so a caller's own struct can be invalid with no errors. They are reachable
independently of `Result` and are tested both with `default(Result<string>)` and with a hand-crafted
`IResultObject` implementation.

### Documentation wording

The acceptance criterion asks the XML documentation to state that "a default result is neither a success nor a
failure and cannot be written". That sentence is written on `Result<T>`, qualified by the value type, and on
`MustNotBeDefaultInstance`. On the non-generic `Result` the opposite is documented instead: its default instance is
a valid success, indistinguishable from `Result.Ok()`. Documenting the criterion verbatim there would have been
false and would invite a later "fix" that breaks the successful default.

`Validator.CheckForErrors` documents that `failure` is meaningful only when the method returns `true`, as required,
without repeating the claim that the assigned `default` is neither a success nor a failure — on `Result` it is a
success.

### Mutation testing baselines

The `AspNetCore.Shared` baseline row and the `Light.PortableResults` smoke-check test count in `tests/AGENTS.md`
were re-measured as required. Unrelated observation while doing so: the mutant inventory table's
`Light.PortableResults` row (4,867 mutants, 520 `CompileError`) was already stale before this change — the
smoke-check run reports 5,735 created mutants and 507 `CompileError` for the project. That row was left untouched,
because re-measuring it is not part of this change and attributing the drift to it would be misleading.
