namespace TurboVision.Dialogs;

/// <summary>
/// Directory entry with display text and path.
/// Used in directory tree views.
/// </summary>
public class TDirEntry
{
    /// <summary>
    /// Display text (may include tree graphics).
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Actual directory path.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Creates a new directory entry.
    /// </summary>
    public TDirEntry(string displayText, string directory)
    {
        DisplayText = displayText;
        Directory = directory;
    }

    /// <summary>
    /// Gets the display text.
    /// </summary>
    public string Text()
    {
        return DisplayText;
    }

    /// <summary>
    /// Gets the directory path.
    /// </summary>
    public string Dir()
    {
        return Directory;
    }
}
