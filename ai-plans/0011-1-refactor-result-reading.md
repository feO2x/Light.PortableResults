# Optimize JSON Result Reading

## Rationale

`HttpResponseMessage` deserialization now uses intermediary read payloads (`HttpRead*Payload`) as its integration boundary, but the current JSON converters still parse into `Result`/`Result<T>` first and then re-map to those payloads. This introduces avoidable layering and misses optimization opportunities in a hot path. The refactor should make payloads the primary parse target while preserving existing behavior, configurability via `JsonSerializerOptions`/`JsonSerializerContext`, and HTTP-read semantics.

## Acceptance Criteria

- [ ] `ResultJsonReader` is refactored so the primary parsing APIs produce intermediary HTTP read payloads (`HttpReadFailureResultPayload`, `HttpReadSuccessResultPayload`, `HttpReadAutoSuccessResultPayload<T>`, `HttpReadBareSuccessResultPayload<T>`, `HttpReadWrappedSuccessResultPayload<T>`).
- [ ] `HttpRead*PayloadJsonConverter` implementations call payload-oriented reader APIs directly and no longer deserialize through `Result`/`Result<T>` as an intermediate step.
- [ ] Public behavior for `ReadResultAsync`/`ReadResultAsync<T>` remains unchanged, including empty-body rules, failure parsing rules, metadata handling, and `PreferSuccessPayload` behavior.
- [ ] Existing `JsonSerializerOptions` extensibility and source-generated context scenarios continue to work without additional caller configuration.
- [ ] Auto success-payload shape detection is optimized to reduce unnecessary full-object inspection work in common non-wrapper cases.
- [ ] Automated tests are updated/extended to validate the refactor and prevent regressions.
- [ ] Micro benchmarks are added or updated for HTTP read deserialization and show no regression; the refactored path should match or improve allocations and throughput versus the current baseline.

## Technical Details

Change `Light.Results.Http.Reading.Json.ResultJsonReader` into a payload-centric parser layer. The parser should expose focused methods for failure payload parsing and each success payload mode (auto, bare, wrapped), with shared internal helpers for metadata, error parsing, and token validation. `HttpRead*PayloadJsonConverter` types should become thin wrappers over these payload methods.

Keep current HTTP extension orchestration in `HttpResponseMessageExtensions` intact: it should continue to deserialize payload types through `JsonSerializer.DeserializeAsync`, apply `LightResultsHttpReadOptions`, and materialize final `Result`/`Result<T>` only at the extension boundary. This preserves the existing System.Text.Json integration model and keeps user-provided serializer configuration fully effective.

For performance, reduce duplicate work in auto success parsing. Today, wrapper detection relies on object inspection that can scan more than necessary. The refactor should use an early-exit inspection strategy (on a reader copy) that quickly classifies obvious bare-value objects while still enforcing the current wrapper rule (`value` plus optional `metadata`, no additional top-level properties). Preserve current validation and exception semantics.

To minimize migration risk, keep existing `Result`-returning reader methods as compatibility adapters (or replace their usage and retire them if no longer needed), but ensure there is a single payload-oriented parsing core to avoid logic drift. Update unit tests around `ResultJsonReader` and converter behavior to target payload-first parsing, and keep integration coverage for context-backed deserialization, wrapper edge cases, and failure formats (rich + ASP.NET Core-compatible validation payloads).

The library is not published yet, you can make breaking changes. Especially, delete code that you deem unnecessary.
