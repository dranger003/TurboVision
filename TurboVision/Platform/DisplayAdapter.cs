using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Abstract base class for display adapters.
/// Matches upstream DisplayAdapter in platform.h:17-34
/// </summary>
public abstract class DisplayAdapter
{
    /// <summary>
    /// Reloads screen information and returns the current screen size.
    /// </summary>
    /// <returns>Current screen dimensions (columns, rows)</returns>
    public virtual TPoint ReloadScreenInfo() => new TPoint(0, 0);

    /// <summary>
    /// Gets the number of colors supported by the display.
    /// </summary>
    /// <returns>Number of colors (16 for basic, 256+ for extended, 16M for true color)</returns>
    public virtual int GetColorCount() => 0;

    /// <summary>
    /// Gets the font size (character cell dimensions).
    /// </summary>
    /// <returns>Font size in pixels (width, height)</returns>
    public virtual TPoint GetFontSize() => new TPoint(0, 0);

    /// <summary>
    /// Writes a single cell to the display at the specified position.
    /// </summary>
    /// <param name="pos">Position (x, y)</param>
    /// <param name="text">Text to write (may be multiple characters for grapheme clusters)</param>
    /// <param name="attr">Color attributes</param>
    /// <param name="doubleWidth">True if the character occupies two columns</param>
    public virtual void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth) { }

    /// <summary>
    /// Sets the caret (cursor) position.
    /// </summary>
    /// <param name="pos">Position (x, y)</param>
    public virtual void SetCaretPosition(TPoint pos) { }

    /// <summary>
    /// Sets the caret (cursor) size.
    /// </summary>
    /// <param name="size">Cursor size (0 = hidden, 1-100 = percentage of cell height)</param>
    public virtual void SetCaretSize(int size) { }

    /// <summary>
    /// Clears the entire screen.
    /// </summary>
    public virtual void ClearScreen() { }

    /// <summary>
    /// Flushes any buffered output to the display.
    /// </summary>
    public virtual void Flush() { }
}
