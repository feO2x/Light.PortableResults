using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// Analyzes marked validators and produces generated OpenAPI contract source.
/// </summary>
public static class ValidatorOpenApiAnalyzer
{
    private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    /// <summary>
    /// Analyzes a validator symbol and returns diagnostics plus generated source when the validator is supported.
    /// </summary>
    public static ValidatorOpenApiAnalysis Analyze(
        Compilation compilation,
        INamedTypeSymbol validatorType,
        CancellationToken cancellationToken
    )
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var hintName = CreateHintName(validatorType);
        var validatorLocation = validatorType.Locations.FirstOrDefault();

        if (!TryGetPrimaryClassDeclaration(validatorType, cancellationToken, out var classDeclaration))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedValidatorShape,
                    validatorLocation,
                    validatorType.Name,
                    "the class declaration could not be found"
                )
            );
            return new ValidatorOpenApiAnalysis(hintName, null, diagnostics.ToImmutable());
        }

        ValidateValidatorShape(validatorType, classDeclaration, diagnostics, cancellationToken);
        var hasShapeError = diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (!TryGetPerformValidationMethod(
                validatorType,
                cancellationToken,
                out var performValidation,
                out var methodDeclaration
            ))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.MissingPerformValidation,
                    validatorLocation,
                    validatorType.Name
                )
            );
            hasShapeError = true;
        }

        if (hasShapeError || performValidation is null || methodDeclaration is null)
        {
            return new ValidatorOpenApiAnalysis(hintName, null, diagnostics.ToImmutable());
        }

        var semanticModel = compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
        var metadataValueReconstructor = new MetadataValueReconstructor(compilation);
        var sourceParameterName =
            performValidation.Parameters.Length >= 3 ? performValidation.Parameters[2].Name : null;
        var rules = ImmutableArray.CreateBuilder<RuleCallModel>();
        AnalyzePerformValidationBody(
            semanticModel,
            metadataValueReconstructor,
            methodDeclaration,
            sourceParameterName,
            rules,
            diagnostics,
            cancellationToken
        );

        var hints = GetErrorHints(validatorType, performValidation, diagnostics).ToImmutableArray();
        var examples = GetExampleHints(validatorType, performValidation, diagnostics).ToImmutableArray();
        ValidateHintContracts(rules, hints, examples, diagnostics);
        var allowUnknownErrorCodes = GetAllowUnknownErrorCodes(validatorType);
        if (rules.Count == 0 && hints.Length == 0 && examples.Length == 0 && !allowUnknownErrorCodes)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.NoDocumentedRules,
                    validatorLocation,
                    validatorType.Name
                )
            );
        }

        var model = new ValidatorModel(
            validatorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            validatorType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            validatorType.ContainingNamespace.IsGlobalNamespace ?
                null :
                validatorType.ContainingNamespace.ToDisplayString(),
            GetAccessibility(validatorType.DeclaredAccessibility),
            validatorType.Name,
            allowUnknownErrorCodes,
            rules.ToImmutable(),
            hints,
            examples
        );
        var source = ValidatorOpenApiEmitter.Emit(model);
        return new ValidatorOpenApiAnalysis(hintName, source, diagnostics.ToImmutable());
    }

    private static void ValidateValidatorShape(
        INamedTypeSymbol validatorType,
        ClassDeclarationSyntax classDeclaration,
        ICollection<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validatorLocation = classDeclaration.Identifier.GetLocation();

        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.ValidatorMustBePartial,
                    validatorLocation,
                    validatorType.Name
                )
            );
        }

        if (validatorType.ContainingType is not null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedValidatorShape,
                    validatorLocation,
                    validatorType.Name,
                    "nested validators are not supported"
                )
            );
        }

        if (validatorType.TypeParameters.Length != 0)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedValidatorShape,
                    validatorLocation,
                    validatorType.Name,
                    "generic validators are not supported"
                )
            );
        }

        var baseType = validatorType.BaseType;
        if (baseType is null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedValidatorShape,
                    validatorLocation,
                    validatorType.Name,
                    "v1 requires directly inheriting from Validator<T> or Validator<TSource, TValidated>"
                )
            );
            return;
        }

        var baseMetadataName = GetMetadataName(baseType.OriginalDefinition);
        if (baseMetadataName is KnownTypeNames.AsyncValidator or KnownTypeNames.TransformingAsyncValidator)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.AsyncValidatorUnsupported,
                    validatorLocation,
                    validatorType.Name
                )
            );
            return;
        }

        if (baseMetadataName != KnownTypeNames.Validator &&
            baseMetadataName != KnownTypeNames.TransformingValidator)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedValidatorShape,
                    validatorLocation,
                    validatorType.Name,
                    "custom or indirect validator base classes are not supported; v1 requires directly inheriting from Validator<T> or Validator<TSource, TValidated>"
                )
            );
        }
    }

    private static void AnalyzePerformValidationBody(
        SemanticModel semanticModel,
        MetadataValueReconstructor metadataValueReconstructor,
        MethodDeclarationSyntax methodDeclaration,
        string? sourceParameterName,
        ICollection<RuleCallModel> rules,
        ICollection<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    )
    {
        if (methodDeclaration.Body is null)
        {
            return;
        }

        foreach (var statement in methodDeclaration.Body.Statements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryGetTopLevelCheckExpression(statement, out var expression))
            {
                AnalyzeCheckExpression(
                    semanticModel,
                    metadataValueReconstructor,
                    expression,
                    sourceParameterName,
                    rules,
                    diagnostics,
                    cancellationToken
                );
                continue;
            }

            WarnForNestedChecks(semanticModel, statement, diagnostics, cancellationToken);
        }
    }

    private static void AnalyzeCheckExpression(
        SemanticModel semanticModel,
        MetadataValueReconstructor metadataValueReconstructor,
        ExpressionSyntax expression,
        string? sourceParameterName,
        ICollection<RuleCallModel> rules,
        ICollection<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    )
    {
        var invocations = CollectInvocationChain(expression);
        if (invocations.Count == 0 || !IsValidationContextCheck(semanticModel, invocations[0], cancellationToken))
        {
            return;
        }

        var target = TryInferTarget(semanticModel, invocations[0], sourceParameterName, cancellationToken);
        var displayName = TryGetExplicitDisplayName(semanticModel, invocations[0], cancellationToken) ?? target;
        for (var i = 1; i < invocations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var invocation = invocations[i];
            var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            if (symbol is null)
            {
                continue;
            }

            if (UsesErrorOverrides(symbol))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.OpaqueValidationFlow,
                        invocation.GetLocation(),
                        symbol.Name
                    )
                );
                continue;
            }

            var ruleAttribute = GetAttribute(symbol.ReducedFrom ?? symbol, KnownTypeNames.ValidationRuleAttribute);
            if (ruleAttribute is null)
            {
                if (symbol.Name == "Must" || symbol.Name == "Custom")
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.OpaqueValidationFlow,
                            invocation.GetLocation(),
                            symbol.Name
                        )
                    );
                }

                continue;
            }

            var rule = CreateRuleCall(
                semanticModel,
                metadataValueReconstructor,
                invocation,
                symbol,
                ruleAttribute,
                target,
                displayName,
                diagnostics,
                cancellationToken
            );
            if (rule is not null)
            {
                rules.Add(rule);
            }
        }
    }

    private static RuleCallModel? CreateRuleCall(
        SemanticModel semanticModel,
        MetadataValueReconstructor metadataValueReconstructor,
        InvocationExpressionSyntax invocation,
        IMethodSymbol symbol,
        AttributeData ruleAttribute,
        string? target,
        string? displayName,
        ICollection<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    )
    {
        var definitionSymbol = symbol.ReducedFrom ?? symbol;
        var code = ruleAttribute.ConstructorArguments.Length > 0 ?
            ruleAttribute.ConstructorArguments[0].Value as string :
            null;
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var shape = RuleMetadataShape.Registered;
        if (ruleAttribute.ConstructorArguments.Length > 1 &&
            ruleAttribute.ConstructorArguments[1].Value is int shapeValue)
        {
            shape = (RuleMetadataShape) shapeValue;
        }

        var metadataValues = ImmutableArray.CreateBuilder<MetadataValueModel>();
        var diagnosedArguments = new HashSet<string>(StringComparer.Ordinal);
        foreach (var metadataAttribute in definitionSymbol.GetAttributes()
                    .Where(static attribute => IsAttribute(attribute, KnownTypeNames.ValidationRuleMetadataAttribute)))
        {
            var metadataKey = metadataAttribute.ConstructorArguments.Length > 0 ?
                metadataAttribute.ConstructorArguments[0].Value as string :
                null;
            var sourceArgument = metadataAttribute.ConstructorArguments.Length > 1 ?
                metadataAttribute.ConstructorArguments[1].Value as string :
                null;
            if (string.IsNullOrWhiteSpace(metadataKey))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(sourceArgument))
            {
                if (!TryResolveArgumentConstant(
                        semanticModel,
                        metadataValueReconstructor,
                        invocation,
                        symbol,
                        sourceArgument!,
                        cancellationToken,
                        out var value,
                        out var valueTypeName,
                        out var hasConstantValue,
                        out var argument,
                        out var reconstructionResult
                    ))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.InvalidRuleMetadata,
                            invocation.GetLocation(),
                            symbol.Name,
                            sourceArgument
                        )
                    );
                    return null;
                }

                if (!hasConstantValue &&
                    argument is not null &&
                    diagnosedArguments.Add(sourceArgument!))
                {
                    var descriptor =
                        reconstructionResult == MetadataReconstructionResult.MultiFileFieldUnsupported ?
                            DiagnosticDescriptors.MultiFileMetadataFieldUnsupported :
                            DiagnosticDescriptors.MetadataValueCannotBeReconstructed;
                    diagnostics.Add(
                        Diagnostic.Create(
                            descriptor,
                            argument.GetLocation(),
                            symbol.Name,
                            sourceArgument
                        )
                    );
                }

                metadataValues.Add(new MetadataValueModel(metadataKey!, value, hasConstantValue, valueTypeName));
                continue;
            }

            if (TryGetConstantMetadataValue(metadataAttribute, out var constantValue, out var constantTypeName))
            {
                metadataValues.Add(new MetadataValueModel(metadataKey!, constantValue, true, constantTypeName));
            }
        }

        if (!TryGetMetadataSchemaProperties(
                ruleAttribute,
                code!,
                symbol.Name,
                invocation.GetLocation(),
                diagnostics,
                out var metadataSchemaProperties
            ))
        {
            return null;
        }

        var typedValueTypeName = ResolveTypedValueTypeName(symbol, shape);
        var message = CreateExampleMessage(definitionSymbol, symbol.Name, displayName, metadataValues, diagnostics);
        return new RuleCallModel(
            code!,
            shape,
            target,
            message,
            typedValueTypeName,
            metadataValues.ToImmutable(),
            metadataSchemaProperties
        );
    }

    private static string? CreateExampleMessage(
        IMethodSymbol definitionSymbol,
        string ruleName,
        string? displayName,
        ImmutableArray<MetadataValueModel>.Builder metadataValues,
        ICollection<Diagnostic> diagnostics
    )
    {
        var messageAttribute = GetAttribute(definitionSymbol, KnownTypeNames.ValidationRuleMessageAttribute);
        var template = messageAttribute?.ConstructorArguments.Length > 0 ?
            messageAttribute.ConstructorArguments[0].Value as string :
            null;
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var allowedPlaceholders = new HashSet<string>(
            metadataValues.Select(static metadata => metadata.Key),
            StringComparer.Ordinal
        ) { "displayName" };
        if (!TryParseMessageTemplate(
                template!,
                allowedPlaceholders,
                ruleName,
                messageAttribute!,
                diagnostics,
                out var parts
            ))
        {
            return null;
        }

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
        if (displayName is not null)
        {
            replacements.Add("displayName", displayName);
        }

        foreach (var metadata in metadataValues)
        {
            if (!metadata.HasConstantValue)
            {
                continue;
            }

            replacements[metadata.Key] = FormatMessageValue(metadata.Value);
        }

        var builder = new StringBuilder(template!.Length);
        foreach (var part in parts)
        {
            if (part.Placeholder is null)
            {
                builder.Append(part.Text);
                continue;
            }

            if (!replacements.TryGetValue(part.Placeholder, out var replacement))
            {
                return null;
            }

            builder.Append(replacement);
        }

        return builder.ToString();
    }

    private static bool TryParseMessageTemplate(
        string template,
        ISet<string> allowedPlaceholders,
        string ruleName,
        AttributeData messageAttribute,
        ICollection<Diagnostic> diagnostics,
        out ImmutableArray<MessageTemplatePart> parts
    )
    {
        var builder = ImmutableArray.CreateBuilder<MessageTemplatePart>();
        var literal = new StringBuilder();
        var isValid = true;

        for (var i = 0; i < template.Length; i++)
        {
            var c = template[i];
            if (c == '{')
            {
                if (i + 1 < template.Length && template[i + 1] == '{')
                {
                    literal.Append('{');
                    i++;
                    continue;
                }

                var closeIndex = template.IndexOf('}', i + 1);
                if (closeIndex < 0)
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MalformedMessageTemplate,
                            GetAttributeLocation(messageAttribute),
                            ruleName,
                            "unmatched '{'"
                        )
                    );
                    isValid = false;
                    break;
                }

                var placeholder = template.Substring(i + 1, closeIndex - i - 1);
                if (!IsBarePlaceholderName(placeholder))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MalformedMessageTemplate,
                            GetAttributeLocation(messageAttribute),
                            ruleName,
                            $"placeholder '{{{placeholder}}}' is not a bare identifier"
                        )
                    );
                    isValid = false;
                    i = closeIndex;
                    continue;
                }

                if (literal.Length > 0)
                {
                    builder.Add(MessageTemplatePart.Literal(literal.ToString()));
                    literal.Clear();
                }

                if (!allowedPlaceholders.Contains(placeholder))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.UnknownMessageTemplatePlaceholder,
                            GetAttributeLocation(messageAttribute),
                            ruleName,
                            placeholder
                        )
                    );
                    isValid = false;
                }

                builder.Add(MessageTemplatePart.PlaceholderValue(placeholder));
                i = closeIndex;
                continue;
            }

            if (c == '}')
            {
                if (i + 1 < template.Length && template[i + 1] == '}')
                {
                    literal.Append('}');
                    i++;
                    continue;
                }

                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MalformedMessageTemplate,
                        GetAttributeLocation(messageAttribute),
                        ruleName,
                        "unmatched '}'"
                    )
                );
                isValid = false;
                continue;
            }

            literal.Append(c);
        }

        if (literal.Length > 0)
        {
            builder.Add(MessageTemplatePart.Literal(literal.ToString()));
        }

        parts = builder.ToImmutable();
        return isValid;
    }

    private static bool IsBarePlaceholderName(string value)
    {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatMessageValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            ReconstructedMetadataValue reconstructedValue => reconstructedValue.ToCanonicalString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool TryGetMetadataSchemaProperties(
        AttributeData ruleAttribute,
        string code,
        string ruleName,
        Location location,
        ICollection<Diagnostic> diagnostics,
        out ImmutableArray<MetadataSchemaPropertyModel> metadataSchemaProperties
    )
    {
        var builder = ImmutableArray.CreateBuilder<MetadataSchemaPropertyModel>();
        metadataSchemaProperties = builder.ToImmutable();

        var errorDefinitionType = ruleAttribute.NamedArguments.FirstOrDefault(
            static argument =>
                argument.Key == "ErrorDefinitionType"
        ).Value.Value as INamedTypeSymbol;
        if (errorDefinitionType is null)
        {
            return true;
        }

        var contractAttribute = GetAttribute(errorDefinitionType, KnownTypeNames.ValidationErrorContractAttribute);
        var contractCode = contractAttribute?.ConstructorArguments.Length > 0 ?
            contractAttribute.ConstructorArguments[0].Value as string :
            null;
        if (contractCode != code)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidErrorContract,
                    location,
                    ruleName,
                    errorDefinitionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    code
                )
            );
            return false;
        }

        foreach (var metadataAttribute in errorDefinitionType.GetAttributes()
                    .Where(
                         static attribute => IsAttribute(
                             attribute,
                             KnownTypeNames.ValidationErrorMetadataContractAttribute
                         )
                     ))
        {
            if (metadataAttribute.ConstructorArguments.Length < 2 ||
                metadataAttribute.ConstructorArguments[0].Value is not string metadataKey ||
                metadataAttribute.ConstructorArguments[1].Value is not ITypeSymbol metadataType)
            {
                continue;
            }

            builder.Add(
                new MetadataSchemaPropertyModel(
                    metadataKey,
                    metadataType.ToDisplayString(FullyQualifiedTypeFormat)
                )
            );
        }

        metadataSchemaProperties = builder
           .OrderBy(static property => property.Key, StringComparer.Ordinal)
           .ToImmutableArray();
        return true;
    }

    private static bool TryGetConstantMetadataValue(
        AttributeData metadataAttribute,
        out object? value,
        out string typeName
    )
    {
        value = null;
        typeName = "object";
        string? stringValue = null;
        long int64Value = 0;
        var hasInt64Value = false;
        var booleanValue = false;
        var hasBooleanValue = false;
        ITypeSymbol? typeValue = null;

        foreach (var namedArgument in metadataAttribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "ConstantStringValue":
                    stringValue = namedArgument.Value.Value as string;
                    break;
                case "ConstantInt64Value" when namedArgument.Value.Value is long longValue:
                    int64Value = longValue;
                    break;
                case "HasConstantInt64Value" when namedArgument.Value.Value is bool boolValue:
                    hasInt64Value = boolValue;
                    break;
                case "ConstantBooleanValue" when namedArgument.Value.Value is bool boolValue:
                    booleanValue = boolValue;
                    break;
                case "HasConstantBooleanValue" when namedArgument.Value.Value is bool boolValue:
                    hasBooleanValue = boolValue;
                    break;
                case "ConstantTypeValue" when namedArgument.Value.Value is ITypeSymbol symbolValue:
                    typeValue = symbolValue;
                    break;
            }
        }

        if (stringValue is not null)
        {
            value = stringValue;
            typeName = "global::System.String";
            return true;
        }

        if (hasInt64Value)
        {
            value = int64Value;
            typeName = "global::System.Int64";
            return true;
        }

        if (hasBooleanValue)
        {
            value = booleanValue;
            typeName = "global::System.Boolean";
            return true;
        }

        if (typeValue is not null)
        {
            value = typeValue.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            typeName = "global::System.String";
            return true;
        }

        return false;
    }

    private static bool TryResolveArgumentConstant(
        SemanticModel semanticModel,
        MetadataValueReconstructor metadataValueReconstructor,
        InvocationExpressionSyntax invocation,
        IMethodSymbol symbol,
        string sourceArgument,
        CancellationToken cancellationToken,
        out object? value,
        out string typeName,
        out bool hasConstantValue,
        out ArgumentSyntax? resolvedArgument,
        out MetadataReconstructionResult reconstructionResult
    )
    {
        value = null;
        typeName = "object";
        hasConstantValue = false;
        resolvedArgument = null;
        reconstructionResult = MetadataReconstructionResult.Unsupported;

        var parameterIndex = -1;
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            if (symbol.Parameters[i].Name == sourceArgument)
            {
                parameterIndex = i;
                typeName = symbol.Parameters[i].Type.ToDisplayString(FullyQualifiedTypeFormat);
                break;
            }
        }

        if (parameterIndex < 0)
        {
            return false;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.NameColon is not null)
            {
                if (argument.NameColon.Name.Identifier.ValueText != sourceArgument)
                {
                    continue;
                }
            }
            else if (invocation.ArgumentList.Arguments.IndexOf(argument) != parameterIndex)
            {
                continue;
            }

            var constant = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
            resolvedArgument = argument;
            if (constant.HasValue)
            {
                value = constant.Value;
                hasConstantValue = true;
                reconstructionResult = MetadataReconstructionResult.Success;
            }
            else
            {
                reconstructionResult = metadataValueReconstructor.TryReconstruct(
                    semanticModel,
                    argument.Expression,
                    cancellationToken,
                    out var reconstructedValue
                );
                if (reconstructionResult == MetadataReconstructionResult.Success)
                {
                    value = reconstructedValue;
                    hasConstantValue = true;
                }
            }

            return true;
        }

        var parameter = symbol.Parameters[parameterIndex];
        if (parameter.HasExplicitDefaultValue)
        {
            value = parameter.ExplicitDefaultValue;
            hasConstantValue = true;
            reconstructionResult = MetadataReconstructionResult.Success;
        }

        return true;
    }

    private static void WarnForNestedChecks(
        SemanticModel semanticModel,
        StatementSyntax statement,
        ICollection<Diagnostic> diagnostics,
        CancellationToken cancellationToken
    )
    {
        foreach (var invocation in statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsValidationContextCheck(semanticModel, invocation, cancellationToken))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NestedCheckSkipped,
                        invocation.GetLocation(),
                        invocation.Expression.ToString()
                    )
                );
            }
        }
    }

    private static bool TryGetTopLevelCheckExpression(StatementSyntax statement, out ExpressionSyntax expression)
    {
        expression = null!;
        switch (statement)
        {
            case ExpressionStatementSyntax expressionStatement:
                expression = UnwrapAssignment(expressionStatement.Expression);
                return true;
            case LocalDeclarationStatementSyntax { Declaration.Variables.Count: 1 } localDeclaration:
                var initializer = localDeclaration.Declaration.Variables[0].Initializer;
                if (initializer is null)
                {
                    return false;
                }

                expression = initializer.Value;
                return true;
            default:
                return false;
        }
    }

    private static ExpressionSyntax UnwrapAssignment(ExpressionSyntax expression)
    {
        return expression is AssignmentExpressionSyntax assignment ?
            assignment.Right :
            expression;
    }

    private static List<InvocationExpressionSyntax> CollectInvocationChain(ExpressionSyntax expression)
    {
        var invocations = new List<InvocationExpressionSyntax>();
        CollectInvocationChain(expression, invocations);
        return invocations;
    }

    private static void CollectInvocationChain(
        ExpressionSyntax expression,
        ICollection<InvocationExpressionSyntax> invocations
    )
    {
        if (expression is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            CollectInvocationChain(memberAccess.Expression, invocations);
        }

        invocations.Add(invocation);
    }

    private static bool IsValidationContextCheck(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken
    )
    {
        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        return symbol is not null &&
               symbol.Name == "Check" &&
               GetMetadataName(symbol.ContainingType) == KnownTypeNames.ValidationContext;
    }

    private static string? TryInferTarget(
        SemanticModel semanticModel,
        InvocationExpressionSyntax checkInvocation,
        string? sourceParameterName,
        CancellationToken cancellationToken
    )
    {
        if (sourceParameterName is null || checkInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var valueExpression = checkInvocation.ArgumentList.Arguments[0].Expression;
        if (valueExpression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Expression is not IdentifierNameSyntax identifier ||
            identifier.Identifier.ValueText != sourceParameterName)
        {
            return null;
        }

        var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        if (symbol is IPropertySymbol propertySymbol)
        {
            var jsonName = GetJsonPropertyName(propertySymbol);
            if (!string.IsNullOrWhiteSpace(jsonName))
            {
                return jsonName;
            }
        }

        return ToCamelCase(memberAccess.Name.Identifier.ValueText);
    }

    private static string? TryGetExplicitDisplayName(
        SemanticModel semanticModel,
        InvocationExpressionSyntax checkInvocation,
        CancellationToken cancellationToken
    )
    {
        var symbol = semanticModel.GetSymbolInfo(checkInvocation, cancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return null;
        }

        var displayNameParameterIndex = -1;
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            if (symbol.Parameters[i].Name == "displayName")
            {
                displayNameParameterIndex = i;
                break;
            }
        }

        if (displayNameParameterIndex < 0)
        {
            return null;
        }

        for (var i = 0; i < checkInvocation.ArgumentList.Arguments.Count; i++)
        {
            var argument = checkInvocation.ArgumentList.Arguments[i];
            if (argument.NameColon is not null)
            {
                if (argument.NameColon.Name.Identifier.ValueText != "displayName")
                {
                    continue;
                }
            }
            else if (i != displayNameParameterIndex)
            {
                continue;
            }

            var constant = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
            return constant.HasValue ? constant.Value as string : null;
        }

        return null;
    }

    private static string? GetJsonPropertyName(IPropertySymbol propertySymbol)
    {
        foreach (var attribute in propertySymbol.GetAttributes())
        {
            if (IsAttribute(attribute, KnownTypeNames.JsonPropertyNameAttribute) &&
                attribute.ConstructorArguments.Length == 1)
            {
                return attribute.ConstructorArguments[0].Value as string;
            }
        }

        return null;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string? ResolveTypedValueTypeName(IMethodSymbol symbol, RuleMetadataShape shape)
    {
        if (shape == RuleMetadataShape.Registered)
        {
            return null;
        }

        if (symbol.TypeArguments.Length > 0)
        {
            return symbol.TypeArguments[0].ToDisplayString(FullyQualifiedTypeFormat);
        }

        return null;
    }

    private static bool UsesErrorOverrides(IMethodSymbol symbol)
    {
        return symbol.Parameters.Any(
            static parameter => GetMetadataName(parameter.Type) == KnownTypeNames.ErrorOverrides
        );
    }

    private static AttributeData? GetAttribute(ISymbol symbol, string metadataName)
    {
        return symbol.GetAttributes().FirstOrDefault(attribute => IsAttribute(attribute, metadataName));
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return attribute.AttributeClass is not null && GetMetadataName(attribute.AttributeClass) == metadataName;
    }

    private static IEnumerable<ErrorHintModel> GetErrorHints(
        INamedTypeSymbol validatorType,
        IMethodSymbol performValidation,
        ICollection<Diagnostic> diagnostics
    )
    {
        foreach (var hint in GetErrorHints(validatorType.GetAttributes(), diagnostics))
        {
            yield return hint;
        }

        foreach (var hint in GetErrorHints(performValidation.GetAttributes(), diagnostics))
        {
            yield return hint;
        }
    }

    private static IEnumerable<ErrorHintModel> GetErrorHints(
        IEnumerable<AttributeData> attributes,
        ICollection<Diagnostic> diagnostics
    )
    {
        var attributeArray = attributes.ToArray();
        var metadataProperties = attributeArray
           .Where(static attribute => IsAttribute(attribute, KnownTypeNames.ErrorMetadataPropertyAttribute))
           .Select(
                static attribute => new
                {
                    Attribute = attribute,
                    Code = attribute.ConstructorArguments.Length > 0 ?
                        attribute.ConstructorArguments[0].Value as string :
                        null
                }
            )
           .Where(static item => !string.IsNullOrWhiteSpace(item.Code))
           .GroupBy(static item => item.Code!, StringComparer.Ordinal)
           .ToDictionary(
                static group => group.Key,
                group => group.Select(static item => item.Attribute).ToArray(),
                StringComparer.Ordinal
            );
        var parentCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var attribute in attributeArray)
        {
            if (!IsAttribute(attribute, KnownTypeNames.ErrorHintAttribute) ||
                attribute.ConstructorArguments.Length == 0 ||
                attribute.ConstructorArguments[0].Value is not string code)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "error hint code must not be empty"
                    )
                );
                continue;
            }

            parentCodes.Add(code);
            var metadataTypeName = attribute.ConstructorArguments.Length > 1 &&
                                   attribute.ConstructorArguments[1].Value is ITypeSymbol metadataType ?
                metadataType.ToDisplayString(FullyQualifiedTypeFormat) :
                null;
            if (metadataTypeName == "void" || metadataTypeName == "global::System.Void")
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "metadata type must not be void"
                    )
                );
                continue;
            }

            var properties = GetInlineHintMetadataProperties(
                metadataProperties.TryGetValue(code, out var matchedProperties) ? matchedProperties : [],
                diagnostics
            );
            yield return new ErrorHintModel(code, metadataTypeName, properties);
        }

        foreach (var metadataProperty in attributeArray.Where(
                     static attribute => IsAttribute(attribute, KnownTypeNames.ErrorMetadataPropertyAttribute)
                 ))
        {
            var code = metadataProperty.ConstructorArguments.Length > 0 ?
                metadataProperty.ConstructorArguments[0].Value as string :
                null;
            if (string.IsNullOrWhiteSpace(code) || parentCodes.Contains(code!))
            {
                continue;
            }

            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidHint,
                    GetAttributeLocation(metadataProperty),
                    $"inline metadata property for code '{code}' has no matching error hint in the same scope"
                )
            );
        }
    }

    private static ImmutableArray<MetadataSchemaPropertyModel> GetInlineHintMetadataProperties(
        IEnumerable<AttributeData> attributes,
        ICollection<Diagnostic> diagnostics
    )
    {
        var builder = ImmutableArray.CreateBuilder<MetadataSchemaPropertyModel>();
        foreach (var attribute in attributes)
        {
            if (attribute.ConstructorArguments.Length < 3 ||
                attribute.ConstructorArguments[1].Value is not string key ||
                attribute.ConstructorArguments[2].Value is not ITypeSymbol type)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "metadata key must not be empty"
                    )
                );
                continue;
            }

            var typeName = type.ToDisplayString(FullyQualifiedTypeFormat);
            if (typeName == "void" || typeName == "global::System.Void")
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "metadata property type must not be void"
                    )
                );
                continue;
            }

            builder.Add(new MetadataSchemaPropertyModel(key, typeName));
        }

        return builder
           .GroupBy(static property => property.Key, StringComparer.Ordinal)
           .Select(static group => group.First())
           .OrderBy(static property => property.Key, StringComparer.Ordinal)
           .ToImmutableArray();
    }

    private static IEnumerable<ExampleHintModel> GetExampleHints(
        INamedTypeSymbol validatorType,
        IMethodSymbol performValidation,
        ICollection<Diagnostic> diagnostics
    )
    {
        foreach (var hint in GetExampleHints(validatorType.GetAttributes(), diagnostics))
        {
            yield return hint;
        }

        foreach (var hint in GetExampleHints(performValidation.GetAttributes(), diagnostics))
        {
            yield return hint;
        }
    }

    private static IEnumerable<ExampleHintModel> GetExampleHints(
        IEnumerable<AttributeData> attributes,
        ICollection<Diagnostic> diagnostics
    )
    {
        var attributeArray = attributes.ToArray();
        var parentCodes = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attribute in attributeArray.Where(
                     static attribute => IsAttribute(attribute, KnownTypeNames.ExampleHintAttribute)
                 ))
        {
            var code = attribute.ConstructorArguments.Length > 0 ?
                attribute.ConstructorArguments[0].Value as string :
                null;
            if (string.IsNullOrWhiteSpace(code))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "example hint code must not be empty"
                    )
                );
                continue;
            }

            if (!parentCodes.Add(code!))
            {
                duplicates.Add(code!);
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        $"more than one example hint for code '{code}' is declared in the same scope"
                    )
                );
                continue;
            }

            var target = attribute.NamedArguments.FirstOrDefault(static argument => argument.Key == "Target")
               .Value.Value as string;
            var message = attribute.NamedArguments.FirstOrDefault(static argument => argument.Key == "Message")
               .Value.Value as string;
            var metadataValues = GetExampleMetadataValues(attributeArray, code!, diagnostics);
            yield return new ExampleHintModel(code!, target, message, metadataValues);
        }

        foreach (var metadataAttribute in attributeArray.Where(
                     static attribute => IsAttribute(attribute, KnownTypeNames.ExampleMetadataAttribute)
                 ))
        {
            var code = metadataAttribute.ConstructorArguments.Length > 0 ?
                metadataAttribute.ConstructorArguments[0].Value as string :
                null;
            if (string.IsNullOrWhiteSpace(code) || parentCodes.Contains(code!) || duplicates.Contains(code!))
            {
                continue;
            }

            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidHint,
                    GetAttributeLocation(metadataAttribute),
                    $"example metadata for code '{code}' has no matching example hint in the same scope"
                )
            );
        }
    }

    private static ImmutableArray<MetadataValueModel> GetExampleMetadataValues(
        IEnumerable<AttributeData> attributes,
        string code,
        ICollection<Diagnostic> diagnostics
    )
    {
        var builder = ImmutableArray.CreateBuilder<MetadataValueModel>();
        foreach (var attribute in attributes.Where(
                     attribute => IsAttribute(attribute, KnownTypeNames.ExampleMetadataAttribute) &&
                                  attribute.ConstructorArguments.Length >= 3 &&
                                  attribute.ConstructorArguments[0].Value as string == code
                 ))
        {
            if (attribute.ConstructorArguments[1].Value is not string key ||
                string.IsNullOrWhiteSpace(key))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.InvalidHint,
                        GetAttributeLocation(attribute),
                        "example metadata key must not be empty"
                    )
                );
                continue;
            }

            var argument = attribute.ConstructorArguments[2];
            var value = argument.Value is ITypeSymbol typeSymbol ?
                typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) :
                argument.Value;
            var typeName = argument.Value is ITypeSymbol ?
                "global::System.String" :
                argument.Type?.ToDisplayString(FullyQualifiedTypeFormat) ?? "object";
            builder.Add(new MetadataValueModel(key, value, true, typeName));
        }

        return builder
           .OrderBy(static metadata => metadata.Key, StringComparer.Ordinal)
           .ToImmutableArray();
    }

    private static void ValidateHintContracts(
        IEnumerable<RuleCallModel> rules,
        ImmutableArray<ErrorHintModel> hints,
        ImmutableArray<ExampleHintModel> examples,
        ICollection<Diagnostic> diagnostics
    )
    {
        var typedHints = new Dictionary<string, string>(StringComparer.Ordinal);
        var inlineHints = new Dictionary<string, ImmutableArray<MetadataSchemaPropertyModel>>(StringComparer.Ordinal);
        foreach (var hint in hints)
        {
            if (hint.MetadataTypeName is not null)
            {
                if (typedHints.TryGetValue(hint.Code, out var existingType) &&
                    existingType != hint.MetadataTypeName)
                {
                    diagnostics.Add(
                        Diagnostic.Create(DiagnosticDescriptors.ConflictingHint, Location.None, hint.Code)
                    );
                }

                typedHints[hint.Code] = hint.MetadataTypeName;
            }

            if (hint.MetadataSchemaProperties.Length > 0)
            {
                if (hint.MetadataTypeName is not null)
                {
                    diagnostics.Add(
                        Diagnostic.Create(DiagnosticDescriptors.ConflictingHint, Location.None, hint.Code)
                    );
                }

                if (inlineHints.TryGetValue(hint.Code, out var existingProperties) &&
                    !MetadataSchemaPropertiesEqual(existingProperties, hint.MetadataSchemaProperties))
                {
                    diagnostics.Add(
                        Diagnostic.Create(DiagnosticDescriptors.ConflictingHint, Location.None, hint.Code)
                    );
                }

                inlineHints[hint.Code] = hint.MetadataSchemaProperties;
            }
        }

        foreach (var rule in rules.Where(static rule => rule.MetadataSchemaProperties.Length > 0))
        {
            if (inlineHints.TryGetValue(rule.Code, out var hintProperties) &&
                !MetadataSchemaPropertiesEqual(hintProperties, rule.MetadataSchemaProperties))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.ConflictingHint, Location.None, rule.Code));
            }
        }

        foreach (var example in examples)
        {
            if (!inlineHints.TryGetValue(example.Code, out var schemaProperties))
            {
                continue;
            }

            var schemaKeys = new HashSet<string>(
                schemaProperties.Select(static property => property.Key),
                StringComparer.Ordinal
            );
            foreach (var metadataValue in example.MetadataValues)
            {
                if (!schemaKeys.Contains(metadataValue.Key))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            DiagnosticDescriptors.ExampleMetadataWithoutSchema,
                            Location.None,
                            example.Code,
                            metadataValue.Key
                        )
                    );
                }
            }
        }
    }

    private static bool MetadataSchemaPropertiesEqual(
        ImmutableArray<MetadataSchemaPropertyModel> left,
        ImmutableArray<MetadataSchemaPropertyModel> right
    )
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i].Key != right[i].Key || left[i].TypeName != right[i].TypeName)
            {
                return false;
            }
        }

        return true;
    }

    private static Location GetAttributeLocation(AttributeData attribute)
    {
        var syntaxReference = attribute.ApplicationSyntaxReference;
        return syntaxReference?.GetSyntax().GetLocation() ?? Location.None;
    }

    private static bool GetAllowUnknownErrorCodes(INamedTypeSymbol validatorType)
    {
        var attribute = GetAttribute(validatorType, KnownTypeNames.GenerateAttribute);
        if (attribute is null)
        {
            return false;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument is { Key: "AllowUnknownErrorCodes", Value.Value: bool allowUnknownErrorCodes })
            {
                return allowUnknownErrorCodes;
            }
        }

        return false;
    }

    private static bool TryGetPerformValidationMethod(
        INamedTypeSymbol validatorType,
        CancellationToken cancellationToken,
        out IMethodSymbol? method,
        out MethodDeclarationSyntax? declaration
    )
    {
        foreach (var candidate in validatorType.GetMembers("PerformValidation").OfType<IMethodSymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.Parameters.Length != 3 ||
                candidate.DeclaredAccessibility != Accessibility.Protected ||
                candidate.DeclaringSyntaxReferences.Length == 0)
            {
                continue;
            }

            var syntax = candidate.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            if (syntax is MethodDeclarationSyntax methodDeclaration)
            {
                method = candidate;
                declaration = methodDeclaration;
                return true;
            }
        }

        method = null;
        declaration = null;
        return false;
    }

    private static bool TryGetPrimaryClassDeclaration(
        INamedTypeSymbol validatorType,
        CancellationToken cancellationToken,
        out ClassDeclarationSyntax declaration
    )
    {
        foreach (var syntaxReference in validatorType.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (syntaxReference.GetSyntax(cancellationToken) is ClassDeclarationSyntax classDeclaration)
            {
                declaration = classDeclaration;
                return true;
            }
        }

        declaration = null!;
        return false;
    }

    private static string GetAccessibility(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => "public"
        };

    private static string CreateHintName(INamedTypeSymbol validatorType)
    {
        var metadataName = validatorType
           .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
           .Replace("global::", string.Empty);
        var chars = metadataName.Select(static c => char.IsLetterOrDigit(c) || c == '.' ? c : '_').ToArray();
        return new string(chars) + ".PortableValidationOpenApi.g.cs";
    }

    private static string GetMetadataName(ITypeSymbol typeSymbol)
    {
        var containingNamespace = typeSymbol.ContainingNamespace?.IsGlobalNamespace == false ?
            typeSymbol.ContainingNamespace.ToDisplayString() + "." :
            string.Empty;
        return containingNamespace + typeSymbol.MetadataName;
    }

    private readonly struct MessageTemplatePart
    {
        private MessageTemplatePart(string text, string? placeholder)
        {
            Text = text;
            Placeholder = placeholder;
        }

        public string Text { get; }
        public string? Placeholder { get; }

        public static MessageTemplatePart Literal(string text) => new (text, null);

        public static MessageTemplatePart PlaceholderValue(string placeholder) => new (string.Empty, placeholder);
    }
}
