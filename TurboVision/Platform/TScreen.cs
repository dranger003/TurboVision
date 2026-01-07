using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Static screen state and management.
/// </summary>
public static class TScreen
{
    private static IScreenDriver? _driver;
    private static TScreenCell[]? _screenBuffer;

    public static ushort StartupMode { get; set; }
    public static ushort StartupCursor { get; set; }
    public static ushort ScreenMode { get; set; } = TDisplay.smCO80;
    public static ushort ScreenWidth { get; private set; } = 80;
    public static ushort ScreenHeight { get; private set; } = 25;
    public static bool HiResScreen { get; set; }
    public static bool CheckSnow { get; set; }
    public static ushort CursorLines { get; set; }
    public static bool ClearOnSuspend { get; set; } = true;

    public static TScreenCell[]? ScreenBuffer
    {
        get { return _screenBuffer; }
    }

    public static IScreenDriver? Driver
    {
        get { return _driver; }
    }

    /// <summary>
    /// Initializes the screen with the specified driver.
    /// </summary>
    public static void Initialize(IScreenDriver driver)
    {
        _driver = driver;
        ScreenWidth = (ushort)driver.Cols;
        ScreenHeight = (ushort)driver.Rows;
        _screenBuffer = new TScreenCell[ScreenWidth * ScreenHeight];
        CursorLines = driver.GetCursorType();
    }

    /// <summary>
    /// Sets the video mode.
    /// </summary>
    public static void SetVideoMode(ushort mode)
    {
        ScreenMode = mode;

        // If smUpdate is specified, refresh screen dimensions from driver
        if ((mode & TDisplay.smUpdate) != 0 && _driver != null)
        {
            ScreenWidth = (ushort)_driver.Cols;
            ScreenHeight = (ushort)_driver.Rows;
            _screenBuffer = new TScreenCell[ScreenWidth * ScreenHeight];
        }
    }

    /// <summary>
    /// Clears the screen.
    /// </summary>
    public static void ClearScreen()
    {
        _driver?.ClearScreen(' ', new TColorAttr(0x07));
    }

    /// <summary>
    /// Flushes the screen buffer to the display.
    /// </summary>
    public static void FlushScreen()
    {
        _driver?.Flush();
    }

    /// <summary>
    /// Suspends the screen driver.
    /// </summary>
    public static void Suspend()
    {
        if (_driver == null)
            return;

        if (ClearOnSuspend)
            ClearScreen();

        _driver.SetCursorType(StartupCursor);
        _driver.Suspend();
    }

    /// <summary>
    /// Resumes the screen driver.
    /// </summary>
    public static void Resume()
    {
        _driver?.Resume();
    }
}
