using System.Text;
using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// ANSI/VT escape sequence writer for modern consoles.
/// Generates VT sequences for rendering text with attributes.
/// Matches upstream AnsiScreenWriter in ansiwrit.cpp
/// </summary>
internal sealed class AnsiScreenWriter
{
    /// <summary>
    /// Internal buffer for building VT sequences.
    /// </summary>
    private sealed class Buffer
    {
        private byte[] _data = new byte[4096];
        private int _size = 0;

        public void Reserve(int extraCapacity)
        {
            int needed = _size + extraCapacity;
            if (needed > _data.Length)
            {
                int newCapacity = Math.Max(_data.Length * 2, needed);
                Array.Resize(ref _data, newCapacity);
            }
        }

        public void Push(ReadOnlySpan<byte> data)
        {
            Reserve(data.Length);
            data.CopyTo(_data.AsSpan(_size));
            _size += data.Length;
        }

        public void Push(byte b)
        {
            Reserve(1);
            _data[_size++] = b;
        }

        public void Clear() => _size = 0;

        public ReadOnlySpan<byte> Data => _data.AsSpan(0, _size);

        public int Size => _size;

        public byte LastByte => _size > 0 ? _data[_size - 1] : (byte)0;

        public void RemoveLast(int count)
        {
            if (count > 0 && count <= _size)
                _size -= count;
        }
    }

    private readonly ConsoleCtl _con;
    private readonly TermCap _termcap;
    private readonly Buffer _buf = new();
    private TPoint _caretPos = new(-1, -1);
    private TermAttr _lastAttr;

    /// <summary>
    /// Initializes a new instance of the AnsiScreenWriter class.
    /// </summary>
    /// <param name="con">Console control instance</param>
    /// <param name="termcap">Terminal capabilities</param>
    public AnsiScreenWriter(ConsoleCtl con, TermCap termcap)
    {
        _con = con;
        _termcap = termcap;
    }

    /// <summary>
    /// Resets the screen writer state.
    /// Matches upstream AnsiScreenWriter::reset()
    /// </summary>
    public void Reset()
    {
        _buf.Reserve(4);
        _buf.Push("\x1b[0m"u8); // SGR 0: Reset all attributes
        _caretPos = new TPoint(-1, -1);
        _lastAttr = default;
    }

    /// <summary>
    /// Clears the screen.
    /// Matches upstream AnsiScreenWriter::clearScreen()
    /// </summary>
    public void ClearScreen()
    {
        _buf.Reserve(8);
        _buf.Push("\x1b[0m\x1b[2J"u8); // Reset attributes + clear screen
        _lastAttr = default;
    }

    /// <summary>
    /// Writes a single cell to the screen at the specified position.
    /// Matches upstream AnsiScreenWriter::writeCell()
    /// </summary>
    /// <param name="pos">Position (x, y)</param>
    /// <param name="text">Text to write</param>
    /// <param name="attr">Color attributes</param>
    /// <param name="doubleWidth">True if character occupies two columns</param>
    public void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
        _buf.Reserve(256); // Reserve space for cursor movement + attributes + text

        // Move cursor if position changed
        if (pos.Y != _caretPos.Y)
        {
            // Full cursor position (row and column changed)
            BufWriteCSI2(pos.Y + 1, pos.X + 1, 'H'); // CUP - Cursor Position
        }
        else if (pos.X != _caretPos.X)
        {
            // Horizontal-only movement (more efficient)
            BufWriteCSI1(pos.X + 1, 'G'); // CHA - Cursor Horizontal Absolute
        }

        // Convert and write attributes
        ConvertAttributes(attr, ref _lastAttr);

        // Write UTF-8 encoded text
        if (text.Length > 0)
        {
            Span<byte> utf8 = stackalloc byte[text.Length * 3]; // Max UTF-8 expansion
            int bytesWritten = Encoding.UTF8.GetBytes(text, utf8);
            _buf.Push(utf8[..bytesWritten]);
        }

        // Update caret position
        _caretPos = new TPoint(pos.X + (doubleWidth ? 2 : 1), pos.Y);
    }

    /// <summary>
    /// Sets the caret position.
    /// Matches upstream AnsiScreenWriter::setCaretPosition()
    /// </summary>
    /// <param name="pos">Position (x, y)</param>
    public void SetCaretPosition(TPoint pos)
    {
        _buf.Reserve(32);
        BufWriteCSI2(pos.Y + 1, pos.X + 1, 'H'); // CUP - Cursor Position
        _caretPos = pos;
    }

    /// <summary>
    /// Flushes the buffer to the console.
    /// Matches upstream AnsiScreenWriter::flush()
    /// </summary>
    public void Flush()
    {
        _con.Write(_buf.Data);
        _buf.Clear();
    }

    /// <summary>
    /// Writes a CSI sequence with one parameter.
    /// Format: ESC [ a F
    /// </summary>
    private void BufWriteCSI1(int a, char F)
    {
        _buf.Reserve(32);
        _buf.Push("\x1b["u8); // CSI - Control Sequence Introducer
        PushNumber((uint)a);
        _buf.Push((byte)F);
    }

    /// <summary>
    /// Writes a CSI sequence with two parameters.
    /// Format: ESC [ a ; b F
    /// </summary>
    private void BufWriteCSI2(int a, int b, char F)
    {
        _buf.Reserve(32);
        _buf.Push("\x1b["u8); // CSI
        PushNumber((uint)a);
        _buf.Push((byte)';');
        PushNumber((uint)b);
        _buf.Push((byte)F);
    }

    /// <summary>
    /// Pushes a decimal number to the buffer.
    /// </summary>
    private void PushNumber(uint value)
    {
        Span<byte> digits = stackalloc byte[10];
        int pos = digits.Length;

        // Build digits in reverse
        do
        {
            digits[--pos] = (byte)('0' + (value % 10));
            value /= 10;
        } while (value > 0);

        _buf.Push(digits[pos..]);
    }

    // ========================================
    // Color Conversion Functions
    // Matches upstream ansiwrit.cpp:358-448
    // ========================================

    /// <summary>
    /// Result of color conversion with potential extra style bits.
    /// Matches upstream colorconv_r struct in ansiwrit.cpp:175-178
    /// </summary>
    private readonly struct ColorConvResult
    {
        public readonly TermColor Color;
        public readonly ushort ExtraStyle;

        public ColorConvResult(TermColor color, ushort extraStyle = 0)
        {
            Color = color;
            ExtraStyle = extraStyle;
        }
    }

    /// <summary>
    /// Converts color to NoColor mode (monochrome emulation with styles).
    /// Matches upstream convertNoColor() in ansiwrit.cpp:358-376
    /// </summary>
    private static ColorConvResult ConvertNoColor(byte biosColor, bool isFg)
    {
        ushort extraStyle = 0;
        // Mimic the mono palettes with styles
        if (isFg)
        {
            if ((biosColor & 0x8) != 0)
                extraStyle |= 0x01; // slBold
            else if (biosColor == 0x1)
                extraStyle |= 0x02; // slUnderline
        }
        else if ((biosColor & 0x7) == 0x7)
            extraStyle |= 0x10; // slReverse

        return new ColorConvResult(TermColor.NoColor, extraStyle);
    }

    /// <summary>
    /// Converts color to Indexed8 mode (with BoldIsBright/BlinkIsBright quirks).
    /// Matches upstream convertIndexed8() in ansiwrit.cpp:378-398
    /// </summary>
    private ColorConvResult ConvertIndexed8(byte biosColor, bool isFg)
    {
        var cnv = ConvertIndexed16(biosColor, isFg);
        if (cnv.Color.Type == TermColorType.Indexed && cnv.Color.Idx >= 8)
        {
            byte idx = (byte)(cnv.Color.Idx - 8);
            ushort extraStyle = cnv.ExtraStyle;

            if (isFg)
            {
                if ((_termcap.Quirks & TermQuirks.BoldIsBright) != 0)
                    extraStyle |= 0x01; // slBold
            }
            else
            {
                if ((_termcap.Quirks & TermQuirks.BlinkIsBright) != 0)
                    extraStyle |= 0x08; // slBlink
            }

            return new ColorConvResult(new TermColor(idx, TermColorType.Indexed), extraStyle);
        }
        return cnv;
    }

    /// <summary>
    /// Converts color to Indexed16 mode (with downconversion from 256 or RGB).
    /// Matches upstream convertIndexed16() in ansiwrit.cpp:400-421
    /// </summary>
    private static ColorConvResult ConvertIndexed16(byte biosColor, bool isFg)
    {
        // BIOS colors need bit swap
        byte idx = ColorConversion.BIOStoXTerm16(biosColor);
        return new ColorConvResult(new TermColor(idx, TermColorType.Indexed));
    }

    /// <summary>
    /// Converts color to Indexed256 mode (with conversion from RGB).
    /// Matches upstream convertIndexed256() in ansiwrit.cpp:423-437
    /// </summary>
    private static ColorConvResult ConvertIndexed256(byte biosColor, bool isFg)
    {
        // For BIOS colors, convert through 16-color
        return ConvertIndexed16(biosColor, isFg);
    }

    /// <summary>
    /// Converts color to Direct (24-bit RGB) mode.
    /// Matches upstream convertDirect() in ansiwrit.cpp:439-448
    /// </summary>
    private static ColorConvResult ConvertDirect(byte biosColor, bool isFg)
    {
        // Convert BIOS color to XTerm16, then to RGB using palette
        byte xtermColor = ColorConversion.BIOStoXTerm16(biosColor);

        // Standard VGA color palette in XTerm16 order
        ReadOnlySpan<uint> palette = stackalloc uint[16]
        {
            0x000000, 0xAA0000, 0x00AA00, 0xAA5500, // Black, Red, Green, Brown/Yellow
            0x0000AA, 0xAA00AA, 0x00AAAA, 0xAAAAAA, // Blue, Magenta, Cyan, Light Gray
            0x555555, 0xFF5555, 0x55FF55, 0xFFFF55, // Dark Gray, Light Red, Light Green, Yellow
            0x5555FF, 0xFF55FF, 0x55FFFF, 0xFFFFFF  // Light Blue, Light Magenta, Light Cyan, White
        };

        uint rgb = palette[xtermColor & 0x0F];
        byte r = (byte)((rgb >> 16) & 0xFF);
        byte g = (byte)((rgb >> 8) & 0xFF);
        byte b = (byte)(rgb & 0xFF);

        return new ColorConvResult(new TermColor(r, g, b));
    }

    /// <summary>
    /// Converts BIOS color based on terminal capabilities.
    /// Dispatcher for the 5 color conversion functions above.
    /// </summary>
    private ColorConvResult ConvertColor(byte biosColor, bool isFg)
    {
        return _termcap.Colors switch
        {
            TermCapColors.NoColor => ConvertNoColor(biosColor, isFg),
            TermCapColors.Indexed8 => ConvertIndexed8(biosColor, isFg),
            TermCapColors.Indexed16 => ConvertIndexed16(biosColor, isFg),
            TermCapColors.Indexed256 => ConvertIndexed256(biosColor, isFg),
            TermCapColors.Direct => ConvertDirect(biosColor, isFg),
            _ => new ColorConvResult(TermColor.Default)
        };
    }

    // ========================================
    // Attribute Conversion and Writing
    // Matches upstream ansiwrit.cpp:173-298
    // ========================================

    /// <summary>
    /// Converts TColorAttr to terminal attributes and generates SGR sequences.
    /// Matches upstream convertAttributes() in ansiwrit.cpp:173-192
    /// </summary>
    private void ConvertAttributes(TColorAttr attr, ref TermAttr lastAttr)
    {
        var newAttr = new TermAttr();
        ushort style = attr.Style;

        // Convert foreground - matches upstream convertColor(getFore(c), ...)
        byte fgBios = attr.ForegroundColor.ToBIOS(true);
        var fgConv = ConvertColor(fgBios, true);
        newAttr.Fg = fgConv.Color;
        style |= fgConv.ExtraStyle;

        // Convert background - matches upstream convertColor(getBack(c), ...)
        byte bgBios = attr.BackgroundColor.ToBIOS(false);
        var bgConv = ConvertColor(bgBios, false);
        newAttr.Bg = bgConv.Color;
        style |= bgConv.ExtraStyle;

        // Apply quirks (matches upstream lines 182-185)
        if ((_termcap.Quirks & TermQuirks.NoItalic) != 0)
            style &= unchecked((ushort)~0x02);  // slItalic
        if ((_termcap.Quirks & TermQuirks.NoUnderline) != 0)
            style &= unchecked((ushort)~0x04);  // slUnderline

        newAttr.Style = style;

        // Check if attributes changed
        if (AttributesEqual(newAttr, lastAttr))
            return;

        // Generate SGR sequence (matches upstream line 187)
        WriteAttributes(newAttr, lastAttr);
        lastAttr = newAttr;
    }

    /// <summary>
    /// Checks if two terminal attributes are equal.
    /// </summary>
    private static bool AttributesEqual(TermAttr a, TermAttr b)
    {
        return ColorsEqual(a.Fg, b.Fg) &&
               ColorsEqual(a.Bg, b.Bg) &&
               a.Style == b.Style;
    }

    /// <summary>
    /// Checks if two terminal colors are equal.
    /// </summary>
    private static bool ColorsEqual(TermColor a, TermColor b)
    {
        if (a.Type != b.Type)
            return false;

        return a.Type switch
        {
            TermColorType.Indexed => a.Idx == b.Idx,
            TermColorType.RGB => a.R == b.R && a.G == b.G && a.B == b.B,
            _ => true
        };
    }

    /// <summary>
    /// Writes SGR (Select Graphic Rendition) sequences for the given attributes.
    /// Matches upstream writeAttributes() in ansiwrit.cpp:276-298
    /// </summary>
    private void WriteAttributes(TermAttr newAttr, TermAttr lastAttr)
    {
        _buf.Reserve(128);

        // Track position before CSI
        int startPos = _buf.Size;

        // Start SGR sequence
        _buf.Push("\x1b["u8); // CSI

        // Write style flags (matches upstream lines 281-286)
        WriteFlag(newAttr, lastAttr, 0x01, "1", "22");  // Bold / Normal intensity
        WriteFlag(newAttr, lastAttr, 0x02, "3", "23");  // Italic / Not italic
        WriteFlag(newAttr, lastAttr, 0x04, "4", "24");  // Underline / Not underlined
        WriteFlag(newAttr, lastAttr, 0x08, "5", "25");  // Blink / Not blinking
        WriteFlag(newAttr, lastAttr, 0x10, "7", "27");  // Reverse / Not reversed
        WriteFlag(newAttr, lastAttr, 0x20, "9", "29");  // Strike / Not struck

        // Write colors if changed (matches upstream lines 288-291)
        if (!ColorsEqual(newAttr.Fg, lastAttr.Fg))
            WriteColor(newAttr.Fg, true);
        if (!ColorsEqual(newAttr.Bg, lastAttr.Bg))
            WriteColor(newAttr.Bg, false);

        // CRITICAL: Remove empty SGR sequences (matches upstream lines 293-296)
        if (_buf.LastByte == ';')
            _buf.RemoveLast(1);  // Replace trailing ';' with...
        else
            _buf.RemoveLast(2);  // Back out CSI entirely (ESC[)

        // Only write 'm' if we wrote something
        if (_buf.Size > startPos)
            _buf.Push((byte)'m');
    }

    /// <summary>
    /// Writes a style flag with on/off toggle.
    /// Matches upstream writeFlag() in ansiwrit.cpp:258-266
    /// </summary>
    private void WriteFlag(TermAttr attr, TermAttr lastAttr, ushort mask, string onCode, string offCode)
    {
        if ((attr.Style & mask) != (lastAttr.Style & mask))
        {
            string code = (attr.Style & mask) != 0 ? onCode : offCode;
            _buf.Push(Encoding.ASCII.GetBytes(code));
            _buf.Push((byte)';');
        }
    }

    /// <summary>
    /// Splits SGR sequence for compatibility with problematic terminals.
    /// Matches upstream splitSGR() in ansiwrit.cpp:300-307
    /// </summary>
    private void SplitSGR()
    {
        if (_buf.LastByte == ';')
        {
            _buf.RemoveLast(1);      // Remove trailing ';'
            _buf.Push((byte)'m');    // End current SGR
            _buf.Push("\x1b["u8);    // Start new SGR
        }
    }

    /// <summary>
    /// Writes a color to the SGR sequence.
    /// Matches upstream writeColor() in ansiwrit.cpp:309-354
    /// </summary>
    private void WriteColor(TermColor color, bool isForeground)
    {
        // RGB and XTerm256 colors get a separate SGR sequence because some
        // terminal emulators may otherwise have trouble processing them.
        // Matches upstream comment at lines 311-312
        switch (color.Type)
        {
            case TermColorType.Default:
                // 39 for FG, 49 for BG
                PushNumber((uint)(isForeground ? 39 : 49));
                _buf.Push((byte)';');
                break;

            case TermColorType.Indexed:
                if (color.Idx >= 16)
                {
                    // <38,48>;5;i; (matches upstream lines 319-326)
                    SplitSGR();
                    PushNumber((uint)(isForeground ? 38 : 48));
                    _buf.Push((byte)';');
                    PushNumber(5);
                    _buf.Push((byte)';');
                    PushNumber(color.Idx);
                    _buf.Push((byte)';');
                    SplitSGR();
                }
                else if (color.Idx >= 8)
                {
                    // <90-97,100-107>; (matches upstream lines 330-332)
                    PushNumber((uint)(color.Idx - 8 + (isForeground ? 90 : 100)));
                    _buf.Push((byte)';');
                }
                else
                {
                    // <30-37,40-47>; (matches upstream lines 334-335)
                    PushNumber((uint)(color.Idx + (isForeground ? 30 : 40)));
                    _buf.Push((byte)';');
                }
                break;

            case TermColorType.RGB:
                // <38,48>;2;r;g;b; (matches upstream lines 339-348)
                SplitSGR();
                PushNumber((uint)(isForeground ? 38 : 48));
                _buf.Push((byte)';');
                PushNumber(2);
                _buf.Push((byte)';');
                PushNumber(color.R);
                _buf.Push((byte)';');
                PushNumber(color.G);
                _buf.Push((byte)';');
                PushNumber(color.B);
                _buf.Push((byte)';');
                SplitSGR();
                break;

            case TermColorType.NoColor:
                // No output (matches upstream lines 350-351)
                break;
        }
    }
}
