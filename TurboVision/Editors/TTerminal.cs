using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// Terminal view that displays text output in a scrollable buffer.
/// Uses a circular buffer to efficiently manage text history.
/// </summary>
public class TTerminal : TTextDevice
{
    /// <summary>
    /// Size of the circular buffer.
    /// </summary>
    protected ushort BufSize { get; private set; }

    /// <summary>
    /// The circular buffer storing terminal output.
    /// </summary>
    protected char[]? Buffer { get; private set; }

    /// <summary>
    /// Front of the queue (where new data is written).
    /// </summary>
    protected ushort QueFront { get; set; }

    /// <summary>
    /// Back of the queue (oldest data).
    /// </summary>
    protected ushort QueBack { get; set; }

    public TTerminal(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar, ushort bufSize)
        : base(bounds, hScrollBar, vScrollBar)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        BufSize = Math.Min((ushort)32000, bufSize);
        Buffer = new char[BufSize];
        SetLimit(0, 1);
        SetCursor(0, 0);
        ShowCursor();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Buffer = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Decrements a buffer position, wrapping around if necessary.
    /// </summary>
    protected void BufDec(ref ushort val)
    {
        if (val == 0)
            val = (ushort)(BufSize - 1);
        else
            val--;
    }

    /// <summary>
    /// Increments a buffer position, wrapping around if necessary.
    /// </summary>
    protected void BufInc(ref ushort val)
    {
        if (++val >= BufSize)
            val = 0;
    }

    /// <summary>
    /// Checks if there's enough room in the buffer for the specified amount of data.
    /// </summary>
    protected bool CanInsert(ushort amount)
    {
        long t = QueFront < QueBack
            ? QueFront + amount
            : (long)QueFront - BufSize + amount;
        return QueBack > t;
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var color = MapColor(1);
        ushort endLine;
        ushort bottomLine = (ushort)(Size.Y + Delta.Y);

        SetCursor(-1, -1);

        if (Limit.Y > bottomLine)
        {
            endLine = PrevLines(QueFront, (ushort)(Limit.Y - bottomLine));
            BufDec(ref endLine);
        }
        else
        {
            endLine = QueFront;
        }

        int y;
        if (Limit.Y > Size.Y)
        {
            y = Size.Y - 1;
        }
        else
        {
            for (y = Limit.Y; y < Size.Y; y++)
                WriteChar(0, y, ' ', 1, Size.X);
            y = Limit.Y - 1;
        }

        for (; y >= 0; y--)
        {
            int x = 0;
            ushort begLine = PrevLines(endLine, 1);
            ushort linePos = begLine;

            while (linePos != endLine)
            {
                int copyLen;
                if (endLine >= linePos)
                {
                    copyLen = Math.Min(endLine - linePos, Size.X - x);
                    for (int i = 0; i < copyLen && linePos + i < BufSize; i++)
                    {
                        buf.Data[x + i] = new TScreenCell(Buffer![linePos + i], color);
                    }
                }
                else
                {
                    // Wrapping case
                    int firstPart = Math.Min(BufSize - linePos, Size.X - x);
                    for (int i = 0; i < firstPart; i++)
                    {
                        buf.Data[x + i] = new TScreenCell(Buffer![linePos + i], color);
                    }
                    int secondPart = Math.Min(endLine, Size.X - x - firstPart);
                    for (int i = 0; i < secondPart; i++)
                    {
                        buf.Data[x + firstPart + i] = new TScreenCell(Buffer![i], color);
                    }
                    copyLen = firstPart + secondPart;
                }

                x += copyLen;

                // Update linePos
                if (linePos + copyLen >= BufSize)
                    linePos = (ushort)(copyLen - (BufSize - linePos));
                else
                    linePos += (ushort)copyLen;
            }

            // Fill rest of line with spaces
            for (int i = x; i < Size.X; i++)
            {
                buf.Data[i] = new TScreenCell(' ', color);
            }

            WriteBuf(0, y, Size.X, 1, buf);

            // Draw cursor on last line
            if (endLine == QueFront)
                SetCursor(x, y);

            endLine = begLine;
            BufDec(ref endLine);
        }
    }

    /// <summary>
    /// Finds the position after the next newline character.
    /// </summary>
    protected ushort NextLine(ushort pos)
    {
        while (pos != QueFront && Buffer![pos] != '\n')
            BufInc(ref pos);
        if (pos != QueFront)
            BufInc(ref pos);
        return pos;
    }

    /// <summary>
    /// Finds the position of the start of the line 'lines' lines before 'pos'.
    /// </summary>
    protected ushort PrevLines(ushort pos, ushort lines)
    {
        if (lines > 0 && pos != QueBack)
        {
            do
            {
                if (pos == QueBack)
                    return QueBack;
                BufDec(ref pos);
                ushort count = (ushort)((pos >= QueBack ? pos - QueBack : pos) + 1);
                if (FindLfBackwards(pos, count))
                    lines--;
            } while (lines > 0);
            BufInc(ref pos);
        }
        return pos;
    }

    /// <summary>
    /// Searches backwards for a newline character.
    /// </summary>
    private bool FindLfBackwards(ushort pos, ushort count)
    {
        pos++;
        do
        {
            pos--;
            if (Buffer![pos] == '\n')
                return true;
        } while (--count > 0);
        return false;
    }

    public override int DoSputn(ReadOnlySpan<char> s)
    {
        int count = s.Length;
        int screenLines = Limit.Y;

        // If data is larger than buffer, truncate from start
        if (count > BufSize - 1)
        {
            s = s.Slice(count - (BufSize - 1));
            count = BufSize - 1;
        }

        // Count newlines in input
        for (int i = 0; i < count; i++)
        {
            if (s[i] == '\n')
                screenLines++;
        }

        // Make room by discarding old lines if necessary
        while (!CanInsert((ushort)count))
        {
            QueBack = NextLine(QueBack);
            if (screenLines > 1)
                screenLines--;
        }

        // Copy data to buffer
        if (QueFront + count >= BufSize)
        {
            int firstPart = BufSize - QueFront;
            s.Slice(0, firstPart).CopyTo(Buffer.AsSpan(QueFront));
            s.Slice(firstPart, count - firstPart).CopyTo(Buffer.AsSpan(0));
            QueFront = (ushort)(count - firstPart);
        }
        else
        {
            s.Slice(0, count).CopyTo(Buffer.AsSpan(QueFront));
            QueFront += (ushort)count;
        }

        // Update display
        DrawLock++;
        SetLimit(Limit.X, screenLines);
        ScrollTo(0, screenLines + 1);
        DrawLock--;

        DrawView();
        return count;
    }

    /// <summary>
    /// Returns true if the buffer is empty.
    /// </summary>
    public bool QueEmpty()
    {
        return QueBack == QueFront;
    }
}
