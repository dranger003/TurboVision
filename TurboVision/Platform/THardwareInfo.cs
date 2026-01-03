namespace TurboVision.Platform;

/// <summary>
/// Platform detection and hardware information.
/// </summary>
public static class THardwareInfo
{
    /// <summary>
    /// Gets whether the current platform is Windows.
    /// </summary>
    public static bool IsWindows
    {
        get { return OperatingSystem.IsWindows(); }
    }

    /// <summary>
    /// Gets whether the current platform is Linux.
    /// </summary>
    public static bool IsLinux
    {
        get { return OperatingSystem.IsLinux(); }
    }

    /// <summary>
    /// Gets whether the current platform is macOS.
    /// </summary>
    public static bool IsMacOS
    {
        get { return OperatingSystem.IsMacOS(); }
    }

    /// <summary>
    /// Gets the platform-specific newline string.
    /// </summary>
    public static string NewLine
    {
        get { return Environment.NewLine; }
    }
}
