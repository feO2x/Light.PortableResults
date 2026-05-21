# Validation OpenAPI Example Messages

## Rationale

The validation OpenAPI source generator can already produce useful response-level examples with concrete error codes, targets, and metadata values. The generated examples still show the generic message `"Validation failed."` for every error entry because the OpenAPI example pipeline has no place to carry a per-error example message. This makes Scalar and Swagger UI less useful than the framework can reasonably provide.

This plan extends the example model so generated and manually configured validation examples can include representative error messages. The goal is to document the framework's default message shape for inferred rules and allow explicit messages for non-inferable rules, while preserving the current NativeAOT-safe source-generation design. The generated messages are documentation examples, not a promise that every runtime response will use the same text when applications customize validation templates, culture, display names, or error overrides.

## Acceptance Criteria

- [ ] OpenAPI response examples can carry a per-error message in addition to the existing code, target, category, and metadata values.
- [ ] `WithErrorExample(...)` and `PortableOpenApiErrorExampleEntry` use a message-before-metadata signature; existing internal call sites are updated because the feature has not been published in a stable release yet.
- [ ] The OpenAPI document transformer uses the supplied example message for both rich validation problem responses and ASP.NET Core-compatible validation problem responses.
- [ ] The validation rule annotation model can describe a default example-message template for built-in and explicitly annotated custom rules without referencing ASP.NET Core or `Microsoft.OpenApi`.
- [ ] Built-in validation rules are annotated with default example-message templates that match the framework's default `ValidationErrorTemplates` as closely as possible.
- [ ] Message-template parsing supports literal braces via `{{` and `}}`. Placeholder names are case-sensitive, must match the metadata key (or `displayName`) exactly, and disallow inner whitespace (`{ minLength }` is malformed).
- [ ] The source generator emits per-rule example messages when it can resolve every value needed by the message template at generation time.
- [ ] The source generator omits the rule-specific example message when required inputs cannot be resolved statically, allowing the OpenAPI transformer to apply its centralized fallback message.
- [ ] The source generator reports two distinct Roslyn warning diagnostics for message-template annotation problems: one for unknown placeholders (a name that is neither `displayName` nor a metadata key declared by the rule) and one for malformed brace sequences (unmatched `{` or `}`).
- [ ] Explicit OpenAPI example hints can supply a message for opaque or custom validation paths.
- [ ] Generated source remains deterministic, NativeAOT-safe, reflection-free, and independent of consumer implicit usings, global usings, aliases, and local using directives.
- [ ] Automated tests are written for builder APIs, document transformation, source-generator output, inferred built-in rule messages, explicit hint messages, fallback behavior, and non-constant message omission.
- [ ] Documentation is updated to explain that generated messages are representative examples based on framework defaults and may differ from runtime messages when applications customize validation behavior.

## Technical Details

### Runtime Example Model

Extend `PortableOpenApiErrorExampleEntry` with a nullable `Message` property. Because the library is still pre-stable and this example API has not been published in a stable release, the existing constructor can be reshaped directly:

```csharp
public PortableOpenApiErrorExampleEntry(
    string code,
    string? target,
    string? message,
    IReadOnlyDictionary<string, object?>? metadata
)
```

The property assignment stores `Message = message`. Equality and hashing include `Message` so two otherwise-identical example entries with different messages remain distinct.

Reshape `PortableProblemOpenApiBuilder.WithErrorExample(...)` and `PortableValidationProblemOpenApiBuilder.WithErrorExample(...)` the same way. Put `message` before `metadata` so the API reads like the resulting error entry:

```csharp
builder.WithErrorExample(
    code: "NotEmpty",
    target: "id",
    message: "id must not be empty",
    metadata: null);
```

Callers that do not have a specific message pass `message: null`. The builder methods keep `message` and `metadata` as optional parameters with `null` defaults so hand-written calls such as `WithErrorExample("Code", target: "x", message: "…")` remain readable; only the parameter order changes. All existing repository call sites should be updated in the same change set.

The OpenAPI document transformer should use:

```csharp
entry.Message ?? "Validation failed."
```

for the rich `errors[*].message` value and for the ASP.NET Core-compatible `errors[target][index]` message value. The fallback stays centralized and unchanged for callers that do not opt into message examples.

### Rule Message Metadata

Add source-generator-facing message metadata to `Light.PortableResults.Validation.Definitions`, next to `ValidationRuleAttribute` and `ValidationRuleMetadataAttribute`. The attribute should describe a compile-time message template, not runtime behavior:

```csharp
[ValidationRuleMessage("{displayName} must not be empty")]
```

The supported placeholders should be deliberately small:

- `{displayName}` for the inferred or explicit display name.
- Metadata placeholders such as `{minLength}`, `{maxLength}`, `{lowerBoundary}`, `{upperBoundary}`, and `{comparativeValue}` for values already modeled with `[ValidationRuleMetadata]`. The exact placeholder names must match the string values of the corresponding `ValidationErrorMetadataKeys` constants verbatim; if a constant value differs from the spelling used here, the template should use the constant's value. Placeholder matching is case-sensitive.

The attribute must use source-generator-friendly constructor shapes only. A single string template is sufficient for this iteration. Do not introduce delegates, resource lookups, arbitrary object graphs, or references to OpenAPI types.

Template parsing should support literal braces by escaping them as doubled braces: `{{` emits `{`, and `}}` emits `}`. A single unmatched `{` or `}` is a malformed template. Inside a placeholder, only the bare identifier is permitted: leading or trailing whitespace, format specifiers, and alignment syntax are not supported and should be treated as malformed.

The generator should validate templates against the annotated rule method. A placeholder is valid only when it is `displayName` or when it matches a metadata key declared by `[ValidationRuleMetadata]` on the same method. Two distinct Roslyn warning diagnostics are emitted at the rule or template attribute location:

- One for unknown placeholders — a name that is neither `displayName` nor a metadata key declared by the rule.
- One for malformed brace sequences — unmatched `{` or `}`, or placeholders containing anything other than a bare identifier.

Both warnings suppress only the rule-specific message emission for affected rule calls; schema and non-message example generation continue. This is different from a valid placeholder whose call-site value is not a compile-time constant; that case is not malformed and should only suppress the rule-specific example message for that particular call site without raising a diagnostic.

Built-in check methods should be annotated with templates that mirror the default `ValidationErrorTemplates`:

```csharp
[ValidationRule(ValidationErrorCodes.NotEmpty)]
[ValidationRuleMessage("{displayName} must not be empty")]
public static Check<T> IsNotEmpty<T>(...)

[ValidationRule(ValidationErrorCodes.InRange, ValidationRuleMetadataShape.TypedRange)]
[ValidationRuleMetadata(ValidationErrorMetadataKeys.LowerBoundary, nameof(lowerBoundary))]
[ValidationRuleMetadata(ValidationErrorMetadataKeys.UpperBoundary, nameof(upperBoundary))]
[ValidationRuleMessage("{displayName} must be between {lowerBoundary} and {upperBoundary}")]
public static Check<T> IsInRange<T>(...)
```

For rules whose runtime default message depends on formatting that is hard to represent statically, prefer a conservative template that matches the normal case. If a rule cannot be represented without misleading users, omit the rule-message annotation and let the example fall back to `"Validation failed."`.

Built-in rule coverage guidance: annotate every built-in check whose default `ValidationErrorTemplates` entry can be reproduced statically with the available placeholders. This includes at least the no-metadata templates (`NotEmpty`, `Empty`), the comparable-value templates (`EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`) using `{comparativeValue}`, the typed-range templates (`InRange`, `NotInRange`) using `{lowerBoundary}` and `{upperBoundary}`, and the length-based templates (`MinLength`, `MaxLength`, `LengthInRange`) including the trailing `" characters long"` segment from `DisplayNameWithParameter`/`DisplayNameWithRange`. Implementers should walk through `ValidationErrorTemplates` and annotate each entry whose shape can be expressed without runtime formatting; skip any whose default depends on locale-sensitive formatting or runtime-only state.

### Display Name Inference

The generator already infers the JSON target for simple `context.Check(dto.SomeProperty)` calls. It should also determine the example display name used by message templates.

Use this precedence:

1. A constant `displayName` argument supplied to `ValidationContext.Check(...)`.
2. The inferred normalized target when available.
3. No display name, which means no generated message for that example.

This intentionally differs from fully executing runtime validation. It gives useful examples for the common source-generator-supported shape while avoiding a fake message for expressions whose target cannot be inferred.

Falling back to the normalized JSON target means generated messages read with the camelCase property name (for example, `"firstName must not be empty"`). At runtime, the validator typically substitutes the unnormalized property identifier or an application-configured friendly name, so generated text will deliberately differ. This is acceptable because the typical consumer is a web API where the JSON name is the most accurate identifier for documentation, and because the documentation already states that generated messages are representative defaults.

The generator should not instantiate `ValidationContext`, `ValidationErrorDefinition`, `ValidationErrorTemplates`, or validators to compute messages. Doing so would break the current architecture: analyzers do not reference runtime assemblies normally, generated contracts stay NativeAOT-safe, and the generator remains a pure static analysis pass.

### Message Formatting

Extend the generator model with nullable message values:

- `RuleCallModel.Message`
- `ExampleHintModel.Message`

When a rule has a valid message template, the analyzer substitutes placeholders only if every referenced value is known:

- `{displayName}` requires a resolved example display name.
- Metadata placeholders require matching `MetadataValueModel` entries with `HasConstantValue == true`.

Parameter formatting should be simple and deterministic. It should use invariant-culture literal formatting consistent with the existing generated metadata literals, not the application's runtime culture. This should be documented as part of the "representative example" behavior.

If substitution cannot be completed, the analyzer leaves `Message` as `null` and does not emit a diagnostic. Missing rule-specific example messages are not contract violations; they only mean the OpenAPI transformer will use its centralized fallback message for that entry.

The no-diagnostic rule applies only after the template itself has been validated. For example, `{minimum}` on a rule that declares only `minLength` is a diagnostic because the annotation is wrong. `{minLength}` on a call like `HasMinLength(configuredMinimum)` is not a diagnostic when `configuredMinimum` is not a compile-time constant; the generator simply cannot produce an exact example message for that call site.

The emitter should use the message-aware overload when `Message` is non-null:

```csharp
builder.WithErrorExample(
    "InRange",
    "rating",
    "rating must be between 1 and 5",
    new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["lowerBoundary"] = 1,
        ["upperBoundary"] = 5
    });
```

When `Message` is null, emit the same message-before-metadata call shape with `null` as the message argument:

```csharp
builder.WithErrorExample(
    "NotEmpty",
    "id",
    null,
    null);
```

### Explicit Example Hints

Extend `PortableValidationOpenApiExampleHintAttribute` with a nullable settable `Message` property:

```csharp
[PortableValidationOpenApiExampleHint("MovieAlreadyRated", Target = "movieId", Message = "movieId has already been rated")]
```

The message property is only example content. It does not declare schema, does not affect error-code exhaustiveness, and does not participate in metadata-shape conflict checks.

Example-hint deduplication should include `Message` so users can intentionally document distinct entries that differ by text. Existing structural diagnostics from 0043-1 still apply; if the current implementation allows only one example hint per code per scope, that rule remains unless the implementation deliberately broadens it.

### Compatibility and Boundaries

This change is intentionally source-breaking for the unpublished example-builder API shape: repository call sites that pass metadata positionally must add the new `message` argument before metadata. This is acceptable because the library is still pre-stable and the feature has not been published in a stable release. Existing OpenAPI documents may gain more specific example messages when validators are regenerated, but the schema contract is unchanged.

Do not attempt to model:

- `ErrorOverrides.Message`
- application-specific `ValidationErrorTemplates`
- localized messages
- culture-specific runtime formatting
- messages produced by imperative `Custom(...)`, `Must(...)`, or delegate-based validation unless supplied through explicit example hints

Those paths remain runtime concerns or explicit documentation-hint concerns. The source generator should prefer omitting a rule-specific message over emitting inaccurate text, while the transformer remains responsible for providing the generic fallback.

### Tests

Add focused unit tests for the runtime OpenAPI side:

- `WithErrorExample(...)` with `message: null` still produces `"Validation failed."`.
- `WithErrorExample(...)` with a message produces that message in rich validation examples.
- `WithErrorExample(...)` with a message produces that message in ASP.NET Core-compatible validation examples.
- Example-entry equality and hashing include the message.

Add source-generation tests for:

- built-in no-metadata messages such as `NotEmpty`;
- built-in single-parameter messages such as `MinLength`;
- built-in range messages such as `InRange` and `LengthInRange`;
- explicit constant `displayName`;
- non-constant metadata values causing message omission while preserving code, target, and metadata schema generation;
- unknown template placeholders producing the unknown-placeholder Roslyn diagnostic;
- malformed brace sequences (unmatched `{`/`}`, whitespace or format specifiers inside placeholders) producing the malformed-template Roslyn diagnostic;
- escaped literal braces in message templates;
- explicit `PortableValidationOpenApiExampleHintAttribute.Message`;
- annotated custom rule messages using metadata placeholders.

End-to-end document-generation tests should assert the concrete message text in the generated OpenAPI example for the movie-rating sample or a similarly representative validator.

### Documentation

Update the source-generation documentation to show before-and-after OpenAPI examples with real messages. The docs should state plainly that generated messages are representative defaults. Runtime responses can differ when users configure validation templates, display names, target normalization, culture, or error overrides. Users who need exact documentation for opaque or customized flows should use explicit example hints or endpoint-level manual configuration.
