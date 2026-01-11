using FluentAssertions;
using Light.Results.Metadata;

namespace Light.Results.Tests;

public sealed class ErrorsEqualsWithMetadataTests
{
    [Fact]
    public void Equals_WithCompareMetadataTrue_SameErrors_ShouldBeEqual()
    {
        var error = new Error { Message = "Error" };
        var errors1 = new Errors(error);
        var errors2 = new Errors(error);

        errors1.Equals(errors2, compareMetadata: true).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataTrue_DifferentMetadata_ShouldNotBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = metadata1 };
        var error2 = new Error { Message = "Error", Metadata = metadata2 };
        var errors1 = new Errors(error1);
        var errors2 = new Errors(error2);

        errors1.Equals(errors2, compareMetadata: true).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = metadata1 };
        var error2 = new Error { Message = "Error", Metadata = metadata2 };
        var errors1 = new Errors(error1);
        var errors2 = new Errors(error2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentCounts_ShouldNotBeEqual()
    {
        var errors1 = new Errors(new Error { Message = "Error" });
        var errors2 = new Errors(new[] { new Error { Message = "Error" }, new Error { Message = "Error2" } });

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_BothDefault_ShouldBeEqual()
    {
        var errors1 = default(Errors);
        var errors2 = default(Errors);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_MultipleErrors_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var errorsArray1 = new[]
        {
            new Error { Message = "E1", Metadata = metadata1 },
            new Error { Message = "E2", Metadata = metadata1 }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1", Metadata = metadata2 },
            new Error { Message = "E2", Metadata = metadata2 }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_MultipleErrors_DifferentMessages_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E3" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SameReference_ShouldBeEqual()
    {
        var errorsArray = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray);
        var errors2 = new Errors(errorsArray);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_ShouldUseUnrolledLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex0_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex2_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex3_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex4_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex5_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex6_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex7_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_ShouldUseUnrolledLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex0_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex2_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex3_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwoErrors_ShouldUseRemainingLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwoErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_NineErrors_ShouldCoverBothLoops()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_NineErrors_DifferentAtIndex8_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwelveErrors_ShouldCoverAllLoops()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SixteenErrors_ShouldCoverTwoIterationsOfEightLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SixteenErrors_DifferentAtIndex15_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }
}
