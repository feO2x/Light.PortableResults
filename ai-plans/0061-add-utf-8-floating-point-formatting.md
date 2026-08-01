# Add UTF-8 Floating-Point Formatting

## Rationale

`CanonicalFloatingPointFormatter` currently renders only UTF-16 text. UTF-8 serializers must therefore format into a temporary `char` span and copy the formatter's ASCII output into bytes, adding an avoidable pass over every result. Add explicit UTF-8 span overloads that write the existing runtime-independent canonical representation directly to the caller's byte destination while preserving the UTF-16 API and its performance.

## Acceptance Criteria

- [ ] `CanonicalFloatingPointFormatter` exposes `TryFormatUtf8` overloads for `double` and `float` on both package assets, while its existing public API remains source- and behavior-compatible.
- [ ] Every finite value produces bytes exactly equal to the UTF-8 encoding of the existing canonical text, without a byte-order mark or terminator and without routing through a UTF-16 buffer or a framework floating-point formatter.
- [ ] UTF-8 formatting rejects NaN and both infinities with the same `ArgumentException` contract as UTF-16 formatting; an insufficient destination returns `false`, writes zero to `bytesWritten`, and leaves the destination unchanged.
- [ ] After warm-up, both UTF-8 overloads allocate nothing for `double` and `float`.
- [ ] Automated tests cover both output encodings for the named canonical scenarios, deterministic finite-value and exponent corpora, the forced-Dragon4 path, non-finite values, and insufficient destinations. Formatter tests pass unchanged against the `net10.0` and `netstandard2.0` library assets.
- [ ] BenchmarkDotNet compares direct UTF-8 formatting with UTF-16 formatting followed by an ASCII copy for the existing common-short and random-finite workloads. Across the aggregate workloads for both number types, direct UTF-8 is not slower than the two-stage baseline by more than 5%, and the shared implementation does not regress existing UTF-16 throughput by more than 5% relative to the pre-change results.
- [ ] Both target frameworks build in Release with warnings as errors, package validation succeeds, the Native AOT sample publishes successfully, and test coverage remains above 95%.

## Technical Details

The exact public API addition is:

```csharp
public static bool TryFormatUtf8(double value, Span<byte> destination, out int bytesWritten);
public static bool TryFormatUtf8(float value, Span<byte> destination, out int bytesWritten);
```

Do not add an array-returning `FormatUtf8` API: ownership and pooling of byte storage belong to the caller. The bytes represent the canonical numeric token itself, not JSON-escaped content.

The canonical alphabet is entirely ASCII: digits, sign characters, decimal point, and uppercase `E`. Consequently, every output character maps to one UTF-8 byte and the existing maximum lengths of 32 units for `double` and 24 units for `float` still apply. Required length must be calculated before the first destination write so both encodings retain the all-or-nothing `TryFormat` contract.

Keep concrete `double` and `float` entry points and digit-generation paths. Generic-math interfaces are unavailable to the `netstandard2.0` contract, and the legacy target's primitive types do not implement them. Instead, share the formatting logic across the output code unit. A private generic core and renderer, specialized only for `char` and `byte`, are the intended shape; the exact helper decomposition remains an implementation detail:

```csharp
private static bool TryRender<TCodeUnit>(
    ReadOnlySpan<byte> digits,
    int scale,
    bool isNegative,
    int positionalMaximumScale,
    Span<TCodeUnit> destination,
    out int unitsWritten
)
    where TCodeUnit : unmanaged;
```

The digit generators already emit ASCII digits into `Span<byte>`. The shared renderer should store those digits and its ASCII punctuation directly as the selected code-unit type, using a small JIT/AOT-specializable conversion helper or an equivalent zero-allocation mechanism. It must not format to `char` and then transcode, call `Encoding`, or duplicate the notation and length-calculation rules in separate UTF-8 and UTF-16 renderers. Verify through benchmarks that code-unit specialization removes its type checks from the hot loops; prefer separate narrow store helpers only if a supported runtime fails to specialize the generic implementation adequately.

Retain a per-call forced-Dragon4 test seam for both output encodings without mutable global state. Extend the existing corpus assertions to compare the UTF-8 bytes with the ASCII bytes of the same expected canonical strings; the UTF-16 assertions remain the normative compatibility check. Allocation measurements must warm up both Grisu3 and Dragon4 paths before using `GC.GetAllocatedBytesForCurrentThread`.

Add benchmark cases beside the existing formatter benchmarks. The two-stage UTF-8 baseline calls the canonical UTF-16 `TryFormat` overload into a stack buffer and narrows its known-ASCII code units into the byte destination, so it differs from the new path only by the extra rendering/copy work. A framework `Utf8Formatter` result is not a semantic baseline because its notation and whole-number marker contract differ.

This issue does not add UTF-8 APIs to `MetadataValue` and does not change JSON or other serializers. Those integrations should follow after the floating-point primitive and its performance characteristics have been reviewed.
