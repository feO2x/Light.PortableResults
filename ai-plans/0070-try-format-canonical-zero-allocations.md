# Allocation-Free Canonical Formatting for MetadataValue

## Rationale

`MetadataValue.TryFormatCanonical` promises a span-based formatting API, but only `Double` and `Single` honor it. Every other kind falls through to `ToCanonicalString()` and copies the resulting string into the destination. For `Null`, `Boolean`, `String`, and `Uri` that is already free — they return a literal or the stored reference — but for the remaining ten kinds the caller pays a string allocation for exactly the values it tried to format without one. `JsonCloudEventsExtensions.WriteStringAttribute` is the visible victim: it stack-allocates a buffer, calls `TryFormatCanonical`, and still allocates a throwaway string for every `Guid`, `DateTime`, or `Int64` extension attribute it writes. The same missing primitives cost allocations on the read path, where every `TryGetXxx` overload that accepts canonical text validates by formatting the parsed value back to a `string` and comparing it.

The library's serializers ultimately write UTF-8. `CanonicalFloatingPointFormatter` already renders both encodings from one implementation by generalizing its renderer over the output code unit, and #61 deferred the serializer integration that makes the saving observable — `MetadataExtensions.WriteNumberValue` still allocates a string per `Double` and lets `System.Text.Json` transcode it back to ASCII. Because the remaining canonical encodings are ASCII too, extending that same generic renderer to them yields the UTF-8 path at almost no additional cost, and skipping it now would mean a second pass over these files later.

Introduce canonical span formatters for the remaining primitive kinds in both encodings, add the matching UTF-16 and UTF-8 APIs to `MetadataValue`, derive `ToCanonicalString` and the round-trip validators from those formatters so a single implementation defines each encoding, and adopt them in the JSON and CloudEvents writers.

## Acceptance Criteria

- [ ] `MetadataValue` exposes `TryFormatCanonical` and a new `TryFormatCanonicalUtf8`, and neither allocates for any primitive kind — including `String`, `Char`, and `Uri`, whose text is copied or transcoded into the caller's destination.
- [ ] One implementation serves both package assets and both encodings: no target-specific formatter, no routing through the other encoding, and no intermediate output buffer — each method writes directly into the caller's destination in the requested encoding. The shared expected-output corpus passes unchanged against both assets.
- [ ] A public `CanonicalTextFormatter` with XML documentation exposes the new canonical formatting primitives in both encodings, plus per-type maximum-length constants that bound both encodings, on both package assets.
- [ ] `MetadataValue` exposes a documented public constant bounding the canonical length of every bounded primitive kind in either encoding, and `JsonCloudEventsExtensions` sizes its stack buffer with that constant instead of a private magic number.
- [ ] For every value of every primitive kind, `TryFormatCanonical` produces exactly the text that `ToCanonicalString` produced before this change, and `charsWritten` equals that text's length. `TryFormatCanonicalUtf8` produces the replacement-fallback UTF-8 encoding of that same text — lossy only for malformed UTF-16, which has no valid UTF-8 encoding — with `bytesWritten` equal to `charsWritten` for every kind whose canonical text is ASCII.
- [ ] The existing failure contracts are unchanged and hold in both encodings: a destination that is too small returns `false`, writes zero to the count, and leaves the destination unmodified; `Array` and `Object` still throw `InvalidOperationException`; and a corrupt `DateTime`, `DateOnly`, or `TimeOnly` payload still throws `InvalidOperationException`.
- [ ] `CanonicalTextFormatter.TryFormat(DateTime, …)` normalizes `DateTimeKind.Local` to UTC, matching `MetadataValue.FromDateTime`, so every `DateTime` it accepts fits `MaximumDateTimeLength`. `Utc` and `Unspecified` values are unaffected, and the behavior is covered by a test against the formatter itself.
- [ ] The `TryGetXxx` round-trip validators compare against span-formatted text and no longer allocate a string per candidate value.
- [ ] `ToCanonicalString` still allocates nothing for the four kinds that are allocation-free today — `Null`, `Boolean`, `String`, and `Uri` — and its output is unchanged for every kind.
- [ ] The JSON metadata writer emits string-shaped and `Double`/`Single` number-shaped metadata values without materializing canonical text, and the bytes it produces for every metadata value are unchanged under both the default and the `UnsafeRelaxedJsonEscaping` encoder, including for text containing non-ASCII characters and unpaired surrogates.
- [ ] Automated tests cover, per kind and per encoding, the canonical text, the written count, the exactly-sufficient and one-short destination, the boundary values of each kind, the invalid-payload throws, and the absence of allocations. The metadata test project passes against both the `net10.0` and the `netstandard2.0` library asset.
- [ ] A microbenchmark compares the new `Guid` and `DateTime` formatters with the framework `TryFormat` APIs on `net10.0`, and its result is recorded in the pull request as evidence for the deferred follow-up decision. No target-specific implementation is introduced in this issue regardless of the result.
- [ ] The CloudEvents and HTTP write benchmark fixtures carry metadata of the kinds this issue affects, and before/after allocation figures for both are recorded in the pull request.
- [ ] `THIRD-PARTY-NOTICES.md` and the folder README record the newly adapted upstream files and the adaptations made to them, following the existing provenance pattern, and every file containing adapted runtime code retains the .NET Foundation MIT header.
- [ ] The `netstandard2.0` decimal path documents its runtime assumption and enforces it without allocating, surfacing `PlatformNotSupportedException` from the decimal formatting path itself, so a runtime that violates it fails loudly instead of producing wrong text and no other kind's formatting is affected.
- [ ] The package release notes mention the new formatting APIs and the removed allocations.
- [ ] Both target frameworks build in Release with warnings as errors, package validation succeeds, the Native AOT sample publishes successfully, and test coverage remains above 95%.

## Technical Details

### Why the framework APIs are not enough

`netstandard2.0` has no span-formatting API for any of the affected types, and the `Polyfill` package's `TryFormat` extensions are not a substitute: they call `ToString` and copy the result, which is what this issue removes. Meeting the criterion on both assets requires the library to own these formatters regardless of what the `net10.0` asset does, and UTF-8 widens the gap further — `IUtf8SpanFormattable` does not exist on `netstandard2.0` at all.

This issue therefore ships exactly one implementation, serving both assets and both encodings, with no `#if NET10_0_OR_GREATER` split onto framework APIs anywhere. A single path removes the risk of the two assets diverging in wire format — the failure #58 fixed for floating-point text — keeps one test surface, and costs one implementation per type instead of two per encoding.

Calling into the framework span formatters on `net10.0` remains an open question, not a rejected one: `Guid.TryFormat` in particular has a dedicated vectorized `D` path upstream. That question is deliberately deferred to a follow-up issue, in the same way #61 deferred its serializer integration. This plan produces the evidence for that decision — the microbenchmark below — and stops there. Do not introduce a target-specific branch here, even if the benchmark favors one.

The benchmark also tells the follow-up where to look. `Guid` is the plausible candidate. The date kinds likely are not: their canonical form is a custom format string, so `DateTime.TryFormat` routes into the general custom-format interpreter rather than the fixed-shape `TryFormatO` fast path this plan ports.

### Porting from the BCL

Do not write these formatters from scratch. `Numbers/` already establishes the pattern — adapted `dotnet/runtime` code under the .NET Foundation MIT header, with provenance in `THIRD-PARTY-NOTICES.md` and the folder README — and every encoding this issue needs has a portable upstream implementation. Porting also retires a correctness risk: these encodings are pinned by the `TryGetXxx` validators, so a hand-derived rule that is subtly wrong breaks round-tripping of data already on the wire.

Stay on the tag the repository already cites, `v6.0.36` (commit `f1dd57165bfd91875761329ac3a8b17f6606ad18`). That is not only for consistency: `release/6.0` predates the `System.Runtime.Intrinsics` rewrites of `Guid.TryFormat` and the number formatters, so its implementations are scalar and compile on `netstandard2.0`, where intrinsics do not exist. Newer lines would have to be de-vectorized by hand.

| Kind | Upstream source | Notes |
| --- | --- | --- |
| `Int64`, `UInt64` | `Number.Formatting.cs`: `TryUInt64ToDecStr`, `UInt32ToDecChars`, `Int64DivMod1E9`; `FormattingHelpers.CountDigits` | Scalar integer math over a `fixed` destination. Direct port. |
| `Decimal` | `Number.Formatting.cs`: `DecimalToNumber`; `Decimal.DecCalc.cs`: `DecDivMod1E9` | `DecDivMod1E9` is twelve lines of integer arithmetic. `DecimalToNumber` emits digits and scale into a `NumberBuffer` — the type this repository already ported — so decimal reuses existing infrastructure and only needs a renderer that honors the scale. |
| `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` | `DateTimeFormat.cs`: `TryFormatO`, `WriteTwoDecimalDigits`, `WriteFourDecimalDigits`, `WriteDigits` | Port `TryFormatO`, **not** `FormatCustomized`. See below. |
| `TimeSpan` | `System.Private.Xml`, `XsdDuration.cs`: the `TimeSpan` constructor and `ToString(DurationType)` | This is the normative source of `XmlConvert.ToString(TimeSpan)`, which the validators compare against, so porting it settles the encoding by construction rather than by inference. |
| `Guid` | `Guid.cs`: the `D` branch of `TryFormat`, `HexsToChars`, `HexConverter.ToCharLower` | Confirms the field-reinterpretation approach: upstream formats the numeric fields (`_a >> 24`, …), which is endianness-independent. |

Two adaptations are unavoidable and one target is a trap:

- **`FormatCustomized` is the wrong target for the date kinds.** It is a general custom-format interpreter driven by `DateTimeFormatInfo` and `Calendar`, carrying Hebrew and Japanese calendar special cases and `StringBuilderCache`; porting it would be far more work than the fixed template it would produce. `TryFormatO` is roughly seventy-five lines, span-based, culture-free, and already writes `yyyy-MM-ddTHH:mm:ss` followed by a fraction and an offset or `Z`. Adapt it in three places: it writes exactly seven fractional digits where the canonical form omits the fraction entirely when the sub-second ticks are zero and otherwise trims trailing zeros; it reads the components through the runtime-internal `GetDate`/`GetTimePrecise`, so either port those two helpers as well or use the public `Year`/`Month`/`Day`/… properties and accept that each recomputes from the tick count; and its `DateTimeKind.Local` branch, which queries `TimeZoneInfo.Local` and appends the six-character offset, is dropped for `DateTime` because the kind contract normalizes `Local` beforehand. Only the `DateTimeOffset` overload keeps an offset path.
- **Everything upstream renders to `char`.** The ported renderers must write through this repository's `TCodeUnit` conversion helper instead, so both encodings come from one body. This is the same adaptation `CanonicalFloatingPointFormatter` already documents.
- **`decimal.GetBits` is a `netstandard2.0` trap, and the way around it is narrower than it looks.** The `Span<int>` overload is .NET Core 3.0 and later; on `netstandard2.0` only `int[] GetBits(decimal)` exists, and it allocates a four-element array per call — which would defeat the entire issue. Upstream sidesteps it by reading the value through `Unsafe.As<decimal, DecCalc>`, but that is a weaker guarantee than it appears. `decimal.GetBits` documents the *logical* representation — low, middle, high, flags — not the private field order, and upstream `DecCalc` carries an explicit `#if BIGENDIAN` layout, so endianness is part of the assumption rather than incidental to it.

  Split the value extraction by target and keep the formatter itself single:

  - `net10.0` calls `decimal.GetBits(decimal, Span<int>)`. It is contractual, allocation-free, and endianness-independent, so this target carries no layout assumption at all.
  - `netstandard2.0` uses the reinterpret, with the assumption stated in the XML documentation: a little-endian runtime with the historical `flags`, `hi`, `lo`, `mid` field order.

  This does not reopen the single-implementation decision. What differs is how the four integers are obtained; the digit generation and the renderer are one body, and both extractions are pinned to the same contractual quadruple by `GetBits` semantics.

  Verify the `netstandard2.0` assumption once rather than trusting it, and verify it **without allocating**. Do not reach for `decimal.GetBits(decimal)` in the guard: it allocates a four-element array on first use, which breaches the unqualified no-allocation criterion, and the warmed-up allocation test cannot catch it, because the warm-up iteration triggers type initialization before measurement starts. Seed the probe from the constructor instead:

  ```csharp
  var probe = new decimal(lo: 0x11111111, mid: 0x22222222, hi: 0x33333333, isNegative: true, scale: 5);
  ```

  Reinterpret that value and compare the fields against the constructor arguments and the expected flags word — `0x80050000` for the parameters above, being the sign bit and the scale in bits 16 to 23. `decimal(int, int, int, bool, byte)` defines the logical representation exactly as `GetBits` does, so this is no weaker a check, and it touches nothing on the heap: a struct construction, a reinterpret, and four integer comparisons.

  Isolate the guard so its failure is proportionate. Put it in a decimal-specific private helper rather than in `CanonicalTextFormatter`'s own initializer; a throwing initializer on the outer type would disable integer, date, `TimeSpan`, and `Guid` formatting too, none of which depend on the layout. Store the outcome in a `static readonly bool` and throw `PlatformNotSupportedException` from the decimal formatting path, rather than throwing from the initializer — an initializer that throws surfaces to callers as `TypeInitializationException` wrapping the real cause, which contradicts the exception documented on the method. The flag read folds away once the type is initialized, so it costs nothing on the formatting path.

  Note that a mutant in that initializer will be reported as survived regardless of coverage, per the blind spot in `tests/AGENTS.md`.

  A unit test comparing the reinterpret against `decimal.GetBits` is still worth having, but be precise about what it proves: the test suite runs both package assets on the same .NET 10 host, so it validates the compilation target, never the `netstandard2.0` asset's behavior on .NET Framework or Mono. Only the runtime guard covers that case.

### Formatting rules to verify

The ports above define the encodings; these are the cases to assert explicitly, because they are where an adaptation slip stays invisible until data fails to round-trip:

- **Decimal.** The scale is significant — `19.50m` renders as `19.50`, not `19.5`. A negative zero (`decimal.Negate(0m)`, sign bit set, all digits zero) renders **without** a sign, matching the framework.
- **TimeSpan.** `PT0S` for zero, a leading `-` before the `P`, the days component omitted when zero, the `T` present only when a time component follows, and fractional seconds trimmed of trailing zeros (`P2DT3H4M5.06S`, `PT0.0000001S`). Upstream derives the magnitude with `unchecked((ulong)-ticks)` precisely so that `TimeSpan.MinValue`, whose magnitude is not representable as a positive `long`, does not overflow; keep that.
- **DateTime.** `Utc` values end in `Z` and `Unspecified` values carry no designator, matching `yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK`. `Local` values are normalized as described below. `DateTimeOffset` uses the same shape with `+hh:mm`/`-hh:mm` and never `Z`.
- **Guid.** Lowercase `D` format.

### The DateTime kind contract

`TryFormat(DateTime, …)` normalizes `DateTimeKind.Local` to UTC and renders the result with the `Z` designator, matching `MetadataValue.FromDateTime`. `Utc` and `Unspecified` values are rendered as they are. Document this on the method: for `Local` input the output depends on the machine's local time zone, which is exactly why the library normalizes at construction rather than at the boundary.

Without this rule the public formatter would contradict its own bound. `K` renders a `Local` value with a `±hh:mm` offset — 33 characters, five past `MaximumDateTimeLength`, and the upstream `TryFormatO` reserves those six characters for precisely that case. Normalizing first keeps the bound at 28 and keeps the standalone formatter consistent with the only kinds a `MetadataValue` can hold.

Two consequences for the implementation:

- **Normalize conditionally.** Call `ToUniversalTime()` only when `Kind == DateTimeKind.Local`. `DateTime.ToUniversalTime` treats an `Unspecified` value as local and shifts it, so an unconditional call would silently move every `Unspecified` value by the host's offset and destroy the no-designator form that this library deliberately preserves. `FromDateTime` already guards the same way.
- **Normalization cannot throw.** `ToUniversalTime` saturates at `DateTime.MinValue` and `DateTime.MaxValue` instead of overflowing, so the formatter gains no failure mode and the conversion needs no guard.

Because `FromDateTime` normalizes on the way in and `TryReadStoredDateTime` rejects a stored `Local` payload as corrupt, no `MetadataValue` can drive this branch. Cover it with a direct test against `CanonicalTextFormatter` — the case `tests/AGENTS.md` reserves solitary tests for — asserting the normalized text, the written count, and that the output still fits `MaximumDateTimeLength`.

### Public API

```csharp
namespace Light.PortableResults.Text;

public static class CanonicalTextFormatter
{
    public const int MaximumInt64Length = 20;
    public const int MaximumUInt64Length = 20;
    public const int MaximumDecimalLength = 31;
    public const int MaximumCharLength = 3;

    public const int MaximumDayNumber = 3_652_058;          // DateOnly.MaxValue.DayNumber
    public const long MaximumTimeOfDayTicks = 863_999_999_999; // TimeSpan.TicksPerDay - 1
    public const int MaximumDateTimeLength = 28;
    public const int MaximumDateTimeOffsetLength = 33;
    public const int MaximumDateLength = 10;
    public const int MaximumTimeLength = 16;
    public const int MaximumTimeSpanLength = 27;
    public const int MaximumGuidLength = 36;

    public static bool TryFormat(char value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(long value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(ulong value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(decimal value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(DateTime value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(DateTimeOffset value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(TimeSpan value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(Guid value, Span<char> destination, out int charsWritten);
    public static bool TryFormatDate(int dayNumber, Span<char> destination, out int charsWritten);
    public static bool TryFormatTime(long ticks, Span<char> destination, out int charsWritten);

    // One TryFormatUtf8 counterpart per overload above, taking Span<byte> and reporting bytesWritten.

    public static bool TryFormatUtf8(
        ReadOnlySpan<char> text,
        Span<byte> destination,
        out int bytesWritten
    );
}
```

The signatures are exact.

The `char` overload is required for correctness, not convenience, and must not be dropped as redundant with the `long` one. `char` converts implicitly to `long`, so without an exact overload `TryFormat('x', destination, out _)` compiles and writes `120` — silently, in both encodings. `char` is the only affected type: `int`, `short`, `byte`, and `uint` also bind to the `long` overload, but their canonical text *is* their integer text, so those bindings are correct. Renaming the integer methods instead does not fix it; the call then binds to `TryFormat(decimal)` and still writes `120`. Only an exact match resolves ahead of the implicit conversions.

**`false` means one thing: the destination was too small.** Invalid input throws. `TryFormatDate` and `TryFormatTime` accept a `dayNumber` in `[0, MaximumDayNumber]` and `ticks` in `[0, MaximumTimeOfDayTicks]` respectively, and throw `ArgumentOutOfRangeException` outside those ranges. Overloading `false` to mean both "invalid" and "does not fit" would leave callers unable to distinguish them and would force `MetadataValue` to re-derive the ranges to know which exception to raise. This also matches the sibling class: `CanonicalFloatingPointFormatter.TryFormat` throws `ArgumentException` for a non-finite value and reserves `false` for capacity.

`MetadataValue` keeps its own guard rather than relying on that throw, because its contract is `InvalidOperationException` with the existing message, not `ArgumentOutOfRangeException`. The published bounds are what makes that guard safe: it checks against the same constants the formatter enforces, so no magic number is duplicated and the two cannot drift. The formatter's own `ArgumentOutOfRangeException` is consequently unreachable through `MetadataValue` and needs a direct test.

`MaximumCharLength` is 3 rather than 1 because, by the same both-encodings convention as the other constants, it must bound the UTF-8 output: a BMP character occupies up to three UTF-8 bytes, and an unpaired surrogate encodes to the three-byte replacement character. The UTF-16 overload never writes more than one.

`MaximumDateTimeLength` is 28 rather than 33 because `TryFormat(DateTime, …)` normalizes `Local` before rendering, so no input reaches the offset form; see the kind contract above. `TryFormatDate` and `TryFormatTime` take the stored payload rather than `DateOnly` and `TimeOnly`, because `MetadataKind.DateOnly` and `MetadataKind.TimeOnly` are formatted on both assets while the BCL types exist only on `net10.0`. The trailing `TryFormatUtf8(ReadOnlySpan<char>, …)` is the transcoding entry point that the `String`, `Char`, and `Uri` kinds need; it has no UTF-16 counterpart because copying chars is `TryCopyTo`.

`CanonicalFloatingPointFormatter` stays where it is and keeps its own constants and API; moving it would break callers for no gain.

`MetadataValue` gains one constant and one method:

```csharp
public const int MaximumPrimitiveCanonicalLength = 36;

public bool TryFormatCanonicalUtf8(Span<byte> destination, out int bytesWritten);
```

The constant's value is the `Guid` bound, the largest of the bounded kinds. Document that it bounds both encodings and that it excludes `String` and `Uri`, whose canonical text is the caller's own unbounded text — those are the only kinds for which a destination of this size can still return `false`, which is why `WriteStringAttribute` keeps its materializing fallback. `Char` is bounded in both encodings and needs no exclusion: every UTF-16 code unit encodes to at most three UTF-8 bytes.

### The shared renderer

Follow the `TCodeUnit` pattern that `CanonicalFloatingPointFormatter.TryRender` established: a private generic core per type, `where TCodeUnit : unmanaged`, writing ASCII through the existing `typeof(TCodeUnit) == typeof(byte)` conversion helper that the JIT folds away. Every encoding below is pure ASCII, so one renderer produces both outputs and the two public overloads are thin forwarders. Compute the required length before the first write so the all-or-nothing contract holds in both encodings.

Lift the conversion helper to a shared internal home rather than duplicating it, and keep it out of the public surface. The `unmanaged` constraint permits instantiations other than `char` and `byte`; as in #61, do not add a guard for the impossible case, because an unreachable `throw` is a coverage hole.

### Transcoding the text-bearing kinds

`String`, `Char`, and `Uri` are the only kinds whose canonical text is not ASCII, so they are the only ones the UTF-8 path cannot render through the shared renderer. Transcode them with replacement fallback: invalid UTF-16 — an unpaired surrogate, which `FromChar` and `FromString` both accept — becomes U+FFFD rather than a failure, because returning `false` would conflate invalid text with an undersized destination.

That is a lossy encoding, and it must be described as one. Malformed UTF-16 has no corresponding valid UTF-8, so the UTF-8 output for these three kinds is the *replacement-fallback* encoding of the canonical text, not the encoding of that text. `bytesWritten` therefore does not equal `charsWritten` for them, unlike every other kind, and the round trip through UTF-8 is not identity. Scope both assertions accordingly.

**These three kinds must not be routed through the UTF-8 API by the serializers.** `Utf8JsonWriter` does not agree bytewise with pre-transcoded replacement bytes: when it transcodes malformed UTF-16 itself it emits the *escaped* `�`, whereas valid UTF-8 replacement bytes handed to it are subject only to the encoder's normal escaping rules. Measured against the pinned System.Text.Json 10.0.10, the two routes agree under the default encoder — which escapes all non-ASCII — and diverge under `UnsafeRelaxedJsonEscaping`:

```text
input "a\uD800b", relaxed encoder
  via ReadOnlySpan<char>  22-61-5C-75-46-46-46-44-62-22   "a�b"
  via transcoded UTF-8    22-61-EF-BF-BD-62-22            "a<EF BF BD>b"
```

The results are equivalent after parsing but not byte-identical, which violates the unchanged-output criterion. Note that an already-valid U+FFFD in the input converges under both encoders; only malformed input diverges. See the adoption rules below for what each kind uses instead.

On `net10.0` the transcoding is `Utf8.FromUtf16` with `replaceInvalidSequences: true`. On `netstandard2.0` neither that API nor the `Span`-based `Encoding.GetBytes` overload exists; use the pointer overload under `fixed`, whose default replacement fallback produces the same U+FFFD bytes. Both paths need the byte count before the first write to preserve the all-or-nothing contract, which costs a second pass over the text — acceptable, and unavoidable without partial-write semantics.

### Single source of truth

Invert the current relationship: the span methods become primary and `ToCanonicalString` formats into a stack buffer and calls `ToString` on the written slice.

The inversion must not regress `ToCanonicalString`, which is allocation-free today for exactly four kinds. Keep every one of them on a fast path that returns an existing reference rather than building a string:

- `Null` and `Boolean` return string literals (`"null"`, `"true"`, `"false"`).
- `String` and `Uri` return the stored instance.

Routing the first two through a stack buffer and `ToString()` would allocate on every call, on paths that are free now — `HttpHeaderValueFormatter` formats every Boolean header value this way, and `ToString()` and the validation message formatter both go through `ToCanonicalString`.

Single-source the literals through `private const string` fields so the two APIs cannot drift: `ToCanonicalString` returns the constant, and the span methods write it through a small ASCII writer built on the shared `TCodeUnit` conversion helper, which serves both encodings from the same constant. Do not introduce a separate `u8` literal for the UTF-8 path; a second literal is a second source of truth.

Apply the same inversion to the private `FormatXxx` helpers behind the `TryGetXxx` validators: format into a stack buffer and compare with `MemoryExtensions.SequenceEqual` instead of allocating a string per comparison. `TryGetInt64` compares against `value.ToString(CultureInfo.InvariantCulture)` inline and needs the same treatment.

### Serializer adoption

The rule is that the writer performs any UTF-16 transcoding itself; the serializers only hand it UTF-8 for kinds whose canonical text is ASCII, where the two encodings cannot disagree.

- `MetadataExtensions.WriteNumberValue` writes the `Double` and `Single` canonical text as UTF-8 through `WriteRawValue(ReadOnlySpan<byte>, skipInputValidation: true)`, which closes the integration deferred by #61. These are ASCII, so this is safe.
- `MetadataExtensions.WriteMetadataValue` writes the ASCII-bounded string-shaped kinds — the date kinds, `TimeSpan`, `Guid`, `UInt64` — from a stack buffer, in either encoding.
- `String` and `Uri` keep their current path and are passed to the writer as the stored string. They already own one, so nothing is allocated, and the writer's own transcoding is what produces today's bytes.
- `Char` is written through the UTF-16 span overload, not the UTF-8 one. It is a single code unit that may be an unpaired surrogate, so the writer must be the one to transcode it.
- `JsonCloudEventsExtensions.WriteStringAttribute` keeps its shape and switches to the constant. It stays on the UTF-16 path, which is what it already uses; CloudEvents extension attributes additionally reject unpaired surrogates upstream of it, so the divergence above cannot arise there.
- `HttpHeaderValueFormatter` is unchanged: it returns `StringValues` and needs strings regardless.

### Benchmarks

The existing write benchmarks cannot demonstrate anything as they stand: both fixtures build their metadata exclusively from `MetadataValue.FromString`, and the `String` kind returns the stored reference before and after this change. Measured unmodified, the before/after delta is identically zero. Extend the fixtures first, then capture the figures.

- **CloudEvents.** Add `Guid`, `DateTime`, `Double`, and an `Int64` outside the inclusive 32-bit signed range, annotated `SerializeInCloudEventsExtensionAttributes`. The range qualifier is load-bearing: an in-range `Int64` takes the `Integer` attribute encoding and is written with `WriteNumberValue`, never touching canonical text, so only the out-of-range value routes to `WriteStringAttribute` — the method this issue fixes.
- **HTTP.** Add the same kinds annotated `SerializeInHttpResponseBody`. Do not try to show the improvement through `SerializeInHttpHeader`: `HttpHeaderValueFormatter` returns `StringValues` and needs a materialized string regardless, so the header path keeps its allocation by design and would flatline.

### Testing

Test the canonical text through `MetadataValue` (sociable), and reach `CanonicalTextFormatter` directly only for inputs no `MetadataValue` can carry. Derive expectations from the framework `ToString` calls the current implementation uses, so the tests state that the encoding is unchanged rather than restating the new implementation; assert the UTF-8 output against the ASCII bytes of those same expectations, following #61's precedent. Cover per kind: minimum and maximum values, zero and negative zero where representable, the scale-bearing decimal cases, `TimeSpan.MinValue`/`MaxValue`/`Zero` and the component-omission combinations, sub-second-tick presence and absence for the date kinds, and both `DateTimeKind` values `FromDateTime` can store. Reach `CanonicalTextFormatter` directly for the `Local` kind, which no `MetadataValue` can carry, and assert there that an `Unspecified` value is *not* shifted — the regression an unconditional `ToUniversalTime` would introduce. For the text-bearing kinds add non-ASCII text, a surrogate pair, and an unpaired surrogate in both encodings, asserting the replacement-fallback result rather than a round trip. Pair that with a writer-level test asserting the emitted JSON bytes are unchanged for those inputs under **both** the default and the `UnsafeRelaxedJsonEscaping` encoder — the default encoder escapes all non-ASCII and hides the divergence that motivated the adoption rules, so a single-encoder test proves nothing here.

Pin the `char` overload resolution with a test that calls `CanonicalTextFormatter.TryFormat` with a `char` literal and asserts the character, not its code point — the failure this guards against is a binding change, so the test must pass a `char` typed argument rather than a variable already narrowed elsewhere.

Assert the absence of allocations with `GC.GetAllocatedBytesForCurrentThread` around a warmed-up loop over one value of each affected kind in each encoding, following the precedent in the floating-point formatter tests. Cover `ToCanonicalString` for `Null`, `Boolean`, `String`, and `Uri` in the same way: those four are allocation-free today, so the assertion guards a regression the inversion could otherwise introduce silently, since the returned text would still be correct.

Be aware of what the warm-up hides: it triggers type initialization and any lazy setup before measurement begins, so a one-time allocation on first use is invisible to these tests. One-time initialization must therefore be allocation-free by construction, not merely amortized — the assertion cannot enforce it.

Extend the same assertions to the `TryGetXxx` validator paths, which the formatting-API tests do not reach. Those paths run only when the value is a `String` kind being parsed — `TryGetGuid` on a `Guid`-kind value returns early without formatting anything — so the test must build `String`-kind values holding canonical text and call `TryGetGuid`, `TryGetDateTime`, `TryGetInt64`, and the rest against them. That is where the per-candidate string allocation lives today.

Every new method carries an `out` parameter, which puts it in Stryker's Safe Mode blind spot documented in `tests/AGENTS.md`: the mutants fail to compile and the enclosing methods receive no mutation coverage at all. Mutation score therefore carries no information about this change. Argue adequacy by hand in the pull request, naming the behavior each formatter promises and the test that constrains it.

### Scope

Deferred to follow-up issues:

- Whether the `net10.0` asset should call the framework span formatters for any type. This plan measures it and records the numbers; it does not act on them.
- `HttpHeaderValueFormatter` stays as it is: it returns `StringValues` and needs strings regardless.
