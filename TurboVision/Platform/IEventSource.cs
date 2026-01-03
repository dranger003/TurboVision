using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Interface for input event sources.
/// </summary>
public interface IEventSource
{
    /// <summary>
    /// Gets the next available event, if any.
    /// </summary>
    /// <returns>True if an event was available, false otherwise.</returns>
    bool GetEvent(out TEvent ev);

    /// <summary>
    /// Waits for events for up to the specified timeout.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (-1 for infinite).</param>
    void WaitForEvents(int timeoutMs);

    /// <summary>
    /// Wakes up the event loop if it's waiting.
    /// </summary>
    void WakeUp();

    /// <summary>
    /// Checks if a mouse is present.
    /// </summary>
    bool MousePresent { get; }

    /// <summary>
    /// Suspends the event source.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Resumes the event source.
    /// </summary>
    void Resume();
}
