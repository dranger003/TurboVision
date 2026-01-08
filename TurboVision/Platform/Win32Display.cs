using System.Diagnostics;
using System.Text;
using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Win32 display adapter with dual rendering path (modern VT sequences + legacy Win32 API).
/// Matches upstream Win32Display in win32con.cpp:255-475
/// </summary>
internal sealed class Win32Display : DisplayAdapter, IDisposable
{
    private readonly ConsoleCtl _con;
    private TPoint _size;
    private CONSOLE_FONT_INFO _lastFontInfo;

    // Modern console path (Windows 10+ with VT support)
    private AnsiScreenWriter? _ansiScreenWriter;

    // Legacy console path (Windows 7/8, Wine, or no VT support)
    private TPoint _caretPos = new(-1, -1);
    private ushort _lastAttr = 0x07; // Default: light gray on black
    private readonly List<byte> _buf = new(); // UTF-8 buffer for legacy console

    // Debug counters
    private int _writeCellCount = 0;
    private int _flushCount = 0;

    /// <summary>
    /// Initializes a new instance of the Win32Display class.
    /// Matches upstream Win32Display constructor in win32con.cpp
    /// </summary>
    /// <param name="con">Console control instance</param>
    /// <param name="isLegacyConsole">True for legacy console (Win32 API), false for modern (VT sequences)</param>
    public Win32Display(ConsoleCtl con, bool isLegacyConsole)
    {
        _con = con;
        //Debug.WriteLine($"[DEBUG] Win32Display constructor: isLegacyConsole={isLegacyConsole}");

        if (!isLegacyConsole)
        {
            // Modern console: use VT sequences via AnsiScreenWriter
            //Debug.WriteLine("[DEBUG] Creating AnsiScreenWriter for modern console...");
            var termcap = TermCap.GetDisplayCapabilities(con, this);
            //Debug.WriteLine($"[DEBUG] TermCap: Colors={termcap.Colors}, Quirks={termcap.Quirks}");
            _ansiScreenWriter = new AnsiScreenWriter(con, termcap);
            //Debug.WriteLine("[DEBUG] AnsiScreenWriter created successfully");
        }
        else
        {
            //Debug.WriteLine("[DEBUG] Using legacy Win32 API rendering path");
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _ansiScreenWriter?.Flush();
    }

    /// <summary>
    /// Reloads screen information and returns the current size.
    /// Matches upstream Win32Display::reloadScreenInfo() in win32con.cpp:285-327
    /// </summary>
    public override TPoint ReloadScreenInfo()
    {
        TPoint lastSize = _size;
        _size = _con.GetSize();

        // Check if size changed
        if (lastSize.X != _size.X || lastSize.Y != _size.Y)
        {
            // Work around Windows Terminal crash when resizing
            // https://github.com/microsoft/terminal/issues/7511
            // Temporarily move cursor to (0,0) to prevent crash
            if (GetConsoleScreenBufferInfo(_con.Out(), out var sbInfo))
            {
                var savedPos = sbInfo.dwCursorPosition;

                // Move to (0,0)
                SetConsoleCursorPosition(_con.Out(), new COORD(0, 0));

                // Resize buffer to match viewport
                SetConsoleScreenBufferSize(_con.Out(), new COORD((short)_size.X, (short)_size.Y));

                // Restore cursor position (clamped to new size)
                savedPos.X = Math.Min(savedPos.X, (short)(_size.X - 1));
                savedPos.Y = Math.Min(savedPos.Y, (short)(_size.Y - 1));
                SetConsoleCursorPosition(_con.Out(), savedPos);
            }
        }

        // Check for font changes
        if (GetCurrentConsoleFont(_con.Out(), false, out var fontInfo))
        {
            bool fontChanged = _lastFontInfo.nFont != fontInfo.nFont ||
                              _lastFontInfo.dwFontSize.X != fontInfo.dwFontSize.X ||
                              _lastFontInfo.dwFontSize.Y != fontInfo.dwFontSize.Y;

            if (fontChanged)
            {
                // Font changed - character width calculations need to be reset
                _lastFontInfo = fontInfo;
                // WinWidth would be reset here if font-dependent width detection was needed
            }
        }

        // Reset rendering state
        if (_ansiScreenWriter != null)
        {
            _ansiScreenWriter.Reset();
        }
        else
        {
            _caretPos = new TPoint(-1, -1);
            _lastAttr = 0x07;
        }

        return _size;
    }

    /// <summary>
    /// Gets the number of colors supported.
    /// Matches upstream Win32Display::getColorCount() in win32con.cpp:329-332
    /// </summary>
    public override int GetColorCount()
    {
        // Modern console supports 24-bit true color (16M colors)
        // Legacy console supports 16 colors
        return _ansiScreenWriter != null ? 256 * 256 * 256 : 16;
    }

    /// <summary>
    /// Gets the font size (character cell dimensions).
    /// Matches upstream Win32Display::getFontSize() in win32con.cpp:334-337
    /// </summary>
    public override TPoint GetFontSize()
    {
        return _con.GetFontSize();
    }

    /// <summary>
    /// Writes a single cell to the display.
    /// Matches upstream Win32Display::writeCell() in win32con.cpp:339-401
    /// </summary>
    public override void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
        _writeCellCount++;
        if (_writeCellCount <= 5)
        {
            string textStr = text.Length > 0 ? new string(text) : "<empty>";
            //Debug.WriteLine($"[DEBUG] WriteCell #{_writeCellCount}: pos=({pos.X},{pos.Y}), text='{textStr}', attr=0x{attr.ToBIOS():X4}");
        }
        else if (_writeCellCount == 6)
        {
            //Debug.WriteLine("[DEBUG] WriteCell: (suppressing further logs...)");
        }

        if (_ansiScreenWriter != null)
        {
            // Modern path: use VT sequences
            _ansiScreenWriter.WriteCell(pos, text, attr, doubleWidth);
        }
        else
        {
            // Legacy path: use Win32 API with buffering
            LegacyWriteCell(pos, text, attr, doubleWidth);
        }
    }

    /// <summary>
    /// Legacy console write cell implementation with buffering.
    /// </summary>
    private void LegacyWriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
        // Check if position changed
        if (pos.X != _caretPos.X || pos.Y != _caretPos.Y)
        {
            Flush(); // Flush buffer before moving cursor
            SetConsoleCursorPosition(_con.Out(), new COORD((short)pos.X, (short)pos.Y));
            _caretPos = pos;
        }

        // Check if attribute changed
        ushort biosAttr = attr.ToBIOS();
        if (biosAttr != _lastAttr)
        {
            Flush(); // Flush buffer before changing attribute
            SetConsoleTextAttribute(_con.Out(), biosAttr);
            _lastAttr = biosAttr;
        }

        // Buffer UTF-8 bytes for later write
        if (text.Length > 0)
        {
            Span<byte> utf8 = stackalloc byte[text.Length * 3];
            int bytesWritten = Encoding.UTF8.GetBytes(text, utf8);

            foreach (byte b in utf8[..bytesWritten])
            {
                _buf.Add(b);
            }
        }

        // Update caret position
        _caretPos = new TPoint(pos.X + (doubleWidth ? 2 : 1), pos.Y);
    }

    /// <summary>
    /// Sets the caret (cursor) position.
    /// Matches upstream Win32Display::setCaretPosition() in win32con.cpp:403-410
    /// </summary>
    public override void SetCaretPosition(TPoint pos)
    {
        if (_ansiScreenWriter != null)
        {
            _ansiScreenWriter.SetCaretPosition(pos);
        }
        else
        {
            Flush();
            SetConsoleCursorPosition(_con.Out(), new COORD((short)pos.X, (short)pos.Y));
            _caretPos = pos;
        }
    }

    /// <summary>
    /// Sets the caret (cursor) size.
    /// Matches upstream Win32Display::setCaretSize() in win32con.cpp:412-419
    /// </summary>
    public override void SetCaretSize(int size)
    {
        var info = new CONSOLE_CURSOR_INFO
        {
            dwSize = size > 0 ? (uint)Math.Clamp(size, 1, 100) : 1,
            bVisible = size > 0
        };

        SetConsoleCursorInfo(_con.Out(), ref info);
    }

    /// <summary>
    /// Clears the entire screen.
    /// Matches upstream Win32Display::clearScreen() in win32con.cpp:421-441
    /// </summary>
    public override void ClearScreen()
    {
        if (_ansiScreenWriter != null)
        {
            // Modern path: use VT sequence
            _ansiScreenWriter.ClearScreen();
        }
        else
        {
            // Legacy path: use Win32 API
            var coord = new COORD(0, 0);
            uint length = (uint)(_size.X * _size.Y);
            ushort attr = 0x07; // Light gray on black

            // Fill with spaces
            FillConsoleOutputCharacterW(_con.Out(), ' ', length, coord, out _);

            // Fill with default attributes
            FillConsoleOutputAttribute(_con.Out(), attr, length, coord, out _);

            _lastAttr = attr;
        }
    }

    /// <summary>
    /// Flushes buffered output to the console.
    /// Matches upstream Win32Display::flush() in win32con.cpp:443-475
    /// </summary>
    public override void Flush()
    {
        _flushCount++;
        if (_flushCount <= 5)
        {
            //Debug.WriteLine($"[DEBUG] Flush #{_flushCount}: ansiWriter={_ansiScreenWriter != null}, bufSize={_buf.Count}");
        }
        else if (_flushCount == 6)
        {
            //Debug.WriteLine("[DEBUG] Flush: (suppressing further logs...)");
        }

        if (_ansiScreenWriter != null)
        {
            // Modern path: flush VT sequences
            _ansiScreenWriter.Flush();
        }
        else if (_buf.Count > 0)
        {
            // Legacy path: write buffered UTF-8 text via WriteConsoleOutputW
            // Convert UTF-8 buffer to UTF-16
            string text = Encoding.UTF8.GetString(_buf.ToArray());
            int wcharCount = text.Length;

            if (wcharCount > 0)
            {
                // Create CHAR_INFO array
                var cells = new CHAR_INFO[wcharCount];
                for (int i = 0; i < wcharCount; i++)
                {
                    cells[i].UnicodeChar = text[i];
                    cells[i].Attributes = _lastAttr;
                }

                // Calculate write region
                int startX = _caretPos.X - wcharCount;
                if (startX < 0) startX = 0;

                var region = new SMALL_RECT
                {
                    Left = (short)startX,
                    Top = (short)_caretPos.Y,
                    Right = (short)(_caretPos.X - 1),
                    Bottom = (short)_caretPos.Y
                };

                // Write to console
                WriteConsoleOutputW(
                    _con.Out(),
                    cells,
                    new COORD((short)wcharCount, 1),
                    new COORD(0, 0),
                    ref region);
            }

            _buf.Clear();
        }
    }
}
