using System;

namespace Light.PortableResults.Validation;

internal sealed class ValidationErrorSink
{
    private Error[]? _additionalErrors;
    private Error _firstError;

    public int Count { get; private set; }

    public bool HasErrors => Count > 0;

    public void Add(Error error)
    {
        if (error.IsDefaultInstance)
        {
            throw new ArgumentException("The error must not be the default instance.", nameof(error));
        }

        switch (Count)
        {
            case 0:
                _firstError = error;
                Count = 1;
                return;
            case 1:
                EnsureCapacity(1);
                _additionalErrors![0] = error;
                Count = 2;
                return;
            default:
                EnsureCapacity(Count - 1 + 1);
                _additionalErrors![Count - 1] = error;
                Count++;
                return;
        }
    }

    public bool TryBuildErrors(out Errors errors)
    {
        if (Count == 0)
        {
            errors = default;
            return false;
        }

        if (Count == 1)
        {
            errors = new Errors(_firstError);
            return true;
        }

        var buffer = new Error[Count];
        buffer[0] = _firstError;
        Array.Copy(_additionalErrors!, 0, buffer, 1, Count - 1);
        errors = new Errors(buffer);
        return true;
    }

    private void EnsureCapacity(int requiredAdditionalCount)
    {
        if (_additionalErrors is not null && _additionalErrors.Length >= requiredAdditionalCount)
        {
            return;
        }

        var newCapacity = _additionalErrors is null ?
            4 :
            Math.Max(_additionalErrors.Length * 2, requiredAdditionalCount);
        var newBuffer = new Error[newCapacity];
        if (_additionalErrors is not null)
        {
            Array.Copy(_additionalErrors, newBuffer, Count - 1);
        }

        _additionalErrors = newBuffer;
    }
}
