namespace TurboVision.Platform;

/// <summary>
/// Simple internal clipboard for text operations.
/// </summary>
public static class TClipboard
{
    private static string _text = "";

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    public static void SetText(ReadOnlySpan<char> text)
    {
        _text = text.ToString();
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public static string GetText()
    {
        return _text;
    }

    /// <summary>
    /// Request text from clipboard (async operation, for compatibility).
    /// In this simple implementation, it's a no-op since we use internal clipboard.
    /// </summary>
    public static void RequestText()
    {
        // No-op for internal clipboard
    }
}
