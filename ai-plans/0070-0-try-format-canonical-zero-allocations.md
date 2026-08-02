# Allocation-Free Canonical Formatting for MetadataValue

## Rationale

`MetadataValue.TryFormatCanonical` is genuinely span-based only for `Double` and `Single`; every other kind calls `ToCanonicalString()` and copies the result. `Null`, `Boolean`, `String`, and `Uri` already return a literal or stored reference, but the other ten kinds allocate. This affects `JsonCloudEventsExtensions.WriteStringAttribute`, which still creates a throwaway string for values such as `Guid`, `DateTime`, and `Int64`, and the nine `TryGetXxx` validators that reformat candidates (`Int64`, `UInt64`, `Single`, four date/time kinds, `TimeSpan`, and `Guid`). `TryGetDecimal`, `TryGetChar`, and `TryGetUri` do not reformat and are unaffected.

The serializers ultimately write UTF-8. `CanonicalFloatingPointFormatter` already renders either encoding from one generic implementation, while #61 deferred its serializer integration: `MetadataExtensions.WriteNumberValue` still creates a `Double` string that System.Text.Json transcodes back to ASCII. The remaining non-text formats are also ASCII; only `String`, `Char`, and `Uri` require transcoding.

Add allocation-free canonical formatters for both encodings, expose them through `MetadataValue`, make them the source for `ToCanonicalString` and validation, and adopt them in the JSON and CloudEvents writers.

## Acceptance Criteria

- [x] `MetadataValue` exposes `TryFormatCanonical` and `TryFormatCanonicalUtf8`; neither allocates for any primitive kind, including copying or transcoding `String`, `Char`, and `Uri` into the caller's destination.
- [x] One canonical renderer serves both package assets and encodings. Only decimal field extraction and UTF-16-to-UTF-8 transcoding vary by target, and neither decides the output text. There is no target-specific formatter, renderer, cross-encoding route, or intermediate output buffer; every method writes directly to the requested destination. One expected-output corpus passes unchanged against both assets.
- [x] A public, XML-documented `CanonicalTextFormatter` exposes the new primitives in both encodings and per-type maximum lengths bounding both encodings on both assets.
- [x] `MetadataValue` exposes a documented public bound for every bounded primitive in either encoding; `JsonCloudEventsExtensions` uses it instead of a private magic number.
- [x] UTF-16 output exactly matches the previous `ToCanonicalString` text and count. UTF-8 output is that text's replacement-fallback encoding—lossy only for malformed UTF-16—with equal byte/character counts whenever the canonical text is ASCII.
- [x] In both encodings, insufficient capacity returns `false`, reports zero, and leaves the destination unchanged; `Array` and `Object` still throw `InvalidOperationException`; corrupt `DateTime`, `DateOnly`, and `TimeOnly` payloads still throw `InvalidOperationException`.
- [x] `CanonicalTextFormatter.TryFormat(DateTime, …)` normalizes `Local` to UTC like `MetadataValue.FromDateTime`, so all accepted values fit `MaximumDateTimeLength`; `Utc` and `Unspecified` are unchanged. A direct test covers this.
- [x] `TryGetXxx` round-trip validators compare span-formatted text without allocating a candidate string.
- [x] `ToCanonicalString` remains allocation-free for `Null`, `Boolean`, `String`, and `Uri`, and remains textually unchanged for every kind.
- [x] The JSON metadata writer materializes no canonical string for string-shaped or `Double`/`Single` number-shaped values. Its bytes remain unchanged under the default and `UnsafeRelaxedJsonEscaping` encoders, including non-ASCII text and unpaired surrogates.
- [x] Tests cover every kind and encoding: canonical output/count, exact and one-short capacity, boundaries, invalid-payload exceptions, and allocations. Metadata tests pass against the `net10.0` and `netstandard2.0` library assets.
- [x] A `net10.0` microbenchmark compares the new `Guid` and `DateTime` formatters with framework `TryFormat`; results are recorded in the pull request as evidence for a deferred follow-up, with no target-specific implementation added here regardless of outcome.
- [x] CloudEvents and HTTP write benchmarks contain affected metadata kinds; before/after allocations for both are recorded in the pull request.
- [x] `THIRD-PARTY-NOTICES.md` and the folder README identify all adapted upstream files and adaptations; every adapted source retains the .NET Foundation MIT header.
- [x] The `netstandard2.0` decimal path documents and allocation-freely enforces its runtime-layout assumption. A violation throws `PlatformNotSupportedException` from decimal formatting itself without disabling other formatters.
- [x] Package release notes mention the new APIs and removed allocations.
- [x] Both targets build in Release with warnings as errors, package validation succeeds, the Native AOT sample publishes, and coverage remains above 95%.

## Technical Details

### Architecture and upstream sources

`netstandard2.0` has no affected span-formatting APIs; `Polyfill` extensions call `ToString` and copy, and `IUtf8SpanFormattable` is unavailable. Ship one owned implementation for both assets and encodings, with no `NET10_0_OR_GREATER` branch to framework formatting. This avoids the asset wire-format divergence fixed for floating point in #58 and keeps one implementation and test surface.

Framework fast paths remain a follow-up informed by the required benchmark. `Guid.TryFormat` has a vectorized `D` path and is plausible; the date kinds are less likely because their canonical custom pattern enters the general format interpreter instead of fixed-shape `TryFormatO`. Do not branch in this issue even if a framework API wins.

Adapt—not rederive—the scalar implementations from the repository's existing `dotnet/runtime` baseline, tag `v6.0.36`, commit `f1dd57165bfd91875761329ac3a8b17f6606ad18`. This line predates the `System.Runtime.Intrinsics` rewrites and compiles for `netstandard2.0`; newer sources would require de-vectorization. `Numbers/` already establishes the pattern: adapted upstream code under the .NET Foundation MIT header, with provenance in `THIRD-PARTY-NOTICES.md` and a folder README. Rederiving carries the real risk—the `TryGetXxx` validators pin these encodings, so a subtly wrong hand-derived rule breaks round-tripping of data already on the wire.

| Kind | Upstream source and adaptation |
| --- | --- |
| `Int64`, `UInt64` | `Number.Formatting.cs`: `TryUInt64ToDecStr`, `UInt32ToDecChars`, `Int64DivMod1E9`; `FormattingHelpers.CountDigits`. Direct scalar port over a fixed destination. |
| `Decimal` | `Number.Formatting.cs`: `DecimalToNumber`; `Decimal.DecCalc.cs`: `DecDivMod1E9`. Reuse the existing `NumberBuffer`; add a scale-preserving renderer. |
| Date/time kinds | `DateTimeFormat.cs`: `TryFormatO`, `WriteTwoDecimalDigits`, `WriteFourDecimalDigits`, `WriteDigits`. Port `TryFormatO`, not `FormatCustomized`. |
| `TimeSpan` | `System.Private.Xml/XsdDuration.cs`: `TimeSpan` constructor and `ToString(DurationType)`, the normative implementation of `XmlConvert.ToString(TimeSpan)`. |
| `Guid` | `Guid.cs`: `TryFormat`'s `D` branch, `HexsToChars`, `HexConverter.ToCharLower`. Numeric-field formatting is endianness-independent. |

`FormatCustomized` brings culture, calendars, Hebrew/Japanese cases, and `StringBuilderCache`; `TryFormatO` is a small culture-free fixed template. Adapt it to omit a zero fraction and otherwise trim trailing zeros; replace internal `GetDate`/`GetTimePrecise` with ported helpers or public component properties (accepting their repeated tick calculations); drop its local-offset branch for `DateTime` because local values are normalized, while retaining offsets for `DateTimeOffset`.

All upstream renderers target `char`; route ASCII through the repository's `TCodeUnit` conversion pattern so one body writes either encoding.

### Decimal extraction

`decimal.GetBits(decimal, Span<int>)` is unavailable on `netstandard2.0`; its array overload allocates. Upstream sidesteps this with `Unsafe.As<decimal, DecCalc>`, but that guarantee is weaker than it looks: `GetBits` documents the *logical* representation, not the private field order, and upstream `DecCalc` carries an explicit `#if BIGENDIAN` layout—endianness is part of the assumption rather than incidental to it. Split only extraction:

- `net10.0` uses the contractual, allocation-free, endian-independent span overload.
- `netstandard2.0` reinterprets the value, documented as requiring a little-endian runtime with historical `flags`, `hi`, `lo`, `mid` field order. Digit generation and rendering remain shared.

Validate the legacy assumption once without calling the allocating array overload. Construct

```csharp
var probe = new decimal(
    lo: 0x11111111,
    mid: 0x22222222,
    hi: 0x33333333,
    isNegative: true,
    scale: 5
);
```

then reinterpret and compare the fields with those arguments and flags `0x80050000`. The constructor defines the same logical representation as `GetBits`; this uses only a struct and integers.

Keep the guard in a decimal-only private helper, store its result in `static readonly bool`, and throw `PlatformNotSupportedException` from decimal formatting—not a type initializer, which would wrap it in `TypeInitializationException` and disable unrelated formatters. The initialized flag folds away. A unit test should compare reinterpretation with `decimal.GetBits`, but both assets run on the .NET 10 host, so only the runtime guard covers .NET Framework or Mono. Per `tests/AGENTS.md`, mutants in the initializer are a known static-analysis blind spot. Warmed allocation tests also miss initialization, so first use must be allocation-free by construction.

### Canonical contracts

- **Decimal:** preserve scale (`19.50m` → `19.50`); omit the sign of negative zero, matching the framework.
- **TimeSpan:** zero is `PT0S`; `-` precedes `P`; omit zero days; include `T` only when a time component follows; trim fractional zeros (`P2DT3H4M5.06S`, `PT0.0000001S`). Preserve upstream's `unchecked((ulong)-ticks)` handling of `TimeSpan.MinValue`.
- **Date/time:** `DateTime` UTC ends in `Z`, Unspecified has no designator, matching `yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK`; `DateTimeOffset` uses `+hh:mm`/`-hh:mm`, never `Z`. Date/time fractions are omitted at zero and otherwise trimmed.
- **Guid:** lowercase `D` format.

For `DateTime`, conditionally call `ToUniversalTime()` only for `Kind.Local` and render `Z`, matching `FromDateTime`. Document that this input depends on the machine's local time zone. Never normalize Unspecified: `ToUniversalTime()` would treat it as local and shift it. Conversion saturates at `DateTime.MinValue`/`MaxValue`, so it adds no exception. Normalization also keeps the maximum at 28 rather than the 33 characters required by a local `±hh:mm` form.

`FromDateTime` already normalizes Local and `TryReadStoredDateTime` rejects a stored Local payload, so only direct formatter tests can reach this branch. Assert normalized text/count/bound plus an Unspecified value that is not shifted.

### Public API and failure behavior

```csharp
namespace Light.PortableResults.Text;

public static class CanonicalTextFormatter
{
    public const int MaximumInt64Length = 20;
    public const int MaximumUInt64Length = 20;
    public const int MaximumDecimalLength = 31;
    public const int MaximumCharLength = 3;

    public const int MaximumDayNumber = 3_652_058;             // DateOnly.MaxValue.DayNumber
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

    // One Span<byte>/bytesWritten TryFormatUtf8 counterpart for every overload above.
    public static bool TryFormatUtf8(
        ReadOnlySpan<char> text,
        Span<byte> destination,
        out int bytesWritten
    );
}
```

These signatures are exact. The `char` overload prevents a char literal from binding to `long` (or, if integer methods were renamed, `decimal`) and rendering `'x'` as `120`. Other integral types may correctly bind to `long`. Test overload resolution with a char literal.

`false` means only insufficient capacity. `TryFormatDate` accepts `[0, MaximumDayNumber]`; `TryFormatTime` accepts `[0, MaximumTimeOfDayTicks]`; invalid values throw `ArgumentOutOfRangeException`, consistent with the floating formatter reserving `false` for capacity and throwing for non-finite input. `MetadataValue` pre-validates with the same public constants to retain its existing `InvalidOperationException` and message; test the formatter's otherwise-unreachable range exceptions directly.

`MaximumCharLength` is 3 because the constants bound both encodings: one BMP code unit, including an unpaired surrogate replaced by U+FFFD, needs at most three UTF-8 bytes. The UTF-16 method writes one. `TryFormatDate` and `TryFormatTime` accept stored payloads because `DateOnly`/`TimeOnly` BCL types are absent from `netstandard2.0`. The text-transcoding overload needs no UTF-16 peer because chars use `TryCopyTo`.

Keep `CanonicalFloatingPointFormatter` and its API/constants in place. Add to `MetadataValue`:

```csharp
public const int MaximumPrimitiveCanonicalLength = 36;
public bool TryFormatCanonicalUtf8(Span<byte> destination, out int bytesWritten);
```

The bound is `Guid`'s length and covers both encodings. Document that it excludes unbounded `String` and `Uri`; only they may still outgrow this buffer. `Char` remains bounded at three. CloudEvents therefore retains its materializing fallback.

### Rendering, transcoding, and single sourcing

Follow `CanonicalFloatingPointFormatter.TryRender`: private generic cores constrained to `unmanaged`, a shared internal conversion helper, and JIT-foldable `typeof(TCodeUnit) == typeof(byte)` ASCII writes. Do not expose the helper, duplicate it, or guard impossible non-`char`/`byte` instantiations (an unreachable throw is a coverage hole). Calculate required length before writing to preserve all-or-nothing behavior.

`String`, `Char`, and `Uri` instead transcode with replacement fallback: unpaired surrogates accepted by `FromChar`/`FromString` become U+FFFD. This is intentionally lossy because malformed UTF-16 has no valid UTF-8 representation; `false` must still mean only insufficient capacity. Counts vary for non-ASCII/malformed text, but match for ASCII; UTF-8 round-trip identity is not promised.

Use `Utf8.FromUtf16(replaceInvalidSequences: true)` on `net10.0`. On `netstandard2.0`, use `Encoding`'s pointer overload under `fixed`, whose default fallback also emits U+FFFD. Both paths count bytes before writing, requiring a second pass but preventing partial output.

The serializers must not use pre-transcoded UTF-8 for these three kinds. With System.Text.Json 10.0.10 and `UnsafeRelaxedJsonEscaping`, malformed input differs bytewise:

```text
input "a\uD800b"
char route: 22-61-5C-75-46-46-46-44-62-22
UTF-8 route: 22-61-EF-BF-BD-62-22
```

Both parse equivalently, and both routes converge for valid U+FFFD; the default encoder also hides the difference by escaping non-ASCII. Preserve bytes by letting the writer transcode text-bearing kinds.

Make span formatting primary. `ToCanonicalString` stack-formats bounded values and creates one string from the written slice, but retains allocation-free fast paths: literals for `Null`/`Boolean`, stored references for `String`/`Uri`. Share literals as `private const string`; both span encodings write the same constant through the ASCII helper—do not add separate `u8` literals.

Likewise, validator `FormatXxx` helpers stack-format and compare with `MemoryExtensions.SequenceEqual`; replace `TryGetInt64`'s inline invariant `ToString` too.

### Serializer adoption

The rule: the writer performs any UTF-16 transcoding itself; serializers hand it UTF-8 only for kinds whose canonical text is ASCII, where the two encodings cannot disagree.

- `MetadataExtensions.WriteNumberValue` stack-formats `Double`/`Single` as UTF-8 and calls `WriteRawValue(ReadOnlySpan<byte>, skipInputValidation: true)`, completing #61.
- `WriteMetadataValue` stack-writes ASCII-bounded string shapes (date kinds, `TimeSpan`, `Guid`, `UInt64`) in either encoding.
- `String`/`Uri` pass their stored string to the writer; `Char` uses UTF-16 so the writer owns surrogate handling.
- `JsonCloudEventsExtensions.WriteStringAttribute` remains UTF-16, switches its stack size to the public constant, and retains fallback. CloudEvents rejects unpaired surrogates upstream.
- `HttpHeaderValueFormatter` remains unchanged because returning `StringValues` inherently requires strings.

### Benchmarks and tests

Current write fixtures contain only `FromString`, so their allocation delta is necessarily zero. Add `Guid`, `DateTime`, `Double`, and an `Int64` outside the inclusive 32-bit range:

- CloudEvents: annotate with `SerializeInCloudEventsExtensionAttributes`; the range matters because an in-range `Int64` uses numeric `Integer` encoding and bypasses `WriteStringAttribute`.
- HTTP: annotate with `SerializeInHttpResponseBody`; do not claim a header improvement, because `StringValues` still materializes text.

Test canonical behavior sociably through `MetadataValue`, using direct `CanonicalTextFormatter` tests only for inaccessible inputs such as Local `DateTime` and invalid raw date/time ranges. Derive expected text from the same framework `ToString` calls used before this change; derive UTF-8 expectations from those strings (ASCII where applicable), as in #61.

Cover each kind's minimum/maximum, zero/negative zero, decimal scale, `TimeSpan.MinValue`/`MaxValue`/`Zero` and component omission, fractions present/absent for date/time values, and both storable `DateTimeKind` values. For text kinds cover non-ASCII, surrogate pairs, and unpaired surrogates in both encodings. At writer level assert exact bytes with both JSON encoders; default-only coverage is insufficient because it hides the malformed-input divergence.

Measure allocations with `GC.GetAllocatedBytesForCurrentThread` around warmed loops for every affected kind and encoding, plus the four allocation-free `ToCanonicalString` kinds. Extend this to validators using `String`-kind canonical input; typed values return early and would miss the old allocation. Warm-up hides lazy initialization, hence the allocation-free-construction requirement above.

All new methods have `out` parameters and therefore fall into Stryker Safe Mode's documented compile-failure blind spot. Mutation score cannot assess them; the pull request must manually map each formatter contract to its constraining tests.

Framework target-specific calls and any resulting optimization remain deferred. Provenance, release notes, benchmark results, build/package/AOT validation, and coverage are required by the acceptance criteria above.
