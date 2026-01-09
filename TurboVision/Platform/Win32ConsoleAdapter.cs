using System.Diagnostics;
using System.Runtime.InteropServices;
using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Win32 console adapter - orchestrator for Win32 console system.
/// Factory pattern with static Create() method.
/// Matches upstream Win32ConsoleAdapter in win32con.cpp:40-253 + win32con.h:20-55
/// </summary>
public sealed class Win32ConsoleAdapter : ConsoleAdapter, IScreenDriver, IEventSource
{
    private readonly ConsoleCtl _con;
    private new readonly Win32Input _input;
    private new readonly Win32Display _display;
    private readonly uint _startupMode;
    private readonly uint _cpInput;
    private readonly uint _cpOutput;

    private Win32ConsoleAdapter(
        ConsoleCtl con,
        uint startupMode,
        uint cpInput,
        uint cpOutput,
        Win32Display display,
        Win32Input input)
        : base(display, input)
    {
        _con = con;
        _startupMode = startupMode;
        _cpInput = cpInput;
        _cpOutput = cpOutput;
        _display = display;
        _input = input;
    }

    /// <summary>
    /// Creates and initializes the Win32 console adapter.
    /// Matches upstream Win32ConsoleAdapter::create() in win32con.cpp:40-53
    /// </summary>
    public static Win32ConsoleAdapter Create()
    {
        //Debug.WriteLine("[DEBUG] Win32ConsoleAdapter.Create() starting...");
        var con = ConsoleCtl.GetInstance();
        //Debug.WriteLine($"[DEBUG] ConsoleCtl: In={con.In():X}, Out={con.Out():X}");

        uint startupMode = InitInputMode(con);
        //Debug.WriteLine($"[DEBUG] InitInputMode complete, startupMode=0x{startupMode:X}");

        bool isLegacyConsole = InitOutputMode(con);
        //Debug.WriteLine($"[DEBUG] InitOutputMode complete, isLegacyConsole={isLegacyConsole}");

        if (isLegacyConsole)
        {
            //Debug.WriteLine("[DEBUG] Disabling bitmap font...");
            DisableBitmapFont(con);
        }

        InitEncoding(isLegacyConsole, out uint cpInput, out uint cpOutput);
        //Debug.WriteLine($"[DEBUG] InitEncoding complete, cpInput={cpInput}, cpOutput={cpOutput}");

        // Reset character width detection
        WinWidth.Reset(isLegacyConsole);

        //Debug.WriteLine("[DEBUG] Creating Win32Display...");
        var display = new Win32Display(con, isLegacyConsole);
        //Debug.WriteLine("[DEBUG] Creating Win32Input...");
        var input = new Win32Input(con);

        //Debug.WriteLine("[DEBUG] Win32ConsoleAdapter.Create() complete!");
        return new Win32ConsoleAdapter(con, startupMode, cpInput, cpOutput, display, input);
    }

    /// <summary>
    /// Disposes the adapter and restores original console state.
    /// Matches upstream Win32ConsoleAdapter::~Win32ConsoleAdapter() in win32con.cpp:55-62
    /// </summary>
    public override void Dispose()
    {
        _display.Dispose();
        SetConsoleCP(_cpInput);
        SetConsoleOutputCP(_cpOutput);
        SetConsoleMode(_con.In(), _startupMode);
    }

    /// <summary>
    /// Checks if the console is still alive.
    /// Matches upstream Win32ConsoleAdapter::isAlive() in win32con.cpp:119-122
    /// </summary>
    public override bool IsAlive()
    {
        return GetNumberOfConsoleInputEvents(_con.In(), out _);
    }

    /// <summary>
    /// Sets text to the system clipboard.
    /// Matches upstream Win32ConsoleAdapter::setClipboardText() in win32con.cpp:196-228
    /// </summary>
    public override bool SetClipboardText(string text)
    {
        if (!OpenClipboardWithRetry())
            return false;

        try
        {
            if (!EmptyClipboard())
                return false;

            if (string.IsNullOrEmpty(text))
                return true;

            // Convert UTF-8 to UTF-16
            byte[] utf16Bytes = System.Text.Encoding.Unicode.GetBytes(text + "\0");

            // Allocate global memory
            nint hData = GlobalAlloc(GMEM_MOVEABLE, (nuint)utf16Bytes.Length);
            if (hData == nint.Zero)
                return false;

            // Lock and copy data
            nint pData = GlobalLock(hData);
            if (pData == nint.Zero)
            {
                GlobalFree(hData);
                return false;
            }

            Marshal.Copy(utf16Bytes, 0, pData, utf16Bytes.Length);
            GlobalUnlock(hData);

            // Set clipboard data
            bool success = SetClipboardData(CF_UNICODETEXT, hData) != nint.Zero;
            if (!success)
                GlobalFree(hData); // Free on failure (clipboard owns it on success)

            return success;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// Requests text from the system clipboard.
    /// Matches upstream Win32ConsoleAdapter::requestClipboardText() in win32con.cpp:230-253
    ///
    /// KNOWN DEVIATION #2 & #3 (.NET Idiomatic Patterns):
    /// - Uses Marshal.PtrToStringUni() instead of manual wide-char to UTF-8 conversion
    /// - Uses .NET Encoding.UTF8 throughout instead of Win32 MultiByteToWideChar
    /// These are functionally equivalent and follow C# best practices.
    /// Marshal.PtrToStringUni provides automatic null-termination handling and memory safety.
    /// </summary>
    public override bool RequestClipboardText(Action<string> accept)
    {
        if (!OpenClipboardWithRetry())
            return false;

        try
        {
            nint hData = GetClipboardData(CF_UNICODETEXT);
            if (hData == nint.Zero)
                return false;

            nint pData = GlobalLock(hData);
            if (pData == nint.Zero)
                return false;

            try
            {
                string text = Marshal.PtrToStringUni(pData) ?? "";
                accept(text);
                return true;
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// Initializes input mode (mouse, window events, raw input).
    /// Matches upstream Win32ConsoleAdapter::initInputMode() in win32con.cpp:64-77
    /// </summary>
    private static uint InitInputMode(ConsoleCtl con)
    {
        GetConsoleMode(con.In(), out uint consoleMode);
        uint startupMode = consoleMode;

        consoleMode |= ENABLE_WINDOW_INPUT;          // Report buffer size changes
        consoleMode |= ENABLE_MOUSE_INPUT;           // Report mouse events
        consoleMode &= ~ENABLE_PROCESSED_INPUT;      // Report CTRL+C and SHIFT+Arrow
        consoleMode &= ~(ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT); // Report Ctrl+S
        consoleMode |= ENABLE_EXTENDED_FLAGS;        // Enable extended flags
        consoleMode &= ~ENABLE_QUICK_EDIT_MODE;      // Disable Quick Edit mode (inhibits mouse)

        SetConsoleMode(con.In(), consoleMode);
        return startupMode;
    }

    /// <summary>
    /// Initializes output mode and detects legacy console.
    /// Matches upstream Win32ConsoleAdapter::initOutputMode() in win32con.cpp:79-106
    /// </summary>
    private static bool InitOutputMode(ConsoleCtl con)
    {
        GetConsoleMode(con.Out(), out uint consoleMode);
        consoleMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;   // Avoid scrolling when reaching end of line
        SetConsoleMode(con.Out(), consoleMode);

        bool isLegacyConsole;

        if (IsWine())
        {
            // Wine is not exactly a legacy console, but it does not support VT
            // sequences even though it returns no errors when setting the flag.
            isLegacyConsole = true;
        }
        else
        {
            consoleMode |= DISABLE_NEWLINE_AUTO_RETURN; // Do not do CR on LF
            consoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING; // Allow ANSI escape sequences
            SetConsoleMode(con.Out(), consoleMode);
            GetConsoleMode(con.Out(), out consoleMode);

            // The legacy console does not support VT sequences
            isLegacyConsole = (consoleMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0;
        }

        return isLegacyConsole;
    }

    /// <summary>
    /// Initializes console encoding (UTF-8 for modern, OEM for legacy).
    /// Matches upstream Win32ConsoleAdapter::initEncoding() in win32con.cpp:105-125
    /// </summary>
    private static void InitEncoding(bool isLegacyConsole, out uint cpInput, out uint cpOutput)
    {
        cpInput = GetConsoleCP();
        cpOutput = GetConsoleOutputCP();

        // We would like to set all console codepages to UTF-8, but the legacy
        // console with bitmap font is unable to display UTF-8 text properly.
        // However, when using the OEM codepage (which is usually the one supported
        // by the default bitmap font) in the legacy console, unsupported characters
        // may be replaced automatically with a similar one (e.g. '╪' gets replaced
        // with '╬' when the OEM codepage is 850, which doesn't support '╪').
        // If the legacy console is used with a non-bitmap font (such as Lucida
        // Console), then the output codepage does not make a difference.
        SetConsoleCP(CP_UTF8);

        if (isLegacyConsole)
            SetConsoleOutputCP(GetOEMCP()); // Use OEM code page for bitmap fonts
        else
            SetConsoleOutputCP(CP_UTF8); // Use UTF-8 for modern consoles

        // This only affects the C runtime functions. It has to be invoked every
        // time SetConsoleCP() gets called.
        setlocale(LC_ALL, ".utf8");
    }

    /// <summary>
    /// Checks if current font is a bitmap font (monospace bitmap has all flags clear).
    /// Matches upstream Win32ConsoleAdapter::isBitmapFont() in win32con.cpp:147-150
    /// </summary>
    private static bool IsBitmapFont(uint fontFamily)
    {
        return (fontFamily & (TMPF_FIXED_PITCH | TMPF_VECTOR | TMPF_TRUETYPE | TMPF_DEVICE)) == 0;
    }

    /// <summary>
    /// Disables bitmap font by switching to TrueType font (Consolas or Lucida Console).
    /// Matches upstream Win32ConsoleAdapter::disableBitmapFont() in win32con.cpp:158-179
    /// </summary>
    private static void DisableBitmapFont(ConsoleCtl con)
    {
        // These functions only exist since Vista
        var pGetCurrentConsoleFontEx = GetModuleProc<GetCurrentConsoleFontExDelegate>(
            "KERNEL32", "GetCurrentConsoleFontEx");
        var pSetCurrentConsoleFontEx = GetModuleProc<SetCurrentConsoleFontExDelegate>(
            "KERNEL32", "SetCurrentConsoleFontEx");

        if (pGetCurrentConsoleFontEx == null || pSetCurrentConsoleFontEx == null)
            return;

        var fontInfo = new CONSOLE_FONT_INFOEX();
        if (!pGetCurrentConsoleFontEx(con.Out(), false, ref fontInfo) ||
            !IsBitmapFont(fontInfo.FontFamily))
            return;

        // Try to set a TrueType font
        short fontY = (short)(2 * Math.Min(fontInfo.dwFontSize.X, fontInfo.dwFontSize.Y));

        foreach (string name in new[] { "Consolas", "Lucida Console" })
        {
            fontInfo.nFont = 0;
            fontInfo.FontFamily = FF_DONTCARE;
            fontInfo.FontWeight = FW_NORMAL;
            fontInfo.dwFontSize = new COORD(0, fontY);

            // Copy font name
            for (int i = 0; i < name.Length && i < 32; i++)
                fontInfo.FaceName[i] = name[i];
            if (name.Length < 32)
                fontInfo.FaceName[name.Length] = '\0';

            pSetCurrentConsoleFontEx(con.Out(), false, ref fontInfo);
            pGetCurrentConsoleFontEx(con.Out(), false, ref fontInfo);

            // Check if font was actually set
            string setName = new string(fontInfo.FaceName, 0, Array.IndexOf(fontInfo.FaceName, '\0'));
            if (setName == name)
                return; // Success
        }
    }

    /// <summary>
    /// Detects Wine environment by checking for wine_get_version in NTDLL.
    /// Matches upstream isWine() in win32con.cpp:35-38
    /// </summary>
    private static bool IsWine()
    {
        return GetModuleProc<WineGetVersionDelegate>("NTDLL", "wine_get_version") != null;
    }

    /// <summary>
    /// Opens clipboard with retry logic (5 attempts, 5ms delay).
    /// Matches upstream openClipboard() in win32con.cpp:181-194
    /// </summary>
    private static bool OpenClipboardWithRetry()
    {
        for (int i = 0; i < 5; i++)
        {
            if (OpenClipboard(nint.Zero))
                return true;
            Thread.Sleep(5);
        }
        return false;
    }

    // ========================================
    // IScreenDriver Implementation
    // ========================================

    public int Cols => _display.ReloadScreenInfo().X;
    public int Rows => _display.ReloadScreenInfo().Y;
    public bool MousePresent => true;

    public void ClearScreen(char c, TColorAttr attr)
    {
        _display.ClearScreen();
    }

    public void WriteBuffer(int x, int y, int width, int height, ReadOnlySpan<TScreenCell> buffer)
    {
        // Convert buffer to individual WriteCell calls
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int index = row * width + col;
                if (index < buffer.Length)
                {
                    var cell = buffer[index];
                    char ch = cell.Char;
                    _display.WriteCell(
                        new TPoint(x + col, y + row),
                        new ReadOnlySpan<char>(ref ch),
                        cell.Attr,
                        false);
                }
            }
        }
    }

    public void Flush()
    {
        //Debug.WriteLine("[DEBUG] Win32ConsoleAdapter.Flush() called");
        _display.Flush();
    }

    public void SetCursorPosition(int x, int y)
    {
        _display.SetCaretPosition(new TPoint(x, y));
    }

    public void SetCursorType(ushort cursorType)
    {
        _display.SetCaretSize(cursorType);
    }

    public ushort GetCursorType()
    {
        if (GetConsoleCursorInfo(_con.Out(), out var info))
            return info.bVisible ? (ushort)info.dwSize : (ushort)0;
        return 0;
    }

    public void Suspend()
    {
        // Restore original console mode for suspension
        SetConsoleMode(_con.In(), _startupMode);
    }

    public void Resume()
    {
        // Reinitialize console mode after resumption
        InitInputMode(_con);
        _display.ReloadScreenInfo();
    }

    // ========================================
    // IEventSource Implementation
    // ========================================

    public bool GetEvent(out TEvent ev)
    {
        return _input.GetEvent(out ev);
    }

    public void WaitForEvents(int timeoutMs)
    {
        WaitForSingleObject(_con.In(), timeoutMs < 0 ? INFINITE : (uint)timeoutMs);
    }

    public void WakeUp()
    {
        // Post a null input record to wake up WaitForSingleObject
        var ir = new INPUT_RECORD { EventType = 0 };
        WriteConsoleInputW(_con.In(), ref ir, 1, out _);
    }
}
