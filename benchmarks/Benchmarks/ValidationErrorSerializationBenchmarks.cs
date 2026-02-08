using System.Buffers;
using System.Net;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Light.Results;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;

namespace Benchmarks;

[MemoryDiagnoser]
public class ValidationErrorSerializationBenchmarks
{
    private ArrayBufferWriter<byte> _buffer = null!;
    private Errors _errors1;
    private Errors _errors10SharedTargets;
    private Errors _errors10UniqueTargets;
    private Errors _errors3SharedTarget;
    private Errors _errors3UniqueTargets;
    private Errors _errors5SharedTargets;
    private Errors _errors5UniqueTargets;
    private JsonSerializerOptions _jsonOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new ArrayBufferWriter<byte>(4096);
        _jsonOptions = new JsonSerializerOptions();

        _errors1 = new Errors(new Error { Message = "Field is required", Target = "email", Code = "Required" });

        _errors3UniqueTargets = new Errors(
            new Error[]
            {
                new () { Message = "Field is required", Target = "email", Code = "Required" },
                new () { Message = "Must be at least 8 characters", Target = "password", Code = "MinLength" },
                new () { Message = "Invalid format", Target = "phone", Code = "InvalidFormat" }
            }
        );

        _errors3SharedTarget = new Errors(
            new Error[]
            {
                new () { Message = "Field is required", Target = "email", Code = "Required" },
                new () { Message = "Invalid email format", Target = "email", Code = "InvalidFormat" },
                new () { Message = "Email already exists", Target = "email", Code = "Duplicate" }
            }
        );

        _errors5UniqueTargets = new Errors(
            new Error[]
            {
                new () { Message = "Field is required", Target = "email", Code = "Required" },
                new () { Message = "Must be at least 8 characters", Target = "password", Code = "MinLength" },
                new () { Message = "Invalid format", Target = "phone", Code = "InvalidFormat" },
                new () { Message = "Must be a valid date", Target = "birthDate", Code = "InvalidDate" },
                new () { Message = "Required field", Target = "name", Code = "Required" }
            }
        );

        _errors5SharedTargets = new Errors(
            new Error[]
            {
                new () { Message = "Field is required", Target = "email", Code = "Required" },
                new () { Message = "Invalid email format", Target = "email", Code = "InvalidFormat" },
                new () { Message = "Must be at least 8 characters", Target = "password", Code = "MinLength" },
                new () { Message = "Must contain uppercase", Target = "password", Code = "Uppercase" },
                new () { Message = "Must contain number", Target = "password", Code = "Number" }
            }
        );

        _errors10UniqueTargets = new Errors(
            new Error[]
            {
                new () { Message = "Field is required", Target = "field1", Code = "Required" },
                new () { Message = "Field is required", Target = "field2", Code = "Required" },
                new () { Message = "Field is required", Target = "field3", Code = "Required" },
                new () { Message = "Field is required", Target = "field4", Code = "Required" },
                new () { Message = "Field is required", Target = "field5", Code = "Required" },
                new () { Message = "Field is required", Target = "field6", Code = "Required" },
                new () { Message = "Field is required", Target = "field7", Code = "Required" },
                new () { Message = "Field is required", Target = "field8", Code = "Required" },
                new () { Message = "Field is required", Target = "field9", Code = "Required" },
                new () { Message = "Field is required", Target = "field10", Code = "Required" }
            }
        );

        _errors10SharedTargets = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Target = "email", Code = "E1" },
                new () { Message = "Error 2", Target = "email", Code = "E2" },
                new () { Message = "Error 3", Target = "email", Code = "E3" },
                new () { Message = "Error 4", Target = "password", Code = "E4" },
                new () { Message = "Error 5", Target = "password", Code = "E5" },
                new () { Message = "Error 6", Target = "password", Code = "E6" },
                new () { Message = "Error 7", Target = "phone", Code = "E7" },
                new () { Message = "Error 8", Target = "phone", Code = "E8" },
                new () { Message = "Error 9", Target = "name", Code = "E9" },
                new () { Message = "Error 10", Target = "name", Code = "E10" }
            }
        );
    }

    [Benchmark(Baseline = true)]
    public void Errors1()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors1,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors3_UniqueTargets()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors3UniqueTargets,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors3_SharedTarget()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors3SharedTarget,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors5_UniqueTargets()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors5UniqueTargets,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors5_SharedTargets()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors5SharedTargets,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors10_UniqueTargets()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors10UniqueTargets,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }

    [Benchmark]
    public void Errors10_SharedTargets()
    {
        _buffer.ResetWrittenCount();
        using var writer = new Utf8JsonWriter(_buffer);
        writer.WriteStartObject();
        writer.WriteErrors(
            _errors10SharedTargets,
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            HttpStatusCode.BadRequest,
            _jsonOptions
        );
        writer.WriteEndObject();
        writer.Flush();
    }
}
