# 0034 Plan Deviations

This document compares `ai-plans/0034-configuration-validation.md` with the current implementation state of the configuration-validation integration in `Light.PortableResults.Validation`.

## Summary

The main feature from plan 0034 was implemented successfully: `Validator<TOptions>` can participate in the `IValidateOptions<TOptions>` pipeline, named options are filtered correctly, DI registration is idempotent, and automated tests cover the core scenarios.

The remaining differences are mostly about API organization and two behavioral details where the implementation chose a leaner shape than the original plan text described.

## Deviations From The Original Plan

### 1. Configuration integration was grouped into a dedicated sub-namespace instead of staying in the root validation namespace

**Original plan:**
The plan described adding `PortableResultsValidateOptions<TOptions>` "inside the existing `Light.PortableResults.Validation` project and namespace" and suggested a dedicated static extension class such as `OptionsBuilderExtensions`.

**Implemented:**
The configuration-related types were grouped under `Light.PortableResults.Validation.ConfigurationIntegration`:

- `PortableResultsValidateOptions<TOptions>`
- `ConfigurationConstants`

The `ValidateWithPortableResults<TOptions, TValidator>()` extension method was added to the existing `Module` class in the root namespace instead of a separate `OptionsBuilderExtensions` class.

**Impact:**
This is an organizational deviation, not a capability gap. The public API is still available, but the code is structured around a dedicated configuration-integration area rather than keeping every type directly in the root namespace.

### 2. Unnamed options are represented by an absent context item instead of storing `OptionsNameKey` with a `null` value

**Original plan:**
The plan explicitly specified forwarding the incoming options name via:

`context.SetItem(OptionsNameKey, name)`

This implies that validators could read the key for both named and unnamed options, with unnamed options represented by a stored `null` value.

**Implemented:**
`PortableResultsValidateOptions<TOptions>.Validate(...)` only stores the item when `name` is not `null`:

- named options: `ConfigurationConstants.OptionsNameKey` is present
- unnamed options: the key is not stored at all

This means validators must interpret unnamed options via `TryGetItem(...) == false` rather than via a present key whose value is `null`.

**Impact:**
This is a real behavioral deviation from the plan wording. The current implementation still lets validators distinguish named options, but it does not preserve the exact "forward the incoming `null` name" semantics that the plan described.

### 3. The adapter relies on a fresh root `ValidationContext` instead of explicitly passing `ValidationTarget.Absolute("")`

**Original plan:**
The bridge adapter was supposed to call:

`_validator.CheckForErrors(options, context, out var failure, ValidationTarget.Absolute(""))`

The plan used this explicit target to guarantee clean property paths such as `ConnectionString`.

**Implemented:**
The adapter creates a fresh root context from `ValidationContextFactory` and then calls:

`_validator.CheckForErrors(options, context, out var failure)`

No explicit `ValidationTarget.Absolute("")` is passed.

**Impact:**
With the current `DefaultValidationContextFactory`, this is functionally equivalent because a newly created root `ValidationContext` already starts with an empty target prefix. As a result, property paths remain clean today.

The deviation is architectural: the root-target behavior now depends on root-context semantics rather than being made explicit at the adapter call site.

### 4. The final API only supports `Validator<TOptions>`, not transforming `Validator<TSource, TValidated>`

**Original plan:**
The plan text is internally inconsistent:

- the acceptance criteria state that synchronous `Validator<T>` and `Validator<TSource, TValidated>` should be supported when `TSource == TOptions`
- the later scope section says transforming validators are not relevant for options validation and can be added later if needed

The git history shows why this inconsistency surfaced in the final implementation. Shortly before the configuration-validation work landed, commit `acec552` (`chore!: remove TryValidate from all validators, remove CheckForErrors from Validator<TSource, TValidated>`) simplified the validator surface and removed the non-generic failure wrapper from transforming validators. The actual configuration-validation feature then landed in commit `3f73fae` (`feat: add support for IValidateOptions<T>`).

**Implemented:**
The implementation resolved this in favor of the narrower scope:

- `PortableResultsValidateOptions<TOptions>` accepts `Validator<TOptions>` only
- `ValidateWithPortableResults<TOptions, TValidator>()` is constrained to `where TValidator : Validator<TOptions>`
- there is no overload for `Validator<TOptions, TValidated>`

**Impact:**
This is the most important functional deviation from the acceptance-criteria wording. The branch supports only non-transforming synchronous validators for options validation.

Given the later scope section in the original plan and the preceding API simplification in `acec552`, this looks like an intentional narrowing rather than an accidental omission. In other words, the implementation did not merely skip a planned overload; it aligned the feature with the validator API that existed by the time `IValidateOptions<T>` support was introduced.

## Notes On Items Implemented As Planned

The following parts of plan 0034 match the current implementation:

- `PortableResultsValidateOptions<TOptions>` implements `IValidateOptions<TOptions>`
- failures are mapped to `ValidateOptionsResult.Fail(IEnumerable<string>)`
- named-options filtering returns `ValidateOptionsResult.Skip`
- `AddValidationForPortableResults()` uses idempotent registration patterns
- `ValidateWithPortableResults<TOptions, TValidator>()` returns the original `OptionsBuilder<TOptions>` for chaining
- automated tests cover the adapter, registration, name filtering, skip behavior, and failure mapping
