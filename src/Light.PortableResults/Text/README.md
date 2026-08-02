# Runtime canonical-text provenance

The scalar formatting routines in this folder are adapted from the
[dotnet/runtime](https://github.com/dotnet/runtime) repository's `release/6.0` servicing line,
using immutable tag `v6.0.36` at commit
`f1dd57165bfd91875761329ac3a8b17f6606ad18`.

The following upstream files supplied the implementation:

- `src/libraries/System.Private.CoreLib/src/System/Number.Formatting.cs`
- `src/libraries/System.Private.CoreLib/src/System/Decimal.DecCalc.cs`
- `src/libraries/System.Private.CoreLib/src/System/Globalization/DateTimeFormat.cs`
- `src/libraries/System.Private.CoreLib/src/System/Guid.cs`
- `src/libraries/System.Private.Xml/src/System/Xml/Schema/XsdDuration.cs`

## Adaptations

- Retained only invariant signed and unsigned 64-bit decimal formatting and made the renderer generic
  over UTF-16 characters and UTF-8 bytes.
- Retained decimal's 96-bit division by one billion, added a scale-preserving direct renderer, and
  split field extraction by target. `net10.0` uses `decimal.GetBits(decimal, Span<int>)`;
  `netstandard2.0` validates and uses the historical little-endian decimal layout without allocating.
- Reduced round-trip date/time formatting to the metadata canonical forms, omitted zero fractions,
  trimmed trailing fractional zeros, normalized local `DateTime` values to UTC, and retained explicit
  offsets for `DateTimeOffset`.
- Reduced `XsdDuration` to the `TimeSpan` constructor and duration renderer, including the unchecked
  `TimeSpan.MinValue` magnitude conversion and XML Schema component-omission rules.
- Retained GUID's lowercase `D` branch and hexadecimal conversion, reading the sequential numeric
  fields without an intermediate byte or string allocation.
- Added all-or-nothing capacity checks and direct generic output so both encodings share each renderer.
  Only UTF-16-to-UTF-8 transcoding differs by target.

The complete upstream license is reproduced in the repository-root `THIRD-PARTY-NOTICES.md`, which
is packed at the root of the `Light.PortableResults` NuGet package.

## Floating-point formatting

`CanonicalTextFormatter.FloatingPoint.cs` supplies the `double` and `float` portions of the partial
formatter. Its invariant renderer is Light.PortableResults code, while its shortest-digit generation
uses the internal Grisu3 and Dragon4 implementation under `Numbers/`. See `Numbers/README.md` for that
implementation's provenance and adaptations.
