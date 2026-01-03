using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Interface for screen display operations.
/// </summary>
public interface IScreenDriver
{
    /// <summary>
    /// Gets the number of columns in the display.
    /// </summary>
    int Cols { get; }

    /// <summary>
    /// Gets the number of rows in the display.
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// Clears the screen with the specified character and attribute.
    /// </summary>
    void ClearScreen(char c, TColorAttr attr);

    /// <summary>
    /// Writes a buffer of cells to the screen at the specified position.
    /// </summary>
    void WriteBuffer(int x, int y, int width, int height, ReadOnlySpan<TScreenCell> buffer);

    /// <summary>
    /// Flushes any buffered screen output.
    /// </summary>
    void Flush();

    /// <summary>
    /// Sets the cursor position.
    /// </summary>
    void SetCursorPosition(int x, int y);

    /// <summary>
    /// Sets the cursor visibility and type.
    /// </summary>
    void SetCursorType(ushort cursorType);

    /// <summary>
    /// Gets the current cursor type.
    /// </summary>
    ushort GetCursorType();

    /// <summary>
    /// Suspends the screen driver (e.g., for shell-out).
    /// </summary>
    void Suspend();

    /// <summary>
    /// Resumes the screen driver after suspension.
    /// </summary>
    void Resume();
}
