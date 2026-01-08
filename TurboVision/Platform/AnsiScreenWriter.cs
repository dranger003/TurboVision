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

    /// <summary>
    /// Converts TColorAttr to terminal attributes and generates SGR sequences.
    /// Matches upstream convertAttributes() in ansiwrit.cpp:173-192
    /// </summary>
    private void ConvertAttributes(TColorAttr attr, ref TermAttr lastAttr)
    {
        var newAttr = new TermAttr();
        ushort style = attr.Style;

        // Convert foreground - matches upstream convertColor(getFore(c), ...)
        newAttr.Fg = ConvertColorDesired(attr.ForegroundColor, ref style, true);

        // Convert background - matches upstream convertColor(getBack(c), ...)
        newAttr.Bg = ConvertColorDesired(attr.BackgroundColor, ref style, false);

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
    /// Converts TColorDesired to TermColor.
    /// Matches upstream convertColor() + convertIndexed16() in ansiwrit.cpp
    /// </summary>
    private TermColor ConvertColorDesired(TColorDesired color, ref ushort style, bool isForeground)
    {
        // Convert to BIOS color (quantization for RGB/XTerm)
        byte biosColor = color.ToBIOS(isForeground);  // Implicit conversion
        return ConvertBiosColor(biosColor, ref style, isForeground);
    }

    /// <summary>
    /// Converts BIOS color (0-15) to XTerm16 by swapping Red and Blue bits.
    /// Matches upstream BIOStoXTerm16() in colors.h
    ///
    /// BIOS colors: bit0=Blue, bit1=Green, bit2=Red, bit3=Bright
    /// XTerm colors: bit0=Red, bit1=Green, bit2=Blue, bit3=Bright
    /// </summary>
    private static byte BiosToXTerm16(byte bios)
    {
        byte b = (byte)(bios & 0x1);  // Blue bit
        byte g = (byte)(bios & 0x2);  // Green bit (unchanged)
        byte r = (byte)(bios & 0x4);  // Red bit
        byte bright = (byte)(bios & 0x8);  // Bright bit (unchanged)

        // Swap Red and Blue: XTerm = (b→r) | g | (r→b) | bright
        return (byte)((b << 2) | g | (r >> 2) | bright);
    }

    /// <summary>
    /// Converts BIOS color (0-15) to TermColor based on terminal capabilities.
    /// Matches upstream color conversion functions in ansiwrit.cpp
    /// </summary>
    private TermColor ConvertBiosColor(byte biosColor, ref ushort style, bool isForeground)
    {
        // Map based on terminal color capability
        switch (_termcap.Colors)
        {
            case TermCapColors.Direct:
                // 24-bit true color: convert BIOS color to RGB
                // Note: RGB conversion also needs the bit swap
                byte xterm = BiosToXTerm16(biosColor);
                return ConvertXTermToRgb(xterm);

            case TermCapColors.Indexed256:
            case TermCapColors.Indexed16:
                // Convert BIOS to XTerm16 (swap Red/Blue bits)
                // Matches upstream convertIndexed16() calling BIOStoXTerm16()
                byte xtermColor = BiosToXTerm16(biosColor);
                return new TermColor(xtermColor, TermColorType.Indexed);

            case TermCapColors.Indexed8:
                // 8-color mode with BoldIsBright quirk
                byte xtermColor8 = BiosToXTerm16(biosColor);
                if ((_termcap.Quirks & TermQuirks.BoldIsBright) != 0 && xtermColor8 >= 8 && isForeground)
                {
                    // Use bold SGR for bright colors (8-15)
                    style |= 0x01; // slBold
                    return new TermColor((byte)(xtermColor8 - 8), TermColorType.Indexed);
                }
                // Regular 8-color (strip bright bit if present)
                return new TermColor((byte)(xtermColor8 & 0x07), TermColorType.Indexed);

            default:
                return TermColor.Default;
        }
    }

    /// <summary>
    /// Converts an XTerm16 color (0-15) to RGB.
    /// Uses standard VGA color palette in XTerm color order.
    /// XTerm colors: bit0=Red, bit1=Green, bit2=Blue (not BIOS order!)
    /// </summary>
    private static TermColor ConvertXTermToRgb(byte xtermColor)
    {
        // Standard VGA color palette in XTerm16 order
        // XTerm: 0=Black, 1=Red, 2=Green, 3=Yellow, 4=Blue, 5=Magenta, 6=Cyan, 7=White
        // (and bright variants 8-15)
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

        return new TermColor(r, g, b);
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
    /// </summary>
    private void WriteAttributes(TermAttr newAttr, TermAttr lastAttr)
    {
        _buf.Reserve(128);

        // Start SGR sequence
        _buf.Push("\x1b["u8);
        bool needSeparator = false;

        // Reset if colors changed significantly
        bool needReset = !ColorsEqual(newAttr.Fg, lastAttr.Fg) ||
                         !ColorsEqual(newAttr.Bg, lastAttr.Bg);

        if (needReset)
        {
            _buf.Push((byte)'0'); // SGR 0: Reset
            needSeparator = true;
        }

        // Foreground color
        if (!ColorsEqual(newAttr.Fg, lastAttr.Fg) || needReset)
        {
            if (needSeparator) _buf.Push((byte)';');
            WriteColor(newAttr.Fg, true);
            needSeparator = true;
        }

        // Background color
        if (!ColorsEqual(newAttr.Bg, lastAttr.Bg) || needReset)
        {
            if (needSeparator) _buf.Push((byte)';');
            WriteColor(newAttr.Bg, false);
            needSeparator = true;
        }

        // Style attributes
        if (newAttr.Style != lastAttr.Style || needReset)
        {
            // Bold (SGR 1)
            if ((newAttr.Style & 0x01) != 0)
            {
                if (needSeparator) _buf.Push((byte)';');
                _buf.Push((byte)'1');
                needSeparator = true;
            }

            // Underline (SGR 4)
            if ((newAttr.Style & 0x02) != 0)
            {
                if (needSeparator) _buf.Push((byte)';');
                _buf.Push((byte)'4');
                needSeparator = true;
            }
        }

        // End SGR sequence
        _buf.Push((byte)'m');
    }

    /// <summary>
    /// Writes a color to the SGR sequence.
    /// </summary>
    private void WriteColor(TermColor color, bool isForeground)
    {
        int baseCode = isForeground ? 30 : 40; // 30-37 for FG, 40-47 for BG

        switch (color.Type)
        {
            case TermColorType.Indexed:
                if (color.Idx < 8)
                {
                    // Standard colors (30-37 or 40-47)
                    PushNumber((uint)(baseCode + color.Idx));
                }
                else if (color.Idx < 16)
                {
                    // Bright colors (90-97 or 100-107)
                    PushNumber((uint)(baseCode + 60 + (color.Idx - 8)));
                }
                else
                {
                    // Extended colors (256-color mode: 38;5;N or 48;5;N)
                    PushNumber((uint)(isForeground ? 38 : 48));
                    _buf.Push((byte)';');
                    PushNumber(5);
                    _buf.Push((byte)';');
                    PushNumber(color.Idx);
                }
                break;

            case TermColorType.RGB:
                // True color (38;2;R;G;B or 48;2;R;G;B)
                PushNumber((uint)(isForeground ? 38 : 48));
                _buf.Push((byte)';');
                PushNumber(2);
                _buf.Push((byte)';');
                PushNumber(color.R);
                _buf.Push((byte)';');
                PushNumber(color.G);
                _buf.Push((byte)';');
                PushNumber(color.B);
                break;

            case TermColorType.Default:
                // Default color (39 or 49)
                PushNumber((uint)(isForeground ? 39 : 49));
                break;
        }
    }
}
