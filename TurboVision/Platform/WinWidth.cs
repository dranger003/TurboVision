using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Character width detection using dynamic measurement on Win32 console.
/// Uses temporary console screen buffer to test actual character widths.
/// Matches upstream WinWidth in winwidth.cpp
/// </summary>
internal sealed class WinWidth
{
    // Static atomic fields shared across all threads
    // Matches upstream winwidth.cpp:9-10
    private static int s_lastReset = 0;
    private static bool s_isLegacyConsole = false;

    // Thread-local instance - each thread gets its own WinWidth
    // Matches upstream winwidth.cpp:11
    private static readonly ThreadLocal<WinWidth> s_localInstance = new(() => new WinWidth());

    // Per-thread instance fields
    private nint _cnHandle = INVALID_HANDLE_VALUE;
    private int _currentReset = 0;
    private readonly Dictionary<uint, int> _results = new();

    /// <summary>
    /// Private constructor - use GetInstance() instead.
    /// </summary>
    private WinWidth()
    {
    }

    /// <summary>
    /// Resets the character width detection state.
    /// Invalidates all thread-local caches.
    /// Matches upstream WinWidth::reset() in winwidth.cpp:49
    /// </summary>
    public static void Reset(bool isLegacyConsole)
    {
        s_isLegacyConsole = isLegacyConsole;
        // Increment counter to invalidate all thread-local caches
        Interlocked.Increment(ref s_lastReset);
    }

    /// <summary>
    /// Gets the display width of a UTF-32 character (1 or 2 columns).
    /// Legacy console: Each UTF-16 code unit = 1 cell (Borland behavior)
    /// Modern console: Measures actual width by writing to test buffer
    /// Matches upstream WinWidth::getWidth() wrapper
    /// </summary>
    public static int GetWidth(uint codePoint)
    {
        return s_localInstance.Value!.CalcWidth(codePoint);
    }

    /// <summary>
    /// Gets the display width of a character (convenience overload for char).
    /// </summary>
    public static int GetWidth(char ch)
    {
        return GetWidth((uint)ch);
    }

    /// <summary>
    /// Gets the width of a text span (handles surrogate pairs).
    /// </summary>
    public static int GetWidth(ReadOnlySpan<char> text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                // Combine surrogate pair to UTF-32
                uint codePoint = (uint)char.ConvertToUtf32(text[i], text[i + 1]);
                width += GetWidth(codePoint);
                i++; // Skip low surrogate
            }
            else
            {
                width += GetWidth(text[i]);
            }
        }
        return width;
    }

    /// <summary>
    /// Sets up the temporary console screen buffer for width testing.
    /// Called lazily on first width calculation.
    /// Matches upstream WinWidth::setUp() in winwidth.cpp:28-48
    /// </summary>
    private void SetUp()
    {
        if (_cnHandle == INVALID_HANDLE_VALUE || _currentReset != s_lastReset)
        {
            TearDown();
            _currentReset = s_lastReset;

            // Allocating a buffer in order to print characters is only necessary
            // when not in the legacy console.
            if (!s_isLegacyConsole)
            {
                _cnHandle = CreateConsoleScreenBuffer(
                    GENERIC_READ | GENERIC_WRITE,
                    0,
                    nint.Zero,
                    CONSOLE_TEXTMODE_BUFFER,
                    nint.Zero);

                if (_cnHandle != INVALID_HANDLE_VALUE)
                {
                    // Hide cursor in test buffer
                    var info = new CONSOLE_CURSOR_INFO
                    {
                        dwSize = 1,
                        bVisible = false
                    };
                    SetConsoleCursorInfo(_cnHandle, ref info);
                }
            }
        }
    }

    /// <summary>
    /// Tears down the temporary console screen buffer.
    /// Matches upstream WinWidth::tearDown() in winwidth.cpp:50-58
    /// </summary>
    private void TearDown()
    {
        if (_cnHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle(_cnHandle);
            _cnHandle = INVALID_HANDLE_VALUE;
        }
        _results.Clear();
    }

    /// <summary>
    /// Calculates the display width of a UTF-32 character.
    /// Uses actual measurement by writing character + marker to test buffer.
    /// Matches upstream WinWidth::calcWidth() in winwidth.cpp:60-100
    /// </summary>
    private int CalcWidth(uint u32)
    {
        SetUp();

        // Check cache first
        if (_results.TryGetValue(u32, out int cachedWidth))
        {
            return cachedWidth;
        }

        // Convert UTF-32 to UTF-16
        Span<char> u16 = stackalloc char[3];
        int len = Utf32To16(u32, u16);

        if (_cnHandle == INVALID_HANDLE_VALUE)
        {
            // In the legacy console, each code unit takes one cell.
            return len;
        }

        int res = -1;
        if (len > 0)
        {
            // We print an additional character so that we can distinguish
            // actual double-width characters from the ones affected by
            // https://github.com/microsoft/terminal/issues/11756.
            u16[len] = '#';

            // Write character + marker to test buffer
            SetConsoleCursorPosition(_cnHandle, new COORD(0, 0));
            unsafe
            {
                fixed (char* ptr = u16)
                {
                    WriteConsoleW(_cnHandle, (byte*)ptr, (uint)(len + 1), out _, nint.Zero);
                }
            }

            // Read cursor position to determine width
            if (GetConsoleScreenBufferInfo(_cnHandle, out var sbInfo))
            {
                res = sbInfo.dwCursorPosition.X - 1;

                // Check for Windows Terminal bug workaround
                if (res > 1)
                {
                    var coord = new COORD(1, sbInfo.dwCursorPosition.Y);
                    char[] charAfter = new char[1];
                    if (ReadConsoleOutputCharacterW(_cnHandle, charAfter, 1, coord, out uint count))
                    {
                        if (count == 1 && charAfter[0] == '#')
                        {
                            // Bug detected: reported width is incorrect
                            res = -1;
                        }
                    }
                }
            }
        }

        // Memoize the result
        _results[u32] = res;
        return res;
    }

    /// <summary>
    /// Converts UTF-32 code point to UTF-16 (handles surrogate pairs).
    /// Returns the number of UTF-16 code units (1 or 2).
    /// Matches upstream utf32To16() behavior
    /// </summary>
    private static int Utf32To16(uint u32, Span<char> u16)
    {
        if (u32 <= 0xFFFF)
        {
            // BMP character (single UTF-16 code unit)
            u16[0] = (char)u32;
            return 1;
        }
        else if (u32 <= 0x10FFFF)
        {
            // Non-BMP character (surrogate pair)
            u32 -= 0x10000;
            u16[0] = (char)(0xD800 + (u32 >> 10));      // High surrogate
            u16[1] = (char)(0xDC00 + (u32 & 0x3FF));    // Low surrogate
            return 2;
        }
        else
        {
            // Invalid code point
            u16[0] = '\uFFFD'; // Replacement character
            return 1;
        }
    }

    /// <summary>
    /// Finalizer to clean up console buffer.
    /// Matches upstream WinWidth::~WinWidth() in winwidth.cpp:23-26
    /// </summary>
    ~WinWidth()
    {
        TearDown();
    }
}
