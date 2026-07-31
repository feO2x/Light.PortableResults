# Runtime-Independent Canonical Floating-Point Text

## Rationale

`MetadataValue` derives canonical `Double` and `Single` text from `ToString("R", CultureInfo.InvariantCulture)`. `"R"` returns the shortest round-trippable representation on .NET Core 3.0+, but the `netstandard2.0` asset can run on .NET Framework and Mono, where it uses a legacy algorithm with different digit selection, notation thresholds, negative-zero behavior, and a documented round-trip defect. The text feeds JSON bodies, HTTP headers, CloudEvents attributes, and validation messages, so equivalent metadata is not interoperable across those hosts.

Introduce one public `CanonicalFloatingPointFormatter`, used on every TFM, whose contract includes both shortest round-trippable digits and the trailing `.0` marker that preserves the floating-point JSON shape of positional whole numbers. Base its digit generation on the MIT-licensed .NET runtime Grisu3 implementation with its Dragon4 fallback, and keep its aggregate formatting cost within 25% of the equivalent modern runtime path.

## Acceptance Criteria

- [ ] `CanonicalFloatingPointFormatter` exposes the specified `Format` and `TryFormat` overloads for `double` and `float` on both package assets, and `MetadataValue` uses it for all `Double` and `Single` canonical text without target-framework dispatch.
- [ ] The formatter implements and documents one invariant encoding for finite values: shortest round-trippable digits, both rounding tie rules, notation thresholds, uppercase signed exponents with at least two digits, negative zero, and a trailing `.0` for positional whole numbers.
- [ ] Non-finite values are rejected with `ArgumentException`; an insufficient `TryFormat` destination returns `false`, writes zero to `charsWritten`, and performs no allocation.
- [ ] Named scenario tests cover each encoding rule, including both notation boundaries for both types, positional zero-padding when the decimal scale exceeds the coefficient length (`2^55` → `36028797018963970.0`, `123456789f` → `123456790.0`), `-0.0`, a non-minimal subnormal, `Epsilon`, `MaxValue`, `MinValue`, the boundary tie at `1e23`, a half-to-even final-digit tie, values requiring 15, 16, and 17 significant digits, and rejection of NaN and both infinities by every overload. Non-obvious cases record the exact IEEE bit pattern and expected text.
- [ ] A deterministic differential corpus of at least 50,000 finite random bit patterns per type, plus a sweep over every binary exponent, matches the .NET 10 `"R"` output after independently applying the canonical `.0` rule.
- [ ] The same differential corpus passes through a per-call Dragon4-only formatter path, independently verifying Dragon4 and the shared bignum; selecting that path changes neither process-wide state nor the public API.
- [ ] The formatter contains no call to `ToString`, `TryFormat`, or `Parse` on `double` or `float`; its production result does not depend on the host's floating-point formatter or parser.
- [ ] After warm-up, `TryFormat` and `MetadataValue.TryFormatCanonical` allocate nothing for either floating-point type, while `Format` and `MetadataValue.ToCanonicalString` allocate only the returned string, including values that require `.0`.
- [ ] Existing JSON, HTTP header, CloudEvents, validation-message, and `TryGetSingle` expectations pass unchanged on .NET 10; tests pin the corrected legacy-host representations at the metadata integration points.
- [ ] BenchmarkDotNet compares both public formatter shapes with equivalent .NET 10 baselines that use `"R"` span formatting plus the same in-buffer `.0` rule. Across the designated aggregate random-finite and common short-form workloads for both types, the canonical formatter is no more than 25% slower and introduces no additional allocations.
- [ ] Both target frameworks build in Release with warnings as errors, SDK package validation is enabled for `Light.PortableResults` and confirms the intended cross-target API compatibility during packing, and the Native AOT sample publishes successfully.
- [ ] `src/Light.PortableResults/Numbers/README.md` records the exact upstream repository, branch, immutable commit SHA, copied files, and every adaptation; copied files retain their .NET Foundation MIT headers and are named in the repository-root `THIRD-PARTY-NOTICES.md`.
- [ ] Test code coverage remains above 95%, and unused upstream members are trimmed rather than retained solely to be excluded from coverage.
- [ ] Package release notes state that canonical `Double` and `Single` text is now runtime-independent, changes on .NET Framework and legacy Mono hosts, and retains its existing output on .NET Core 3.0+ hosts.

## Technical Details

### Canonical encoding

The formatter accepts finite IEEE 754 binary values and produces the following invariant representation:

- **Coefficient digits:** the shortest decimal digit sequence `d₁d₂…dₙ` and decimal scale `s` for which `0.d₁d₂…dₙ × 10^s` parses to the same binary value under round-to-nearest-even. If several sequences of that length round-trip at that scale, choose the one nearest the exact value; an exact tie resolves to an even final digit.
- **Rounding boundaries:** a decimal candidate exactly on a binary rounding boundary belongs to the value only when its mantissa is even. This permits the one-digit representation of the `double` with bits `0x44B52D02C7E14AF6` as `1E+23`.
- **Notation and positional zero-padding:** use positional notation when `-3 <= s <= 17` for `double` and `-3 <= s <= 9` for `float`. If `s <= 0`, render `0.`, then `-s` zeroes, then the coefficient digits. If `0 < s < n`, place the decimal point after coefficient digit `s`. If `s >= n`, render the coefficient digits followed by `s - n` zeroes. Otherwise use scientific notation with the first coefficient digit before the decimal point, any remaining coefficient digits after it, uppercase `E`, an explicit sign, and the exponent `s - 1` padded to at least two digits.
- **Floating-point marker:** after notation is selected, append `.0` when the result contains neither a decimal point nor an exponent. Consequently, zero is `0.0`, negative zero is `-0.0`, `1e16` is `10000000000000000.0`, and `1e17` is `1E+17`; the equivalent `float` upper boundary is `1e8f`/`1e9f`.

The marker is part of this library's canonical floating-point contract rather than the shortest-digit algorithm. It prevents a positional whole-number token from being interpreted as `Int64` and deliberately differentiates the formatter from `"R"`.

### Public API and integration

The exact public surface is:

```csharp
namespace Light.PortableResults.Numbers;

public static class CanonicalFloatingPointFormatter
{
    public static string Format(double value);
    public static string Format(float value);
    public static bool TryFormat(double value, Span<char> destination, out int charsWritten);
    public static bool TryFormat(float value, Span<char> destination, out int charsWritten);
}
```

`TryFormat` is the primary implementation. It generates digits and renders the sign, notation, exponent, and optional marker directly into the caller's span. `Format` uses a bounded stack buffer and materializes the final string once; 32 characters for `double` and 24 for `float` are sufficient. Neither the formatter nor its compatibility helpers have target-specific behavioral branches: both assets compile and use the same implementation.

`MetadataValue.FormatDouble`, `FormatSingle`, and the floating-point arms of `TryFormatCanonical` delegate to this formatter. `MetadataPayload` widens a `Single` to `double` only as a lossless storage optimization; `MetadataKind.Single` retains the semantic precision. Formatting that stored value therefore casts it back to `float` and calls the `float` overload. Passing the widened value to the `double` overload would select binary64 rounding boundaries, digit limits, and notation thresholds and produce the wrong canonical text. The existing `TryGetSingle` canonical-string validation then acquires the corrected representation without its own changes.

Enable the .NET SDK's package validation for `Light.PortableResults`:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
```

Do not configure a baseline package because the library is pre-1.0 and permits breaking changes. Do not enable strict compatible-framework equality because the package intentionally exposes some APIs only on `net10.0`; ordinary compatible-framework validation must still confirm that the `netstandard2.0` contract remains compatible with the `net10.0` asset.

### Runtime port

Pin an immutable commit on `dotnet/runtime`'s `release/6.0` branch, the last pre-generic-math implementation with concrete `double` and `float` entry points. Port the required portions of:

- `Number.Grisu3.cs` and `Number.DiyFp.cs`;
- `Number.Dragon4.cs`;
- the digit-generation bignum and power-of-ten tables from `Number.BigInteger.cs`;
- `NumberBuffer`, IEEE bit extraction, and the small invariant rendering helpers needed by those algorithms.

Remove `Half`, parsing, and unused general-number-formatting members. Replace framework APIs unavailable on `netstandard2.0`, such as the required `BitOperations` and bit-conversion operations, with local helpers used by both target builds. Keep the upstream structure and naming where practical so the source remains comparable with its pinned origin.

The algorithms do not call a host formatter or parser. Grisu3 performs its digit work with managed integer arithmetic. Dragon4 also uses a floating-point calculation for a bounded decimal-exponent estimate, but exact integer comparisons correct that estimate and determine the emitted digits.

The bignum's fixed inline block buffer requires `AllowUnsafeBlocks`; unsafe code remains confined to the numbers implementation. Its fixed-size stack representation adds no allocation and is compatible with Native AOT. The folder README records this choice and all other deviations from upstream.

### Verification and performance

The differential oracle is defined only in test and benchmark code: format with invariant `"R"`, then append `.0` if the result contains neither a decimal point nor an exponent. Named tests remain the normative specification; the corpus detects transcription defects in tables, loop bounds, bit helpers, and rare rounding paths. The corpus samples the bit space rather than ordinary numeric distributions, and its seed is fixed for reproducible failures.

The public overloads use the normal Grisu3-with-Dragon4-fallback path. Private formatter-core overloads accept the algorithm choice per call:

```csharp
private static bool TryFormatCore(
    double value,
    Span<char> destination,
    out int charsWritten,
    bool forceDragon4);

private static bool TryFormatCore(
    float value,
    Span<char> destination,
    out int charsWritten,
    bool forceDragon4);
```

Test code locates these exact non-public overloads once through reflection and binds them to strongly typed delegates with `MethodInfo.CreateDelegate`; the corpus invokes the delegates rather than the MethodInfos. This seam requires neither `InternalsVisibleTo` nor friend-assembly signing, does not alter the public formatter API, and stores no algorithm-selection state in static fields.

Allocation assertions warm up all static data and JIT paths before measuring repeated operations with `GC.GetAllocatedBytesForCurrentThread`. Timing is measured only with BenchmarkDotNet. The performance baseline performs `double.TryFormat` or `float.TryFormat` with invariant `"R"` into a span, applies the same marker in that span, and uses the same final string-materialization strategy as the canonical formatter. The 1.25 ratio applies to aggregate random-finite and representative short-form workloads; forced fallback measurements are reported separately because they do not represent the production value distribution.

Parsing remains out of scope. A legacy `float.TryParse` defect can still make string-based `TryGetSingle` return a validated false negative, while a misrounded `Utf8JsonReader.GetDouble` can still produce an adjacent `MetadataKind.Double`; a correctly rounded decimal-to-binary parser is a separate port. Allocation-free canonical formatting for the remaining metadata kinds and `ValidatorOpenApiEmitter.ToLiteral` also remain separate work.
