# Add `DateTimeKind` Assertions

## Rationale

A `DateTime` arriving in a request DTO carries a `DateTimeKind` that records how the client encoded the timestamp: `System.Text.Json` maps a trailing `Z` to `Utc`, any explicit numeric offset to a value shifted into the server's local time with `Local`, and a timestamp with no zone at all to `Unspecified`. That kind is the only surviving evidence of the distinction, and the library cannot currently state a requirement about it — `Check<DateTime>` reaches only the generic comparable and equality assertions.

The consequence is the classic boundary bug this library exists to prevent. An `Unspecified` value is persisted or compared as if it were UTC and is silently wrong by the server's offset; a `Local` value has already been shifted by an offset the client chose and the server never sees. Both look like ordinary timestamps at every later layer. `IsUtc`, `IsLocal`, and `IsUnspecified` make the requirement explicit at the boundary, where the information still exists, and turn a silent corruption into a validation error the client can act on.

This is the first candidate from #74. It follows the shape `IsUuidV7` established in #72: dedicated error codes, customizable message templates, metadata-free OpenAPI contracts.

## Acceptance Criteria

- [ ] `Check<DateTime>` exposes `IsUtc`, `IsLocal`, and `IsUnspecified`, each in both the built-in-message and `ErrorOverrides` overloads, honoring `shortCircuitOnError` and the already-short-circuited state like every other built-in assertion.
- [ ] Failures carry the new `ValidationErrorCodes.Utc`, `Local`, and `Unspecified` codes, with messages from new customizable `ValidationErrorTemplates` properties that survive `with`-expression copies.
- [ ] Each of the three assertions is annotated for OpenAPI source generation, and a validator using all three generates a document successfully against the built-in contract registry — no generator errors, no `WithErrorMetadata` escape hatch, and no unregistered-error-code failure during document construction.
- [ ] A kind matrix asserts all three assertions against all three `DateTimeKind` values, establishing that each assertion accepts exactly one kind and that the three together partition the enum.
- [ ] A test deserializes the four ISO 8601 wire forms in the table below through `System.Text.Json` into a DTO and asserts which assertion accepts each, pinning the premise the whole feature rests on. `+00:00` is covered as its own case, distinct from a non-zero offset.
- [ ] The documented contract of `IsUtc` is that the normalized value's `Kind` is `DateTimeKind.Utc`, stated without reference to how the value was produced. The `System.Text.Json` consequence — a trailing `Z` is required, `+00:00` yields `Local` and is rejected — is documented as a consequence of the default converter, in the XML remarks and the README, not as the contract.
- [ ] Automated tests additionally cover both overload forms, short-circuit propagation, template customization, and the generated OpenAPI metadata. Solution test coverage stays above 95%.
- [ ] The README documents the assertions where request timestamps are validated, and the 0.7.0 `<PackageReleaseNotes>` of both affected packages record their own part of the change.

## Technical Details

### Semantics

The predicate is `check.Value.Kind == DateTimeKind.Utc` and its two siblings — no portability question, since `DateTime.Kind` exists on `netstandard2.0`. Unlike #72 there is no bit manipulation to hide, so there is deliberately **no standalone `DateTimeExtensions` predicate class**: `value.Kind == DateTimeKind.Utc` is already the clearest possible spelling at a call site outside a check chain, and a public wrapper would add a name without adding meaning. That is the one structural departure from the `IsUuidV7` shape.

**The contract is the kind and nothing more:** `IsUtc` accepts exactly those normalized values whose `Kind` is `DateTimeKind.Utc`. The predicate cannot see where a value came from, and must not be documented as though it could — `DateTime.UtcNow`, `DateTime.SpecifyKind`, a custom converter, and a gRPC or messaging transport all produce `Utc` without any JSON encoding being involved.

What the wire format determines is *which values reach the assertion* over an HTTP JSON boundary, and that is a fact about the default `System.Text.Json` converter rather than about the rule. Measured against .NET 10 on a machine at UTC+2:

| Wire value | `DateTime.Kind` | Accepted by |
| --- | --- | --- |
| `2026-08-02T10:00:00Z` | `Utc` | `IsUtc` |
| `2026-08-02T10:00:00+00:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00+02:00` | `Local` | `IsLocal` |
| `2026-08-02T10:00:00` | `Unspecified` | `IsUnspecified` |

The second row is the one that surprises: `+00:00` denotes exactly the same instant as `Z`, yet deserializes to `Local` and is rejected, because `System.Text.Json` resolves every explicit offset — including a zero one — against the server's time zone before the DTO exists. So over the default JSON stack, `IsUtc` does amount to "the client must send `Z`", and the README should say so plainly, since that is the actionable form for an API consumer. The distinction matters because the two statements come apart the moment a value arrives by any other route, and only the kind statement holds in every case.

### Normalization

These assertions inspect the **normalized** value, not the value as the deserializer produced it. `ValidationContext.Check<T>` applies the per-check normalizer or `Options.ValueNormalizer` before constructing the `Check<T>`, and `ValidationContextOptions.ValueNormalizer` is a public `init` property.

The default `TrimStringNormalizer` returns every non-string value unchanged, so under the default configuration the deserialized kind reaches the assertion intact — which is what makes these assertions meaningful. A custom normalizer can rewrite a `DateTime`, and then the assertions describe the normalized value. That is the caller's choice and needs no defense in the implementation, but it is the second reason the XML documentation must state the contract as "the normalized value's `Kind`" and nothing stronger: neither the normalizer nor the deserializer is fixed, so the kind is the only thing the assertion can promise. Put the `System.Text.Json` behavior in `<remarks>`, where it reads as the guidance it is.

A normalizer that coerced `Unspecified` to `Utc` would destroy the signal these assertions exist to surface. That is worth a sentence in the README, not a guard in code.

### Error codes and message templates

Three separate metadata-free codes rather than one `HasKind(DateTimeKind)` rule carrying an `expectedKind` metadata value. The existing families already prefer named assertions with their own codes over a parameterized one — `IsEmpty`/`IsNotEmpty`, `IsNull`/`IsNotNull` — the messages are better when they are not assembled from a parameter, and each contract is free.

The codes are terse, matching `Empty`, `Null`, and `Email`. `Unspecified` read alone is vague, but a code never appears alone: the error payload always carries the `target`, so a consumer sees which property is being described.

```csharp
public const string Utc = "Utc";
public const string Local = "Local";
public const string Unspecified = "Unspecified";
```

Templates default to `new DisplayName(" must be represented in UTC")`, `new DisplayName(" must be a local date and time")`, and `new DisplayName(" must not specify a time zone")`. The first is deliberately about *representation* rather than about the instant or the encoding. `"must be in UTC"` describes the instant and so reads as already satisfied on a `+00:00` payload; `"must be encoded with a trailing 'Z'"` describes an origin the assertion cannot observe and would be simply false for a value that arrived over gRPC. "Represented in UTC" is what `DateTimeKind.Utc` actually means, and it is the only phrasing true on every transport. The `Z` guidance belongs in the README and the OpenAPI description, where the JSON context is established. The third message is likewise phrased as an instruction rather than `"must have an unspecified kind"`, which names a CLR concept the client cannot see.

`IsLocal` is included for completeness of the enum rather than because it is good API design — a server-relative kind rarely belongs in a portable result. It exists so the three assertions partition `DateTimeKind`, and so the matrix test can state that.

### Registration points

The six from #72, applied three times. Two still fail silently when missed:

- `ValidationErrorTemplates`' **copy constructor** (`ValidationErrorTemplates.cs:118`) must copy all three new properties, since it is what `with` expressions run. Omitting a line silently resets a caller's customized template to the default.
- `BuiltInValidationErrorContracts.Contracts` must register all three as `ErrorMetadataContract.NoMetadata`, and the expected no-metadata list in `BuiltInValidationErrorContractsTests` must grow with them, because that test asserts the registry's full key set. Without the entries, document construction fails at runtime with the unregistered-error-code message from `PortableResultsOpenApiMessages` — there is no generator diagnostic for this, so the OpenAPI acceptance criterion has to be verified by generating a document, not by compiling a validator.

The rest follow their `UuidV7` counterparts: the three `ValidationErrorCodes` constants; three definitions modeled on `UuidV7ValidationErrorDefinition`, each overriding `TryGetStableMessageProvider` because these messages have no per-error parameters and are cacheable; the three shared `BuiltInValidationErrorDefinitions` properties the assertions pass to `AddBuiltInError`; and the three `ValidationErrorTemplates` properties with their `Default*Template` fields.

New files `Checks.Temporal.cs` and `Definitions/BuiltInValidationErrorDefinitions.Temporal.cs`, matching the partial-class-per-family layout. Source generation needs no generator change — it discovers each rule through `[ValidationRule(...)]` plus `[ValidationRuleMessage(...)]` on the built-in-message overload, and the default `ValidationRuleMetadataShape.Registered` is correct because none of the rules carries metadata. Note that these attributes are method-level: an assertion added later to this family without them is silently invisible to the generator.

### Test data and cases

The kind matrix is the central test: three assertions × three `DateTimeKind` values, asserting for each cell whether an error was added. Nine cells state both that each assertion accepts its own kind and that it rejects the other two — which is what makes the equality mutations Stryker generates on `Kind == DateTimeKind.X` killable, since flipping any comparison moves at least one cell.

The wire-format test is the one that guards the rationale rather than the implementation. Deserialize a DTO through `System.Text.Json` from the four forms in the table above and assert which assertion accepts each. Assert on the resulting kind and validation outcome, never on the wall-clock value: both offset forms produce a value that depends on the machine's time zone, while their kind does not. If a future `System.Text.Json` changes this mapping, the assertions keep working but their documented meaning shifts, and this test is what surfaces that.

Keep `DateTime.UtcNow`, `DateTime.Now`, and `default(DateTime)` as named cases — they state things about the platform's own values that the matrix cannot. `DateTime.UtcNow` carries a second job: it passes `IsUtc` without any JSON encoding existing, which is the executable statement that the contract is the kind rather than the wire format. Name it for that. `default(DateTime)` being `Unspecified` is a genuine trap for anyone reaching for `IsUtc` as a not-set check.

Assertion-level tests then stay small, as in #72: both overload forms, short-circuit propagation, and template customization through `ValidationErrorTemplates.Default with { … }`.

### Release notes

**Validation** gains the three assertions, the three error codes, and the three customizable templates. **Validation.OpenApi** gains three built-in metadata contracts. The second is easy to skip because that package's own source barely changes, but its registry's key set is public behavior — a consumer reading the built-in contracts, or narrowing error schemas against them, sees three new entries.

### Deliberately out of scope

- **`Check<DateTimeOffset>` assertions.** Originally drafted here as an `IsUtc` overload sharing the `Utc` code, and removed: `DateTimeOffset` has no kind, so any such assertion tests `Offset == TimeSpan.Zero`, which is a different requirement wearing the same name. Measured on .NET 10, `2026-08-02T10:00:00+00:00` deserializes to `Local` as a `DateTime` (rejected) and to a zero offset as a `DateTimeOffset` (accepted), so one shared code and message would give the same wire value opposite verdicts — and OpenAPI cannot expose the difference, since both types map to `string`/`date-time` in `PortableOpenApiSchemaTypeMapper`. Worse, an unzoned wire value deserializes to a `DateTimeOffset` carrying the *server's* offset, so a zero-offset assertion would accept it on a UTC-configured host and reject it elsewhere. A `DateTimeOffset` rule is a canonicalization policy rather than an ambiguity check, needs its own code (`ZeroOffset`) and message, and needs a decision about that host dependence. Tracked separately in #74.
- **`IsInThePast` / `IsInTheFuture`.** Tracked separately in #74. They need a clock abstraction and a configurable tolerance, and the `TimeProvider` availability question on `netstandard2.0` is unresolved.
- **A UTC-converting normalizer.** Coercing `Unspecified` to `Utc` during normalization would silence exactly the signal these assertions exist to surface. If a service wants coercion rather than rejection, that is a mapping concern, not a validation one.
- **`Check<DateTime?>` overloads.** Consistent with #72: no built-in assertion offers nullable value-type overloads, and `IsNotNull` covers the null case generically.
- **`DateOnly`, `TimeOnly`, and `TimeSpan`.** None carries a kind or an offset, so there is nothing to assert.
