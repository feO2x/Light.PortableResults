using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        var sourceParameterName =
            performValidation.Parameters.Length >= 3 ? performValidation.Parameters[2].Name : null;
        var rules = ImmutableArray.CreateBuilder<RuleCallModel>();
        AnalyzePerformValidationBody(
            semanticModel,
            methodDeclaration,
            sourceParameterName,
            rules,
            diagnostics,
            cancellationToken
        );

        var hints = GetErrorHints(validatorType, performValidation).ToImmutableArray();
        var allowUnknownErrorCodes = GetAllowUnknownErrorCodes(validatorType);
        if (rules.Count == 0 && hints.Length == 0 && !allowUnknownErrorCodes)
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
            hints
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
                invocation,
                symbol,
                ruleAttribute,
                target,
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
        InvocationExpressionSyntax invocation,
        IMethodSymbol symbol,
        AttributeData ruleAttribute,
        string? target,
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
                        invocation,
                        symbol,
                        sourceArgument!,
                        cancellationToken,
                        out var value,
                        out var valueTypeName,
                        out var hasConstantValue
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
        return new RuleCallModel(
            code!,
            shape,
            target,
            typedValueTypeName,
            metadataValues.ToImmutable(),
            metadataSchemaProperties
        );
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
        InvocationExpressionSyntax invocation,
        IMethodSymbol symbol,
        string sourceArgument,
        CancellationToken cancellationToken,
        out object? value,
        out string typeName,
        out bool hasConstantValue
    )
    {
        value = null;
        typeName = "object";
        hasConstantValue = false;

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
            if (constant.HasValue)
            {
                value = constant.Value;
                hasConstantValue = true;
            }

            return true;
        }

        var parameter = symbol.Parameters[parameterIndex];
        if (parameter.HasExplicitDefaultValue)
        {
            value = parameter.ExplicitDefaultValue;
            hasConstantValue = true;
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
        IMethodSymbol performValidation
    )
    {
        foreach (var hint in GetErrorHints(validatorType.GetAttributes()))
        {
            yield return hint;
        }

        foreach (var hint in GetErrorHints(performValidation.GetAttributes()))
        {
            yield return hint;
        }
    }

    private static IEnumerable<ErrorHintModel> GetErrorHints(IEnumerable<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (!IsAttribute(attribute, KnownTypeNames.ErrorHintAttribute) ||
                attribute.ConstructorArguments.Length == 0 ||
                attribute.ConstructorArguments[0].Value is not string code)
            {
                continue;
            }

            var metadataTypeName = attribute.ConstructorArguments.Length > 1 &&
                                   attribute.ConstructorArguments[1].Value is ITypeSymbol metadataType ?
                metadataType.ToDisplayString(FullyQualifiedTypeFormat) :
                null;
            yield return new ErrorHintModel(code, metadataTypeName);
        }
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
}
