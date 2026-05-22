using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// Generates Minimal API OpenAPI metadata contracts for marked synchronous validators.
/// </summary>
[Generator]
public sealed class PortableValidationOpenApiGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var validators = context.SyntaxProvider.ForAttributeWithMetadataName(
            KnownTypeNames.GenerateAttribute,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, cancellationToken) =>
                ValidatorOpenApiAnalyzer.Analyze(
                    attributeContext.SemanticModel.Compilation,
                    (INamedTypeSymbol) attributeContext.TargetSymbol,
                    cancellationToken
                )
        );

        context.RegisterSourceOutput(
            validators,
            static (sourceProductionContext, analysis) =>
            {
                foreach (var diagnostic in analysis.Diagnostics)
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }

                if (analysis.Source is not null)
                {
                    sourceProductionContext.AddSource(analysis.HintName, analysis.Source);
                }
            }
        );
    }
}
