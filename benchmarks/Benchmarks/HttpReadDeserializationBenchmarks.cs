using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Json;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class HttpReadDeserializationBenchmarks
{
    private string _autoBareJson = null!;
    private string _autoWrappedJson = null!;
    private string _failureJson = null!;
    private JsonSerializerOptions _legacyOptions = null!;
    private JsonSerializerOptions _optimizedOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _optimizedOptions = Module.CreateDefaultSerializerOptions();

        _legacyOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _legacyOptions.AddDefaultPortableResultsHttpReadJsonConverters();
        _legacyOptions.Converters.Insert(0, new LegacyAutoContactPayloadJsonConverter());

        _autoBareJson =
            """
            {
              "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
              "name": "Contact A"
            }
            """;

        _autoWrappedJson =
            """
            {
              "value": {
                "id": "6B8A4DCA-779D-4F36-8274-487FE3E86B5A",
                "name": "Contact A"
              },
              "metadata": {
                "trace": "t-1"
              }
            }
            """;

        _failureJson =
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "errors": [
                {
                  "message": "Name is required",
                  "code": "NameRequired",
                  "target": "name",
                  "category": "Validation"
                }
              ]
            }
            """;
    }

    [Benchmark(Baseline = true)]
    public HttpReadAutoSuccessResultPayload<ContactDto> AutoSuccessBare_LegacyFullScan()
    {
        return JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<ContactDto>>(_autoBareJson, _legacyOptions);
    }

    [Benchmark]
    public HttpReadAutoSuccessResultPayload<ContactDto> AutoSuccessBare_Optimized()
    {
        return JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<ContactDto>>(
            _autoBareJson,
            _optimizedOptions
        );
    }

    [Benchmark]
    public HttpReadAutoSuccessResultPayload<ContactDto> AutoSuccessWrapped_LegacyFullScan()
    {
        return JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<ContactDto>>(
            _autoWrappedJson,
            _legacyOptions
        );
    }

    [Benchmark]
    public HttpReadAutoSuccessResultPayload<ContactDto> AutoSuccessWrapped_Optimized()
    {
        return JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<ContactDto>>(
            _autoWrappedJson,
            _optimizedOptions
        );
    }

    [Benchmark]
    public HttpReadFailureResultPayload Failure_Optimized()
    {
        return JsonSerializer.Deserialize<HttpReadFailureResultPayload>(_failureJson, _optimizedOptions);
    }

    private sealed class LegacyAutoContactPayloadJsonConverter :
        JsonConverter<HttpReadAutoSuccessResultPayload<ContactDto>>
    {
        public override HttpReadAutoSuccessResultPayload<ContactDto> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            return LegacyAutoPayloadReader.ReadAutoSuccessPayload<ContactDto>(ref reader, options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            HttpReadAutoSuccessResultPayload<ContactDto> value,
            JsonSerializerOptions options
        ) => throw new NotSupportedException();
    }

    private static class LegacyAutoPayloadReader
    {
        public static HttpReadAutoSuccessResultPayload<T> ReadAutoSuccessPayload<T>(
            ref Utf8JsonReader reader,
            JsonSerializerOptions serializerOptions
        )
        {
            EnsureReaderHasToken(ref reader);

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                var barePayload = ResultJsonReader.ReadBareSuccessPayload<T>(ref reader, serializerOptions);
                return new HttpReadAutoSuccessResultPayload<T>(barePayload.Value, metadata: null);
            }

            if (IsWrapperCandidateByFullScan(ref reader))
            {
                var wrappedPayload = ResultJsonReader.ReadWrappedSuccessPayload<T>(ref reader, serializerOptions);
                return new HttpReadAutoSuccessResultPayload<T>(wrappedPayload.Value, wrappedPayload.Metadata);
            }

            var fallbackBarePayload = ResultJsonReader.ReadBareSuccessPayload<T>(ref reader, serializerOptions);
            return new HttpReadAutoSuccessResultPayload<T>(fallbackBarePayload.Value, metadata: null);
        }

        private static bool IsWrapperCandidateByFullScan(ref Utf8JsonReader reader)
        {
            var lookahead = reader;
            EnsureReaderHasToken(ref lookahead);

            if (lookahead.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            var hasOtherProperties = false;

            while (lookahead.Read())
            {
                if (lookahead.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (lookahead.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in JSON object.");
                }

                if (!lookahead.ValueTextEquals("value") && !lookahead.ValueTextEquals("metadata"))
                {
                    hasOtherProperties = true;
                }

                if (!lookahead.Read())
                {
                    throw new JsonException("Unexpected end of JSON while inspecting object.");
                }

                lookahead.Skip();
            }

            return !hasOtherProperties;
        }

        private static void EnsureReaderHasToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.None)
            {
                return;
            }

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON.");
            }
        }
    }

    public sealed class ContactDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
