using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BenchmarkDotNet.Attributes;
using Light.Results;
using Light.Results.CloudEvents;
using Light.Results.CloudEvents.Reading;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class CloudEventsReadingBenchmarks
{
    private ReadOnlyMemory<byte> _failureCloudEvents;
    private ReadOnlyMemory<byte> _largeSuccessCloudEvents;
    private ReadOnlyMemory<byte> _mediumSuccessCloudEvents;
    private LightResultsCloudEventsReadOptions _options = null!;
    private ReadOnlyMemory<byte> _smallSuccessCloudEvents;

    [GlobalSetup]
    public void Setup()
    {
        _options = new LightResultsCloudEventsReadOptions
        {
            SerializerOptions = Module.CreateDefaultSerializerOptions()
        };

        // Small payload (~100 bytes data)
        _smallSuccessCloudEvents = CreateCloudEventsBytes(
            """
            {
              "specversion": "1.0",
              "type": "com.example.success",
              "source": "/test",
              "id": "event-1",
              "lroutcome": "success",
              "data": {
                "value": {
                  "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
                  "name": "Contact A"
                }
              }
            }
            """
        );

        // Medium payload (~1KB data)
        _mediumSuccessCloudEvents = CreateCloudEventsBytes(CreateMediumCloudEvents());

        // Large payload (~10KB data)
        _largeSuccessCloudEvents = CreateCloudEventsBytes(CreateLargeCloudEvents());

        // Failure payload
        _failureCloudEvents = CreateCloudEventsBytes(
            """
            {
              "specversion": "1.0",
              "type": "com.example.failure",
              "source": "/test",
              "id": "event-2",
              "lroutcome": "failure",
              "data": {
                "errors": [
                  {
                    "message": "Name is required",
                    "code": "NameRequired",
                    "target": "name",
                    "category": "Validation"
                  }
                ]
              }
            }
            """
        );
    }

    [Benchmark(Baseline = true)]
    public Result<ContactDto> ReadResult_SmallPayload()
    {
        return _smallSuccessCloudEvents.ReadResult<ContactDto>(_options);
    }

    [Benchmark]
    public Result<ContactDto> ReadResult_MediumPayload()
    {
        return _mediumSuccessCloudEvents.ReadResult<ContactDto>(_options);
    }

    [Benchmark]
    public Result<ContactDto> ReadResult_LargePayload()
    {
        return _largeSuccessCloudEvents.ReadResult<ContactDto>(_options);
    }

    [Benchmark]
    public Result ReadResult_Failure()
    {
        return _failureCloudEvents.ReadResult(_options);
    }

    [Benchmark]
    public CloudEventsEnvelope<ContactDto> ReadResultWithEnvelope_SmallPayload()
    {
        return _smallSuccessCloudEvents.ReadResultWithCloudEventsEnvelope<ContactDto>(_options);
    }

    private static ReadOnlyMemory<byte> CreateCloudEventsBytes(string json)
    {
        return Encoding.UTF8.GetBytes(json);
    }

    private static string CreateMediumCloudEvents()
    {
        // Create a ~1KB data payload
        var items = new StringBuilder();
        for (var i = 0; i < 10; i++)
        {
            if (i > 0)
            {
                items.Append(',');
            }

            items.Append(
                $$"""
                  {"id":"{{Guid.NewGuid()}}","name":"Contact {{i}}","email":"contact{{i}}@example.com","phone":"+1-555-000-{{i:D4}}"}
                  """
            );
        }

        return $$"""
                 {
                   "specversion": "1.0",
                   "type": "com.example.success",
                   "source": "/test",
                   "id": "event-medium",
                   "lroutcome": "success",
                   "data": {
                     "value": {
                       "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
                       "name": "Contact A",
                       "items": [{{items}}]
                     }
                   }
                 }
                 """;
    }

    private static string CreateLargeCloudEvents()
    {
        // Create a ~10KB data payload
        var items = new StringBuilder();
        for (var i = 0; i < 100; i++)
        {
            if (i > 0)
            {
                items.Append(',');
            }

            items.Append(
                $$"""
                  {"id":"{{Guid.NewGuid()}}","name":"Contact {{i}}","email":"contact{{i}}@example.com","phone":"+1-555-000-{{i:D4}}","address":"123 Main Street, Suite {{i}}, City, State 12345"}
                  """
            );
        }

        return $$"""
                 {
                   "specversion": "1.0",
                   "type": "com.example.success",
                   "source": "/test",
                   "id": "event-large",
                   "lroutcome": "success",
                   "data": {
                     "value": {
                       "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
                       "name": "Contact A",
                       "items": [{{items}}]
                     }
                   }
                 }
                 """;
    }

    public sealed class ContactDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public object[]? Items { get; set; }
    }
}
