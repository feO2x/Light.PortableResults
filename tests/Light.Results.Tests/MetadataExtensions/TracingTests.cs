using FluentAssertions;
using Light.Results.Metadata;
using Light.Results.MetadataExtensions;
using Xunit;

namespace Light.Results.Tests.MetadataExtensions;

public sealed class TracingTests
{
    [Fact]
    public void WithSource_ShouldAddSourceToMetadata()
    {
        var result = Result<int>.Ok(42);

        var withSource = result.WithSource("UserService");

        withSource.Metadata.Should().NotBeNull();
        withSource.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("UserService");
        withSource.Value.Should().Be(42);
    }

    [Fact]
    public void WithSource_OnResultWithExistingMetadata_ShouldMerge()
    {
        var result = Result<string>.Ok("test").MergeMetadata(("requestId", "req-123"));

        var withSource = result.WithSource("OrderService");

        withSource.Metadata.Should().NotBeNull();
        withSource.Metadata!.Value.Should().HaveCount(2);
        withSource.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("OrderService");
        withSource.Metadata.Value.TryGetString("requestId", out var requestId).Should().BeTrue();
        requestId.Should().Be("req-123");
    }

    [Fact]
    public void WithSource_OnFailedResult_ShouldPreserveErrors()
    {
        var error = new Error { Message = "Validation failed" };
        var result = Result<int>.Fail(error);

        var withSource = result.WithSource("ValidationService");

        withSource.IsValid.Should().BeFalse();
        withSource.Errors.Should().ContainSingle().Which.Message.Should().Be("Validation failed");
        withSource.Metadata.Should().NotBeNull();
        withSource.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("ValidationService");
    }

    [Fact]
    public void WithSource_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok();

        var withSource = result.WithSource("PaymentService");

        withSource.Metadata.Should().NotBeNull();
        withSource.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("PaymentService");
    }

    [Fact]
    public void WithCorrelationId_ShouldAddCorrelationIdToMetadata()
    {
        var result = Result<int>.Ok(42);
        var correlationId = "550e8400-e29b-41d4-a716-446655440000";

        var withCorrelationId = result.WithCorrelationId(correlationId);

        withCorrelationId.Metadata.Should().NotBeNull();
        withCorrelationId.TryGetCorrelationId(out var id).Should().BeTrue();
        id.Should().Be(correlationId);
        withCorrelationId.Value.Should().Be(42);
    }

    [Fact]
    public void WithCorrelationId_OnResultWithExistingMetadata_ShouldMerge()
    {
        var result = Result<string>.Ok("test").MergeMetadata(("userId", "user-456"));
        var correlationId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        var withCorrelationId = result.WithCorrelationId(correlationId);

        withCorrelationId.Metadata.Should().NotBeNull();
        withCorrelationId.Metadata!.Value.Should().HaveCount(2);
        withCorrelationId.TryGetCorrelationId(out var id).Should().BeTrue();
        id.Should().Be(correlationId);
        withCorrelationId.Metadata.Value.TryGetString("userId", out var userId).Should().BeTrue();
        userId.Should().Be("user-456");
    }

    [Fact]
    public void WithCorrelationId_OnFailedResult_ShouldPreserveErrors()
    {
        var error = new Error { Message = "Not found" };
        var result = Result<int>.Fail(error);
        var correlationId = "trace-abc-123";

        var withCorrelationId = result.WithCorrelationId(correlationId);

        withCorrelationId.IsValid.Should().BeFalse();
        withCorrelationId.Errors.Should().ContainSingle().Which.Message.Should().Be("Not found");
        withCorrelationId.Metadata.Should().NotBeNull();
        withCorrelationId.TryGetCorrelationId(out var id).Should().BeTrue();
        id.Should().Be(correlationId);
    }

    [Fact]
    public void WithCorrelationId_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok();
        var correlationId = "req-2024-001-xyz";

        var withCorrelationId = result.WithCorrelationId(correlationId);

        withCorrelationId.Metadata.Should().NotBeNull();
        withCorrelationId.Metadata!.Value.TryGetString(Tracing.CorrelationIdKey, out var id).Should().BeTrue();
        id.Should().Be(correlationId);
    }

    [Fact]
    public void WithTracing_ShouldAddBothSourceAndCorrelationId()
    {
        var result = Result<int>.Ok(42);
        const string source = "InventoryService";
        const string correlationId = "550e8400-e29b-41d4-a716-446655440000";

        var withTracing = result.WithTracing(source, correlationId);

        withTracing.Metadata.Should().NotBeNull();
        withTracing.Metadata!.Value.Should().HaveCount(2);
        withTracing.TryGetSource(out var actualSource).Should().BeTrue();
        actualSource.Should().Be(source);
        withTracing.Metadata.Value.TryGetString(Tracing.CorrelationIdKey, out var actualId).Should().BeTrue();
        actualId.Should().Be(correlationId);
        withTracing.Value.Should().Be(42);
    }

    [Fact]
    public void WithTracing_OnResultWithExistingMetadata_ShouldMerge()
    {
        var result = Result<string>.Ok("test").MergeMetadata(("environment", "production"));
        const string source = "AuthService";
        const string correlationId = "trace-xyz-789";

        var withTracing = result.WithTracing(source, correlationId);

        withTracing.Metadata.Should().NotBeNull();
        withTracing.Metadata!.Value.Should().HaveCount(3);
        withTracing.Metadata.Value.TryGetString("environment", out var env).Should().BeTrue();
        env.Should().Be("production");
        withTracing.TryGetSource(out var actualSource).Should().BeTrue();
        actualSource.Should().Be(source);
        withTracing.TryGetCorrelationId(out var actualId).Should().BeTrue();
        actualId.Should().Be(correlationId);
    }

    [Fact]
    public void WithTracing_OnFailedResult_ShouldPreserveErrors()
    {
        var error = new Error { Message = "Unauthorized" };
        var result = Result<int>.Fail(error);
        const string source = "SecurityService";
        const string correlationId = "sec-trace-001";

        var withTracing = result.WithTracing(source, correlationId);

        withTracing.IsValid.Should().BeFalse();
        withTracing.Errors.Should().ContainSingle().Which.Message.Should().Be("Unauthorized");
        withTracing.Metadata.Should().NotBeNull();
        withTracing.Metadata!.Value.Should().HaveCount(2);
    }

    [Fact]
    public void WithTracing_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok();
        const string source = "NotificationService";
        const string correlationId = "notif-123-abc";

        var withTracing = result.WithTracing(source, correlationId);

        withTracing.Metadata.Should().NotBeNull();
        withTracing.Metadata!.Value.Should().HaveCount(2);
        withTracing.TryGetSource(out var actualSource).Should().BeTrue();
        actualSource.Should().Be(source);
        withTracing.TryGetCorrelationId(out var actualId).Should().BeTrue();
        actualId.Should().Be(correlationId);
    }

    [Fact]
    public void TryGetSource_WhenSourceExists_ShouldReturnTrueAndValue()
    {
        var result = Result<int>.Ok(42).WithSource("DataService");

        var found = result.TryGetSource(out var source);

        found.Should().BeTrue();
        source.Should().Be("DataService");
    }

    [Fact]
    public void TryGetSource_WhenSourceDoesNotExist_ShouldReturnFalse()
    {
        var result = Result<int>.Ok(42);

        var found = result.TryGetSource(out var source);

        found.Should().BeFalse();
        source.Should().BeNull();
    }

    [Fact]
    public void TryGetSource_WhenMetadataIsNull_ShouldReturnFalse()
    {
        var result = Result<int>.Ok(42);

        var found = result.TryGetSource(out var source);

        found.Should().BeFalse();
        source.Should().BeNull();
    }

    [Fact]
    public void TryGetSource_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok().WithSource("CacheService");

        var found = result.TryGetSource(out var source);

        found.Should().BeTrue();
        source.Should().Be("CacheService");
    }

    [Fact]
    public void TryGetCorrelationId_WhenCorrelationIdExists_ShouldReturnTrueAndValue()
    {
        var expectedId = "550e8400-e29b-41d4-a716-446655440000";
        var result = Result<int>.Ok(42).WithCorrelationId(expectedId);

        var found = result.TryGetCorrelationId(out var correlationId);

        found.Should().BeTrue();
        correlationId.Should().Be(expectedId);
    }

    [Fact]
    public void TryGetCorrelationId_WhenCorrelationIdDoesNotExist_ShouldReturnFalse()
    {
        var result = Result<int>.Ok(42);

        var found = result.TryGetCorrelationId(out var correlationId);

        found.Should().BeFalse();
        correlationId.Should().BeNull();
    }

    [Fact]
    public void TryGetCorrelationId_WhenMetadataIsNull_ShouldReturnFalse()
    {
        var result = Result<int>.Ok(42);

        var found = result.TryGetCorrelationId(out var correlationId);

        found.Should().BeFalse();
        correlationId.Should().BeNull();
    }

    [Fact]
    public void TryGetCorrelationId_OnNonGenericResult_ShouldWork()
    {
        var expectedId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        var result = Result.Ok().WithCorrelationId(expectedId);

        var found = result.TryGetCorrelationId(out var correlationId);

        found.Should().BeTrue();
        correlationId.Should().Be(expectedId);
    }

    [Fact]
    public void TracingKeys_ShouldBeAccessible()
    {
        Tracing.SourceKey.Should().Be("source");
        Tracing.CorrelationIdKey.Should().Be("correlationId");
    }

    [Fact]
    public void Chaining_WithSourceAndWithCorrelationId_ShouldWork()
    {
        var result = Result<int>.Ok(42)
           .WithSource("ChainService")
           .WithCorrelationId("chain-123");

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().HaveCount(2);
        result.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("ChainService");
        result.TryGetCorrelationId(out var correlationId).Should().BeTrue();
        correlationId.Should().Be("chain-123");
    }

    [Fact]
    public void Chaining_WithTracingAndOtherMetadata_ShouldWork()
    {
        var result = Result<string>.Ok("test")
           .MergeMetadata(("version", "1.0"))
           .WithTracing("ApiService", "api-trace-001")
           .MergeMetadata(("timestamp", 1234567890L));

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().HaveCount(4);
        result.TryGetSource(out var source).Should().BeTrue();
        source.Should().Be("ApiService");
        result.TryGetCorrelationId(out var correlationId).Should().BeTrue();
        correlationId.Should().Be("api-trace-001");
        result.Metadata.Value.TryGetString("version", out var version).Should().BeTrue();
        version.Should().Be("1.0");
        result.Metadata.Value.TryGetInt64("timestamp", out var timestamp).Should().BeTrue();
        timestamp.Should().Be(1234567890L);
    }

    [Fact]
    public void WithSource_SupportsVariousCorrelationIdFormats()
    {
        const string uuidFormat = "550e8400-e29b-41d4-a716-446655440000";
        const string w3CFormat = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        const string customFormat = "req-2024-001-abc123";

        var result1 = Result<int>.Ok(1).WithCorrelationId(uuidFormat);
        var result2 = Result<int>.Ok(2).WithCorrelationId(w3CFormat);
        var result3 = Result<int>.Ok(3).WithCorrelationId(customFormat);

        result1.TryGetCorrelationId(out var id1).Should().BeTrue();
        id1.Should().Be(uuidFormat);

        result2.TryGetCorrelationId(out var id2).Should().BeTrue();
        id2.Should().Be(w3CFormat);

        result3.TryGetCorrelationId(out var id3).Should().BeTrue();
        id3.Should().Be(customFormat);
    }
}
