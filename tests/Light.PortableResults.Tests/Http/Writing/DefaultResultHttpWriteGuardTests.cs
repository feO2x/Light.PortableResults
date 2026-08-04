using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Light.PortableResults.Http;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Json;
using Xunit;

namespace Light.PortableResults.Tests.Http.Writing;

/// <summary>
/// <para>
/// Covers the HTTP body write boundaries for the default result instance.
/// </para>
/// <para>
/// Only <c>Result&lt;T></c> with a reference type or a nullable value type can take that shape.
/// <c>default(Result)</c> and <c>default(Result&lt;int>)</c> are ordinary successes, because their encapsulated
/// value can never be null.
/// </para>
/// </summary>
public sealed class DefaultResultHttpWriteGuardTests
{
    private static readonly ResolvedHttpWriteOptions DefaultOptions =
        new PortableResultsHttpWriteOptions().ToResolvedHttpWriteOptions();

    private static readonly ResolvedHttpWriteOptions OptionsWithCustomProblemDetailsFactory =
        new PortableResultsHttpWriteOptions
        {
            CreateProblemDetailsInfo = static (_, _) => new ProblemDetailsInfo
            {
                Type = "https://example.com/problem",
                Status = HttpStatusCode.InternalServerError,
                Title = "Custom",
                Detail = "Custom problem details"
            }
        }.ToResolvedHttpWriteOptions();

    [Fact]
    public void ToHttpResultForWritingShouldRejectDefaultResult()
    {
        Action act = () => default(Result<string>).ToHttpResultForWriting(new PortableResultsHttpWriteOptions());

        AssertDefaultInstanceRejected(act);
    }

    [Fact]
    public void ToHttpResultForWritingWithResolvedOptionsShouldRejectDefaultResult()
    {
        Action act = () => default(Result<string>).ToHttpResultForWriting(DefaultOptions);

        AssertDefaultInstanceRejected(act);
    }

    [Fact]
    public void ConverterShouldRejectHandConstructedWrapperWithoutWritingBytes()
    {
        var wrapper = new HttpResultForWriting<string>(default, DefaultOptions);

        AssertNothingWasWritten(wrapper, new HttpResultForWritingJsonConverter<string>());
    }

    [Fact]
    public void ConverterShouldRejectDefaultResultWhenCustomProblemDetailsFactoryIsConfigured()
    {
        var wrapper = new HttpResultForWriting<string>(default, OptionsWithCustomProblemDetailsFactory);

        AssertNothingWasWritten(wrapper, new HttpResultForWritingJsonConverter<string>());
    }

    [Fact]
    public void ToHttpResultForWritingShouldAcceptFailureResults()
    {
        var result = Result<string>.Fail(new Error { Message = "Something went wrong" });

        var wrapper = result.ToHttpResultForWriting(DefaultOptions);

        wrapper.Data.Should().Be(result);
    }

    [Fact]
    public void ToHttpResultForWritingShouldAcceptDefaultNonGenericResultAsASuccess()
    {
        var wrapper = default(Result).ToHttpResultForWriting(DefaultOptions);

        wrapper.Data.IsValid.Should().BeTrue();
    }

    private static void AssertDefaultInstanceRejected(Action act, string parameterName = "result") =>
        act.Should()
           .Throw<ArgumentException>()
           .WithParameterName(parameterName)
           .WithMessage("*default instance*Result.Ok or Result.Fail*");

    private static void AssertNothingWasWritten<T>(T wrapper, JsonConverter<T> converter)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var serializerOptions = new JsonSerializerOptions();

        Action act = () => converter.Write(writer, wrapper, serializerOptions);

        AssertDefaultInstanceRejected(act, "wrapper");
        writer.BytesPending.Should().Be(0);
        writer.BytesCommitted.Should().Be(0);
        stream.Length.Should().Be(0);
    }
}
