using System;
using System.Text;

namespace Light.PortableResults.Validation.OpenApi.SourceGeneration;

/// <summary>
/// A small writer over <see cref="StringBuilder" /> that tracks indentation while
/// generated source is composed. Indent is written lazily on the first character of
/// each new line, so chained <see cref="Write(string)" /> calls produce a single
/// indented line.
/// </summary>
internal sealed class CodeWriter
{
    private const string IndentString = "    ";
    private readonly StringBuilder _builder = new ();
    private int _indentLevel;
    private bool _isAtLineStart = true;

    public int IndentLevel => _indentLevel;

    public CodeWriter IncreaseIndent()
    {
        _indentLevel++;
        return this;
    }

    public CodeWriter DecreaseIndent()
    {
        if (_indentLevel == 0)
        {
            throw new InvalidOperationException("Indent level is already zero.");
        }

        _indentLevel--;
        return this;
    }

    public CodeWriter Write(string value)
    {
        WriteIndentIfAtLineStart();
        _builder.Append(value);
        return this;
    }

    public CodeWriter Write(char value)
    {
        WriteIndentIfAtLineStart();
        _builder.Append(value);
        return this;
    }

    public CodeWriter WriteLine()
    {
        _builder.AppendLine();
        _isAtLineStart = true;
        return this;
    }

    public CodeWriter WriteLine(string value)
    {
        WriteIndentIfAtLineStart();
        _builder.AppendLine(value);
        _isAtLineStart = true;
        return this;
    }

    /// <summary>
    /// Writes a line containing <c>{</c> at the current indent level and increases
    /// the indent so subsequent lines fall inside the block.
    /// </summary>
    public CodeWriter OpenBrace()
    {
        WriteLine("{");
        return IncreaseIndent();
    }

    /// <summary>
    /// Decreases the indent level and writes a line containing <c>}</c> at the new level.
    /// </summary>
    public CodeWriter CloseBrace()
    {
        DecreaseIndent();
        return WriteLine("}");
    }

    public override string ToString() => _builder.ToString();

    private void WriteIndentIfAtLineStart()
    {
        if (!_isAtLineStart)
        {
            return;
        }

        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(IndentString);
        }

        _isAtLineStart = false;
    }
}
