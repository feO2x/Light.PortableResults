# Runtime numerics provenance

The shortest floating-point digit generation in this folder is adapted from the
[dotnet/runtime](https://github.com/dotnet/runtime) repository's `release/6.0` servicing line,
using the immutable `v6.0.36` tag at commit
`f1dd57165bfd91875761329ac3a8b17f6606ad18`.

The following upstream files under
`src/libraries/System.Private.CoreLib/src/System` supplied the port:

- `Number.Grisu3.cs`
- `Number.DiyFp.cs`
- `Number.Dragon4.cs`
- `Number.BigInteger.cs`
- `Number.NumberBuffer.cs`
- `Number.Formatting.cs` (IEEE bit extraction and the floating-point-to-number dispatch)

Files containing adapted runtime code retain the .NET Foundation MIT header. The public
`CanonicalFloatingPointFormatter` and its invariant renderer are Light.PortableResults code.

## Adaptations

- Moved the implementation from the `System.Number` partial type into internal types in the
  `Light.PortableResults.Numbers` namespace.
- Retained only binary64 and binary32 shortest-round-trip formatting. Removed `Half`, parsing,
  counted Grisu3 formatting, and Dragon4's fixed significant-digit and fractional-digit cutoff modes.
- Replaced the pointer-backed upstream `NumberBuffer` with a small `Span<byte>`-backed ref struct
  containing only digits, digit count, and decimal scale.
- Replaced runtime-internal bit conversion and `BitOperations` dependencies with
  `FloatingPointBits` and `BitOperationsCompat`. The `net10.0` asset uses the framework leading-zero
  intrinsics; the `netstandard2.0` asset uses a semantically equivalent portable implementation.
- Retained Grisu3's cached powers and shortest-mode weed/rounding logic. Renamed `DiyFp` fields to
  properties and removed the unused counted-mode code and data.
- Specialized Dragon4 to shortest-unique mode. Algorithm choice is supplied per call by the
  formatter; no process-wide switch or mutable selection state exists.
- Reduced Dragon4's bignum to the operations reachable from shortest mode. Removed division,
  general conversions, fixed-digit support, and the upstream precomputed power-of-ten bignum table.
  Powers of ten are built with fixed-buffer exponentiation by squaring instead. The 128-block inline
  buffer is stack-only, requires `AllowUnsafeBlocks`, and leaves headroom above the binary64
  shortest-mode maximum.
- Replaced runtime-internal memory clearing and copying with bounded fixed-buffer loops.
- Added a shared code-unit renderer, specialized for UTF-16 characters and UTF-8 bytes, for this
  library's notation thresholds, signed uppercase exponent form, negative zero, and positional
  whole-number `.0` marker. Both encodings write directly into the caller's destination.
- Made the span-based `TryFormat` and `TryFormatUtf8` methods the primary rendering paths. `Format`
  uses a bounded stack buffer and constructs only the returned string.

## Retained code that shortest-unique mode cannot reach

Two small regions survive from upstream that no input can execute through
`CanonicalFloatingPointFormatter`. They are kept rather than deleted because they belong to the
published shape of these algorithms, and cutting into them would make the remaining source harder to
compare against its pinned origin. Neither is excluded from code coverage. The reasoning below
records why the test suite cannot cover them, so a later reader does not mistake them for a gap.

### Dragon4's carry-on-round-up block

`Dragon4.GenerateDigits` handles a final digit of `9` that rounds up by propagating a carry through
the preceding digits. Shortest-unique mode never gets there.

Rounding a `9` up produces a `0`, and a shortest representation never ends in `0` — dropping the zero
and raising the decimal scale would be shorter and would round-trip identically. The carry would
therefore have to run through every generated digit, which restricts the candidates to values whose
shortest representation is exactly `1E±k`: the binary value nearest a power of ten, lying below it.

For such a value the loop would first have to emit a leading `9`, and it cannot:

- When the initial decimal-exponent estimate is exact, `estimateTooLow` is false, which means
  `value + highMargin < 10^digitExponent`. A carry needs `value + highMargin >= 10^digitExponent`.
- When the estimate undershoots by one, a leading `9` is possible in general, but the estimate never
  undershoots for these values. It is `ceil(L * log10(2) - 0.69)` with `L = floor(log2(value))`. For a
  value within one high margin of `10^(m+1)`, `L * log10(2)` lies in `(m + 1 - log10(2), m + 1]`, whose
  fractional part exceeds `1 - 0.30103 = 0.69897`. Upstream chose `0.69` precisely so that
  `0.69 + log10(2) < 1`.

These values instead take the `estimateTooLow` branch, which emits a leading `0` and rounds it to `1`
through the ordinary increment. `1E+23` (`0x44B52D02C7E14AF6`) is the named test for that path.

### Three zero and identity guards in `Dragon4BigInteger`

The early returns in `Multiply(ref value, uint multiplier, out result)`,
`Multiply(ref left, ref right, out result)` and `MultiplyPow10` cannot fire. Shortest-unique mode only
ever multiplies by 2 or 10 and never with a zero operand: `scale` and `scaledMarginLow` are non-zero by
construction, and once `scaledValue` reaches zero the digit loop has already stopped, because a zero
numerator always compares below the low margin. `MultiplyPow10`'s only caller guards `digitExponent > 0`.

### How this was established

Both claims were checked against an instrumented copy of these files that counts every entry into the
regions above. The counters stayed at zero across the complete analytic candidate set for the carry
(every binary64 and binary32 whose shortest form is a single digit `1`, plus both neighbours of each),
all-nines coefficients at every length from 1 to 17 digits over the full exponent range, every one of
the 2,139,095,040 finite `binary32` bit patterns, and 64 million random `binary64` bit patterns.

The complete upstream license is reproduced in the repository-root `THIRD-PARTY-NOTICES.md`, which
is also packed at the root of the `Light.PortableResults` NuGet package.
