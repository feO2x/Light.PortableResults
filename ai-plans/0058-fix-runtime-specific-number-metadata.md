# Runtime-Independent Canonical Floating-Point Text

## Rationale

`MetadataValue` derives the canonical text of `Double` and `Single` from `ToString("R", CultureInfo.InvariantCulture)`. The vocabulary table of `0055` specifies that text as "the shortest round-trippable number", but `"R"` only produces that on .NET Core 3.0+. The `netstandard2.0` asset also loads on .NET Framework and Mono, where `"R"` means "15 significant digits, else 17" and carries a documented round-trip defect. The two definitions disagree for roughly half of all computed doubles (97,431 of 200,000 random values need exactly 16 significant digits, where the legacy implementation emits 17), and they disagree for `-0.0`, which legacy hosts render without a sign, and for values at and above `1e16`, which legacy hosts render in scientific notation. The same text feeds JSON bodies, HTTP headers, CloudEvents attributes, and validation messages, so a value crossing between a .NET 10 service and a .NET Framework service is written differently on each side — and `TryGetSingle`, which validates a wire-degraded string by re-formatting it, rejects text the other runtime wrote.

The runtime's own implementation is ordinary managed integer arithmetic — Grisu3 with a Dragon4 fallback, no SIMD, no intrinsics, no P/Invoke — and it is MIT licensed. This plan ports it, uses it on the `netstandard2.0` asset, and keeps `"R"` on `net10.0`, with a differential test that pins the two implementations to each other. Because the ported code never calls a floating-point routine of its host, that test running on .NET 10 is a valid proof for every runtime that loads the `netstandard2.0` asset.

## Acceptance Criteria

- [ ] Named scenario tests document every rule of the encoding, one case per rule: the notation boundaries in both directions for both types (`1e-5`/`1e-4`, `1e16`/`1e17`, `1e8f`/`1e9f`), negative zero, subnormals, `Epsilon`, `MaxValue`, `MinValue`, the boundary tie at `1e23`, the half-to-even digit tie, and values whose shortest form needs 15, 16, and 17 significant digits.
- [ ] `ShortestRoundTripFormatter.Format(double)` and `Format(float)` return text byte-identical to `ToString("R", CultureInfo.InvariantCulture)` on `net10.0` across a deterministic corpus of at least 50,000 random bit patterns per type plus a sweep over every binary exponent, and that corpus adds less than a second to the test run.
- [ ] The same corpus passes with the Grisu3 stage bypassed, so the Dragon4 fallback is verified independently of the fast path that normally hides it.
- [ ] An opt-in test excluded from the default run verifies all 2^32 single-precision bit patterns against `"R"`.
- [ ] The formatter contains no call to `ToString`, `TryFormat`, or `Parse` of `double` or `float`: its result is a function of the input bits alone.
- [ ] `TryFormat` writes into a caller-supplied `Span<char>` without allocating, and `Format` allocates only the returned string, asserted with `GC.GetAllocatedBytesForCurrentThread`.
- [ ] Canonical text for a `Double` or `Single` allocates exactly one string on both targets, including the whole-number case that appends the trailing `.0`, and `MetadataValue.TryFormatCanonical` writes both kinds into the destination without allocating at all.
- [ ] Canonical text for `MetadataKind.Double` and `MetadataKind.Single` comes from the formatter on the `netstandard2.0` asset and from `"R"` on `net10.0`; every existing serialization, header, CloudEvents, and validation-message expectation passes unchanged on `net10.0`.
- [ ] The notation boundaries are pinned at the metadata level, where the trailing `.0` rule and the notation rule interact: `1e16` serializes as `10000000000000000.0` and `1e17` as `1E+17`, with the equivalent pair for `Single`.
- [ ] The ported code is compiled into both assets, and the only public API difference between the targets remains the `DateOnly`/`TimeOnly` members introduced by `0055`.
- [ ] Both targets build in Release with warnings as errors.
- [ ] `src/Light.PortableResults/Numbers/README.md` records the upstream repository, branch, and commit SHA the files were taken from, plus every adaptation applied to them; the ported files keep their .NET Foundation MIT header and are named in a `THIRD-PARTY-NOTICES.md` at the repository root.
- [ ] The canonical encoding of `Double` and `Single` is documented normatively — digit selection, both tie rules, notation thresholds, negative zero — so that `"R"` is an implementation that satisfies the specification rather than the definition of it.
- [ ] A BenchmarkDotNet benchmark reports per-value time and allocations of `"R"` and of the ported formatter, for random and short-form doubles and singles.
- [ ] Test code coverage stays above 95%.
- [ ] The package release notes record that canonical `Double` and `Single` text changes on .NET Framework and Mono hosts and is unchanged on .NET Core 3.0+ hosts.

## Technical Details

### The canonical encoding, stated normatively

The encoding stops being "whatever `"R"` does" and becomes a specification that `"R"` happens to satisfy:

- **Digits.** The shortest decimal digit string that parses back to the same value under round-to-nearest-even. When several strings of that length round-trip, the one nearest the exact value; an exact tie between two such candidates resolves to the even final digit.
- **Boundaries.** A candidate lying exactly on a rounding boundary round-trips only when the value's mantissa is even. This is why `1e23` renders as `1E+23` rather than `9.999999999999999E+22`.
- **Notation.** With the value written as `0.d₁d₂… × 10^s`, positional while `-3 < s <= 17` for `double` and `-3 < s <= 9` for `single`; otherwise scientific with an uppercase `E`, an explicit sign, and at least two exponent digits.
- **Negative zero** renders as `-0`.

The trailing `.0` marker from `0055` stays where it is, applied by `MetadataValue` after the formatter returns. It is a metadata-level rule that keeps a whole-number token from reading back as `Int64`, not part of shortest round-trip text, and keeping it out of the formatter is what lets the differential test compare against `"R"` directly. No .NET format specifier combines shortest round-trip digits with a guaranteed fractional digit — `F*` is lossy for anything not already short, and custom formats cap at fifteen significant digits and never switch to scientific notation — so the marker remains a post-processing step over finished text.

The marker's own behaviour is runtime-dependent today, through its input rather than its rule: `1e16` reaches it as `10000000000000000` on .NET 10 and as `1E+16` on .NET Framework, so the same value acquires a fractional digit on one host and an exponent on the other. Fixing the digits fixes the marker with them.

Both tie rules were found by running a corpus against `"R"`, not by reading the algorithm. They are the two places where a hand-written implementation silently produces plausible, wrong output, which is why the corpus is an acceptance criterion rather than a convenience.

### Dispatch

`MetadataValue.FormatDouble` and `FormatSingle` format into a stack buffer, apply the marker in that same buffer, and materialize one string at the end. The `#if NETSTANDARD2_0` gate sits on the digit-producing call alone — `ShortestRoundTripFormatter.TryFormat` on one side, `double.TryFormat(buffer, out written, "R", CultureInfo.InvariantCulture)` on the other. Illustrative:

```csharp
Span<char> buffer = stackalloc char[MaxCanonicalLength];
var written = FormatShortest(value, buffer);
written = AppendFloatingPointMarker(buffer, written);
return buffer.Slice(0, written).ToString();
```

This removes the second allocation that `EnsureFloatingPointMarker` incurs today whenever it triggers, which is every whole number in positional notation — a common shape for a metadata boundary. It is a strict improvement: when the marker does not trigger, the single string is the one that was allocated before. The buffer bound is provable from the seventeen-digit maximum: 32 characters for `double`, 24 for `float`.

`TryGetSingle`'s round-trip check is fixed as a consequence of formatting being fixed, with no change at that call site.

`Span<char>.ToString()` on `netstandard2.0` comes from System.Memory's override rather than the BCL. It carries the `char` special case, but `new string(char*, int, int)` is the fallback if it disappoints, and unsafe code is enabled for this project anyway.

A runtime capability probe was considered and rejected for now. It would let a .NET 8 or 9 consumer — which resolves the `netstandard2.0` asset, because `net10.0` is not compatible with it — keep its own `"R"`. With Grisu3 included the managed path runs the same algorithms as the runtime, so the gap it would close is small, and the probe adds a canary that can misjudge an unforeseen host. It stays available as a later optimization if the benchmark shows it is warranted.

### The port

Base the port on `dotnet/runtime`, branch `release/6.0`: it is the last branch before generic math, so the entry points are the concrete `Grisu3.TryRunDouble`/`TryRunSingle` and `Number.Dragon4Double`/`Dragon4Single` rather than methods constrained on `IBinaryFloatParseAndFormatInfo<T>`, whose static abstract interface members cannot compile for `netstandard2.0`. Take `Number.Grisu3.cs` (~1,050 lines), `Number.Dragon4.cs` (~520), the digit-generation members of `Number.BigInteger.cs` (~1,050 including the pow-10 tables), and a trimmed `NumberBuffer` carrying ASCII digits, scale, and sign.

Adaptations, all of which belong in the folder README:

- Drop the `Half` overloads and the parse-only members of the bignum (`ToUInt64`, `ToUInt128`, `DivRem`).
- `Span<T>` and `Unsafe` need no new package references: `System.Memory` and `System.Runtime.CompilerServices.Unsafe` already resolve for `netstandard2.0` through System.Text.Json.
- Provide `BitOperations.Log2`/`LeadingZeroCount` and the tuple-returning `Math.DivRem` as small internal helpers.
- Enable `AllowUnsafeBlocks` for the core project. The bignum stores its blocks in a `fixed uint[]` buffer, and `[InlineArray]` does not exist on `netstandard2.0`; threading a caller-allocated `Span<uint>` through every member instead would restructure the whole file. The long-term risk in a port this size is divergence from upstream, so faithfulness beats style here, and the unsafe code is confined to this folder and compatible with Native AOT.

Keep the upstream file names, keep the diff to upstream as small as the adaptations allow, and record the source commit so the port can be re-synced. The ported files will not match the repository's formatting conventions; suppress rather than reformat.

The rendering layer is **not** ported. Upstream's `FormatGeneral` is entangled with `NumberFormatInfo`; the digits-and-scale to text step described above is invariant-only, roughly fifty lines, and is ours.

### Public API

Exact signatures:

```csharp
namespace Light.PortableResults.Numbers;

public static class ShortestRoundTripFormatter
{
    public static string Format(double value);
    public static string Format(float value);
    public static bool TryFormat(double value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(float value, Span<char> destination, out int charsWritten);
}
```

The formatter is public because a caller writing a non-JSON serializer needs the same answer, and because it gives the tests a direct target on `net10.0` without a seam. The ported types stay internal: they are foreign code whose shape belongs to upstream, and making them public would freeze another project's implementation details into this library's API.

`TryFormat` is the primary shape: Grisu3 and Dragon4 write digits into a caller-supplied buffer natively, so `Format` is the wrapper rather than the other way round. It is what makes single-allocation canonical text possible on both targets, and it is what the deferred allocation-free `TryFormatCanonical` will consume for the remaining kinds.

### Testing

The differential corpus is the whole argument of this plan: because the ported code touches no host floating-point routine, matching `"R"` on .NET 10 establishes the behaviour on .NET Framework, which CI cannot execute. Use a deterministic generator so a failure is reproducible, and cover the bit space rather than the value space — random doubles cluster at 16 and 17 significant digits, so a corpus of realistic values would never reach the paths that matter.

The tiers divide the labour. Named scenarios carry the specification and the diagnostics: each one states a rule, and a failure identifies itself. The corpus carries what named cases cannot, because the risk here is transcription rather than algorithm — the pow-10 tables, the loop bounds, and the substituted `BitOperations` and `Math.DivRem` helpers fail on sparse, unpredictable subsets of inputs that nobody can enumerate in advance. Both tie rules in the reference prototype were discovered this way, at observed rates near one in 2,000 and one in 4,000 values, after a curated edge list had passed; 50,000 values per type therefore detects that class with certainty at a cost of tens of milliseconds. The exponent sweep is the deterministic complement, because a defect confined to one decade of exponents is the case random sampling covers worst.

Bypassing Grisu3 for a second corpus run matters because Grisu3 answers the overwhelming majority of inputs; without it, Dragon4 would be exercised only by the handful of values that defeat the fast path, and a defect in the backstop could ship unnoticed. Exhaustive single-precision verification is cheap enough to be worth having as an opt-in test (xUnit v3 `Explicit = true`), and it covers the shared bignum and Dragon4 code that double-precision formatting also depends on.

The corpus is expected to cover the ported code well enough for the 95% threshold. If upstream branches turn out to be unreachable after the `Half` and parse-only members are dropped, exclude those files in `coverage.runsettings` rather than writing tests for dead code, and say so in the folder README.

### Affected components

| Site | Change |
| --- | --- |
| `Numbers/ShortestRoundTripFormatter.cs`, ported Grisu3/Dragon4/bignum/`NumberBuffer` | new |
| `MetadataValue.FormatDouble`, `FormatSingle` | `#if`-gated dispatch |
| `Light.PortableResults.csproj` | `AllowUnsafeBlocks`, release notes |
| `benchmarks/Benchmarks` | formatter comparison benchmark |
| `THIRD-PARTY-NOTICES.md`, `Numbers/README.md` | new |

### Out of scope

- **Parsing.** `Utf8JsonReader.GetDouble` uses System.Text.Json's parser, which is not IEEE-correct on the `netstandard2.0` asset, and `TryGetSingle` uses `float.TryParse`. Fixing that needs a correctly-rounded decimal-to-binary parser (`Number.NumberToFloatingPointBits.cs`), a comparable porting effort. The failure mode after this plan is a false negative from `TryGetSingle`, never a wrong value, because the accessor validates by re-formatting. The ported bignum would serve that work if it is ever taken on.
- **The allocation-free `TryFormatCanonical`** for the other fifteen kinds, which needs per-kind span formatting and a hand-rolled ISO 8601 duration writer — the latter on both targets, since `XmlConvert.ToString(TimeSpan)` has no span overload anywhere. `Double` and `Single` come off that list here, because span formatting is the shape the ported code already has. That work belongs after #51 and #53, which settle the canonical-text call sites and add the `Bytes` kind respectively, so the refactor covers the final vocabulary rather than being redone for it. Its subject is the JSON writer — `WriteStringValue` and `WriteRawValue` both take `ReadOnlySpan<char>` — because `TryFormatCanonical` itself has no production caller today.
- **The runtime probe**, as described under Dispatch.
- **`ValidatorOpenApiEmitter.ToLiteral`**, which also uses `"R"` and runs in the compiler host — .NET Framework under Visual Studio. Its effect is confined to the text of a generated literal: Roslyn parses literals with its own correctly-rounded reader, so the value, and therefore every runtime encoding derived from it, is identical either way.
