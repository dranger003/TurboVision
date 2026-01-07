using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Event queue for polling and dispatching events.
/// Matches upstream tevent.cpp TEventQueue implementation.
/// </summary>
public static class TEventQueue
{
    private static IEventSource? _eventSource;
    private static TEvent _pendingEvent;
    private static bool _hasPendingEvent;

    // Mouse state tracking - matches upstream tevent.cpp variables
    private static MouseEvent _lastMouse;
    private static MouseEvent _downMouse;
    private static long _downTicks;
    private static long _autoTicks;
    private static int _autoDelay;
    private static bool _pendingMouseUp;

    // Configuration
    public static ushort DoubleDelay { get; set; } = 8;  // Ticks for double-click detection
    public static ushort RepeatDelay { get; set; } = 8;  // Ticks before first auto repeat
    public static bool MouseReverse { get; set; }

    /// <summary>
    /// Initializes the event queue with the specified event source.
    /// </summary>
    public static void Initialize(IEventSource source)
    {
        _eventSource = source;
        _lastMouse = default;
        _downMouse = default;
        _autoTicks = 0;
        _autoDelay = 0;
        _downTicks = 0;
        _pendingMouseUp = false;
    }

    /// <summary>
    /// Gets the next mouse event, determining event type from raw mouse data.
    /// This matches upstream tevent.cpp TEventQueue::getMouseEvent.
    /// </summary>
    public static void GetMouseEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;

        // Handle pending mouse up from previous call (upstream pendingMouseUp logic)
        if (_pendingMouseUp)
        {
            ev.What = EventConstants.evMouseUp;
            ev.Mouse = _lastMouse;
            _lastMouse.Buttons = 0;
            _pendingMouseUp = false;
            return;
        }

        // Check for pending mouse event first
        if (_hasPendingEvent && (_pendingEvent.What & EventConstants.evMouse) != 0)
        {
            ev = _pendingEvent;
            _hasPendingEvent = false;
            DetermineMouseEventType(ref ev);
            return;
        }

        // Try to get a new event from the source
        if (_eventSource?.GetEvent(out var sourceEvent) == true)
        {
            if ((sourceEvent.What & EventConstants.evMouse) != 0)
            {
                ev = sourceEvent;
                DetermineMouseEventType(ref ev);
                return;
            }
            else if (sourceEvent.What != EventConstants.evNothing)
            {
                // Save non-mouse event as pending so it's not lost
                _pendingEvent = sourceEvent;
                _hasPendingEvent = true;
            }
        }

        // No new event - check if we should generate evMouseAuto
        // This matches upstream: when button is held and no events occur,
        // generate evMouseAuto periodically
        if (_lastMouse.Buttons != 0)
        {
            long currentTicks = GetTickCount();
            int ticksPerUnit = 55; // Approximate milliseconds per tick unit

            if (currentTicks - _autoTicks > _autoDelay * ticksPerUnit)
            {
                _autoTicks = currentTicks;
                _autoDelay = 1; // After first auto, delay is minimal
                ev.What = EventConstants.evMouseAuto;
                ev.Mouse = _lastMouse;
                return;
            }
        }
    }

    /// <summary>
    /// Gets current tick count in milliseconds.
    /// </summary>
    private static long GetTickCount() => Environment.TickCount64;

    /// <summary>
    /// Determines the specific mouse event type from raw mouse data.
    /// This matches upstream tevent.cpp getMouseEvent logic.
    /// </summary>
    private static void DetermineMouseEventType(ref TEvent ev)
    {
        long currentTicks = GetTickCount();
        ev.Mouse.EventFlags = 0; // Clear event flags, we'll set them as needed

        // Button released? (buttons == 0 && lastMouse.buttons != 0)
        if (ev.Mouse.Buttons == 0 && _lastMouse.Buttons != 0)
        {
            if (ev.Mouse.Where.Equals(_lastMouse.Where))
            {
                // Released without movement
                ev.What = EventConstants.evMouseUp;
                byte buttons = _lastMouse.Buttons;
                _lastMouse = ev.Mouse;
                ev.Mouse.Buttons = buttons; // Report which button was released
            }
            else
            {
                // Released with movement - generate evMouseMove first, schedule evMouseUp
                ev.What = EventConstants.evMouseMove;
                var up = ev.Mouse;
                var where = up.Where;
                ev.Mouse = _lastMouse;
                ev.Mouse.Where = where;
                ev.Mouse.EventFlags |= EventConstants.meMouseMoved;
                up.Buttons = _lastMouse.Buttons;
                _lastMouse = up;
                _pendingMouseUp = true;
            }
            return;
        }

        // Button pressed? (buttons != 0 && lastMouse.buttons == 0)
        if (ev.Mouse.Buttons != 0 && _lastMouse.Buttons == 0)
        {
            // Check for double/triple click (matches upstream doubleDelay logic)
            if (ev.Mouse.Buttons == _downMouse.Buttons &&
                ev.Mouse.Where.Equals(_downMouse.Where) &&
                currentTicks - _downTicks <= DoubleDelay * 55)
            {
                if ((_downMouse.EventFlags & (EventConstants.meDoubleClick | EventConstants.meTripleClick)) == 0)
                {
                    ev.Mouse.EventFlags |= EventConstants.meDoubleClick;
                }
                else if ((_downMouse.EventFlags & EventConstants.meDoubleClick) != 0)
                {
                    ev.Mouse.EventFlags &= unchecked((ushort)~EventConstants.meDoubleClick);
                    ev.Mouse.EventFlags |= EventConstants.meTripleClick;
                }
            }

            _downMouse = ev.Mouse;
            _autoTicks = _downTicks = currentTicks;
            _autoDelay = RepeatDelay;
            ev.What = EventConstants.evMouseDown;
            _lastMouse = ev.Mouse;
            return;
        }

        // Preserve button state from lastMouse during button-held events
        ev.Mouse.Buttons = _lastMouse.Buttons;

        // Wheel event?
        if (ev.Mouse.Wheel != 0)
        {
            ev.What = EventConstants.evMouseWheel;
            _lastMouse = ev.Mouse;
            return;
        }

        // Position changed? (ev.mouse.where != lastMouse.where)
        if (!ev.Mouse.Where.Equals(_lastMouse.Where))
        {
            ev.What = EventConstants.evMouseMove;
            ev.Mouse.EventFlags |= EventConstants.meMouseMoved;
            _lastMouse = ev.Mouse;
            return;
        }

        // Auto repeat? (buttons held and enough time passed)
        if (ev.Mouse.Buttons != 0 && currentTicks - _autoTicks > _autoDelay * 55)
        {
            _autoTicks = currentTicks;
            _autoDelay = 1;
            ev.What = EventConstants.evMouseAuto;
            _lastMouse = ev.Mouse;
            return;
        }

        // No significant change
        ev.What = EventConstants.evNothing;
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
