using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Abstract base class for input adapters.
/// Matches upstream InputAdapter in platform.h:36-46
/// </summary>
public abstract class InputAdapter
{
    /// <summary>
    /// Handle to the input source.
    /// </summary>
    protected readonly nint _handle;

    /// <summary>
    /// Initializes a new instance of the InputAdapter class.
    /// </summary>
    /// <param name="handle">Handle to the input source</param>
    protected InputAdapter(nint handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets the next event from the input source.
    /// </summary>
    /// <param name="ev">Receives the event data</param>
    /// <returns>True if an event was retrieved, false otherwise</returns>
    public abstract bool GetEvent(out TEvent ev);
}
