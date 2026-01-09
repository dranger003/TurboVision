using System.Text;
using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Console handle manager - manages three handles:
/// - Input (STD_INPUT_HANDLE)
/// - StartupOutput (STD_OUTPUT_HANDLE)
/// - ActiveOutput (alternate screen buffer)
/// Matches upstream ConsoleCtl in conctl.h and conctl.cpp:141-352
/// </summary>
internal sealed class ConsoleCtl
{
    private const int Input = 0;
    private const int StartupOutput = 1;
    private const int ActiveOutput = 2;

    private readonly nint[] _handles = new nint[3];
    private readonly bool[] _owning = new bool[3];
    private bool _ownsConsole = false;

    private static ConsoleCtl? _instance;

    /// <summary>
    /// Initializes console handles with sophisticated 3-tier fallback.
    /// Matches upstream ConsoleCtl::ConsoleCtl() in conctl.cpp:168-274
    /// </summary>
    private ConsoleCtl()
    {
        // The console can be accessed in two ways: through GetStdHandle() or through
        // CreateFile(). GetStdHandle() will be unable to return a console handle
        // if standard handles have been redirected.
        //
        // Additionally, we want to spawn a new console when none is visible to the user.
        // This might happen under two circumstances:
        //
        // 1. The console crashed. This is easy to detect because all console operations
        //    fail on the console handles.
        // 2. The console exists somehow but cannot be made visible, not even by doing
        //    GetConsoleWindow() and then ShowWindow(SW_SHOW). This is what happens
        //    under Git Bash without pseudoconsole support. In this case, none of the
        //    standard handles is a console, yet the handles returned by CreateFile()
        //    still work.
        //
        // So, in order to find out if a console needs to be allocated, we
        // check whether at least one of the standard handles is a console. If none
        // of them is, we allocate a new console.

        // Step 1: Try GetStdHandle for each standard handle
        var channels = new[]
        {
            (std: STD_INPUT_HANDLE, index: Input),
            (std: STD_OUTPUT_HANDLE, index: StartupOutput),
            (std: STD_ERROR_HANDLE, index: StartupOutput), // Error also goes to startup output
        };

        bool haveConsole = false;
        foreach (var (std, index) in channels)
        {
            nint h = GetStdHandle(std);
            if (IsConsole(h))
            {
                haveConsole = true;
                if (!IsValid(_handles[index]))
                {
                    _handles[index] = h;
                    _owning[index] = false; // Don't close standard handles
                }
            }
        }

        // Step 2: If no console detected, allocate one
        if (!haveConsole)
        {
            FreeConsole(); // Free any existing console (may fail silently)
            AllocConsole();
            _ownsConsole = true;
        }

        // Step 3: Fallback to CreateFile("CONIN$"/"CONOUT$") if standard handles aren't available
        if (!IsValid(_handles[Input]))
        {
            _handles[Input] = CreateFileW(
                "CONIN$",
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ,
                nint.Zero,
                OPEN_EXISTING,
                0,
                nint.Zero);
            _owning[Input] = true;
        }

        if (!IsValid(_handles[StartupOutput]))
        {
            _handles[StartupOutput] = CreateFileW(
                "CONOUT$",
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_WRITE,
                nint.Zero,
                OPEN_EXISTING,
                0,
                nint.Zero);
            _owning[StartupOutput] = true;
        }

        // Step 4: Create alternate screen buffer
        _handles[ActiveOutput] = CreateConsoleScreenBuffer(
            GENERIC_READ | GENERIC_WRITE,
            0,
            nint.Zero,
            CONSOLE_TEXTMODE_BUFFER,
            nint.Zero);
        _owning[ActiveOutput] = true;

        // Step 5: Initialize buffer size to match window size (Wine compatibility)
        // Matches upstream conctl.cpp:258-266
        if (GetConsoleScreenBufferInfo(_handles[StartupOutput], out var sbInfo))
        {
            // Force the screen buffer size to match the window size.
            // The Console API guarantees this, but some implementations
            // are not compliant (e.g. Wine).
            var windowSize = WindowSize(sbInfo.srWindow);
            SetConsoleScreenBufferSize(_handles[ActiveOutput], windowSize);
        }

        // Step 6: Activate the alternate screen buffer
        SetConsoleActiveScreenBuffer(_handles[ActiveOutput]);

        // Step 7: Verify all handles are valid
        for (int i = 0; i < _handles.Length; i++)
        {
            if (!IsValid(_handles[i]))
            {
                Console.Error.WriteLine("Error: cannot get a console.");
                Environment.Exit(1);
            }
        }
    }

    /// <summary>
    /// Cleans up console handles and restores original state.
    /// Matches upstream ConsoleCtl::~ConsoleCtl() in conctl.cpp:276-317
    /// </summary>
    ~ConsoleCtl()
    {
        // Preserve window size when restoring startup buffer
        if (!GetConsoleScreenBufferInfo(_handles[ActiveOutput], out var activeSbInfo) ||
            !GetConsoleScreenBufferInfo(_handles[StartupOutput], out var startupSbInfo))
        {
            // Can't query info, just restore and cleanup
            RestoreAndCleanup();
            return;
        }

        var activeWindowSize = WindowSize(activeSbInfo.srWindow);
        var startupWindowSize = WindowSize(startupSbInfo.srWindow);

        // Preserve the current window size
        if (activeWindowSize.X != startupWindowSize.X ||
            activeWindowSize.Y != startupWindowSize.Y)
        {
            // The buffer is not allowed to be smaller than the window, so enlarge
            // it if necessary. But do not shrink it in the opposite case, to avoid
            // loss of data.
            var dwSize = startupSbInfo.dwSize;
            if (dwSize.X < activeWindowSize.X)
                dwSize.X = activeWindowSize.X;
            if (dwSize.Y < activeWindowSize.Y)
                dwSize.Y = activeWindowSize.Y;
            SetConsoleScreenBufferSize(_handles[StartupOutput], dwSize);

            // Get the updated cursor position, in case it changed after the resize.
            GetConsoleScreenBufferInfo(_handles[StartupOutput], out startupSbInfo);

            // Make sure the cursor is visible. If possible, show it in the bottom row.
            var srWindow = startupSbInfo.srWindow;
            var dwCursorPosition = startupSbInfo.dwCursorPosition;
            srWindow.Right = (short)Math.Max(dwCursorPosition.X, activeWindowSize.X - 1);
            srWindow.Left = (short)(srWindow.Right - (activeWindowSize.X - 1));
            srWindow.Bottom = (short)Math.Max(dwCursorPosition.Y, activeWindowSize.Y - 1);
            srWindow.Top = (short)(srWindow.Bottom - (activeWindowSize.Y - 1));
            SetConsoleWindowInfo(_handles[StartupOutput], true, ref srWindow);
        }

        RestoreAndCleanup();
    }

    /// <summary>
    /// Restores the startup screen buffer and closes owned handles.
    /// </summary>
    private void RestoreAndCleanup()
    {
        SetConsoleActiveScreenBuffer(_handles[StartupOutput]);

        for (int i = 0; i < _handles.Length; i++)
        {
            if (_owning[i] && IsValid(_handles[i]))
            {
                CloseHandle(_handles[i]);
            }
        }

        if (_ownsConsole)
        {
            FreeConsole();
        }
    }

    /// <summary>
    /// Checks if a handle is valid.
    /// Matches upstream isValid() in conctl.cpp:149-152
    /// </summary>
    private static bool IsValid(nint h)
    {
        return h != nint.Zero && h != INVALID_HANDLE_VALUE;
    }

    /// <summary>
    /// Checks if a handle is a console handle.
    /// Matches upstream isConsole() in conctl.cpp:154-158
    /// </summary>
    private static bool IsConsole(nint h)
    {
        return GetConsoleMode(h, out _);
    }

    /// <summary>
    /// Calculates window size from a screen rectangle.
    /// Matches upstream windowSize() in conctl.cpp:160-166
    /// </summary>
    private static COORD WindowSize(SMALL_RECT srWindow)
    {
        return new COORD(
            (short)(srWindow.Right - srWindow.Left + 1),
            (short)(srWindow.Bottom - srWindow.Top + 1));
    }

    /// <summary>
    /// Gets the singleton instance of ConsoleCtl.
    /// </summary>
    public static ConsoleCtl GetInstance()
    {
        return _instance ??= new ConsoleCtl();
    }

    /// <summary>
    /// Destroys the singleton instance.
    /// </summary>
    public static void DestroyInstance()
    {
        _instance = null;
    }

    /// <summary>
    /// Gets the input handle.
    /// </summary>
    public nint In() => _handles[Input];

    /// <summary>
    /// Gets the active output handle (alternate screen buffer).
    /// </summary>
    public nint Out() => _handles[ActiveOutput];

    /// <summary>
    /// Writes UTF-8 bytes directly to the console.
    /// Used by AnsiScreenWriter for VT sequence output.
    /// Uses WriteFile for raw byte output (VT escape sequences).
    /// Matches upstream ConsoleCtl::write() in conctl.cpp:319-325
    /// </summary>
    /// <param name="data">UTF-8 encoded bytes to write</param>
    public unsafe void Write(ReadOnlySpan<byte> data)
    {
        // CRITICAL: Writing 0 bytes causes cursor to become invisible
        // in old Windows console versions (upstream comment)
        if (data.Length == 0)
            return;

        fixed (byte* ptr = data)
        {
            // Use WriteFile instead of WriteConsoleW for VT sequences (UTF-8 bytes)
            WriteFile(Out(), ptr, (uint)data.Length, out _, nint.Zero);
        }
    }

    /// <summary>
    /// Gets the current console window size.
    /// Matches upstream ConsoleCtl::getSize() in conctl.cpp
    /// </summary>
    /// <returns>Console size (columns, rows)</returns>
    public TPoint GetSize()
    {
        if (GetConsoleScreenBufferInfo(Out(), out var info))
        {
            // Calculate size from window rectangle
            int cols = info.srWindow.Right - info.srWindow.Left + 1;
            int rows = info.srWindow.Bottom - info.srWindow.Top + 1;
            return new TPoint(cols, rows);
        }

        // Default fallback
        return new TPoint(80, 25);
    }

    /// <summary>
    /// Gets the character cell dimensions (font size).
    /// Matches upstream ConsoleCtl::getFontSize() in conctl.cpp
    /// </summary>
    /// <returns>Font size in pixels (width, height)</returns>
    public TPoint GetFontSize()
    {
        if (GetCurrentConsoleFont(Out(), false, out var fontInfo))
        {
            return new TPoint(fontInfo.dwFontSize.X, fontInfo.dwFontSize.Y);
        }

        // Default fallback (8x16 is typical VGA font size)
        return new TPoint(8, 16);
    }
}
