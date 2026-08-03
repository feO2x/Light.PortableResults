# Guard against default result instances at the write boundaries

## Rationale

`default(Result<T>)` and `default(Result)` are neither a success nor a failure: `IsValid` is `false` while `Errors` is empty. The constructors enforce the "a failure carries at least one error" invariant, but C# hands out the default instance for free through array elements, uninitialized fields, `out` parameters and `default` literals, so the struct cannot defend itself. The library even produces one deliberately: `Validator.CheckForErrors` assigns `failure = default` on its success path, where the `false` return value tells the caller not to look at it.

The damage is at the write boundaries. `default(Result<string>).ToCloudEvent(...)` silently emits `{"lproutcome":"failure", ..., "data":{"errors":[]}}`, which this library's own reader then rejects with `JsonException`. For a library built on round-trip integrity, producing a message the consumer side cannot read is the worst failure shape — worse than throwing. The HTTP write path already fails, but only incidentally and with messages that never name the cause. Every write boundary should reject the default instance up front, with one exception type and one message that names the defect and the remedy.

## Acceptance Criteria

- [ ] The public API of `Result`, `Result<T>` and `IResultObject` is unchanged.
- [ ] Serializing a default result over CloudEvents throws instead of emitting a payload, for the generic and non-generic result and for every public entry point: the byte-array, pooled, `Utf8JsonWriter` and envelope-factory overloads.
- [ ] A hand-constructed `CloudEventsEnvelopeForWriting`/`CloudEventsEnvelopeForWriting<T>` carrying a default result is rejected by the JSON writing path, so the guard cannot be bypassed by skipping the envelope factory.
- [ ] Writing a default result over HTTP throws for both the wrapper factories and the JSON converters, including when a custom `CreateProblemDetailsInfo` delegate is configured.
- [ ] `HttpExtensions.SetStatusCodeFromResult`, `SetContentTypeFromResult` and `SetMetadataValuesAsHeadersIfNecessary` reject a default result, so a Minimal API or MVC endpoint returning one — directly or through an `IHttpResultEnricher` — fails before any status code, header or body byte reaches the response.
- [ ] Every guarded site routes through one shared guard clause, throwing the same exception type and a message naming the default instance as the cause and `Result.Ok`/`Result.Fail` as the remedy. `Errors.GetLeadingCategory` and `ProblemDetailsInfo.CreateDefault` are no longer the observable failure for a default result.
- [ ] Automated tests cover every guarded entry point, assert that no bytes are written when the guard trips, and pin the round-trip property that the library never writes a failure payload its own reader rejects.
- [ ] The XML documentation states that a default result is neither a success nor a failure and cannot be written, and `Validator.CheckForErrors` documents that `failure` is meaningful only when it returns `true`.
- [ ] Affected packages update `<PackageReleaseNotes>`.
- [ ] The Stryker figures in `tests/AGENTS.md` are re-measured where this change invalidates them: the `Light.PortableResults` smoke-check test count, and the `AspNetCore.Shared` baseline row, whose mutant inventory grows with the `HttpExtensions` guards.

## Technical Details

### The guard clause

One guard clause in the core package next to `IResultObject`, so the core write paths and `AspNetCore.Shared` share it, and no change to `Result`, `Result<T>` or `IResultObject` at all:

```csharp
public static TResult MustNotBeDefaultInstance<TResult>(
    this TResult result,
    [CallerArgumentExpression("result")] string? parameterName = null
) where TResult : struct, IResultObject;
```

`IResultObject` already exposes `IsValid` and `Errors`, so the guard tests `!IsValid && Errors.Count == 0` with what is there today. The condition is exact: no non-default instance can be invalid with an empty `Errors` collection. Keeping the invariant in exactly one place is the point — a companion `IsDefaultInstance` property on the structs would restate it and could drift from the guard. `Error.IsDefaultInstance` stays the odd one out; a non-throwing predicate on the result structs is a cheap follow-up if a consumer need appears, but nothing in this change wants one.

Returning `TResult` follows the `MustXxx` guard-clause convention and lets the guard sit where the value is captured rather than on a preceding line.

Take the result by value, not by `in`. `in` is not expressible here in any case: a generic receiver rejects it for both classic extension methods (CS8338) and C# 14 extension blocks (CS9301), so `in` would cost the extension syntax outright. It would also buy nothing, because every guarded site already holds a by-value copy — `ToCloudEventsEnvelopeForWriting`, `ToHttpResultForWriting` and the `HttpExtensions` methods all take the result by value today, and the two converter sites read it out of a record-struct property getter. This also matches `CheckIfMetadataShouldBeWrittenForValidResult<TEnvelope, TResult>`, the existing generic struct receiver at these boundaries. Should the copies ever prove to matter, the lever is the enclosing signatures, not the guard.

What does decide the codegen is inlining. Mark the guard `[MethodImpl(MethodImplOptions.AggressiveInlining)]` and outline the throw into a separate `[MethodImpl(MethodImplOptions.NoInlining)]` helper, so the inlineable body stays at two field reads and a branch and the argument copy disappears through forward substitution. Both result types are `readonly struct`, so member access adds no defensive copies.

Throw `ArgumentException`. At every guarded site the offending result arrives as a parameter — including the converters, where the wrapper is the argument — so `ArgumentException` is accurate and keeps a single type across the boundary.

Do not name the guard for validity. `IsValid` means "is a success", so a name built on it reads as rejecting failures, which is the opposite of the contract: any failure carrying at least one error must pass.

### Where to guard

The rule is: every public API that consumes a result in order to write it out. Two sites per transport, because the intermediate wrapper types are public and constructible by callers:

| Transport | Fail-fast site | Non-bypassable site |
| --- | --- | --- |
| CloudEvents | `CloudEventsResultExtensions.ToCloudEventsEnvelopeForWriting` (both overloads) | `JsonCloudEventsExtensions.WriteCloudEvents` (both overloads) |
| HTTP body | `HttpResultForWritingExtensions.ToHttpResultForWriting` (all four overloads) | `HttpResultForWritingJsonConverter` / `HttpResultForWritingJsonConverter<T>` `Write` |
| HTTP headers | — | `HttpExtensions.SetStatusCodeFromResult`, `SetContentTypeFromResult`, `SetMetadataValuesAsHeadersIfNecessary` |

`ToCloudEvent`, `ToCloudEventPooled` and `WriteCloudEvent` need no guard of their own; all six funnel through the envelope factory. The `WriteCloudEvents` guard is what covers a caller who builds `CloudEventsEnvelopeForWriting` directly and hands it to the converter.

Do not guard the `LightResult`, `LightResult<T>`, `LightActionResult` and `LightActionResult<T>` constructors. `IHttpResultEnricher.Enrich` runs during `ExecuteAsync` and can substitute a default result after construction, so a constructor guard is not sound on its own, while `SetHeaders` — which calls the guarded `HttpExtensions` methods on the enriched result — runs before anything is written to the response. One guard at the sound chokepoint is preferable to two that together still need the second one.

Once inlined as described above, the guard is two field reads and a predictable branch on paths that immediately perform JSON serialization or HTTP header work. No microbenchmark is warranted.

### Current behavior being replaced

Worth knowing while writing the negative tests, since only one of the three is a silent corruption:

- CloudEvents write emits `"data":{"errors":[]}` with no error at all; `CloudEventsDataJsonReader` rejects it on the way back in.
- HTTP body write throws `ArgumentException` from `ProblemDetailsInfo.CreateDefault` — but only with default problem-details creation. A configured `CreateProblemDetailsInfo` delegate bypasses that check and emits an empty `errors` array.
- `SetStatusCodeFromResult` throws `InvalidOperationException("Errors collection must contain at least one error.")` from `Errors.GetLeadingCategory`. It runs first in the ASP.NET Core pipeline, so this is the message users actually see today, and it names neither the result nor the default instance.

Leave the checks in `Errors.GetLeadingCategory` and `ProblemDetailsInfo.CreateDefault` in place. They are general-purpose `Errors` guards, not result write boundaries; they simply stop being the first thing a default result hits.

### Deliberately out of scope

- **The functional extensions.** They are the hot in-memory path and none of them can manufacture a default result: `Result<T>.Fail(Errors)` rejects an empty collection, so `Map`, `Bind`, `MapError` and friends already throw when fed one. They are inconsistent about it — `Match` and `Switch` hand the empty `Errors` to the caller's failure callback, `MatchFirst` throws from `FirstError` — but no path produces a corrupt payload, and guarding roughly forty methods buys nothing for round-trip integrity.
- **The struct itself.** No guard belongs in `Result`/`Result<T>` members. `Validator.CheckForErrors` assigns `failure = default` on its success path by design, and a struct cannot intercept its own default construction anyway.
- **The read paths.** Readers build results through `Result.Ok`/`Result.Fail` and cannot produce a default instance.
- **Default instances of the wrapper structs.** `default(CloudEventsEnvelopeForWriting)` carries null `Type`, `Source` and `Id` — a separate defect in the same family, not part of this change.
- **The remaining v0.7.0 preparations.** This is item 2 of #77; Native AOT compatibility is #78 and `EnablePackageValidation` is item 3, tracked separately.
