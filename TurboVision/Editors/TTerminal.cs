using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

// =============================================================================
// TTerminal class
// Upstream: textview.h lines 66-95, textview.cpp lines 57-245
// =============================================================================

/// <summary>
/// Terminal view that displays text output in a scrollable buffer.
/// Uses a circular buffer to efficiently manage text history.
/// Matches upstream TTerminal (textview.h lines 66-95).
/// </summary>
public class TTerminal : TTextDevice
{
    #region Fields (textview.h lines 91-93)

    protected ushort bufSize;
    protected char[]? buffer;
    protected ushort queFront, queBack;

    #endregion

    #region Constructor and Destructor (textview.cpp lines 57-77)

    public TTerminal(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, ushort aBufSize)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        queFront = 0;
        queBack = 0;
        GrowMode = GrowFlags.gfGrowHiX + GrowFlags.gfGrowHiY;
        bufSize = Math.Min((ushort)32000, aBufSize);
        buffer = new char[bufSize];
        SetLimit(0, 1);
        SetCursor(0, 0);
        ShowCursor();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            buffer = null;
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Protected Methods (textview.h line 94, textview.cpp lines 79-85)

    /// <summary>
    /// Decrements a buffer position, wrapping around if necessary.
    /// </summary>
    protected void BufDec(ref ushort val)
    {
        if (val == 0)
            val = (ushort)(bufSize - 1);
        else
            val--;
    }

    #endregion

    #region Public Methods (textview.h lines 82-87)

    /// <summary>
    /// Increments a buffer position, wrapping around if necessary.
    /// Upstream: textview.h line 82 (public method)
    /// </summary>
    public void BufInc(ref ushort val)
    {
        if (++val >= bufSize)
            val = 0;
    }

    /// <summary>
    /// Checks if there's enough room in the buffer for the specified amount of data.
    /// Upstream: textview.h line 83 (public method)
    /// </summary>
    public bool CanInsert(ushort amount)
    {
        long T = queFront < queBack
            ? queFront + amount
            : (long)queFront - bufSize + amount;
        return queBack > T;
    }

    // Upstream: textview.cpp lines 117-186
    public override void Draw()
    {
        TDrawBuffer b;
        char[] s = new char[256];
        int sLen;
        int x, y;
        ushort begLine, endLine, linePos;
        ushort bottomLine;
        TColorAttr color = MapColor(1);

        SetCursor(-1, -1);

        bottomLine = (ushort)(Size.Y + Delta.Y);
        if (Limit.Y > bottomLine)
        {
            endLine = PrevLines(queFront, (ushort)(Limit.Y - bottomLine));
            BufDec(ref endLine);
        }
        else
            endLine = queFront;

        if (Limit.Y > Size.Y)
            y = Size.Y - 1;
        else
        {
            for (y = Limit.Y; y < Size.Y; y++)
                WriteChar(0, y, ' ', 1, Size.X);
            y = Limit.Y - 1;
        }

        b = new TDrawBuffer();
        for (; y >= 0; y--)
        {
            x = 0;
            begLine = PrevLines(endLine, 1);
            linePos = begLine;
            while (linePos != endLine)
            {
                if (endLine >= linePos)
                {
                    int cpyLen = Math.Min(endLine - linePos, s.Length);
                    Array.Copy(buffer!, linePos, s, 0, cpyLen);
                    sLen = cpyLen;
                }
                else
                {
                    int fstCpyLen = Math.Min(bufSize - linePos, s.Length);
                    int sndCpyLen = Math.Min(endLine, s.Length - fstCpyLen);
                    Array.Copy(buffer!, linePos, s, 0, fstCpyLen);
                    Array.Copy(buffer!, 0, s, fstCpyLen, sndCpyLen);
                    sLen = fstCpyLen + sndCpyLen;
                }

                DiscardPossiblyTruncatedCharsAtEnd(s, ref sLen);
                if (linePos >= bufSize - sLen)
                    linePos = (ushort)(sLen - (bufSize - linePos));
                else
                    linePos += (ushort)sLen;

                x += b.MoveStr(x, s.AsSpan(0, sLen), color);
            }

            b.MoveChar(x, ' ', color, Math.Max(Size.X - x, 0));
            WriteBuf(0, y, Size.X, 1, b);
            // Draw the cursor when this is the last line.
            if (endLine == queFront)
                SetCursor(x, y);
            endLine = begLine;
            BufDec(ref endLine);
        }
    }

    // Helper method for multibyte character handling
    // Pre: sLen <= 256
    // Post: sLen adjusted to not truncate multibyte chars
    // Upstream: textview.cpp lines 102-115
    private static void DiscardPossiblyTruncatedCharsAtEnd(char[] s, ref int sLen)
    {
        // In C# with UTF-16, surrogate pairs are 2 char units
        // Check if we're cutting in the middle of a surrogate pair
        if (sLen == s.Length && sLen > 0)
        {
            // Scan backwards to find a safe break point
            int safeLen = 0;
            for (int i = 0; i < sLen; i++)
            {
                if (char.IsHighSurrogate(s[i]))
                {
                    // If there's room for the low surrogate, include both
                    if (i + 1 < sLen && char.IsLowSurrogate(s[i + 1]))
                    {
                        safeLen = i + 2;
                        i++; // Skip the low surrogate
                    }
                    else
                    {
                        // High surrogate at end, truncate it
                        break;
                    }
                }
                else if (!char.IsLowSurrogate(s[i]))
                {
                    // Regular character
                    safeLen = i + 1;
                }
                // else: orphaned low surrogate, don't advance safeLen
            }
            sLen = safeLen;
        }
    }

    /// <summary>
    /// Finds the position after the next newline character.
    /// Upstream: textview.h line 85 (public method)
    /// </summary>
    public ushort NextLine(ushort pos)
    {
        while (pos != queFront && buffer![pos] != '\n')
            BufInc(ref pos);
        if (pos != queFront)
            BufInc(ref pos);
        return pos;
    }

    #endregion

    #region Line Navigation - From ttprvlns.cpp (lines 18-47)

    /// <summary>
    /// Finds the position of the start of the line 'lines' lines before 'pos'.
    /// Upstream: textview.h line 86 (public method), ttprvlns.cpp lines 31-47
    /// </summary>
    public ushort PrevLines(ushort pos, ushort lines)
    {
        if (lines > 0 && pos != queBack)
        {
            do
            {
                if (pos == queBack)
                    return queBack;
                BufDec(ref pos);
                ushort count = (ushort)((pos >= queBack ? pos - queBack : pos) + 1);
                if (FindLfBackwards(buffer!, ref pos, count))
                    lines--;
            } while (lines > 0);
            BufInc(ref pos);
        }
        return pos;
    }

    // Pre: count >= 1.
    // Post: 'pos' points to the last checked character.
    // Upstream: ttprvlns.cpp lines 18-29 (static function)
    private static bool FindLfBackwards(char[] buffer, ref ushort pos, ushort count)
    {
        ++pos;
        do
        {
            if (buffer[--pos] == '\n')
                return true;
        } while (--count > 0);
        return false;
    }

    #endregion

    public override int DoSputn(ReadOnlySpan<char> s, int count)
    {
        ushort screenLines = (ushort)Limit.Y;
        ushort i;

        if (count > bufSize - 1)
        {
            s = s[(count - (bufSize - 1))..];
            count = bufSize - 1;
        }

        for (i = 0; i < count; i++)
            if (s[i] == '\n')
                screenLines++;

        while (!CanInsert((ushort)count))
        {
            queBack = NextLine(queBack);
            if (screenLines > 1)
                screenLines--;
        }

        if (queFront + count >= bufSize)
        {
            i = (ushort)(bufSize - queFront);
            s[..i].CopyTo(buffer.AsSpan(queFront));
            s[i..count].CopyTo(buffer.AsSpan(0));
            queFront = (ushort)(count - i);
        }
        else
        {
            s[..count].CopyTo(buffer.AsSpan(queFront));
            queFront += (ushort)count;
        }

        // drawLock: avoid redundant calls to drawView()
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
        return queBack == queFront;
    }
}

// =============================================================================
// OTStream class
// Upstream: textview.h lines 111-118, textview.cpp lines 247-250
// =============================================================================

/// <summary>
/// Output stream wrapper for TTerminal.
/// Matches upstream otstream class (textview.h lines 111-118).
/// In C++, this inherits from ostream to enable stream operators.
/// In C#, this wraps TTerminal with TextWriter for similar functionality.
/// </summary>
public class OTStream(TTerminal terminal) : System.IO.TextWriter
{
    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(char value)
    {
        terminal.Overflow(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null && count > 0)
        {
            terminal.DoSputn(buffer.AsSpan(index, count), count);
        }
    }

    public override void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            terminal.DoSputn(value.AsSpan(), value.Length);
        }
    }
}
