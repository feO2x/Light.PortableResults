# OpenAPI Error Examples for Non-Constant Validation Boundaries

> Correction: two claims in Technical Details are wrong, without consequence for the implemented design. The integral `TimeSpan.From*` overloads arrived in .NET 9, not .NET 8 — the reasoning about `TimeSpan.FromHours(2)` binding differently per target framework holds, only the version is off. And `TimeSpan.FromDays(double)` rounds to the nearest millisecond only on .NET Framework; modern .NET rounds to the tick. The conclusion still stands, because the implementation calls the real `double` overloads on the compiler host and only reimplements `FromMicroseconds(double)`, which `netstandard2.0` lacks.

## Rationale

The validation source generator learns a rule's boundary value through `SemanticModel.GetConstantValue`, which only succeeds for C# constant expressions. `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `DateOnly`, `TimeOnly`, and `Uri` cannot be C# constants at all, so a temporal or identifier boundary can never be folded — not by writing it differently and not by hoisting it into a `static readonly` field. The rule then loses both its message and its metadata, and the generated example carries neither. Two checks written identically in the same validator produce usable documentation for the `int` one and an empty example for the `DateTime` one, with no diagnostic to explain the difference.

Date and time boundaries are among the cases where a client most needs the boundary value in the example, and the degradation is invisible: nothing in the generated code, the diagnostics, or the published document distinguishes a rule that has no message from one whose message could not be reconstructed. This plan reconstructs the value from syntax where the shape allows it, and makes the remaining gaps visible through a diagnostic instead of silence.

## Acceptance Criteria

- [x] A boundary written as an object creation whose arguments are themselves constants or recognized shapes — `new DateTime(...)`, `new DateTimeOffset(...)`, `new TimeSpan(...)`, `new DateOnly(...)`, `new TimeOnly(...)`, `new Guid(...)`, `new Uri("...")` — produces an error example carrying both the message and the metadata entry, both derived from the same reconstructed value. `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2))` is covered, since no `DateTimeOffset` boundary worth writing has an offset that folds as a constant.
- [x] The accepted shapes are exactly the ones enumerated in the whitelist tables in Technical Details, and the named factories and well-known statics there reconstruct identically to the equivalent object creation. No overload, factory, or static outside those tables is reconstructed, and every entry in the omissions table reaches the diagnostic rather than being silently unsupported.
- [x] A reference to a `static readonly` field **declared in the validator's own file**, whose declaring type has no other source declaration, and whose initializer is one of the supported shapes and is the field's only assignment, reconstructs to the same value as writing that expression inline at the call site. A field that a static constructor also assigns degrades with the diagnostic.
- [x] A field declared in another file or another assembly is not resolved. It reports a distinct warning naming multi-file resolution as unsupported, and the example degrades rather than being reconstructed from a partial view of the field.
- [x] A `DateTime` boundary whose kind is `DateTimeKind.Local` is rejected with the diagnostic instead of reconstructed, while `Utc` and `Unspecified` reconstruct; the generated file for a given source tree is identical regardless of the build machine's time zone.
- [x] The value rendered into the message text and the value carried in the metadata entry are the same text for every reconstructed kind, verified against `MetadataValue.ToCanonicalString()` rather than against a hard-coded expectation.
- [x] A rule whose metadata cannot be reconstructed reports a new diagnostic identifying the rule and the argument, and a boundary computed at runtime such as `DateTime.UtcNow.AddDays(30)` reports it rather than failing the build. An expression that exceeds the recursion bound, and a `static readonly` field whose initializer chain forms a cycle, both reach that same diagnostic instead of hanging or crashing the compiler.
- [x] An expression that is valid C# but throws when evaluated — `new DateTime(2026, 13, 1)`, `Guid.Parse("invalid")`, `new Uri("http://[")`, `TimeSpan.FromDays(double.MaxValue)` — produces the diagnostic and a degraded example rather than an unhandled exception, and the compilation that contains it still succeeds. `OperationCanceledException` is the only exception that propagates out of reconstruction.
- [x] Generator tests assert the emitted `WithErrorExample` call, driven through the analyzer rather than by calling `ToLiteral` directly, and cover every row of the whitelist tables: each accepted constructor family, each named factory, each well-known static, a nested `DateTimeOffset` offset in each accepted `TimeSpan` form, and a `static readonly` field in the validator's own file. The unsuffixed `TimeSpan.FromHours(2)` is covered as written, so that overload resolution itself is exercised rather than assumed, and the multi-argument component overloads are asserted to be rejected.
- [x] Generator tests also cover the rejection paths with their exact severity and location: a cross-file field, a `DateTimeKind.Local` value, each invalid or overflowing expression from the failure contract, a runtime-computed boundary, a recursion-bound and a cycle case, and a user-defined type or member whose name matches an accepted one but whose symbol does not.
- [x] A runtime test asserts that a reconstructed example reaches the published document with its message and metadata intact.
- [x] `ValidatorOpenApiAnalysis` equality and hashing account for each diagnostic's source path and span, and an incremental test that reuses one generator driver across two compilations — moving an unresolved argument without otherwise changing it — observes the reported location move with it.
- [x] `README.md` no longer states that examples require compile-time constant metadata arguments, and describes what is reconstructed and what degrades.
- [x] Test code coverage stays above 95%, and no existing generated output changes for boundaries that already fold today.

## Technical Details

### Where the value is lost

`ValidatorOpenApiAnalyzer` reads the boundary argument with `semanticModel.GetConstantValue` and leaves `hasConstantValue` false when folding fails. Everything downstream keys off that flag: message assembly abandons the whole message when a placeholder has no replacement, and `ValidatorOpenApiEmitter.EmitExamples` drops the metadata dictionary when `rule.MetadataValues.All(m => m.HasConstantValue)` is false.

The fix belongs at the single point where the value is read. When constant folding fails, attempt a syntax-directed reconstruction and set `hasConstantValue` when it succeeds. `ValidatorOpenApiEmitter.ToLiteral` already has arms for `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, and `Uri` plus `TryCreateDateOnlyOrTimeOnlyLiteral`; they are currently unreachable through the analyzer and are the intended landing site, and they emit ticks-based literals, so the round trip is exact and culture-independent.

What reconstruction produces is not a boxed CLR value, for the reasons in the next section, so `MetadataValueModel.Value` and the two rendering paths do change shape. That is the one structural change in this plan.

### What may be reconstructed

Recognition is a closed whitelist of shapes, not an expression evaluator, and it is recursive: an argument of a recognized shape is accepted when it folds through `GetConstantValue` **or** is itself a recognized shape. This is not optional detail — every useful `DateTimeOffset` constructor takes a `TimeSpan` offset or a `DateTime`, neither of which can ever be a C# constant, so a non-recursive rule would promise `DateTimeOffset` support and deliver none of it. `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2))` is the shape to work from, and `TimeSpan.FromHours(2)`, `TimeSpan.Zero`, and `new TimeSpan(2, 0, 0)` must all be acceptable in that offset position. The repository already writes these values this way — `new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2))` in `TypedMetadataRoutingTests`.

Recursion is bounded by a depth limit and a cycle guard rather than by the shape of the grammar. A literal nesting such as `DateTimeOffset` over `TimeSpan` is two levels, and each `static readonly` field hop adds one, so a small fixed bound — four is enough for every shape named here — keeps a pathological or adversarial expression from driving the generator into deep recursion. The cycle guard tracks the field symbols already being resolved on the current path, because field initializers can reference other fields and a cycle must end in the diagnostic rather than a stack overflow inside the compiler. Exceeding either bound is treated exactly like an unrecognized shape.

### Reconstructed values are structural, not boxed CLR objects

A reconstructed value is carried as an explicit kind plus a deterministic payload, not as `object?` holding a framework instance:

| Kind | Payload |
| --- | --- |
| `DateTime` | Ticks and `DateTimeKind` |
| `DateTimeOffset` | Ticks and offset ticks |
| `TimeSpan` | Ticks |
| `DateOnly` | Day number |
| `TimeOnly` | Ticks |
| `Guid` | Canonical `D`-format text |
| `Uri` | `OriginalString` |

The generator targets `netstandard2.0`, where `DateOnly` and `TimeOnly` do not exist, so it cannot construct or even name them. The existing `TryCreateDateOnlyOrTimeOnlyLiteral` copes by reflecting on `type.FullName` and reading `DayNumber` through `GetProperty`, which works only because the analyzer host happens to run a newer runtime than the generator targets. Requiring real CLR values would spread that reflection into the analyzer and make correctness depend on the host's framework version; a payload of `int` and `long` depends on nothing.

`DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, and `Uri` do exist in `netstandard2.0`, so evaluation still uses them — that is what gives argument validation for free — but the result is reduced to its payload immediately rather than stored. `DateOnly` and `TimeOnly` are evaluated through `DateTime` and `TimeSpan` arithmetic, which also yields their range checking: `new DateOnly(2026, 13, 1)` fails because the equivalent `DateTime` construction throws.

Two further reasons this representation is the right one:

- **Rendering is centralized.** One type renders both the C# literal and the canonical message text from the same payload, so the agreement required below holds by construction instead of by two `switch` statements staying in sync.
- **Equality is ordinal and total.** `Uri.Equals` ignores the fragment, so two boundaries differing only after `#` compare equal while emitting different literals; comparing `OriginalString` ordinally matches what is actually emitted. The same applies to the example de-duplication in `EmitExamples`, which currently keys on `metadata.Value?.ToString()` — `Uri.ToString()` is a canonicalized, unescaped form rather than `OriginalString`, so the dedup key and the emitted literal disagree today.

Object creation is recognized in both its explicit and target-typed forms: `new DateTime(2026, 1, 1)` and `new (2026, 1, 1)` resolve to the same constructor symbol, and symbol-based recognition sees no difference between them. This is not a nicety — `new (2026, 7, 26, 13, 45, 30, DateTimeKind.Utc)` in `TypedMetadataValueTests` is how this repository already writes such values, so a recognizer that keyed on the explicit form would miss the dominant authoring style. `default` and `default(T)` remain omitted; unlike target-typed `new`, they are a third syntax form rather than the same one spelled shorter.

Recognition matches **symbols**, not names. A user-defined `DateTime` type, or a static `FromHours` on someone's own struct, must not be reconstructed as if it were the framework's; resolve the symbol and compare against the corresponding `INamedTypeSymbol` from the compilation's core library.

### The accepted set

The whitelist is exhaustive, not indicative. Reconstruction evaluates on the runtime hosting the compiler rather than the target framework, so the set is restricted to shapes whose semantics do not vary between the two; that is the reason for enumerating overloads instead of naming method families.

Constructors, with every argument either a folded constant or a nested accepted shape:

| Type | Accepted constructors |
| --- | --- |
| `DateTime` | `(int, int, int)`, `(int, int, int, int, int, int)`, `(int, int, int, int, int, int, int)`, `(int, int, int, int, int, int, DateTimeKind)`, `(int, int, int, int, int, int, int, DateTimeKind)`, `(long)`, `(long, DateTimeKind)` — there is no year/month/day overload taking a `DateTimeKind` |
| `DateTimeOffset` | `(int, int, int, int, int, int, TimeSpan)`, `(int, int, int, int, int, int, int, TimeSpan)`, `(long, TimeSpan)`, `(DateTime, TimeSpan)` |
| `TimeSpan` | `(int, int, int)`, `(int, int, int, int)`, `(int, int, int, int, int)`, `(long)` |
| `DateOnly` | `(int, int, int)` |
| `TimeOnly` | `(int, int)`, `(int, int, int)`, `(int, int, int, int)`, `(long)` |
| `Guid` | `(string)` |
| `Uri` | `(string)`, `(string, UriKind)` |

Static factory methods:

| Type | Accepted factories |
| --- | --- |
| `TimeSpan` | `FromTicks(long)`, and the **single-argument** overloads of `FromDays`, `FromHours`, `FromMinutes`, `FromSeconds`, `FromMilliseconds`, `FromMicroseconds`, in both their `double` and integral forms — `FromDays(int)`, `FromHours(int)`, `FromMinutes(long)`, `FromSeconds(long)`, `FromMilliseconds(long)`, `FromMicroseconds(long)`. The multi-argument component overloads are excluded |
| `DateOnly` | `FromDayNumber(int)` |
| `DateTimeOffset` | `FromUnixTimeSeconds(long)`, `FromUnixTimeMilliseconds(long)` |
| `Guid` | `Parse(string)`, `ParseExact(string, string)` |

Well-known statics, recognized by symbol rather than syntax:

| Type | Accepted statics |
| --- | --- |
| `DateTime` | `MinValue`, `MaxValue`, `UnixEpoch` |
| `DateTimeOffset` | `MinValue`, `MaxValue`, `UnixEpoch` |
| `TimeSpan` | `Zero`, `MinValue`, `MaxValue` |
| `DateOnly` | `MinValue`, `MaxValue` |
| `TimeOnly` | `MinValue`, `MaxValue` |
| `Guid` | `Empty` |

Both the `double` and integral `TimeSpan.From*` families must be accepted, because which one a boundary binds to is not a property of the source text. .NET 8 added integral overloads, so `TimeSpan.FromHours(2)` binds to `FromHours(double)` when the consumer targets `netstandard2.0` or `net6.0` and to `FromHours(int)` on `net8.0` and later, where the `int` literal is an exact match. Accepting only one family would make reconstruction depend on the consumer's target framework — and would reject `TimeSpan.FromHours(2)`, this plan's own worked example, on any current target.

Recognition must match the full resolved signature, not the method name. The component overloads have optional parameters, so `FromHours(int, long, long, long, long)` is a candidate for a name-based match and must be rejected by comparing the symbol's parameters.

Evaluating the integral overloads needs care for the reason given above: they do not exist in `netstandard2.0`, so the generator cannot call them. Compute those from the payload arithmetically — ticks as the quantity multiplied by the corresponding `TimeSpan.TicksPer*` constant, in checked arithmetic so that an overflow throws and lands in the failure contract. Do not silently substitute the `double` overload for an integral one: the two agree on ordinary integral values, but `FromDays(double)` rounds to the nearest millisecond and has a different range, so at the extremes they are not the same function.

The offset argument of a `DateTimeOffset` accepts any accepted `TimeSpan` shape: `TimeSpan.Zero`, the `From*` factories, `new TimeSpan(...)`, `MinValue`/`MaxValue`, or a `static readonly` `TimeSpan` field declared in source. `MinValue` and `MaxValue` are accepted as shapes and then throw during evaluation, which the failure contract below turns into the diagnostic — the recognizer does not need to know which offsets are legal.

Everything else is omitted, and an omission is a diagnostic rather than a silent gap:

| Omitted | Reason |
| --- | --- |
| `DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Guid.NewGuid()` | No compile-time value by definition |
| `DateTime.Parse`/`ParseExact`/`TryParse` and the `DateTimeOffset` equivalents | Culture-sensitive; see Excluded shapes |
| `new DateTimeOffset(DateTime)` | Resolves an `Unspecified` or `Local` input against the local zone, so it is nondeterministic for the same reason `DateTimeKind.Local` is |
| Constructors taking a `Calendar` | Culture-dependent interpretation of the same components |
| `new Guid(byte[])` and any array or collection argument | Requires evaluating an array creation, which is not an accepted shape |
| `default` and `default(T)` | Keeps the recognizer to two syntax forms, object creation and member access or invocation |
| Locals, parameters, properties, and instance or non-`readonly` fields | Value depends on flow the generator does not track |
| `static readonly` fields declared outside the validator's syntax tree, including referenced assemblies | Multi-file resolution is deliberately unsupported in this iteration and reports the warning |
| Arithmetic and chaining such as `X.AddDays(1)` or `a + b` | Would require a general expression evaluator rather than a whitelist |
| The multi-argument `TimeSpan.From*` component overloads, such as `FromHours(int, long, long, long, long)` | A boundary is written as one quantity; these exist mainly to spell out components, and their optional parameters make them easy to match by accident |
| `DateOnly.FromDateTime`, `TimeOnly.FromDateTime`/`FromTimeSpan` | Not needed for a boundary; add on demand rather than by default |
| Any reconstructed `DateTime` whose `Kind` is `Local` | Nondeterministic; see Excluded shapes |

### Reconstruction must not be able to break a build

Recognizing a shape is not the same as being able to evaluate it. Reconstruction ultimately runs real constructors and factory methods, so it inherits their argument validation, and every one of these compiles cleanly while throwing when evaluated: `new DateTime(2026, 13, 1)`, `Guid.Parse("invalid")`, `new Uri("http://[")`, `TimeSpan.FromDays(double.MaxValue)`. An unhandled exception here does not degrade one example — it fails the generator and breaks compilation of code that was previously building, which is a far worse outcome than the silent degradation this plan exists to fix.

The contract is therefore that evaluation cannot fail the generator: any exception other than `OperationCanceledException` is caught at the evaluation step and turned into "not reconstructable", which routes into the same diagnostic and the same degraded output as an unrecognized shape. Cancellation must keep propagating, because Roslyn relies on it to abandon superseded generator runs and swallowing it converts a responsive IDE into a hanging one.

Scope the handler to the evaluation of a single recognized shape rather than wrapping the analyzer or the generator entry point. A blanket catch would also swallow genuine defects in this code and turn them into quietly missing documentation, which is the failure mode that is hardest to notice.

An expression in this category is usually also a defect in the validator itself, since the same call throws at runtime. Diagnosing that is out of scope: the generator reports only that it could not reconstruct the value, and does not attempt to tell the author their boundary is invalid.

### Excluded shapes

`DateTimeKind.Local` is rejected and takes the diagnostic path. `MetadataValue.FromDateTime` converts a local value with `ToUniversalTime()`, which resolves against the time zone of whichever process performs the conversion. The metadata entry is converted at runtime, in the deployment time zone, by the document transformer; the message text is baked into the generated file at build time, in the build machine's time zone. Two harms follow, and either alone is disqualifying: the generated source stops being a function of the source tree, so two machines building the same commit emit different files, and whenever the build and deployment zones differ the message and the `comparativeValue` in the same example state different instants — the precise disagreement the criteria forbid.

The rejection tests the reconstructed value's `Kind`, not the syntax: a field reference or a nested shape can yield a local value without the enum member appearing at the call site at all.

`Utc` and `Unspecified` are both deterministic — `FromDateTime` stores them unconverted — and `Unspecified` is what every `new DateTime(...)` literal produces, so rejecting `Local` costs nothing on the common authoring path. That `Unspecified` is published without a zone remains an accepted limitation of the OpenAPI output rather than something this work changes; reconstruction only makes existing behaviour reachable for these types, and no reasoning here depends on resolving it. `DateTimeOffset` is unaffected, since it carries its offset explicitly.

Culture-sensitive parsing is excluded: `DateTime.Parse`, `DateTimeOffset.Parse`, and their `TryParse` siblings are **not** supported even with a constant string, because the generator would have to pick a culture and any choice can disagree with what the developer meant. `Guid.Parse`/`ParseExact` are the exception — the `Guid` formats are culture-invariant. A rejected shape is a diagnostic, not a silent degradation, which keeps the unsupported set discoverable.

For a `static readonly` field reference, resolve the symbol to its declaring syntax and apply the same recognition to the initializer. Only the well-known statics in the tables above are recognized by symbol rather than by syntax.

**Field resolution is confined to the validator's own syntax tree.** A field declared in another file, in another partial declaration of the same type, or in a referenced assembly is not resolved at all; it reports the multi-file warning and degrades. This is a deliberate limit on the first iteration rather than a technical obstacle — resolving across trees needs the `Compilation` and a `GetSemanticModel` call per tree, which is mechanical — and it can be lifted later without changing anything else in this design.

The restriction also buys correctness that is otherwise expensive. `static readonly` does not mean "initialized once at the declaration": a static constructor may assign the field again, and that assignment wins, because the initializer runs first and the constructor body overwrites it. Reconstructing from the initializer would then document a value the application never uses, so reconstruction is valid only when the initializer is the field's *sole* assignment. Within one syntax tree that is decidable — examine the static constructors declared there and reject the field if any assigns it. Across files it is not, because a `partial` type can hide a static constructor in a file the analyzer never looked at, and a rule that resolves cross-file fields while checking only one file's assignments would be confidently wrong. A field with no initializer degrades regardless, since there is no syntax to recurse into.

Consequently, if the declaring type has any declaring syntax reference outside the validator's tree, the field is not resolved even when the initializer sits in the validator's own file — the assignments cannot all be seen from here.

### Message and metadata must agree

This is a real defect today rather than a hypothetical: `FormatMessageValue` renders through `IFormattable.ToString(null, CultureInfo.InvariantCulture)`, which for a `DateTime` yields `01/01/2026 00:00:00`, while the runtime metadata pipeline renders through `MetadataValue.ToCanonicalString()`, which is round-trip ISO-8601. Reconstruction makes both paths reachable for the same value at once, so the disagreement would ship in a single example — the message saying one thing and `comparativeValue` another.

`MetadataValue.ToCanonicalString()` is the canonical form and the message must follow it, not the reverse: it is what the published document and the wire format already use. The generator cannot call it directly — `MetadataValue` lives in the runtime library and the generator targets `netstandard2.0` with no reference to it — so the reconstructed value's own canonical rendering has to reproduce it, and `FormatMessageValue` delegates to that instead of growing a parallel set of arms. Delegation is what makes the guarantee structural: the message and the metadata literal are then two renderings of one payload rather than two switch statements that must be kept in agreement. The criteria still require a test comparing the output against `MetadataValue.ToCanonicalString()` rather than against a literal string, because the two libraries can only be pinned to each other by execution.

### All-or-nothing metadata is deliberate

The issue asks whether one unreconstructable entry should still drop the entries that folded. It should. `CreateRangeSchema` marks both `lowerBoundary` and `upperBoundary` as required, and `CreateComparisonSchema` marks `comparativeValue` as required, so every metadata key of every built-in rule is required by its own schema. Emitting the foldable subset would publish an example that violates the schema published alongside it, which is worse than publishing no metadata. The `All(...)` check stays as written; the new diagnostic is what removes the silence that made it look accidental.

### Diagnostic

Two descriptors are added to `DiagnosticDescriptors`, continuing the `LPRSG####` sequence.

Cardinality is **one report per distinct unresolved argument**, located at that argument. Reporting once per rule cannot be reconciled with pointing at the argument: `IsInRange` with two unreconstructable boundaries has two offending locations, and collapsing them either invents a location or hides the second problem. Per-argument reporting also means fixing one boundary visibly removes one diagnostic, which is the feedback an author needs while working through a rule.

The first covers metadata that could not be reconstructed, at `Info` severity: the generated code is valid and the validation itself is unaffected, and a runtime-computed boundary such as `DateTime.UtcNow.AddDays(30)` is a legitimate authoring choice that should stay quiet enough to live with.

The second covers a field reference the analyzer declines to follow out of the validator's syntax tree, at `Warning` severity. It is louder because it reports a tool limitation rather than an authoring decision: the shape is one the generator could support, the author has no way to tell from the output that the value was dropped for this reason instead of any other, and the fix is mechanical — move the constant into the validator's file or write it inline. It names multi-file resolution explicitly so the message is actionable rather than a generic failure to fold.

One consequence to accept knowingly: a consumer building with `TreatWarningsAsErrors` fails on this where they would previously have got silently degraded documentation. That is the intended trade for visibility, and the escape hatch is the ordinary one — suppress `LPRSG####` — but it is a behaviour change for that audience rather than a purely additive diagnostic.

### Incremental generation

The incremental value is `ValidatorOpenApiAnalysis`, whose `Equals` compares `HintName`, the emitted `Source` string, and the diagnostics. Reconstructed values are therefore never compared directly by the pipeline: they reach the cache only through the text they render into. This makes the structural representation above a rendering concern rather than a cache-correctness one, and it means an equality mistake in a value type cannot silently poison incremental results — but it also means any nondeterminism in rendering shows up as spurious cache misses and churn in generated files, which is a further reason the payloads are fixed integers and ordinal text.

Confining field resolution to the validator's own syntax tree keeps invalidation where it already is. The analyzer is handed the `Compilation` regardless, so the point is not that a compilation dependency is avoided — it is that a validator's generated output continues to depend only on its own syntax tree, and no edit to an unrelated file can change it. Had cross-file fields been resolved, an example would depend on a tree nothing tracks, and the cached result would survive an edit to the constant it quotes — stale documentation that looks correct, which is a worse failure than the degradation this plan fixes. Whoever lifts the restriction later has to make that dependency explicit rather than rely on the analyzer happening to hold a `Compilation`.

Diagnostic locations need the same care, and here the current code is already insufficient for what this plan promises. `DiagnosticsEqual` compares only `Id`, `Severity`, and `GetMessage()`, so two analyses whose diagnostics differ only in position compare equal. Since the new diagnostics are located at a specific argument, moving an unresolved argument — adding a line above it, reordering rules — produces an identical comparison and the driver keeps the cached analysis, leaving the squiggle on the old position. `Equals` and `GetHashCode` must therefore incorporate each diagnostic's source path and span. The criteria require an incremental test that reuses one driver across two compilations to catch this, because a single-shot generator test cannot: it never consults the cache and so passes either way.

### Scope

The twelve mutation survivors in `BuiltInValidationErrorBuilderExtensions` are **not** part of this work, despite an earlier triage note that grouped them here. They cover the `target`-provided branch of the hand-written typed helpers, which the generator never calls — it emits `builder.WithGreaterThanError<T>()` without a target and writes examples through separate `WithErrorExample` calls. Those survivors are an independent test-coverage gap in the runtime helper API and need their own issue.
