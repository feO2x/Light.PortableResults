# Add UTF-8 Floating-Point Formatting

## Rationale

`CanonicalFloatingPointFormatter` currently renders only UTF-16 text, so every UTF-8 consumer has to detour through a `char` representation. The JSON metadata writer shows the cost: `MetadataExtensions.WriteNumberValue` calls `writer.WriteRawValue(value.ToCanonicalString(), skipInputValidation: true)`, which allocates a string for every `Double` and `Single` it writes and then lets `System.Text.Json` transcode it back to ASCII bytes. Other UTF-8 producers would have to format into a temporary `char` span and narrow it themselves.

Add explicit UTF-8 span overloads that write the existing runtime-independent canonical representation directly into the caller's byte destination. This plan covers the formatting primitive only. The allocation it removes becomes observable once the serializers adopt it, which is deliberate follow-up work.

## Acceptance Criteria

- [x] `CanonicalFloatingPointFormatter` exposes `TryFormatUtf8` overloads for `double` and `float` plus public maximum-length constants on both package assets, all with XML documentation, while its existing public API remains source- and behavior-compatible.
- [x] For every finite value, the UTF-8 output is byte-for-byte equal to the ASCII bytes of the existing canonical text and `bytesWritten` equals the `charsWritten` of the UTF-16 overload for the same value. No byte-order mark or terminator is written, and the bytes are produced without routing through a UTF-16 buffer or a framework floating-point formatter.
- [x] UTF-8 formatting rejects NaN and both infinities with the same `ArgumentException` contract as UTF-16 formatting; an insufficient destination returns `false`, writes zero to `bytesWritten`, and leaves the destination unchanged.
- [x] After warm-up of both the Grisu3 and the Dragon4 path, both UTF-8 overloads allocate nothing for `double` and `float`.
- [x] Automated tests cover both output encodings for the named canonical scenarios, the deterministic finite-value and exponent corpora, the forced-Dragon4 path, non-finite values, and insufficient destinations. The formatter test project passes against both the `net10.0` and the `netstandard2.0` library asset.
- [x] `src/Light.PortableResults/Numbers/README.md` records the shared code-unit renderer among the adaptations, and the package release notes mention the new UTF-8 formatting API.
- [x] Both target frameworks build in Release with warnings as errors, package validation succeeds, the Native AOT sample publishes successfully, and test coverage remains above 95%.

## Technical Details

### Public API

The exact public API addition is:

```csharp
public const int MaximumDoubleLength = 32;
public const int MaximumSingleLength = 24;

public static bool TryFormatUtf8(double value, Span<byte> destination, out int bytesWritten);
public static bool TryFormatUtf8(float value, Span<byte> destination, out int bytesWritten);
```

Ownership and pooling of the byte storage belong to the caller, so no array-returning `FormatUtf8` API is added. The constants replace the existing private `DoubleTextLength` and `SingleTextLength` fields, which callers currently have to duplicate as magic numbers. They are documented upper bounds with headroom above the true worst cases, and they bound both encodings, because the canonical alphabet is entirely ASCII: digits, sign characters, decimal point, and uppercase `E`. Every output character therefore maps to exactly one UTF-8 byte. The bytes represent the canonical numeric token itself, not JSON-escaped content.

Required length must be calculated before the first destination write so both encodings retain the all-or-nothing `TryFormat` contract.

### Shared renderer

Keep concrete `double` and `float` entry points and digit-generation paths. Generic-math interfaces and static abstract interface members are unavailable to the `netstandard2.0` contract. Instead, share the formatting logic across the output code unit with a private generic core and renderer, specialized on the code-unit type:

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

The digit generators already emit ASCII digits into `Span<byte>`. The renderer stores those digits and its ASCII punctuation as the selected code-unit type through a small conversion helper that branches on `typeof(TCodeUnit) == typeof(byte)` and reinterprets the value with `Unsafe.As<byte, TCodeUnit>` or `Unsafe.As<char, TCodeUnit>`. The JIT specializes generic code over value types and folds the `typeof` comparison away, so neither branch survives into the hot loops. `System.Runtime.CompilerServices.Unsafe` is already used by the `netstandard2.0` asset in `Errors.cs`, and `AllowUnsafeBlocks` is enabled, so this adds no dependency.

The `unmanaged` constraint permits instantiations other than `char` and `byte`, but the renderer is private and only ever instantiated with those two. Do not add a guard clause for the impossible case: an unreachable `throw` is a coverage hole.

The renderer must not format to `char` and then transcode, call `Encoding`, or duplicate the notation and length-calculation rules in separate UTF-8 and UTF-16 renderers. Because the UTF-16 path keeps writing straight into the caller's span, it gains no intermediate buffer and no extra pass.

### Test seam

Retain the per-call forced-Dragon4 test seam for both output encodings without mutable global state. Keep exactly one private `TryFormatCore<TCodeUnit>` per numeric type, so the reflection lookup in `CanonicalFloatingPointFormatterTests` continues to match a single method per numeric type; the tests then bind it per code unit with `MakeGenericMethod` and one delegate type per code unit. Adding per-encoding overloads instead would make that lookup ambiguous.

Extend the existing corpus assertions to compare the UTF-8 bytes with the ASCII bytes of the same expected canonical strings; the UTF-16 assertions remain the normative compatibility check. The allocation test must warm up both digit generators before using `GC.GetAllocatedBytesForCurrentThread`, using values known to take the Dragon4 fallback in addition to the Grisu3-reachable ones it already covers.

### Scope

This issue does not add UTF-8 APIs to `MetadataValue` and does not change JSON or other serializers. Those integrations follow once the primitive has been reviewed, and they are where the removed string allocation becomes measurable.
