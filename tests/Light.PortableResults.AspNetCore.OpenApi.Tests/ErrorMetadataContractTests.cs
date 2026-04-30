using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class ErrorMetadataContractTests
{
    [Fact]
    public void ContractFactories_ShouldExposeClosedSubclassPayloads()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();

        var typeContract = ErrorMetadataContract.FromType(typeof(TestMetadata));
        var schemaContract = ErrorMetadataContract.FromSchema(schemaFactory);
        var noMetadata = ErrorMetadataContract.NoMetadata;

        typeContract.Should().BeOfType<ErrorMetadataTypeContract>()
           .Which.MetadataType.Should().Be(typeof(TestMetadata));
        schemaContract.Should().BeOfType<ErrorMetadataSchemaContract>()
           .Which.SchemaFactory.Should().BeSameAs(schemaFactory);
        ((ErrorMetadataSchemaContract) schemaContract).DiagnosticName.Should().Be(nameof(schemaFactory));
        noMetadata.Should().BeOfType<NoMetadataContract>();
        ErrorMetadataContract.NoMetadata.Should().BeSameAs(noMetadata);
        typeof(ErrorMetadataContract)
           .GetMember("Kind")
           .Should()
           .BeEmpty();

        var discriminator = typeContract switch
        {
            ErrorMetadataTypeContract => "type",
            ErrorMetadataSchemaContract => "schema",
            NoMetadataContract => "none",
            _ => "unknown"
        };
        discriminator.Should().Be("type");
    }

    [Fact]
    public void ContractsBuilder_ShouldBeIdempotentForEquivalentContracts()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();
        var builder = new ErrorMetadataContractsBuilder();

        builder.ForCode<TestMetadata>("TypeCode");
        builder.ForCode("TypeCode", typeof(TestMetadata));
        builder.ForCode("SchemaCode", schemaFactory);
        builder.ForCode("SchemaCode", schemaFactory);
        builder.ForCode("NoMetadataCode");
        builder.ForCode("NoMetadataCode");

        var registry = new DefaultErrorMetadataContractRegistry(builder);
        registry.Contracts.Keys.Should().BeEquivalentTo("TypeCode", "SchemaCode", "NoMetadataCode");
    }

    [Fact]
    public void SchemaContracts_ShouldAllowExplicitDiagnosticNames()
    {
        // ReSharper disable once ConvertToLocalFunction
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();

        var schemaContract = ErrorMetadataContract.FromSchema(schemaFactory, "named schema");

        schemaContract.Should().BeOfType<ErrorMetadataSchemaContract>()
           .Which.DiagnosticName.Should().Be("named schema");
    }

    [Fact]
    public void SchemaContracts_ShouldDeriveDiagnosticNamesFromMethodMetadata_WhenNoNameIsAvailable()
    {
        var schemaContract = new ErrorMetadataSchemaContract(CreateSchema, null);

        schemaContract.DiagnosticName.Should().Contain(nameof(CreateSchema));
    }

    [Fact]
    public void SchemaContracts_ShouldThrow_WhenNoMeaningfulDiagnosticNameCanBeDerived()
    {
        new Action(
                () => ErrorMetadataContract.FromSchema(
                    _ => new OpenApiSchema(),
                    null
                )
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*meaningful diagnostic name*diagnosticName*");
    }

    [Fact]
    public void ContractsBuilder_ShouldRejectConflictingContractsForSameCode()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> firstFactory = _ => new OpenApiSchema();
        Func<OpenApiSpecVersion, OpenApiSchema> secondFactory = _ => new OpenApiSchema();

        new Action(
                () => new ErrorMetadataContractsBuilder()
                   .ForCode<TestMetadata>("Conflict")
                   .ForCode<OtherMetadata>("Conflict")
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*TestMetadata*OtherMetadata*");

        new Action(
                () => new ErrorMetadataContractsBuilder()
                   .ForCode("Conflict", firstFactory)
                   .ForCode("Conflict", secondFactory)
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*firstFactory*secondFactory*");

        new Action(
                () => new ErrorMetadataContractsBuilder()
                   .ForCode("Conflict", firstFactory, "first schema")
                   .ForCode("Conflict", secondFactory, "second schema")
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*first schema*second schema*");

        new Action(
                () => new ErrorMetadataContractsBuilder()
                   .ForCode("Conflict")
                   .ForCode<TestMetadata>("Conflict")
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*no metadata*TestMetadata*");
    }

    [Fact]
    public void ContractRegistry_ShouldExposePortableMetadataContracts()
    {
        var registry = new DefaultErrorMetadataContractRegistry(
            new ErrorMetadataContractsBuilder().ForCode<TestMetadata>("TypeCode")
        );

        registry.Contracts.Should().BeAssignableTo<IReadOnlyDictionary<string, ErrorMetadataContract>>();
        registry.Contracts["TypeCode"].Should().BeOfType<ErrorMetadataTypeContract>();
    }

    [Fact]
    public void ContractRegistry_ShouldSnapshotBuilderState()
    {
        var builder = new ErrorMetadataContractsBuilder().ForCode<TestMetadata>("TypeCode");

        var registry = new DefaultErrorMetadataContractRegistry(builder);
        builder.ForCode<OtherMetadata>("OtherCode");

        registry.Contracts.Keys.Should().BeEquivalentTo("TypeCode");
    }

    [Fact]
    public void ContractRegistry_ShouldRejectSanitizedCodeCollisions_WhenBuilderStateIsComposedExternally()
    {
        var builder = new ErrorMetadataContractsBuilder();
        var contractsField = typeof(ErrorMetadataContractsBuilder).GetField(
            "_contracts",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        contractsField.Should().NotBeNull();

        var contracts = contractsField
           .GetValue(builder)
           .Should()
           .BeOfType<Dictionary<string, ErrorMetadataContract>>()
           .Subject;
        contracts.Add("Code/One", ErrorMetadataContract.NoMetadata);
        contracts.Add("Code_One", ErrorMetadataContract.NoMetadata);

        var act = () => _ = new DefaultErrorMetadataContractRegistry(builder);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Code/One*Code_One*");
    }

    [Fact]
    public void NoMetadataContract_ShouldReturn0_WhenGetHashCodeIsCalled() =>
        ErrorMetadataContract.NoMetadata.GetHashCode().Should().Be(0);

    [Fact]
    public void PortableErrorMetadataSchemaContract_ShouldReturnHashCodeFromDiagnosticName()
    {
        var schemaContract = new ErrorMetadataSchemaContract(CreateSchema, null);

        var hashCode = schemaContract.GetHashCode();

        var expectedHashCode = schemaContract.DiagnosticName.GetHashCode(StringComparison.Ordinal);
        hashCode.Should().Be(expectedHashCode);
    }

    [Fact]
    public void PortableErrorMetadataTypeContract_ShouldReturnHashCodeFromMetadataType()
    {
        var typeContract = new ErrorMetadataTypeContract(typeof(TestMetadata));

        var hashCode = typeContract.GetHashCode();

        var expectedHashCode = typeContract.MetadataType.GetHashCode();
        hashCode.Should().Be(expectedHashCode);
    }

    // ReSharper disable UnusedMember.Local -- required for testing
    private sealed class TestMetadata
    {
        public string Value { get; init; } = string.Empty;
    }

    // ReSharper disable once ClassNeverInstantiated.Local -- required for testing
    private sealed class OtherMetadata
    {
        public string Value { get; init; } = string.Empty;
    }

    private static OpenApiSchema CreateSchema(OpenApiSpecVersion _) => new ();
    // ReSharper restore UnusedMember.Local
}
