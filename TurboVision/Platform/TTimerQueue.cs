using System.Diagnostics;

namespace TurboVision.Platform;

/// <summary>
/// Opaque handle to a timer.
/// </summary>
public readonly struct TTimerId : IEquatable<TTimerId>
{
    public nint Value { get; }

    public TTimerId(nint value) => Value = value;

    public static implicit operator nint(TTimerId id) => id.Value;
    public static implicit operator TTimerId(nint value) => new(value);

    public bool Equals(TTimerId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TTimerId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(TTimerId left, TTimerId right) => left.Equals(right);
    public static bool operator !=(TTimerId left, TTimerId right) => !left.Equals(right);
}

/// <summary>
/// Timer queue for managing timed events.
/// </summary>
public class TTimerQueue
{
    private record struct Timer(TTimerId Id, long ExpiresAt, int PeriodMs, Action<TTimerId>? Callback);

    private readonly List<Timer> _timers = [];
    private readonly Stopwatch _stopwatch;
    private nint _nextId = 1;

    public TTimerQueue()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Sets a timer that expires after timeoutMs milliseconds.
    /// If periodMs is negative, the timer fires once and is auto-removed.
    /// If periodMs is positive, the timer repeats at that interval.
    /// </summary>
    public TTimerId SetTimer(int timeoutMs, int periodMs = -1, Action<TTimerId>? callback = null)
    {
        var id = new TTimerId(_nextId++);
        var expiresAt = _stopwatch.ElapsedMilliseconds + timeoutMs;
        _timers.Add(new Timer(id, expiresAt, periodMs, callback));
        return id;
    }

    /// <summary>
    /// Removes a timer.
    /// </summary>
    public void KillTimer(TTimerId id)
    {
        _timers.RemoveAll(t => t.Id == id);
    }

    /// <summary>
    /// Returns time in milliseconds until the next timer expires, or -1 if no timers.
    /// </summary>
    public int TimeUntilNextTimeout()
    {
        if (_timers.Count == 0)
            return -1;

        long now = _stopwatch.ElapsedMilliseconds;
        long minTime = long.MaxValue;

        foreach (var timer in _timers)
        {
            long remaining = timer.ExpiresAt - now;
            if (remaining < minTime)
                minTime = remaining;
        }

        return minTime < 0 ? 0 : (int)minTime;
    }

    /// <summary>
    /// Processes expired timers and invokes their callbacks.
    /// </summary>
    public void ProcessTimers(Action<TTimerId> defaultCallback)
    {
        long now = _stopwatch.ElapsedMilliseconds;

        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var timer = _timers[i];
            if (now >= timer.ExpiresAt)
            {
                // Invoke callback
                var callback = timer.Callback ?? defaultCallback;
                callback(timer.Id);

                if (timer.PeriodMs < 0)
                {
                    // One-shot timer, remove it
                    _timers.RemoveAt(i);
                }
                else
                {
                    // Periodic timer, reschedule
                    _timers[i] = timer with { ExpiresAt = now + timer.PeriodMs };
                }
            }
        }
    }

    /// <summary>
    /// Checks if a timer with the given ID exists.
    /// </summary>
    public bool HasTimer(TTimerId id)
    {
        return _timers.Exists(t => t.Id == id);
    }
}
