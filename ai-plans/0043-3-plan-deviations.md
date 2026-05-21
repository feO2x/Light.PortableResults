# OpenAPI Source Generation Plan Deviations

This document summarizes how the current implementation evolved from the original plan in `0043-0-openapi-source-generation.md`. It is not a new implementation plan and does not define new acceptance criteria.

## Summary

The original direction stayed intact: validation OpenAPI metadata is generated from synchronous validators, emitted through a static contract, and consumed by Minimal API endpoint metadata without reflection. The implementation added two follow-up capabilities that were not fully covered by `0043-0`: explicit documentation hints for opaque validation paths, and representative per-error example messages.

Most boundaries from the original plan still hold. The generator remains Minimal API-focused, supports direct `Validator<T>` and `Validator<TSource, TValidated>` inheritance only, skips nested control-flow checks with diagnostics, treats `Must(...)`, `Custom(...)`, and `ErrorOverrides` as opaque unless documented explicitly, and keeps generated code NativeAOT-safe.

## Runtime API Deviations

The generated contract target is the public static-abstract interface `IPortableValidationOpenApiContract` in `Light.PortableResults.Validation.OpenApi`. Generated validators implement this interface and expose `ConfigurePortableValidationOpenApi(PortableValidationProblemOpenApiBuilder builder)`, matching the original reflection-free design.

The Minimal API bridge is implemented as `ProducesPortableValidationProblemFor<TValidator>(...)` in `Light.PortableResults.Validation.OpenApi`. It delegates to the existing ASP.NET Core OpenAPI builder, first applying the generated validator contract and then invoking the caller's `configure` callback. This preserves the intended ordering: generated metadata is the baseline, endpoint-local manual configuration can override or extend it.

`PortableOpenApiErrorExampleEntry` and `WithErrorExample(...)` were extended beyond the original plan to include a nullable per-error `message` before metadata. The OpenAPI transformer now uses that message for both rich validation problem examples and ASP.NET Core-compatible validation problem examples, falling back to `"Validation failed."` when no message is supplied.

## Hint Model Deviations

The first plan treated explicit emitted-error hints as an escape hatch but did not fully specify their public API. The implementation made this a first-class feature:

- `PortableValidationOpenApiErrorHintAttribute` documents a known error code and can optionally point to a metadata type.
- `PortableValidationOpenApiErrorMetadataPropertyAttribute` documents inline metadata schema properties when a dedicated metadata type would be unnecessary or awkward.
- `PortableValidationOpenApiExampleHintAttribute` documents response-example entries for opaque paths and can include `Target` and `Message`.
- `PortableValidationOpenApiExampleMetadataAttribute` supplies compile-time constant metadata values for matching example hints.

Hints can be placed on the validator class or directly on `PerformValidation`. They compose with inferred rules, are deduplicated when compatible, and produce diagnostics when schema contracts conflict. Example-only hints also document the code as a code-only schema entry, avoiding a redundant error hint for common opaque paths.

`AllowUnknownErrorCodes` is available on `GeneratePortableValidationOpenApiAttribute` as an explicit opt-in. Hints do not imply it. This is a sharper separation than the original plan text: hints document known contracts, while `AllowUnknownErrorCodes` keeps the response schema non-exhaustive for additional codes that are not enumerable at build time.

## Message Deviations

The implementation added `ValidationRuleMessageAttribute` in `Light.PortableResults.Validation.Definitions`. Built-in validation check methods now carry compile-time message templates where the default framework message can be represented statically.

The generator substitutes `{displayName}` and metadata placeholders such as `{minLength}`, `{maxLength}`, `{lowerBoundary}`, `{upperBoundary}`, and `{comparativeValue}` when all required values are known at compile time. The display name is resolved from an explicit constant `displayName` argument to `ValidationContext.Check(...)`, then from the inferred JSON-style target, and otherwise omitted.

If a template is valid but a value is not statically known, the generator still emits the schema and example entry but leaves the message as `null`, letting the transformer use the fallback message. Invalid templates produce warning diagnostics:

- `LPRSG0013` for unknown placeholders.
- `LPRSG0014` for malformed brace sequences or unsupported placeholder syntax.

This is a deliberate documentation feature only. Generated messages are representative defaults and are not intended to exactly model runtime customization, localization, culture-specific formatting, display-name customization, or `ErrorOverrides.Message`.

## Analysis and Generation Deviations

The source generator is implemented as an `IIncrementalGenerator` using `ForAttributeWithMetadataName(...)`, as planned. The thin generator adapter calls the public `ValidatorOpenApiAnalyzer.Analyze(...)` method, which returns a `ValidatorOpenApiAnalysis` containing diagnostics, hint name, and source. The implementation therefore made the analysis directly testable without a full generator-driver flow.

The generator does not reference runtime projects as normal assembly references. It resolves contracts by metadata name through `KnownTypeNames`, while generated source depends on the runtime packages in the consuming application.

Generated source uses a deterministic controlled using block and emits builder calls rather than runtime reflection. It now emits:

- grouped `WithErrorCodes(...)` calls for registered no-metadata or globally registered contracts;
- typed helper calls such as `WithInRangeError<T>()` for comparison and range contracts;
- inline `WithErrorMetadata(...)` calls for annotated custom rules and inline hint metadata;
- `WithErrorExample(...)` calls containing code, target, optional representative message, and constant metadata values where available;
- `AllowUnknownErrorCodes()` only when explicitly requested.

The implementation also chose direct in-memory Roslyn generator tests instead of introducing snapshot infrastructure such as `Verify.SourceGenerators`. This still covers emitted source, diagnostics, and generated OpenAPI document behavior while keeping the test stack smaller.

## Validation Rule Annotation Deviations

The core validation layer gained source-generator-facing annotations in `Light.PortableResults.Validation.Definitions`:

- `ValidationRuleAttribute`
- `ValidationRuleMetadataAttribute`
- `ValidationRuleMessageAttribute`
- `ValidationErrorContractAttribute`
- `ValidationErrorMetadataContractAttribute`

These remain OpenAPI-agnostic and use source-generator-friendly attribute shapes only. Built-in checks were annotated rather than hard-coded into the generator. The comparative range rename from `IsInBetween` / `IsNotInBetween` to `IsInRange` / `IsNotInRange` was applied, aligning method names with `ValidationErrorCodes.InRange` and `ValidationErrorCodes.NotInRange`.

One practical addition beyond the first plan is inline metadata schema generation for user-defined rules and hints. When an annotated rule references a validation error contract, the generator can emit an endpoint-local schema with `PortableOpenApiSchemaTypeMapper.Map<T>()` instead of requiring a pre-registered metadata type for every custom case.

## Documentation, Sample, and Tests

The `NativeAotMovieRating` sample now uses `[GeneratePortableValidationOpenApi]` on `NewMovieRatingValidator` and `ProducesPortableValidationProblemFor<NewMovieRatingValidator>(...)` on the endpoint. Manual validation error-code configuration was removed from the sample endpoint, with endpoint-local format configuration kept in the callback.

The README now documents the generator opt-in model, supported validator shapes, top-level-only analysis boundary, explicit hints, example metadata, representative messages, and the distinction between hints and `AllowUnknownErrorCodes()`.

Tests were added across the source-generation and OpenAPI projects for generator output, diagnostics, custom annotated rules, hints, example metadata, messages, builder behavior, document transformation, and generated OpenAPI integration. The package layout also bundles the generator into `Light.PortableResults.Validation.OpenApi` as an analyzer asset under `analyzers/dotnet/cs`, matching the original distribution intent.

## Remaining Boundaries

The following limitations remain intentional after the deviations:

- MVC integration is still outside this implementation.
- Async validators remain unsupported.
- Indirect/custom validator base classes remain unsupported.
- Nested checks inside control flow remain skipped rather than analyzed.
- Complex target inference, child validators, automatic source-null errors before `PerformValidation`, and runtime-customized validation messages remain outside the generator's static analysis scope.
- Opaque delegate or imperative validation remains documentable only through explicit hints or endpoint-level manual configuration.
