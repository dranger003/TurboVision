using System.Text;
using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Console handle manager - manages three handles:
/// - Input (STD_INPUT_HANDLE)
/// - StartupOutput (STD_OUTPUT_HANDLE)
/// - ActiveOutput (alternate screen buffer)
/// Matches upstream ConsoleCtl in conctl.h
/// </summary>
internal sealed class ConsoleCtl
{
    private const int Input = 0;
    private const int StartupOutput = 1;
    private const int ActiveOutput = 2;

    private readonly nint[] _handles = new nint[3];
    private readonly bool[] _owning = new bool[3];

    private static ConsoleCtl? _instance;

    private ConsoleCtl()
    {
        // Get standard handles
        _handles[Input] = GetStdHandle(STD_INPUT_HANDLE);
        _handles[StartupOutput] = GetStdHandle(STD_OUTPUT_HANDLE);

        // Create alternate screen buffer for activeOutput
        // This allows us to switch between the original console view and our application view
        _handles[ActiveOutput] = CreateConsoleScreenBuffer(
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            nint.Zero,
            CONSOLE_TEXTMODE_BUFFER,
            nint.Zero);

        if (_handles[ActiveOutput] != INVALID_HANDLE_VALUE)
        {
            _owning[ActiveOutput] = true;
            SetConsoleActiveScreenBuffer(_handles[ActiveOutput]);
        }
        else
        {
            // Fallback to startup output if alternate buffer creation fails
            _handles[ActiveOutput] = _handles[StartupOutput];
            _owning[ActiveOutput] = false;
        }
    }

    ~ConsoleCtl()
    {
        // Restore original screen buffer and clean up
        if (_owning[ActiveOutput] && _handles[ActiveOutput] != INVALID_HANDLE_VALUE)
        {
            SetConsoleActiveScreenBuffer(_handles[StartupOutput]);
            CloseHandle(_handles[ActiveOutput]);
        }
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
    /// </summary>
    /// <param name="data">UTF-8 encoded bytes to write</param>
    public unsafe void Write(ReadOnlySpan<byte> data)
    {
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
