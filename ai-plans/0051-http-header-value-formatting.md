# HTTP Header Value Formatting

## Rationale

`DefaultHttpHeaderConversionService` originally used `MetadataValue.ToString()` when no `HttpHeaderConverter` was registered, emitting scalar strings with debug quotes and arrays as one bracketed value. The metadata restructuring has since made the scalar fallback use `ToCanonicalString()`, incidentally correcting strings and the expanded primitive kinds, but arrays still use their debug representation and the protocol mapping remains split across a conditional in the service.

Introduce one public header value formatter that owns the exhaustive metadata-to-header mapping. Use it from the fallback conversion so scalar values retain their canonical wire text, arrays become multiple header values, null remains absent, and future metadata kinds cannot silently inherit a debug representation.

## Acceptance Criteria

- [ ] Public `HttpHeaderValueFormatter.Format(MetadataValue)` is available on both package assets and is the sole metadata-kind mapping used by the fallback branch of `DefaultHttpHeaderConversionService`.
- [ ] Every non-null primitive kind is emitted as its unquoted canonical text; in particular, strings contain no debug quotes and `Double` and `Single` use the runtime-independent encoding introduced by #58.
- [ ] An array of primitive values is emitted as ordered, distinct `StringValues` entries rather than one bracketed debug string; an empty array produces no values, a single-item array produces one value, and a null array item is represented by its canonical `"null"` text so item positions are not discarded.
- [ ] A top-level `Null` produces `StringValues.Empty`, while `Object` and arrays containing complex children are rejected with `NotSupportedException` when the public formatter is called directly.
- [ ] Registered `HttpHeaderConverter` instances retain precedence and receive the original `MetadataValue` unchanged.
- [ ] `HttpExtensions.SetMetadataValuesAsHeadersIfNecessary` does not add a response header when conversion returns `StringValues.Empty`; this omits empty arrays produced by the fallback and lets a registered converter suppress its header deliberately.
- [ ] `MetadataValue.ToString()`, `MetadataArray.ToString()`, and `MetadataObject.ToString()` retain their existing debug representations.
- [ ] Automated tests cover every metadata kind, empty, single-item, and multi-value arrays, unsupported complex values, converter precedence, and empty output from a custom converter. Reading two or more formatted values through `DefaultHttpHeaderParsingService` reconstructs an ordered `MetadataArray` according to the configured parsing mode, while a single value reads as a scalar; ASP.NET Core tests confirm that the response header collection contains no entry for an empty array or empty converter output, and that a received response contains an unquoted string and separate values for a multi-item array.
- [ ] Core package release notes state that primitive arrays are emitted as multiple header values and custom converters can reuse the new public formatter; ASP.NET Core Shared package release notes state that empty conversion output suppresses the response header. They do not present the already-shipped canonical scalar formatting as new behavior.
- [ ] Formatter and fallback regression tests pass against both the `net10.0` and `netstandard2.0` core assets, both target frameworks build in Release with warnings as errors, and test code coverage remains above 95%.

## Technical Details

Add this exact public surface in `Light.PortableResults.Http.Writing.Headers`:

```csharp
public static class HttpHeaderValueFormatter
{
    public static StringValues Format(MetadataValue value);
}
```

The formatter uses the following exhaustive mapping:

| Metadata kind | Header representation |
| --- | --- |
| `Null` | `StringValues.Empty` |
| Every other primitive kind | `MetadataValue.ToCanonicalString()` as one unquoted value |
| `Array` | One canonical string per primitive child, in source order |
| `Object` | `NotSupportedException` |

Keep the kind dispatch explicit rather than relying only on `IsPrimitive()`, so adding a future primitive or complex kind produces a compiler-visible decision point. Use an exhaustive enum switch without a catch-all arm and locally suppress only `CS8524`, matching `MetadataKindExtensions`; a newly declared enum member must therefore make the Release build report the incomplete mapping.

Special-case array cardinality: zero returns `StringValues.Empty`, one returns the child's canonical string directly without a `string[]`, and two or more allocate exactly one `string[]` and materialize each child's canonical text once. A null child uses `ToCanonicalString()` and therefore becomes the literal text `"null"`; this differs deliberately from a top-level null, which denotes an absent header. Reject a complex array child defensively even though `MetadataValue.FromArray` already prevents header annotation on such arrays, because callers can invoke the public formatter with an unannotated value.

Change the no-converter branch of `DefaultHttpHeaderConversionService.PrepareHttpHeader` to call the formatter. Converter lookup, returned header names, precedence, and the value passed to custom converters remain unchanged.

After conversion, `HttpExtensions.SetMetadataValuesAsHeadersIfNecessary` checks `preparedHttpHeader.Value.Count`; when it is zero, continue without calling `HttpResponse.Headers.Add`. ASP.NET Core's `HeaderDictionary.Add` stores an empty `StringValues` entry, so returning `StringValues.Empty` alone does not guarantee that the response header collection omits the key. Apply the check to all conversion results rather than special-casing an empty metadata array before conversion: registered converters must still receive the original array and can use an empty result to suppress their header deliberately. Do not replace `Add` with the header indexer, because that would change the existing conflict behavior for non-empty header-name collisions.

`HttpExtensions.SetMetadataValuesAsHeadersIfNecessary` already skips top-level null metadata before conversion. The formatter's top-level-null result therefore defines its public direct-call behavior, while the empty-result check carries empty-array and converter suppression through the normal ASP.NET Core response path.

The writer/parser contract is symmetric at the header-value level, not for every `MetadataKind`. `DefaultHttpHeaderParsingService` reconstructs two or more values as an ordered `MetadataArray`, but its configured `HeaderValueParsingMode` controls whether individual values become strings or inferred Boolean, `Int64`, and `Double` values. It reads a single value as a scalar, so a one-item metadata array cannot preserve its array shape without a custom parser; an empty array produces no response header and therefore cannot round-trip as an array at all. Keep those behaviors, cover the multi-value result under both parsing modes, and document the two lossy cardinalities in the formatter remarks rather than expanding this writing fix into a typed header protocol.

Do not validate HTTP field syntax or sanitize CR/LF in the formatter. ASP.NET Core/Kestrel remains responsible for validating values when the response is written; duplicating that validation in the fallback would add work to the hot path and could diverge from the active server's rules.

The response-level regression should exercise the default conversion path, not a registered converter. Assert the received values individually rather than through `StringValues.ToString()`, which comma-joins multiple entries and could hide a return to the old single debug value. Separately cover empty custom-converter output at the `HttpExtensions` level with a hand-written test double, and assert absence with `ContainsKey` so an empty entry cannot satisfy the test.

Header-name collisions and the existing `HttpResponse.Headers.Add` conflict behavior are out of scope; append-versus-replace semantics require a separate API decision.
