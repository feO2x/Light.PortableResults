using System;
using System.Collections.Generic;
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
        noMetadata.Should().BeOfType<PortableErrorMetadataNoMetadataContract>();
        PortableErrorMetadataContract.NoMetadata.Should().BeSameAs(noMetadata);
        typeof(PortableErrorMetadataContract)
           .GetMember("Kind")
           .Should()
           .BeEmpty();

        string discriminator = typeContract switch
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
           .WithMessage("*Conflict*schema factory*");

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

    private sealed class TestMetadata
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class OtherMetadata
    {
        public string Value { get; init; } = string.Empty;
    }
}
