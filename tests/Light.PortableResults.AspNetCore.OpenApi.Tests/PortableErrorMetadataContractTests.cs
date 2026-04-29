using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableErrorMetadataContractTests
{
    [Fact]
    public void ContractFactories_ShouldExposeClosedSubclassPayloads()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();

        var typeContract = PortableErrorMetadataContract.FromType(typeof(TestMetadata));
        var schemaContract = PortableErrorMetadataContract.FromSchema(schemaFactory);
        var noMetadata = PortableErrorMetadataContract.NoMetadata;

        typeContract.Should().BeOfType<PortableErrorMetadataTypeContract>()
           .Which.MetadataType.Should().Be(typeof(TestMetadata));
        schemaContract.Should().BeOfType<PortableErrorMetadataSchemaContract>()
           .Which.SchemaFactory.Should().BeSameAs(schemaFactory);
        ((PortableErrorMetadataSchemaContract) schemaContract).DiagnosticName.Should().Be(nameof(schemaFactory));
        noMetadata.Should().BeOfType<PortableErrorMetadataNoMetadataContract>();
        PortableErrorMetadataContract.NoMetadata.Should().BeSameAs(noMetadata);
        typeof(PortableErrorMetadataContract)
           .GetMember("Kind")
           .Should()
           .BeEmpty();

        var discriminator = typeContract switch
        {
            PortableErrorMetadataTypeContract => "type",
            PortableErrorMetadataSchemaContract => "schema",
            PortableErrorMetadataNoMetadataContract => "none",
            _ => "unknown"
        };
        discriminator.Should().Be("type");
    }

    [Fact]
    public void ContractsBuilder_ShouldBeIdempotentForEquivalentContracts()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();
        var builder = new PortableErrorMetadataContractsBuilder();

        builder.ForCode<TestMetadata>("TypeCode");
        builder.ForCode("TypeCode", typeof(TestMetadata));
        builder.ForCode("SchemaCode", schemaFactory);
        builder.ForCode("SchemaCode", schemaFactory);
        builder.ForCode("NoMetadataCode");
        builder.ForCode("NoMetadataCode");

        var registry = new PortableErrorMetadataContractRegistry(builder);
        registry.Contracts.Keys.Should().BeEquivalentTo("TypeCode", "SchemaCode", "NoMetadataCode");
    }

    [Fact]
    public void SchemaContracts_ShouldAllowExplicitDiagnosticNames()
    {
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory = _ => new OpenApiSchema();

        var schemaContract = PortableErrorMetadataContract.FromSchema(schemaFactory, "named schema");

        schemaContract.Should().BeOfType<PortableErrorMetadataSchemaContract>()
           .Which.DiagnosticName.Should().Be("named schema");
    }

    [Fact]
    public void SchemaContracts_ShouldDeriveDiagnosticNamesFromMethodMetadata_WhenNoNameIsAvailable()
    {
        var schemaContract = new PortableErrorMetadataSchemaContract(CreateSchema, null);

        schemaContract.DiagnosticName.Should().Contain(nameof(CreateSchema));
    }

    [Fact]
    public void SchemaContracts_ShouldThrow_WhenNoMeaningfulDiagnosticNameCanBeDerived()
    {
        new Action(
                () => PortableErrorMetadataContract.FromSchema(
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
                () => new PortableErrorMetadataContractsBuilder()
                   .ForCode<TestMetadata>("Conflict")
                   .ForCode<OtherMetadata>("Conflict")
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*TestMetadata*OtherMetadata*");

        new Action(
                () => new PortableErrorMetadataContractsBuilder()
                   .ForCode("Conflict", firstFactory)
                   .ForCode("Conflict", secondFactory)
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*firstFactory*secondFactory*");

        new Action(
                () => new PortableErrorMetadataContractsBuilder()
                   .ForCode("Conflict", firstFactory, "first schema")
                   .ForCode("Conflict", secondFactory, "second schema")
            )
           .Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*Conflict*first schema*second schema*");

        new Action(
                () => new PortableErrorMetadataContractsBuilder()
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
        var registry = new PortableErrorMetadataContractRegistry(
            new PortableErrorMetadataContractsBuilder().ForCode<TestMetadata>("TypeCode")
        );

        registry.Contracts.Should().BeAssignableTo<IReadOnlyDictionary<string, PortableErrorMetadataContract>>();
        registry.Contracts["TypeCode"].Should().BeOfType<PortableErrorMetadataTypeContract>();
    }

    [Fact]
    public void ContractRegistry_ShouldSnapshotBuilderState()
    {
        var builder = new PortableErrorMetadataContractsBuilder().ForCode<TestMetadata>("TypeCode");

        var registry = new PortableErrorMetadataContractRegistry(builder);
        builder.ForCode<OtherMetadata>("OtherCode");

        registry.Contracts.Keys.Should().BeEquivalentTo("TypeCode");
    }

    [Fact]
    public void ContractRegistry_ShouldRejectSanitizedCodeCollisions_WhenBuilderStateIsComposedExternally()
    {
        var builder = new PortableErrorMetadataContractsBuilder();
        var contractsField = typeof(PortableErrorMetadataContractsBuilder).GetField(
            "_contracts",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        contractsField.Should().NotBeNull();

        var contracts = contractsField
           .GetValue(builder)
           .Should()
           .BeOfType<Dictionary<string, PortableErrorMetadataContract>>()
           .Subject;
        contracts.Add("Code/One", PortableErrorMetadataContract.NoMetadata);
        contracts.Add("Code_One", PortableErrorMetadataContract.NoMetadata);

        var act = () => _ = new PortableErrorMetadataContractRegistry(builder);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Code/One*Code_One*");
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
