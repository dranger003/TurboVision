namespace TurboVision.Platform;

/// <summary>
/// State for input processing, tracking surrogate pairs and mouse state.
/// Matches upstream InputState in termio.h
/// </summary>
internal struct InputState
{
    public InputState()
    {
        HasFullOsc52 = false;
        BracketedPaste = false;
        GotDsrResponse = false;
        PutPaste = null;
    }

    /// <summary>
    /// Current mouse button state.
    /// </summary>
    public byte Buttons;

    /// <summary>
    /// UTF-16 high surrogate character waiting for its low surrogate pair.
    /// Used when pasting emoji or non-BMP characters.
    /// </summary>
    public char Surrogate;

    /// <summary>
    /// Indicates if the terminal supports full OSC 52 clipboard sequences.
    /// </summary>
    public bool HasFullOsc52;

    /// <summary>
    /// Indicates if bracketed paste mode is enabled.
    /// </summary>
    public bool BracketedPaste;

    /// <summary>
    /// Indicates if a Device Status Report (DSR) response has been received.
    /// </summary>
    public bool GotDsrResponse;

    /// <summary>
    /// Callback for handling pasted text.
    /// </summary>
    public Action<string>? PutPaste;
}
