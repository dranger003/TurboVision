namespace TurboVision.Core;

/// <summary>
/// A buffer for drawing operations. Views write to this buffer,
/// which is then copied to the screen.
/// </summary>
public class TDrawBuffer
{
    public const int MaxViewWidth = 255;

    private readonly TScreenCell[] _data;

    public TDrawBuffer()
    {
        _data = new TScreenCell[MaxViewWidth];
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = new TScreenCell(' ', default);
        }
    }

    public TDrawBuffer(int width)
    {
        _data = new TScreenCell[width];
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = new TScreenCell(' ', default);
        }
    }

    public int Length
    {
        get { return _data.Length; }
    }

    public Span<TScreenCell> Data
    {
        get { return _data; }
    }

    public TScreenCell this[int index]
    {
        get { return _data[index]; }
        set { _data[index] = value; }
    }

    /// <summary>
    /// Copies a sequence of characters from a source string to the buffer.
    /// Each character gets the specified attribute.
    /// </summary>
    public void MoveBuf(int indent, ReadOnlySpan<char> source, TColorAttr attr, int count)
    {
        if (indent >= _data.Length)
        {
            return;
        }

        int end = Math.Min(count, Math.Min(source.Length, _data.Length - indent));
        for (int i = 0; i < end; i++)
        {
            _data[indent + i].SetCell(source[i], attr);
        }
    }

    /// <summary>
    /// Fills a portion of the buffer with a character and attribute.
    /// </summary>
    public void MoveChar(int indent, char c, TColorAttr attr, int count)
    {
        if (indent >= _data.Length)
        {
            return;
        }

        int end = Math.Min(indent + count, _data.Length);
        for (int i = indent; i < end; i++)
        {
            _data[i].SetCell(c, attr);
        }
    }

    /// <summary>
    /// Writes a string to the buffer at the specified position.
    /// </summary>
    public int MoveStr(int indent, ReadOnlySpan<char> str, TColorAttr attr)
    {
        if (indent >= _data.Length)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < str.Length && indent + count < _data.Length; i++)
        {
            _data[indent + count].SetCell(str[i], attr);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Writes a C-style string (with ~ shortcuts) to the buffer.
    /// Characters between ~ are rendered with the highlight attribute.
    /// </summary>
    public int MoveCStr(int indent, ReadOnlySpan<char> str, TAttrPair attrs)
    {
        if (indent >= _data.Length)
        {
            return 0;
        }

        int count = 0;
        bool highlight = false;

        for (int i = 0; i < str.Length && indent + count < _data.Length; i++)
        {
            if (str[i] == '~')
            {
                highlight = !highlight;
                continue;
            }

            var attr = highlight ? attrs.Highlight : attrs.Normal;
            _data[indent + count].SetCell(str[i], attr);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Sets the attribute at a specific position.
    /// </summary>
    public void PutAttribute(int indent, TColorAttr attr)
    {
        if (indent < _data.Length)
        {
            _data[indent].Attr = attr;
        }
    }

    /// <summary>
    /// Sets the character at a specific position.
    /// </summary>
    public void PutChar(int indent, char c)
    {
        if (indent < _data.Length)
        {
            _data[indent].Char = c;
        }
    }
}
