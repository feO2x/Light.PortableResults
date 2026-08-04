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
failure and cannot be written". `Result<T>` qualifies that statement by the value type, while
`MustNotBeDefaultInstance` documents the exact rejected state and identifies a default nullable `Result<T>` as its
usual source. On the non-generic `Result` the opposite is documented instead: its default instance is a valid
success, indistinguishable from `Result.Ok()`. Documenting the criterion verbatim there would have been false and
would invite a later "fix" that breaks the successful default.

The exception message follows the same distinction. It names the invalid-without-errors state as the defect and a
default instance as its usual source, rather than asserting that every custom `IResultObject` rejected by the
guard must be its CLR default. It still gives `Result.Ok` and `Result.Fail` as the remedy for built-in results.

`Validator.CheckForErrors` documents that `failure` is meaningful only when the method returns `true`, as required,
without repeating the claim that the assigned `default` is neither a success nor a failure — on `Result` it is a
success.

### Mutation testing baselines

The `AspNetCore.Shared` baseline row and the `Light.PortableResults` smoke-check test count in `tests/AGENTS.md`
were re-measured as required. Unrelated observation while doing so: the mutant inventory table's
`Light.PortableResults` row (4,867 mutants, 520 `CompileError`) was already stale before this change — the
smoke-check run reports 5,735 created mutants and 507 `CompileError` for the project. That row was left untouched,
because re-measuring it is not part of this change and attributing the drift to it would be misleading.

## Housekeeping alongside this change

Not deviations from the plan, but recorded here because they touch shared documents in the same branch.

Two observations from this work are recorded where the knowledge is used:

- The finding that `default(Result)` is a valid success is the first section of this file and is referenced from
  the commit that introduces the guard.
- The existing `AGENTS.md` scratch note on `MetadataValueReconstructor` and `OperationCanceledException` was split,
  leaving the scratch section empty. Why the two evaluation catch filters exclude cancellation is now a comment at
  both filters in the generator, because that is where a reader needs it. That the contract is not observable
  through the generator's public surface became a bullet in the blind spots section of `tests/AGENTS.md`, next to
  the other "do not read as adequate coverage" entries, and it now names the trigger that makes it testable: an
  accepted evaluation gaining a reachable cancellation path.

A `README.md` for `Light.PortableResults.Validation.OpenApi.SourceGeneration` was considered and rejected for now.
The existing folder READMEs under `Numbers/`, `Text/` and `Metadata/` each carry a substantial, multi-faceted
concern, and a document holding a single caveat would sit one directory away from the code it explains. Revisit it
if the generator accumulates more design notes, such as the evaluation whitelist policy, the recursion depth limit,
or the incremental pipeline caching choices; the source comment stays regardless.
