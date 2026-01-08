using System.Runtime.InteropServices;

namespace TurboVision.Platform;

/// <summary>
/// Win32 API interop declarations for console and clipboard operations.
/// Consolidates all P/Invoke declarations used by Win32 console implementation.
/// </summary>
internal static partial class Win32Interop
{
    // ========================================
    // Constants
    // ========================================

    // Standard handles
    public const int STD_INPUT_HANDLE = -10;
    public const int STD_OUTPUT_HANDLE = -11;
    public const int STD_ERROR_HANDLE = -12;

    // Console modes (input)
    public const uint ENABLE_PROCESSED_INPUT = 0x0001;
    public const uint ENABLE_LINE_INPUT = 0x0002;
    public const uint ENABLE_ECHO_INPUT = 0x0004;
    public const uint ENABLE_WINDOW_INPUT = 0x0008;
    public const uint ENABLE_MOUSE_INPUT = 0x0010;
    public const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

    // Console modes (output)
    public const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    public const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
    public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    // Input event types
    public const ushort KEY_EVENT = 0x0001;
    public const ushort MOUSE_EVENT = 0x0002;
    public const ushort WINDOW_BUFFER_SIZE_EVENT = 0x0004;
    public const ushort MENU_EVENT = 0x0008;
    public const ushort FOCUS_EVENT = 0x0010;

    // Control key state
    public const uint RIGHT_ALT_PRESSED = 0x0001;
    public const uint LEFT_ALT_PRESSED = 0x0002;
    public const uint RIGHT_CTRL_PRESSED = 0x0004;
    public const uint LEFT_CTRL_PRESSED = 0x0008;
    public const uint SHIFT_PRESSED = 0x0010;
    public const uint NUMLOCK_ON = 0x0020;
    public const uint SCROLLLOCK_ON = 0x0040;
    public const uint CAPSLOCK_ON = 0x0080;
    public const uint ENHANCED_KEY = 0x0100;

    // Mouse event flags
    public const uint MOUSE_MOVED = 0x0001;
    public const uint DOUBLE_CLICK = 0x0002;
    public const uint MOUSE_WHEELED = 0x0004;
    public const uint MOUSE_HWHEELED = 0x0008;

    // Virtual key codes
    public const ushort VK_MENU = 0x12; // Alt key

    // Code pages
    public const uint CP_UTF8 = 65001;

    // CreateConsoleScreenBuffer constants
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint CONSOLE_TEXTMODE_BUFFER = 1;

    // Handle constants
    public static readonly nint INVALID_HANDLE_VALUE = new nint(-1);

    // Wait constants
    public const uint INFINITE = 0xFFFFFFFF;

    // Clipboard formats
    public const uint CF_TEXT = 1;
    public const uint CF_UNICODETEXT = 13;

    // Global memory flags
    public const uint GMEM_MOVEABLE = 0x0002;

    // Font constants
    public const uint TMPF_FIXED_PITCH = 0x01;
    public const uint TMPF_VECTOR = 0x02;
    public const uint TMPF_TRUETYPE = 0x04;
    public const uint TMPF_DEVICE = 0x08;

    public const uint FF_DONTCARE = 0 << 4;
    public const uint FW_NORMAL = 400;

    // ========================================
    // Structures
    // ========================================

    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;

        public COORD(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_CURSOR_INFO
    {
        public uint dwSize;
        public bool bVisible;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CHAR_INFO
    {
        public char UnicodeChar;
        public ushort Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_FONT_INFO
    {
        public uint nFont;
        public COORD dwFontSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CONSOLE_FONT_INFOEX
    {
        public uint cbSize;
        public uint nFont;
        public COORD dwFontSize;
        public uint FontFamily;
        public uint FontWeight;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] FaceName;

        public CONSOLE_FONT_INFOEX()
        {
            cbSize = (uint)Marshal.SizeOf<CONSOLE_FONT_INFOEX>();
            FaceName = new char[32];
        }
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct INPUT_RECORD
    {
        [FieldOffset(0)]
        public ushort EventType;
        [FieldOffset(4)]
        public KEY_EVENT_RECORD KeyEvent;
        [FieldOffset(4)]
        public MOUSE_EVENT_RECORD MouseEvent;
        [FieldOffset(4)]
        public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct KEY_EVENT_RECORD
    {
        [FieldOffset(0)]
        public int bKeyDown;
        [FieldOffset(4)]
        public ushort wRepeatCount;
        [FieldOffset(6)]
        public ushort wVirtualKeyCode;
        [FieldOffset(8)]
        public ushort wVirtualScanCode;
        [FieldOffset(10)]
        public char UnicodeChar;
        [FieldOffset(12)]
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSE_EVENT_RECORD
    {
        public COORD dwMousePosition;
        public uint dwButtonState;
        public uint dwControlKeyState;
        public uint dwEventFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOW_BUFFER_SIZE_RECORD
    {
        public COORD dwSize;
    }

    // ========================================
    // kernel32.dll - Console Functions
    // ========================================

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetConsoleCP();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetConsoleOutputCP();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleOutputCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetOEMCP();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleScreenBufferInfo(
        nint hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleScreenBufferSize(
        nint hConsoleOutput,
        COORD dwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FillConsoleOutputAttribute(
        nint hConsoleOutput,
        ushort wAttribute,
        uint nLength,
        COORD dwWriteCoord,
        out uint lpNumberOfAttrsWritten);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool FillConsoleOutputCharacterW(
        nint hConsoleOutput,
        char cCharacter,
        uint nLength,
        COORD dwWriteCoord,
        out uint lpNumberOfCharsWritten);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WriteConsoleOutputW(
        nint hConsoleOutput,
        [In] CHAR_INFO[] lpBuffer,
        COORD dwBufferSize,
        COORD dwBufferCoord,
        ref SMALL_RECT lpWriteRegion);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern unsafe bool WriteConsoleW(
        nint hConsoleOutput,
        byte* lpBuffer,
        uint nNumberOfCharsToWrite,
        out uint lpNumberOfCharsWritten,
        nint lpReserved);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern unsafe bool WriteFile(
        nint hFile,
        byte* lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        nint lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleCursorPosition(
        nint hConsoleOutput,
        COORD dwCursorPosition);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleCursorInfo(
        nint hConsoleOutput,
        ref CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleCursorInfo(
        nint hConsoleOutput,
        out CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleTextAttribute(
        nint hConsoleOutput,
        ushort wAttributes);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetNumberOfConsoleInputEvents(
        nint hConsoleInput,
        out uint lpcNumberOfEvents);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ReadConsoleInputW(
        nint hConsoleInput,
        ref INPUT_RECORD lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WriteConsoleInputW(
        nint hConsoleInput,
        ref INPUT_RECORD lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(
        nint hHandle,
        uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetCurrentConsoleFont(
        nint hConsoleOutput,
        bool bMaximumWindow,
        out CONSOLE_FONT_INFO lpConsoleCurrentFont);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateConsoleScreenBuffer(
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwFlags,
        nint lpScreenBufferData);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleActiveScreenBuffer(nint hConsoleOutput);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern nint GetModuleHandleA(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern nint GetProcAddress(nint hModule, string lpProcName);

    // ========================================
    // user32.dll - Clipboard Functions
    // ========================================

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetClipboardData(uint uFormat, nint hMem);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint GetClipboardData(uint uFormat);

    // ========================================
    // kernel32.dll - Global Memory Functions
    // ========================================

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalFree(nint hMem);

    // ========================================
    // Delegates for Dynamic Resolution
    // ========================================

    /// <summary>
    /// Delegate for GetCurrentConsoleFontEx (Vista+)
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate bool GetCurrentConsoleFontExDelegate(
        nint hConsoleOutput,
        bool bMaximumWindow,
        ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

    /// <summary>
    /// Delegate for SetCurrentConsoleFontEx (Vista+)
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public delegate bool SetCurrentConsoleFontExDelegate(
        nint hConsoleOutput,
        bool bMaximumWindow,
        ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

    /// <summary>
    /// Delegate for wine_get_version (Wine detection)
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate nint WineGetVersionDelegate();

    // ========================================
    // Helper Functions
    // ========================================

    /// <summary>
    /// Gets a function pointer from a module.
    /// Used for dynamic resolution of Vista+ APIs.
    /// Matches upstream getModuleProc in win32con.cpp:24-29
    /// </summary>
    public static T? GetModuleProc<T>(string moduleName, string symbolName) where T : Delegate
    {
        nint hModule = GetModuleHandleA(moduleName);
        if (hModule == nint.Zero)
            return null;

        nint proc = GetProcAddress(hModule, symbolName);
        if (proc == nint.Zero)
            return null;

        return Marshal.GetDelegateForFunctionPointer<T>(proc);
    }
}
