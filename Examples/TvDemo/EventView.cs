using System.Text;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Editors;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// Command constants for the event viewer.
/// </summary>
public static class EventViewCommands
{
    public const ushort cmFndEventView = 300;
}

/// <summary>
/// A window that displays TEvents received by the application.
/// Uses TTerminal for scrollable text output.
/// </summary>
public class TEventViewer : TWindow
{
    private static readonly string[] Titles = ["Event Viewer", "Event Viewer (Stopped)"];

    private bool _stopped;
    private int _eventCount;
    private ushort _bufSize;
    private TTerminal? _interior;
    private TScrollBar? _scrollBar;

    public TEventViewer(TRect bounds, ushort bufSize)
        : base(bounds, null, WindowConstants.wnNoNumber)
    {
        EventMask |= EventConstants.evBroadcast;
        Init(bufSize);
    }

    private void Init(ushort bufSize)
    {
        _stopped = false;
        _eventCount = 0;
        _bufSize = bufSize;
        Title = Titles[_stopped ? 1 : 0];

        _scrollBar = StandardScrollBar(ScrollBarParts.sbVertical | ScrollBarParts.sbHandleKeyboard);

        var r = GetExtent();
        r.Grow(-1, -1);
        _interior = new TTerminal(r, null, _scrollBar, _bufSize);
        Insert(_interior);
    }

    public void Toggle()
    {
        _stopped = !_stopped;
        Title = Titles[_stopped ? 1 : 0];
        Frame?.DrawView();
    }

    public void Print(ref TEvent ev)
    {
        if (ev.What != EventConstants.evNothing && !_stopped && _interior != null)
        {
            _interior.WriteLine($"Received event #{++_eventCount}");
            PrintEvent(_interior, ref ev);
        }
    }

    private static void PrintEvent(TTerminal terminal, ref TEvent ev)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TEvent {");
        sb.AppendLine($"  .what = 0x{ev.What:X4},");

        if ((ev.What & EventConstants.evMouse) != 0)
        {
            sb.AppendLine("  .mouse = MouseEventType {");
            sb.AppendLine($"    .where = TPoint {{ x={ev.Mouse.Where.X}, y={ev.Mouse.Where.Y} }},");
            sb.AppendLine($"    .eventFlags = 0x{ev.Mouse.EventFlags:X4},");
            sb.AppendLine($"    .controlKeyState = 0x{ev.Mouse.ControlKeyState:X4},");
            sb.AppendLine($"    .buttons = 0x{ev.Mouse.Buttons:X2},");
            sb.AppendLine($"    .wheel = 0x{ev.Mouse.Wheel:X2}");
            sb.AppendLine("  }");
        }

        if ((ev.What & EventConstants.evKeyboard) != 0)
        {
            sb.AppendLine("  .keyDown = KeyDownEvent {");
            sb.AppendLine($"    .keyCode = 0x{ev.KeyDown.KeyCode:X4},");
            sb.AppendLine($"    .charCode = {ev.KeyDown.CharCode}");
            if (ev.KeyDown.CharCode >= 32 && ev.KeyDown.CharCode < 127)
                sb.AppendLine($"               ('{(char)ev.KeyDown.CharCode}'),");
            else
                sb.AppendLine(",");
            sb.AppendLine($"    .controlKeyState = 0x{ev.KeyDown.ControlKeyState:X4}");
            sb.AppendLine("  }");
        }

        if ((ev.What & EventConstants.evCommand) != 0)
        {
            sb.AppendLine("  .message = MessageEvent {");
            sb.AppendLine($"    .command = {ev.Message.Command},");
            sb.AppendLine($"    .infoPtr = {ev.Message.InfoPtr}");
            sb.AppendLine("  }");
        }

        sb.AppendLine("}");

        terminal.Write(sb.ToString());
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast && ev.Message.Command == EventViewCommands.cmFndEventView)
        {
            ClearEvent(ref ev);
        }
    }

    public bool IsStopped => _stopped;
}
