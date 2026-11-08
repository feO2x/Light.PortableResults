# Validation configuration

`ValidationContextOptions` controls how every `ValidationContext` behaves: how values and targets are normalized, which culture formats messages, which message templates are used, and how `null` values are reported. This page shows how to register customized options, how to localize messages, how to change normalization and target casing, and how to validate `Microsoft.Extensions.Options` options with your validators.

**Package:** `Light.PortableResults.Validation` (namespaces `Light.PortableResults.Validation`, `Light.PortableResults.Validation.Messaging`, `Light.PortableResults.Validation.Normalization`, and `Light.PortableResults.Validation.Targeting`)

The properties of `ValidationContextOptions` and their defaults are listed in the [configuration reference](../configuration-reference.md#validationcontextoptions). All properties are `init`-only, and the options are immutable once a factory uses them.

## Registering custom options

Pass a factory delegate to `AddValidationForPortableResults`:

```csharp
using System.Globalization;
using Light.PortableResults.Validation;

builder.Services.AddValidationForPortableResults(
    _ => new ValidationContextOptions
    {
        CultureInfo = CultureInfo.GetCultureInfo("de-DE")
    }
);
```

`AddValidationForPortableResults` registers `IValidationContextFactory` and `ValidationContextOptions` only when they are not registered yet. You can therefore also register a customized factory before calling it:

```csharp
builder.Services.AddSingleton<IValidationContextFactory>(
    _ => DefaultValidationContextFactory.Create(new ValidationContextOptions
    {
        CultureInfo = CultureInfo.GetCultureInfo("de-DE")
    })
);
builder.Services.AddValidationForPortableResults();
```

Without a DI host:

```csharp
var factory = DefaultValidationContextFactory.Create(new ValidationContextOptions
{
    CultureInfo = CultureInfo.GetCultureInfo("de-DE")
});
var validator = new CreateMovieValidator(factory);
```

Implement `IValidationContextFactory` yourself when contexts need per-call configuration. Every validator exposes its factory as `ValidationContextFactory`.

<a id="message-templates-and-localization"></a>

## Message templates and localization

Built-in assertions take their messages from the `ValidationErrorTemplates` instance in `ValidationContextOptions.ErrorTemplates`. It has one property per rule: `NotNull`, `Null`, `Empty`, `NotEmpty`, `NotNullOrWhiteSpace`, `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `IsInRange`, `NotInRange`, `ExclusiveRange`, `MinLength`, `MaxLength`, `LengthIn`, `Pattern`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly`, `UuidV7`, `Utc`, `Local`, `Unspecified`, `Count`, `MinCount`, `MaxCount`, `Enum`, `EnumName`, `PrecisionScale`, and `Predicate`. The [built-in checks](built-in-checks.md) show the default messages.

`ValidationErrorTemplates` is a record, so you replace individual templates with a `with` expression. To localize messages, replace the templates and set the culture, which formats numbers and dates in messages:

```csharp
using Light.PortableResults.Validation.Messaging;

var germanTemplates = ValidationErrorTemplates.Default with
{
    NotNull = new ValidationErrorTemplates.DisplayName(" darf nicht null sein"),
    NotNullOrWhiteSpace = new ValidationErrorTemplates.DisplayName(" darf nicht leer sein"),
    IsInRange = new ValidationErrorTemplates.DisplayNameWithRange(" muss zwischen ", " und ", " liegen"),
    LengthIn = new ValidationErrorTemplates.DisplayNameWithRange(" muss zwischen ", " und ", " Zeichen lang sein"),
    GreaterThan = new ValidationErrorTemplates.DisplayNameWithComparable(" muss größer sein als ")
};

builder.Services.AddValidationForPortableResults(
    _ => new ValidationContextOptions
    {
        CultureInfo = CultureInfo.GetCultureInfo("de-DE"),
        ErrorTemplates = germanTemplates
    }
);
```

The nested template classes cover the shapes of the built-in messages:

| Template | Produces |
| --- | --- |
| `DisplayName(suffix, key, prefix)` | `prefix + displayName + suffix` |
| `Constant(text, key)` | A fixed text, independent of the display name |
| `DisplayNameWithComparable(prefix, suffix)` | `displayName + prefix + value + suffix` |
| `DisplayNameWithParameter<TParameter>(parameterPrefix, suffix, key, prefix)` | `prefix + displayName + parameterPrefix + parameter + suffix` |
| `DisplayNameWithRange(beforeLowerBoundary, betweenBoundaries, afterUpperBoundary)` | `displayName + before + lower + between + upper + after` |
| `DisplayNameWithPrecisionScale` | The precision-and-scale message |
| `IgnoreParameter<TParameter>(innerTemplate)` | Adapts a parameterless template to a parameterized rule |

For full control, implement `IValidationErrorMessageTemplate`, `IValidationErrorMessageTemplate<TParameter>`, `IComparableValidationErrorMessageTemplate`, or `IRangeValidationErrorMessageTemplate`. A template receives a `ValidationErrorMessageContext<T>` with the display name, the target, the checked value, and a read-only view of the context (including `Options.CultureInfo`), and returns a `ValidationErrorMessage` with the text and an optional key. Return `true` from `IsMessageStable` only when the message depends on nothing but the display name and fixed parameters — such messages are cached.

Templates change messages only; codes, targets, and metadata stay the same, so clients can keep relying on them. Display names are not translated by templates — pass a localized `displayName` to `Check` when the target name should not appear in messages. Because options are fixed per factory, serve several languages by creating one factory per culture and passing a context created by the matching factory to `Validate(value, context)`.

`ErrorTemplates.MessageCache` stores stable messages per display name, target, and culture; assign `MessageCache = null` in the `with` expression to disable caching, or supply your own `IValidationErrorMessageCache`. Templates derived with `with` get their own cache unless you set one explicitly.

<a id="value-normalization"></a>

## Value normalization

`ValidationContextOptions.ValueNormalizer` runs on every value when `Check` creates a check, before any assertion sees it. The default `TrimStringNormalizer` trims strings and converts `null` strings to `""`; values of other types pass through unchanged. That is why the validators on these pages assign the result of a string check back to the DTO:

```csharp
dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
```

- `ValueNormalizers.Trim` / `TrimStringNormalizer.Instance` — the default.
- `ValueNormalizers.NoOp` / `NoOpValueNormalizer.Instance` — leaves values unchanged. String assertions then throw an `InvalidOperationException` for `null`, so guard with `IsNotNull()`.
- A custom `IValueNormalizer` implements `T Normalize<T>(T value)`.

Override the normalizer for a single check with the `valueNormalizer` parameter:

```csharp
// Passwords must not be trimmed
context.Check(dto.Password, ValueNormalizers.NoOp).IsNotNull().HasMinLength(12);
```

Do not normalize values in a way that destroys what you validate — for example, a normalizer that coerces `DateTimeKind.Unspecified` to `Utc` makes `IsUtc` pass for every value. See [`IsUtc`, `IsLocal`, and `IsUnspecified`](built-in-checks.md#isutc-islocal-and-isunspecified).

<a id="targets"></a>

## Targets

`ValidationContextOptions.TargetNormalizer` converts captured caller expressions and explicit targets into the flat paths used as `Error.Target`; see [Targets and display names](getting-started.md#targets-and-display-names). The default is `ValidationTargets.DefaultNormalizer`, a `DefaultValidationTargetNormalizer` with `ValidationTargetCasing.CamelCase`. To produce targets that match PascalCase JSON or to keep the original casing:

```csharp
builder.Services.AddValidationForPortableResults(
    _ => new ValidationContextOptions
    {
        TargetNormalizer = new DefaultValidationTargetNormalizer(ValidationTargetCasing.PascalCase)
    }
);
```

`DefaultValidationTargetNormalizer` caches normalized caller expressions and normalizes long paths in a pooled buffer (`arrayPool`, `arrayPoolThreshold`). Implement `IValidationTargetNormalizer` for other conventions. `ValidationTargets.Compose(prefix, target)` and `ValidationTargets.AppendIndex(prefix, index)` compose paths with the same rules as the framework.

## Automatic null errors

`ValidationContextOptions.AutomaticNullErrorProvider` creates the error for [automatic null checks](advanced.md#automatic-null-checking). The default `DefaultAutomaticNullErrorProvider` creates a `NotNull` error with the `NotNull` template; `NoOpAutomaticNullErrorProvider` disables automatic null errors. A custom `IAutomaticNullErrorProvider` can return another error or decline by returning `false`.

## Error definition cache

`ValidationContextOptions.ErrorDefinitionCache` (default `ValidationErrorDefinitionCache.Default`, a process-wide singleton) caches the definitions that parameterized assertions create, for example one definition per range of `IsInRange(1, 5)`. Supply a separate `IValidationErrorDefinitionCache` only when you need to isolate definitions, for example in tests.

<a id="validating-options"></a>

## Validating `Microsoft.Extensions.Options` options

Use `ValidateWithPortableResults<TOptions, TValidator>()` to integrate your `Validator<T>` implementations with the standard options validation pipeline:

```csharp
public sealed class EmailSenderOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class EmailSenderOptionsValidator : Validator<EmailSenderOptions>
{
    public EmailSenderOptionsValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<EmailSenderOptions> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        EmailSenderOptions options
    )
    {
        context.Check(options.Host).IsNotNullOrWhiteSpace();
        context.Check(options.Port).IsInRange(1, 65535);
        context.Check(options.ApiKey).IsNotNullOrWhiteSpace();
        return checkpoint.ToValidatedValue(options);
    }
}

services
    .AddOptions<EmailSenderOptions>()
    .BindConfiguration("EmailSender")
    .ValidateWithPortableResults<EmailSenderOptions, EmailSenderOptionsValidator>()
    .ValidateOnStart();
```

`ValidateWithPortableResults` calls `AddValidationForPortableResults()`, registers the validator as a singleton if it is not registered yet, and adds a `PortableResultsValidateOptions<TOptions>` adapter that implements `IValidateOptions<TOptions>`. A failed validation becomes `ValidateOptionsResult.Fail` with the error messages.

`ValidateWithPortableResults` supports named options and forwards the current options name to the `ValidationContext`. Use `ValidationContext.TryGetItem(ConfigurationConstants.OptionsNameKey, out var optionsName)` to access it in your validator. Each adapter validates only the options name of the builder it was created for and skips all other names.
