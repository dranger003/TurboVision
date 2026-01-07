using System.Diagnostics;
using TurboVision.Core;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A view that displays the current heap/memory usage.
/// Placed at the right end of the status line.
/// </summary>
public class THeapView : TView
{
    private long _oldMem;
    private long _newMem;
    private string _heapStr = string.Empty;

    public THeapView(TRect bounds) : base(bounds)
    {
        _oldMem = 0;
        _newMem = HeapSize();
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2).Normal;

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _heapStr, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    public virtual void Update()
    {
        if ((_newMem = HeapSize()) != _oldMem)
        {
            _oldMem = _newMem;
            DrawView();
        }
    }

    public long HeapSize()
    {
        long total = Process.GetCurrentProcess().WorkingSet64;
        _heapStr = total.ToString().PadLeft(12);
        return total;
    }
}

/// <summary>
/// A view that displays the current time.
/// Placed at the right end of the menu bar.
/// </summary>
public class TClockView : TView
{
    private string _lastTime = "        ";
    private string _curTime = "        ";

    public TClockView(TRect bounds) : base(bounds)
    {
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2).Normal;

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _curTime, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    public virtual void Update()
    {
        _curTime = DateTime.Now.ToString("HH:mm:ss");

        if (_lastTime != _curTime)
        {
            DrawView();
            _lastTime = _curTime;
        }
    }
}
