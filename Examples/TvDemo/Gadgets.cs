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
    // Upstream: gadgets.cpp line 117-122 (Windows implementation)
    private static readonly Process _currentProcess = Process.GetCurrentProcess();

    // Upstream: gadgets.h lines 35-36
    private uint _oldMem;
    private uint _newMem;
    private string _heapStr = string.Empty;  // Upstream: char heapStr[16]

    // Upstream: gadgets.cpp lines 43-47
    public THeapView(TRect bounds) : base(bounds)
    {
        _oldMem = 0;
        _newMem = HeapSize();
        // Note: _heapStr is formatted inside HeapSize(), matching upstream pattern
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2).Normal;

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _heapStr, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    // Upstream: gadgets.cpp lines 61-68
    public virtual void Update()
    {
        if ((_newMem = HeapSize()) != _oldMem)
        {
            _oldMem = _newMem;
            DrawView();
            // Note: heapStr formatting happens inside HeapSize(), matching upstream
        }
    }

    // Upstream: gadgets.cpp lines 71-127
    // Note: Upstream formats heapStr inside this method using ostrstream
    public uint HeapSize()
    {
        // Upstream: Windows implementation (lines 117-122)
        // Get current process memory usage
        _currentProcess.Refresh();
        uint total = (uint)_currentProcess.PrivateMemorySize64;

        // Format into heapStr (matches upstream: totalStr << setw(12) << pmc.PrivateUsage << ends)
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
    // Upstream: gadgets.h lines 52-53
    private string _lastTime = string.Empty;  // Upstream: char lastTime[9]
    private string _curTime = string.Empty;   // Upstream: char curTime[9]

    // Upstream: gadgets.cpp lines 134-138
    public TClockView(TRect bounds) : base(bounds)
    {
        _lastTime = "        ";  // 8 spaces
        _curTime = "        ";   // 8 spaces
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2).Normal;

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _curTime, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    // Upstream: gadgets.cpp lines 152-165
    public virtual void Update()
    {
        // Upstream uses time(0) and ctime(), extracts HH:MM:SS from position 11-18
        // C# equivalent: DateTime.Now.ToString("HH:mm:ss")
        var time = DateTime.Now.ToString("HH:mm:ss");

        // Upstream: strcpy(curTime, &date[11]) - extract time portion
        _curTime = time;

        // Upstream: if (strcmp(lastTime, curTime)) - if strings differ
        if (_lastTime != _curTime)
        {
            DrawView();
            _lastTime = _curTime;  // Upstream: strcpy(lastTime, curTime)
        }
    }
}
