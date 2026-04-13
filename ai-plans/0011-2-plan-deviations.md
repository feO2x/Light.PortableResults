# 0011 Plan Deviations

This document compares `ai-plans/0011-http-response-deserialization.md` with the current implementation state on branch
`11-http-response-message-integration`.
It also tracks follow-up work from `ai-plans/0011-refactor-result-reading.md`.

## Deviations From Original Plan

1. `ReadResultAsync` API shape was simplified.
- Planned:
  - `ReadResultAsync(..., LightResultHttpReadOptions? options = null, JsonSerializerOptions? serializerOptions = null, ...)`
- Implemented:
  - `ReadResultAsync(..., LightResultsHttpReadOptions? options = null, ...)`
  - Serializer options are provided through `LightResultsHttpReadOptions.SerializerOptions`.

2. Header concerns were consolidated into `IHttpHeaderParsingService`.

- Planned:
    - `HeaderSelectionMode` + `HeaderAllowList` + `HeaderDenyList` on the options type.
- Implemented:
    - `IHttpHeaderParsingService` owns the full header-to-metadata pipeline: selection, parsing, conflict
      resolution, and annotation.
    - `DefaultHttpHeaderParsingService` accepts `IHttpHeaderSelectionStrategy`,
      `FrozenDictionary<string, HttpHeaderParser>`,
      `HeaderConflictStrategy`, `MetadataValueAnnotation`, and `HeaderValueParsingMode` via its constructor.
    - `ParseNoHttpHeadersService` is the default (skips all headers).
    - `LightResultsHttpReadOptions` only exposes `HeaderParsingService` — the three former header properties
      (`HeaderSelectionStrategy`, `HeaderConflictStrategy`, `HeaderMetadataAnnotation`) were removed.
    - `HttpResponseMessageExtensions` delegates entirely to the service; the `AppendHeaders` / `ReadHeaderMetadata`
      helper methods were removed.

3. Header parsing defaults changed to match HTTP round-trip expectations.
- Planned:
    - Default parsing proposed JSON-first fallback behavior for header values.
- Implemented:
    - Default header parsing is primitive-first (`bool` -> `Int64` -> finite `double` -> `string`).
  - Header parsing is opt-in because we do not write to HTTP headers in Minimal APIs and MVC. The default service is
    `ParseNoHttpHeadersService`.

4. Converters were split into explicit read/write converter sets.
- Planned:
    - Add `Read` implementations to existing converter types.
- Implemented:
    - `HttpRead*` converters for deserialization and `HttpWrite*` converters for serialization.
    - Read-only converters throw in `Write`; write-only converters throw in `Read`.
    - `DefaultResultJsonConverter` naming was replaced by `HttpWriteResultJsonConverter` family.

5. HTTP namespace structure became more explicit than originally planned.

- Planned:
    - Shared serializer-oriented grouping.
- Implemented:
  - `Light.Results.Http.Reading`, `Light.Results.Http.Reading.Headers`, `Light.Results.Http.Reading.Json`
  - `Light.Results.Http.Writing`, `Light.Results.Http.Writing.Headers`, `Light.Results.Http.Writing.Json`
  - Options and behavior enums are located in `Reading` or `Writing` according to direction.

6. `HttpResponseMessage` read path received additional allocation-focused optimization.

- Planned:
    - General focus on low allocations.
- Implemented:
    - `Content-Length` is checked first to avoid unnecessary stream allocations.
    - HTTP responses with body are deserialized from stream.
  - Responses with unknown content length are treated as empty-body responses.

7. Native AOT guidance was partially relaxed by design decision.
- Planned:
    - Avoid `Activator.CreateInstance` / `MakeGenericType` in new code paths.
- Implemented:
    - Converter factories still use runtime generic creation (`Activator` + `MakeGenericType`).
  - This is an intentional tradeoff: the corresponding generic JsonConverter types will not be removed by the trimmer as
    they are referenced with the `typeof` keyword. Bound Reflection works perfectly fine in Native AOT. Users of the
    library do not have to deal with registering a converter instance of every T.

8. `ResultJsonReader` became payload-oriented rather than `Result`-oriented.

- Planned:
    - Shared parsing helpers focused on reading `Result` / `Result<T>`.
- Implemented:
    - `ResultJsonReader` now exposes payload-first APIs:
      `ReadSuccessPayload`, `ReadFailurePayload`, `ReadAutoSuccessPayload<T>`,
      `ReadBareSuccessPayload<T>`, `ReadWrappedSuccessPayload<T>`.
    - `Result` and `Result<T>` materialization remains at the `HttpResponseMessageExtensions` orchestration boundary.

## Additional Work Completed Beyond Initial Plan

1. End-to-end round-trip integration tests were added using Minimal API apps and `HttpClient`.

- `Result` and `Result<T>` assertions use Value Object style expected-result comparisons.

2. `JsonSerializerContext`-based read tests were added.

- Context-backed `ReadResultAsync` scenarios are covered for generic success, generic failure, and non-generic success.

3. Targeted coverage improvements were added for HTTP reading/writing internals.

- Additional unit tests cover header selection strategies, parser registry behavior, parsing/conversion helpers, and
  JSON reader edge paths.

4. Auto success wrapper detection was optimized in `ResultJsonReader`.

- The wrapper inspection path now early-exits on the first non-wrapper property (`value`/`metadata`) instead of
  collecting full object-shape flags.

5. `HttpRead*PayloadJsonConverter` types now deserialize directly to payloads via `ResultJsonReader`.

- The previous converter path that deserialized to `Result`/`Result<T>` and then remapped to payloads was removed.

6. Deserialization micro benchmarks were added for HTTP read payload parsing.

- `benchmarks/Benchmarks/HttpReadDeserializationBenchmarks.cs` compares optimized auto-success parsing against a legacy
  full-scan wrapper detection baseline for bare and wrapped payloads.

7. HTTP reading coverage was expanded substantially.

- Added `HttpResponseMessageExtensionsTests` and `DefaultHttpHeaderParsingServiceTests`, plus additional converter and
  `ResultJsonReader` tests for incomplete JSON, null-guard behavior, header conflicts, parsing modes, and cancellation.
