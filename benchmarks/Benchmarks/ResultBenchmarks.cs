using BenchmarkDotNet.Attributes;
using Light.Results;

namespace Benchmarks;

[MemoryDiagnoser]
public class ResultBenchmarks
{
    private Result<int> _failureResult;
    private Result<int> _multiErrorResult;
    private Result<int> _successResult;

    [GlobalSetup]
    public void Setup()
    {
        _successResult = Result<int>.Ok(42);
        _failureResult = Result<int>.Fail(new Error { Message = "Error message", Code = "ERR001" });
        _multiErrorResult = Result<int>.Fail(
            new[]
            {
                new Error { Message = "Error 1", Code = "E1" },
                new Error { Message = "Error 2", Code = "E2" },
                new Error { Message = "Error 3", Code = "E3" }
            }
        );
    }

    [Benchmark]
    public Result<int> CreateSuccess() => Result<int>.Ok(42);

    [Benchmark]
    public Result<int> CreateFailure() => Result<int>.Fail(new Error { Message = "Error" });

    [Benchmark]
    public bool TryGetValue_Success()
    {
        return _successResult.TryGetValue(out _);
    }

    [Benchmark]
    public bool TryGetValue_Failure()
    {
        return _failureResult.TryGetValue(out _);
    }

    [Benchmark]
    public int EnumerateErrors_Single()
    {
        var count = 0;
        foreach (var unused in _failureResult.Errors)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int EnumerateErrors_Multiple()
    {
        var count = 0;
        foreach (var unused in _multiErrorResult.Errors)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public Error AccessErrorByIndex_Single() => _failureResult.Errors[0];

    [Benchmark]
    public Error AccessErrorByIndex_Multiple() => _multiErrorResult.Errors[2];

    [Benchmark]
    public int ErrorsCount() => _multiErrorResult.Errors.Count;

    [Benchmark]
    public Error FirstError() => _failureResult.FirstError;

    [Benchmark]
    public Result<string> Map_Success() => _successResult.Map(x => x.ToString());

    [Benchmark]
    public Result<string> Map_Failure() => _failureResult.Map(x => x.ToString());
}
