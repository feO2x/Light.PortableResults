using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

internal enum MetadataReconstructionResult
{
    Success = 0,
    Unsupported = 1,
    MultiFileFieldUnsupported = 2
}

internal static class MetadataValueReconstructor
{
    private const int MaximumDepth = 4;
    private const long TicksPerMicrosecond = 10L;

    public static MetadataReconstructionResult TryReconstruct(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out object? value
    )
    {
        var frameworkTypes = new FrameworkTypes(semanticModel.Compilation);
        var resolvingFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
        return TryReconstruct(
            semanticModel,
            expression,
            frameworkTypes,
            depth: 0,
            resolvingFields,
            cancellationToken,
            out value
        );
    }

    private static MetadataReconstructionResult TryReconstruct(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        FrameworkTypes frameworkTypes,
        int depth,
        ISet<IFieldSymbol> resolvingFields,
        CancellationToken cancellationToken,
        out object? value
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        value = null;
        if (depth > MaximumDepth)
        {
            return MetadataReconstructionResult.Unsupported;
        }

        var constant = semanticModel.GetConstantValue(expression, cancellationToken);
        if (constant.HasValue)
        {
            value = constant.Value;
            return MetadataReconstructionResult.Success;
        }

        var operation = semanticModel.GetOperation(expression, cancellationToken);
        switch (operation)
        {
            case IObjectCreationOperation objectCreation:
                return TryReconstructObjectCreation(
                    semanticModel,
                    objectCreation,
                    frameworkTypes,
                    depth,
                    resolvingFields,
                    cancellationToken,
                    out value
                );
            case IInvocationOperation invocation:
                return TryReconstructInvocation(
                    semanticModel,
                    invocation,
                    frameworkTypes,
                    depth,
                    resolvingFields,
                    cancellationToken,
                    out value
                );
            case IFieldReferenceOperation fieldReference:
                return TryReconstructField(
                    semanticModel,
                    fieldReference.Field,
                    frameworkTypes,
                    depth,
                    resolvingFields,
                    cancellationToken,
                    out value
                );
            case IPropertyReferenceOperation propertyReference
                when TryGetWellKnownStatic(propertyReference.Property, frameworkTypes, out value):
                return MetadataReconstructionResult.Success;
            default:
                return MetadataReconstructionResult.Unsupported;
        }
    }

    private static MetadataReconstructionResult TryReconstructObjectCreation(
        SemanticModel semanticModel,
        IObjectCreationOperation operation,
        FrameworkTypes frameworkTypes,
        int depth,
        ISet<IFieldSymbol> resolvingFields,
        CancellationToken cancellationToken,
        out object? value
    )
    {
        value = null;
        var constructor = operation.Constructor;
        if (constructor is null ||
            !TryGetSupportedConstructor(constructor, frameworkTypes, out var supportedConstructor))
        {
            return MetadataReconstructionResult.Unsupported;
        }

        var argumentResult = TryReconstructArguments(
            semanticModel,
            constructor,
            operation.Arguments,
            frameworkTypes,
            depth,
            resolvingFields,
            cancellationToken,
            out var arguments
        );
        if (argumentResult != MetadataReconstructionResult.Success)
        {
            return argumentResult;
        }

        try
        {
            value = EvaluateConstructor(supportedConstructor, arguments);
            return MetadataReconstructionResult.Success;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            value = null;
            return MetadataReconstructionResult.Unsupported;
        }
    }

    private static MetadataReconstructionResult TryReconstructInvocation(
        SemanticModel semanticModel,
        IInvocationOperation operation,
        FrameworkTypes frameworkTypes,
        int depth,
        ISet<IFieldSymbol> resolvingFields,
        CancellationToken cancellationToken,
        out object? value
    )
    {
        value = null;
        var method = operation.TargetMethod;
        if (!TryGetSupportedFactory(method, frameworkTypes, out var supportedFactory))
        {
            return MetadataReconstructionResult.Unsupported;
        }

        var argumentResult = TryReconstructArguments(
            semanticModel,
            method,
            operation.Arguments,
            frameworkTypes,
            depth,
            resolvingFields,
            cancellationToken,
            out var arguments
        );
        if (argumentResult != MetadataReconstructionResult.Success)
        {
            return argumentResult;
        }

        try
        {
            value = EvaluateFactory(supportedFactory, arguments);
            return MetadataReconstructionResult.Success;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            value = null;
            return MetadataReconstructionResult.Unsupported;
        }
    }

    private static MetadataReconstructionResult TryReconstructArguments(
        SemanticModel semanticModel,
        IMethodSymbol method,
        ImmutableArray<IArgumentOperation> argumentOperations,
        FrameworkTypes frameworkTypes,
        int depth,
        ISet<IFieldSymbol> resolvingFields,
        CancellationToken cancellationToken,
        out object?[] arguments
    )
    {
        arguments = new object?[method.Parameters.Length];
        var assigned = new bool[method.Parameters.Length];
        foreach (var argumentOperation in argumentOperations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameter = argumentOperation.Parameter;
            if (parameter is null || argumentOperation.IsImplicit)
            {
                return MetadataReconstructionResult.Unsupported;
            }

            if (argumentOperation.Value.Syntax is not ExpressionSyntax argumentExpression)
            {
                return MetadataReconstructionResult.Unsupported;
            }

            var constant = semanticModel.GetConstantValue(argumentExpression, cancellationToken);
            if (constant.HasValue)
            {
                arguments[parameter.Ordinal] = constant.Value;
                assigned[parameter.Ordinal] = true;
                continue;
            }

            var reconstructionResult = TryReconstruct(
                semanticModel,
                argumentExpression,
                frameworkTypes,
                depth + 1,
                resolvingFields,
                cancellationToken,
                out var reconstructedValue
            );
            if (reconstructionResult != MetadataReconstructionResult.Success)
            {
                return reconstructionResult;
            }

            arguments[parameter.Ordinal] = reconstructedValue;
            assigned[parameter.Ordinal] = true;
        }

        return assigned.All(static isAssigned => isAssigned) ?
            MetadataReconstructionResult.Success :
            MetadataReconstructionResult.Unsupported;
    }

    private static MetadataReconstructionResult TryReconstructField(
        SemanticModel semanticModel,
        IFieldSymbol field,
        FrameworkTypes frameworkTypes,
        int depth,
        ISet<IFieldSymbol> resolvingFields,
        CancellationToken cancellationToken,
        out object? value
    )
    {
        value = null;
        if (TryGetWellKnownStatic(field, frameworkTypes, out value))
        {
            return MetadataReconstructionResult.Success;
        }

        if (!field.IsStatic || !field.IsReadOnly)
        {
            return MetadataReconstructionResult.Unsupported;
        }

        if (field.DeclaringSyntaxReferences.Length != 1 ||
            field.ContainingType.DeclaringSyntaxReferences.Length != 1)
        {
            return MetadataReconstructionResult.MultiFileFieldUnsupported;
        }

        var fieldSyntaxReference = field.DeclaringSyntaxReferences[0];
        var typeSyntaxReference = field.ContainingType.DeclaringSyntaxReferences[0];
        if (fieldSyntaxReference.SyntaxTree != semanticModel.SyntaxTree ||
            typeSyntaxReference.SyntaxTree != semanticModel.SyntaxTree)
        {
            return MetadataReconstructionResult.MultiFileFieldUnsupported;
        }

        if (!resolvingFields.Add(field))
        {
            return MetadataReconstructionResult.Unsupported;
        }

        try
        {
            if (fieldSyntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax
                {
                    Initializer.Value: ExpressionSyntax initializer
                } ||
                IsAssignedInStaticConstructor(
                    semanticModel,
                    field,
                    typeSyntaxReference,
                    cancellationToken
                ))
            {
                return MetadataReconstructionResult.Unsupported;
            }

            return TryReconstruct(
                semanticModel,
                initializer,
                frameworkTypes,
                depth + 1,
                resolvingFields,
                cancellationToken,
                out value
            );
        }
        finally
        {
            resolvingFields.Remove(field);
        }
    }

    private static bool IsAssignedInStaticConstructor(
        SemanticModel semanticModel,
        IFieldSymbol field,
        SyntaxReference typeSyntaxReference,
        CancellationToken cancellationToken
    )
    {
        if (typeSyntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
        {
            return true;
        }

        foreach (var constructor in typeDeclaration.Members
                    .OfType<ConstructorDeclarationSyntax>()
                    .Where(static constructor => constructor.Modifiers.Any(SyntaxKind.StaticKeyword)))
        {
            foreach (var assignment in constructor.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var assignedSymbol = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
                if (SymbolEqualityComparer.Default.Equals(assignedSymbol, field))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetWellKnownStatic(
        ISymbol field,
        FrameworkTypes frameworkTypes,
        out object? value
    )
    {
        value = null;
        if (!field.IsStatic)
        {
            return false;
        }

        if (IsType(field.ContainingType, frameworkTypes.DateTime))
        {
            var dateTime = field.Name switch
            {
                "MinValue" => DateTime.MinValue,
                "MaxValue" => DateTime.MaxValue,
                "UnixEpoch" => new DateTime(621_355_968_000_000_000L, DateTimeKind.Utc),
                _ => (DateTime?) null
            };
            if (dateTime.HasValue)
            {
                value = ReconstructedMetadataValue.FromDateTime(dateTime.Value);
                return true;
            }
        }

        if (IsType(field.ContainingType, frameworkTypes.DateTimeOffset))
        {
            var dateTimeOffset = field.Name switch
            {
                "MinValue" => DateTimeOffset.MinValue,
                "MaxValue" => DateTimeOffset.MaxValue,
                "UnixEpoch" => new DateTimeOffset(621_355_968_000_000_000L, TimeSpan.Zero),
                _ => (DateTimeOffset?) null
            };
            if (dateTimeOffset.HasValue)
            {
                value = ReconstructedMetadataValue.FromDateTimeOffset(dateTimeOffset.Value);
                return true;
            }
        }

        if (IsType(field.ContainingType, frameworkTypes.TimeSpan))
        {
            var timeSpan = field.Name switch
            {
                "Zero" => TimeSpan.Zero,
                "MinValue" => TimeSpan.MinValue,
                "MaxValue" => TimeSpan.MaxValue,
                _ => (TimeSpan?) null
            };
            if (timeSpan.HasValue)
            {
                value = ReconstructedMetadataValue.FromTimeSpan(timeSpan.Value);
                return true;
            }
        }

        if (IsType(field.ContainingType, frameworkTypes.DateOnly))
        {
            if (field.Name == "MinValue")
            {
                value = ReconstructedMetadataValue.FromDateOnlyDayNumber(0);
                return true;
            }

            if (field.Name == "MaxValue")
            {
                value = ReconstructedMetadataValue.FromDateOnlyDayNumber(3_652_058);
                return true;
            }
        }

        if (IsType(field.ContainingType, frameworkTypes.TimeOnly))
        {
            if (field.Name == "MinValue")
            {
                value = ReconstructedMetadataValue.FromTimeOnlyTicks(0L);
                return true;
            }

            if (field.Name == "MaxValue")
            {
                value = ReconstructedMetadataValue.FromTimeOnlyTicks(TimeSpan.TicksPerDay - 1L);
                return true;
            }
        }

        if (IsType(field.ContainingType, frameworkTypes.Guid) && field.Name == "Empty")
        {
            value = ReconstructedMetadataValue.FromGuid(Guid.Empty);
            return true;
        }

        return false;
    }

    private static bool TryGetSupportedConstructor(
        IMethodSymbol method,
        FrameworkTypes frameworkTypes,
        out SupportedConstructor constructor
    )
    {
        constructor = default;
        if (Matches(method, frameworkTypes.DateTime, ParameterType.Int32, ParameterType.Int32, ParameterType.Int32))
        {
            constructor = SupportedConstructor.DateTimeDate;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTime,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.DateTimeSecond;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTime,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.DateTimeMillisecond;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTime,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.DateTimeKind
                 ))
        {
            constructor = SupportedConstructor.DateTimeSecondKind;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTime,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.DateTimeKind
                 ))
        {
            constructor = SupportedConstructor.DateTimeMillisecondKind;
        }
        else if (Matches(method, frameworkTypes.DateTime, ParameterType.Int64))
        {
            constructor = SupportedConstructor.DateTimeTicks;
        }
        else if (Matches(method, frameworkTypes.DateTime, ParameterType.Int64, ParameterType.DateTimeKind))
        {
            constructor = SupportedConstructor.DateTimeTicksKind;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTimeOffset,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.TimeSpan
                 ))
        {
            constructor = SupportedConstructor.DateTimeOffsetSecond;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateTimeOffset,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.TimeSpan
                 ))
        {
            constructor = SupportedConstructor.DateTimeOffsetMillisecond;
        }
        else if (Matches(method, frameworkTypes.DateTimeOffset, ParameterType.Int64, ParameterType.TimeSpan))
        {
            constructor = SupportedConstructor.DateTimeOffsetTicks;
        }
        else if (Matches(method, frameworkTypes.DateTimeOffset, ParameterType.DateTime, ParameterType.TimeSpan))
        {
            constructor = SupportedConstructor.DateTimeOffsetDateTime;
        }
        else if (Matches(
                     method,
                     frameworkTypes.TimeSpan,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.TimeSpanHours;
        }
        else if (Matches(
                     method,
                     frameworkTypes.TimeSpan,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.TimeSpanDays;
        }
        else if (Matches(
                     method,
                     frameworkTypes.TimeSpan,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.TimeSpanMilliseconds;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, ParameterType.Int64))
        {
            constructor = SupportedConstructor.TimeSpanTicks;
        }
        else if (Matches(
                     method,
                     frameworkTypes.DateOnly,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.DateOnly;
        }
        else if (Matches(method, frameworkTypes.TimeOnly, ParameterType.Int32, ParameterType.Int32))
        {
            constructor = SupportedConstructor.TimeOnlyMinute;
        }
        else if (Matches(
                     method,
                     frameworkTypes.TimeOnly,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.TimeOnlySecond;
        }
        else if (Matches(
                     method,
                     frameworkTypes.TimeOnly,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32,
                     ParameterType.Int32
                 ))
        {
            constructor = SupportedConstructor.TimeOnlyMillisecond;
        }
        else if (Matches(method, frameworkTypes.TimeOnly, ParameterType.Int64))
        {
            constructor = SupportedConstructor.TimeOnlyTicks;
        }
        else if (Matches(method, frameworkTypes.Guid, ParameterType.String))
        {
            constructor = SupportedConstructor.Guid;
        }
        else if (Matches(method, frameworkTypes.Uri, ParameterType.String))
        {
            constructor = SupportedConstructor.Uri;
        }
        else if (Matches(method, frameworkTypes.Uri, ParameterType.String, ParameterType.UriKind))
        {
            constructor = SupportedConstructor.UriKind;
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryGetSupportedFactory(
        IMethodSymbol method,
        FrameworkTypes frameworkTypes,
        out SupportedFactory factory
    )
    {
        factory = default;
        if (Matches(method, frameworkTypes.TimeSpan, "FromTicks", ParameterType.Int64))
        {
            factory = SupportedFactory.TimeSpanFromTicks;
        }
        else if (TryGetTimeSpanQuantityFactory(method, frameworkTypes, out factory))
        {
            return true;
        }
        else if (Matches(method, frameworkTypes.DateOnly, "FromDayNumber", ParameterType.Int32))
        {
            factory = SupportedFactory.DateOnlyFromDayNumber;
        }
        else if (Matches(method, frameworkTypes.DateTimeOffset, "FromUnixTimeSeconds", ParameterType.Int64))
        {
            factory = SupportedFactory.DateTimeOffsetFromUnixTimeSeconds;
        }
        else if (Matches(method, frameworkTypes.DateTimeOffset, "FromUnixTimeMilliseconds", ParameterType.Int64))
        {
            factory = SupportedFactory.DateTimeOffsetFromUnixTimeMilliseconds;
        }
        else if (Matches(method, frameworkTypes.Guid, "Parse", ParameterType.String))
        {
            factory = SupportedFactory.GuidParse;
        }
        else if (Matches(method, frameworkTypes.Guid, "ParseExact", ParameterType.String, ParameterType.String))
        {
            factory = SupportedFactory.GuidParseExact;
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryGetTimeSpanQuantityFactory(
        IMethodSymbol method,
        FrameworkTypes frameworkTypes,
        out SupportedFactory factory
    )
    {
        factory = default;
        if (Matches(method, frameworkTypes.TimeSpan, "FromDays", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromDaysDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromDays", ParameterType.Int32))
        {
            factory = SupportedFactory.TimeSpanFromDaysIntegral;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromHours", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromHoursDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromHours", ParameterType.Int32))
        {
            factory = SupportedFactory.TimeSpanFromHoursIntegral;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMinutes", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromMinutesDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMinutes", ParameterType.Int64))
        {
            factory = SupportedFactory.TimeSpanFromMinutesIntegral;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromSeconds", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromSecondsDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromSeconds", ParameterType.Int64))
        {
            factory = SupportedFactory.TimeSpanFromSecondsIntegral;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMilliseconds", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromMillisecondsDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMilliseconds", ParameterType.Int64))
        {
            factory = SupportedFactory.TimeSpanFromMillisecondsIntegral;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMicroseconds", ParameterType.Double))
        {
            factory = SupportedFactory.TimeSpanFromMicrosecondsDouble;
        }
        else if (Matches(method, frameworkTypes.TimeSpan, "FromMicroseconds", ParameterType.Int64))
        {
            factory = SupportedFactory.TimeSpanFromMicrosecondsIntegral;
        }
        else
        {
            return false;
        }

        return true;
    }

    private static ReconstructedMetadataValue EvaluateConstructor(
        SupportedConstructor constructor,
        IReadOnlyList<object?> arguments
    ) =>
        constructor switch
        {
            SupportedConstructor.DateTimeDate => ReconstructedMetadataValue.FromDateTime(
                new DateTime(ToInt32(arguments[0]), ToInt32(arguments[1]), ToInt32(arguments[2]))
            ),
            SupportedConstructor.DateTimeSecond => ReconstructedMetadataValue.FromDateTime(
                new DateTime(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5])
                )
            ),
            SupportedConstructor.DateTimeMillisecond => ReconstructedMetadataValue.FromDateTime(
                new DateTime(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5]),
                    ToInt32(arguments[6])
                )
            ),
            SupportedConstructor.DateTimeSecondKind => ReconstructedMetadataValue.FromDateTime(
                new DateTime(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5]),
                    ToDateTimeKind(arguments[6])
                )
            ),
            SupportedConstructor.DateTimeMillisecondKind => ReconstructedMetadataValue.FromDateTime(
                new DateTime(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5]),
                    ToInt32(arguments[6]),
                    ToDateTimeKind(arguments[7])
                )
            ),
            SupportedConstructor.DateTimeTicks => ReconstructedMetadataValue.FromDateTime(
                new DateTime(ToInt64(arguments[0]))
            ),
            SupportedConstructor.DateTimeTicksKind => ReconstructedMetadataValue.FromDateTime(
                new DateTime(ToInt64(arguments[0]), ToDateTimeKind(arguments[1]))
            ),
            SupportedConstructor.DateTimeOffsetSecond => ReconstructedMetadataValue.FromDateTimeOffset(
                new DateTimeOffset(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5]),
                    ToTimeSpan(arguments[6])
                )
            ),
            SupportedConstructor.DateTimeOffsetMillisecond => ReconstructedMetadataValue.FromDateTimeOffset(
                new DateTimeOffset(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4]),
                    ToInt32(arguments[5]),
                    ToInt32(arguments[6]),
                    ToTimeSpan(arguments[7])
                )
            ),
            SupportedConstructor.DateTimeOffsetTicks => ReconstructedMetadataValue.FromDateTimeOffset(
                new DateTimeOffset(ToInt64(arguments[0]), ToTimeSpan(arguments[1]))
            ),
            SupportedConstructor.DateTimeOffsetDateTime => ReconstructedMetadataValue.FromDateTimeOffset(
                new DateTimeOffset(ToDateTime(arguments[0]), ToTimeSpan(arguments[1]))
            ),
            SupportedConstructor.TimeSpanHours => ReconstructedMetadataValue.FromTimeSpan(
                new TimeSpan(ToInt32(arguments[0]), ToInt32(arguments[1]), ToInt32(arguments[2]))
            ),
            SupportedConstructor.TimeSpanDays => ReconstructedMetadataValue.FromTimeSpan(
                new TimeSpan(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3])
                )
            ),
            SupportedConstructor.TimeSpanMilliseconds => ReconstructedMetadataValue.FromTimeSpan(
                new TimeSpan(
                    ToInt32(arguments[0]),
                    ToInt32(arguments[1]),
                    ToInt32(arguments[2]),
                    ToInt32(arguments[3]),
                    ToInt32(arguments[4])
                )
            ),
            SupportedConstructor.TimeSpanTicks => ReconstructedMetadataValue.FromTimeSpan(
                new TimeSpan(ToInt64(arguments[0]))
            ),
            SupportedConstructor.DateOnly => ReconstructedMetadataValue.FromDateOnlyDayNumber(
                checked(
                    (int) (new DateTime(
                               ToInt32(arguments[0]),
                               ToInt32(arguments[1]),
                               ToInt32(arguments[2])
                           ).Ticks /
                           TimeSpan.TicksPerDay)
                )
            ),
            SupportedConstructor.TimeOnlyMinute => CreateTimeOnly(
                ToInt32(arguments[0]),
                ToInt32(arguments[1]),
                second: 0,
                millisecond: 0
            ),
            SupportedConstructor.TimeOnlySecond => CreateTimeOnly(
                ToInt32(arguments[0]),
                ToInt32(arguments[1]),
                ToInt32(arguments[2]),
                millisecond: 0
            ),
            SupportedConstructor.TimeOnlyMillisecond => CreateTimeOnly(
                ToInt32(arguments[0]),
                ToInt32(arguments[1]),
                ToInt32(arguments[2]),
                ToInt32(arguments[3])
            ),
            SupportedConstructor.TimeOnlyTicks => CreateTimeOnly(ToInt64(arguments[0])),
            SupportedConstructor.Guid => ReconstructedMetadataValue.FromGuid(new Guid((string) arguments[0]!)),
            SupportedConstructor.Uri => CreateUri(new Uri((string) arguments[0]!, UriKind.Absolute)),
            SupportedConstructor.UriKind => CreateUri(
                new Uri((string) arguments[0]!, ToUriKind(arguments[1]))
            ),
            _ => throw new InvalidOperationException($"Unsupported constructor '{constructor}'.")
        };

    private static ReconstructedMetadataValue EvaluateFactory(
        SupportedFactory factory,
        IReadOnlyList<object?> arguments
    ) =>
        factory switch
        {
            SupportedFactory.TimeSpanFromTicks => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromTicks(ToInt64(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromDaysDouble => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromDays(ToDouble(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromDaysIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TimeSpan.TicksPerDay
            ),
            SupportedFactory.TimeSpanFromHoursDouble => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromHours(ToDouble(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromHoursIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TimeSpan.TicksPerHour
            ),
            SupportedFactory.TimeSpanFromMinutesDouble => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromMinutes(ToDouble(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromMinutesIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TimeSpan.TicksPerMinute
            ),
            SupportedFactory.TimeSpanFromSecondsDouble => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromSeconds(ToDouble(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromSecondsIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TimeSpan.TicksPerSecond
            ),
            SupportedFactory.TimeSpanFromMillisecondsDouble => ReconstructedMetadataValue.FromTimeSpan(
                TimeSpan.FromMilliseconds(ToDouble(arguments[0]))
            ),
            SupportedFactory.TimeSpanFromMillisecondsIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TimeSpan.TicksPerMillisecond
            ),
            SupportedFactory.TimeSpanFromMicrosecondsDouble => ReconstructedMetadataValue.FromTimeSpan(
                CreateDoubleTimeSpan(ToDouble(arguments[0]), TicksPerMicrosecond)
            ),
            SupportedFactory.TimeSpanFromMicrosecondsIntegral => CreateIntegralTimeSpan(
                ToInt64(arguments[0]),
                TicksPerMicrosecond
            ),
            SupportedFactory.DateOnlyFromDayNumber => CreateDateOnlyFromDayNumber(ToInt32(arguments[0])),
            SupportedFactory.DateTimeOffsetFromUnixTimeSeconds =>
                ReconstructedMetadataValue.FromDateTimeOffset(
                    DateTimeOffset.FromUnixTimeSeconds(ToInt64(arguments[0]))
                ),
            SupportedFactory.DateTimeOffsetFromUnixTimeMilliseconds =>
                ReconstructedMetadataValue.FromDateTimeOffset(
                    DateTimeOffset.FromUnixTimeMilliseconds(ToInt64(arguments[0]))
                ),
            SupportedFactory.GuidParse => ReconstructedMetadataValue.FromGuid(Guid.Parse((string) arguments[0]!)),
            SupportedFactory.GuidParseExact => ReconstructedMetadataValue.FromGuid(
                Guid.ParseExact((string) arguments[0]!, (string) arguments[1]!)
            ),
            _ => throw new InvalidOperationException($"Unsupported factory '{factory}'.")
        };

    private static ReconstructedMetadataValue CreateIntegralTimeSpan(long quantity, long ticksPerUnit) =>
        ReconstructedMetadataValue.FromTimeSpan(TimeSpan.FromTicks(checked(quantity * ticksPerUnit)));

    private static ReconstructedMetadataValue CreateDateOnlyFromDayNumber(int dayNumber)
    {
        if (dayNumber < 0 || dayNumber > 3_652_058)
        {
            throw new ArgumentOutOfRangeException(nameof(dayNumber));
        }

        return ReconstructedMetadataValue.FromDateOnlyDayNumber(dayNumber);
    }

    private static ReconstructedMetadataValue CreateTimeOnly(
        int hour,
        int minute,
        int second,
        int millisecond
    )
    {
        var value = new DateTime(1, 1, 1, hour, minute, second, millisecond, DateTimeKind.Unspecified);
        return ReconstructedMetadataValue.FromTimeOnlyTicks(value.Ticks);
    }

    private static ReconstructedMetadataValue CreateTimeOnly(long ticks)
    {
        if (ticks < 0 || ticks >= TimeSpan.TicksPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        return ReconstructedMetadataValue.FromTimeOnlyTicks(ticks);
    }

    private static ReconstructedMetadataValue CreateUri(Uri value) =>
        ReconstructedMetadataValue.FromUriOriginalString(value.OriginalString);

    private static TimeSpan CreateDoubleTimeSpan(double value, double ticksPerUnit)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("TimeSpan quantity cannot be NaN.", nameof(value));
        }

        var ticks = value * ticksPerUnit;
        if (ticks > TimeSpan.MaxValue.Ticks || ticks < TimeSpan.MinValue.Ticks || double.IsNaN(ticks))
        {
            throw new OverflowException("The TimeSpan quantity is outside the supported range.");
        }

        return ticks == TimeSpan.MaxValue.Ticks ? TimeSpan.MaxValue : TimeSpan.FromTicks((long) ticks);
    }

    private static DateTime ToDateTime(object? value)
    {
        var reconstructedValue = GetReconstructedValue(value, ReconstructedMetadataValueKind.DateTime);
        return new DateTime(
            reconstructedValue.PrimaryValue,
            (DateTimeKind) reconstructedValue.SecondaryValue
        );
    }

    private static TimeSpan ToTimeSpan(object? value)
    {
        var reconstructedValue = GetReconstructedValue(value, ReconstructedMetadataValueKind.TimeSpan);
        return TimeSpan.FromTicks(reconstructedValue.PrimaryValue);
    }

    private static ReconstructedMetadataValue GetReconstructedValue(
        object? value,
        ReconstructedMetadataValueKind expectedKind
    )
    {
        if (value is ReconstructedMetadataValue reconstructedValue && reconstructedValue.Kind == expectedKind)
        {
            return reconstructedValue;
        }

        throw new InvalidOperationException($"Expected a reconstructed {expectedKind} value.");
    }

    private static int ToInt32(object? value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static long ToInt64(object? value) => Convert.ToInt64(value, CultureInfo.InvariantCulture);

    private static double ToDouble(object? value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

    private static DateTimeKind ToDateTimeKind(object? value) => (DateTimeKind) ToInt32(value);

    private static UriKind ToUriKind(object? value) => (UriKind) ToInt32(value);

    private static bool Matches(
        IMethodSymbol method,
        INamedTypeSymbol? containingType,
        params ParameterType[] parameterTypes
    ) =>
        method.MethodKind == MethodKind.Constructor &&
        Matches(method, containingType, method.Name, parameterTypes);

    private static bool Matches(
        IMethodSymbol method,
        INamedTypeSymbol? containingType,
        string name,
        params ParameterType[] parameterTypes
    )
    {
        if (containingType is null ||
            (!method.IsStatic && method.MethodKind != MethodKind.Constructor) ||
            method.Name != name ||
            !IsType(method.ContainingType, containingType) ||
            method.Parameters.Length != parameterTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (!MatchesParameter(method.Parameters[i].Type, parameterTypes[i], containingType.ContainingAssembly))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesParameter(
        ITypeSymbol parameterType,
        ParameterType expectedType,
        IAssemblySymbol coreAssembly
    ) =>
        expectedType switch
        {
            ParameterType.Int32 => parameterType.SpecialType == SpecialType.System_Int32,
            ParameterType.Int64 => parameterType.SpecialType == SpecialType.System_Int64,
            ParameterType.Double => parameterType.SpecialType == SpecialType.System_Double,
            ParameterType.String => parameterType.SpecialType == SpecialType.System_String,
            ParameterType.DateTime => IsFrameworkType(parameterType, coreAssembly, "System.DateTime"),
            ParameterType.DateTimeKind => IsFrameworkType(parameterType, coreAssembly, "System.DateTimeKind"),
            ParameterType.TimeSpan => IsFrameworkType(parameterType, coreAssembly, "System.TimeSpan"),
            ParameterType.UriKind => IsFrameworkType(parameterType, coreAssembly, "System.UriKind"),
            _ => false
        };

    private static bool IsFrameworkType(
        ITypeSymbol type,
        IAssemblySymbol coreAssembly,
        string metadataName
    ) =>
        SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, coreAssembly) &&
        string.Equals(GetMetadataName(type), metadataName, StringComparison.Ordinal);

    private static bool IsType(ITypeSymbol? left, ITypeSymbol? right) =>
        left is not null && right is not null && SymbolEqualityComparer.Default.Equals(left, right);

    private static string GetMetadataName(ITypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace?.IsGlobalNamespace == false ?
            type.ContainingNamespace.ToDisplayString() + "." :
            string.Empty;
        return namespaceName + type.MetadataName;
    }

    private sealed class FrameworkTypes
    {
        public FrameworkTypes(Compilation compilation)
        {
            DateTime = compilation.GetTypeByMetadataName("System.DateTime");
            DateTimeOffset = compilation.GetTypeByMetadataName("System.DateTimeOffset");
            TimeSpan = compilation.GetTypeByMetadataName("System.TimeSpan");
            DateOnly = compilation.GetTypeByMetadataName("System.DateOnly");
            TimeOnly = compilation.GetTypeByMetadataName("System.TimeOnly");
            Guid = compilation.GetTypeByMetadataName("System.Guid");
            Uri = compilation.GetTypeByMetadataName("System.Uri");
        }

        public INamedTypeSymbol? DateTime { get; }
        public INamedTypeSymbol? DateTimeOffset { get; }
        public INamedTypeSymbol? TimeSpan { get; }
        public INamedTypeSymbol? DateOnly { get; }
        public INamedTypeSymbol? TimeOnly { get; }
        public INamedTypeSymbol? Guid { get; }
        public INamedTypeSymbol? Uri { get; }
    }

    private enum ParameterType
    {
        Int32 = 0,
        Int64 = 1,
        Double = 2,
        String = 3,
        DateTime = 4,
        DateTimeKind = 5,
        TimeSpan = 6,
        UriKind = 7
    }

    private enum SupportedConstructor
    {
        DateTimeDate = 0,
        DateTimeSecond = 1,
        DateTimeMillisecond = 2,
        DateTimeSecondKind = 3,
        DateTimeMillisecondKind = 4,
        DateTimeTicks = 5,
        DateTimeTicksKind = 6,
        DateTimeOffsetSecond = 7,
        DateTimeOffsetMillisecond = 8,
        DateTimeOffsetTicks = 9,
        DateTimeOffsetDateTime = 10,
        TimeSpanHours = 11,
        TimeSpanDays = 12,
        TimeSpanMilliseconds = 13,
        TimeSpanTicks = 14,
        DateOnly = 15,
        TimeOnlyMinute = 16,
        TimeOnlySecond = 17,
        TimeOnlyMillisecond = 18,
        TimeOnlyTicks = 19,
        Guid = 20,
        Uri = 21,
        UriKind = 22
    }

    private enum SupportedFactory
    {
        TimeSpanFromTicks = 0,
        TimeSpanFromDaysDouble = 1,
        TimeSpanFromDaysIntegral = 2,
        TimeSpanFromHoursDouble = 3,
        TimeSpanFromHoursIntegral = 4,
        TimeSpanFromMinutesDouble = 5,
        TimeSpanFromMinutesIntegral = 6,
        TimeSpanFromSecondsDouble = 7,
        TimeSpanFromSecondsIntegral = 8,
        TimeSpanFromMillisecondsDouble = 9,
        TimeSpanFromMillisecondsIntegral = 10,
        TimeSpanFromMicrosecondsDouble = 11,
        TimeSpanFromMicrosecondsIntegral = 12,
        DateOnlyFromDayNumber = 13,
        DateTimeOffsetFromUnixTimeSeconds = 14,
        DateTimeOffsetFromUnixTimeMilliseconds = 15,
        GuidParse = 16,
        GuidParseExact = 17
    }
}
