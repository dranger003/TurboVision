namespace TurboVision.Platform;

/// <summary>
/// Base class for console adapters that aggregate display and input functionality.
/// Matches upstream ConsoleAdapter in platform.h:48-66
/// </summary>
public abstract class ConsoleAdapter : IDisposable
{
    /// <summary>
    /// The display adapter for rendering output.
    /// </summary>
    protected readonly DisplayAdapter _display;

    /// <summary>
    /// The input adapter for receiving events.
    /// </summary>
    protected readonly InputAdapter _input;

    /// <summary>
    /// Initializes a new instance of the ConsoleAdapter class.
    /// </summary>
    /// <param name="display">Display adapter</param>
    /// <param name="input">Input adapter</param>
    protected ConsoleAdapter(DisplayAdapter display, InputAdapter input)
    {
        _display = display;
        _input = input;
    }

    /// <summary>
    /// Checks if the console is still alive and functional.
    /// </summary>
    /// <returns>True if console is operational</returns>
    public virtual bool IsAlive() => true;

    /// <summary>
    /// Sets text to the system clipboard.
    /// </summary>
    /// <param name="text">Text to copy to clipboard</param>
    /// <returns>True if successful</returns>
    public virtual bool SetClipboardText(string text) => false;

    /// <summary>
    /// Requests text from the system clipboard.
    /// </summary>
    /// <param name="accept">Callback to receive clipboard text</param>
    /// <returns>True if successful</returns>
    public virtual bool RequestClipboardText(Action<string> accept) => false;

    /// <summary>
    /// Disposes resources used by the console adapter.
    /// </summary>
    public abstract void Dispose();
}
