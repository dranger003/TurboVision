using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Event queue for polling and dispatching events.
/// </summary>
public static class TEventQueue
{
    private static IEventSource? _eventSource;
    private static TEvent _pendingEvent;
    private static bool _hasPendingEvent;

    public static ushort DoubleDelay { get; set; } = 8;
    public static bool MouseReverse { get; set; }

    /// <summary>
    /// Initializes the event queue with the specified event source.
    /// </summary>
    public static void Initialize(IEventSource source)
    {
        _eventSource = source;
    }

    /// <summary>
    /// Gets the next mouse event.
    /// </summary>
    public static void GetMouseEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;

        if (_hasPendingEvent && (_pendingEvent.What & EventConstants.evMouse) != 0)
        {
            ev = _pendingEvent;
            _hasPendingEvent = false;
            return;
        }

        if (_eventSource?.GetEvent(out var sourceEvent) == true)
        {
            if ((sourceEvent.What & EventConstants.evMouse) != 0)
            {
                ev = sourceEvent;
            }
            else if (sourceEvent.What != EventConstants.evNothing)
            {
                // Save non-mouse event as pending so it's not lost
                _pendingEvent = sourceEvent;
                _hasPendingEvent = true;
            }
        }
    }

    /// <summary>
    /// Gets the next keyboard event.
    /// </summary>
    public static void GetKeyEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;

        if (_hasPendingEvent && ((_pendingEvent.What & EventConstants.evKeyboard) != 0 ||
                                  (_pendingEvent.What & EventConstants.evCommand) != 0))
        {
            ev = _pendingEvent;
            _hasPendingEvent = false;
            return;
        }

        if (_eventSource?.GetEvent(out var sourceEvent) == true)
        {
            if ((sourceEvent.What & EventConstants.evKeyboard) != 0 ||
                (sourceEvent.What & EventConstants.evCommand) != 0)
            {
                ev = sourceEvent;
            }
            else if (sourceEvent.What != EventConstants.evNothing)
            {
                // Save non-keyboard event as pending so it's not lost
                _pendingEvent = sourceEvent;
                _hasPendingEvent = true;
            }
        }
    }

    /// <summary>
    /// Posts an event to be returned by the next GetEvent call.
    /// </summary>
    public static void PutEvent(TEvent ev)
    {
        _pendingEvent = ev;
        _hasPendingEvent = true;
    }

    /// <summary>
    /// Waits for events for up to the specified timeout.
    /// </summary>
    public static void WaitForEvents(int timeoutMs)
    {
        _eventSource?.WaitForEvents(timeoutMs);
    }

    /// <summary>
    /// Wakes up the event loop.
    /// </summary>
    public static void WakeUp()
    {
        _eventSource?.WakeUp();
    }

    /// <summary>
    /// Suspends the event queue.
    /// </summary>
    public static void Suspend()
    {
        _eventSource?.Suspend();
    }

    /// <summary>
    /// Resumes the event queue.
    /// </summary>
    public static void Resume()
    {
        _eventSource?.Resume();
    }
}
