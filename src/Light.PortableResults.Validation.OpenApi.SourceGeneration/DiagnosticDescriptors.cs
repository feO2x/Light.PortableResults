using Microsoft.CodeAnalysis;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

internal static class DiagnosticDescriptors
{
    private const string Category = "Light.PortableResults.Validation.OpenApi.SourceGeneration";
    private const string HelpLinkBase =
        "https://github.com/feO2x/Light.PortableResults/blob/main/README.md#validation-openapi-source-generation";

    public static readonly DiagnosticDescriptor ValidatorMustBePartial = new (
        "LPRSG0001",
        "Validator must be partial",
        "Validator '{0}' must be partial to receive generated PortableResults validation OpenAPI metadata",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor UnsupportedValidatorShape = new (
        "LPRSG0002",
        "Validator shape is not supported",
        "Validator '{0}' is not supported by validation OpenAPI source generation: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor AsyncValidatorUnsupported = new (
        "LPRSG0003",
        "Async validators are not supported",
        "Validator '{0}' inherits from an async validator base; v1 supports only direct Validator<T> or Validator<TSource, TValidated> bases",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor MissingPerformValidation = new (
        "LPRSG0004",
        "PerformValidation must be declared on the marked validator",
        "Validator '{0}' must declare a synchronous PerformValidation method directly on the marked partial class",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor NestedCheckSkipped = new (
        "LPRSG0005",
        "Nested validation check was skipped",
        "Validation check '{0}' is nested inside control flow and is not included in generated OpenAPI metadata; lift it to a top-level statement, add explicit hints, or allow unknown error codes",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor OpaqueValidationFlow = new (
        "LPRSG0006",
        "Opaque validation flow is not documentable",
        "Validation flow '{0}' cannot be inferred for generated OpenAPI metadata; use an annotated wrapper rule, add explicit hints, or allow unknown error codes",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor InvalidRuleMetadata = new (
        "LPRSG0007",
        "Validation rule metadata is malformed",
        "Validation rule '{0}' binds metadata to parameter '{1}', but that parameter is not available at the call site",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor NoDocumentedRules = new (
        "LPRSG0008",
        "Validator produced no documentable validation rules",
        "Validator '{0}' produced no documentable validation rules for generated OpenAPI metadata",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );

    public static readonly DiagnosticDescriptor InvalidErrorContract = new (
        "LPRSG0009",
        "Validation error contract is malformed",
        "Validation rule '{0}' references error definition '{1}', but it does not declare a matching validation error contract for code '{2}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkBase
    );
}
