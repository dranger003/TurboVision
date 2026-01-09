using System.Text;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Editors;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A window that displays TEvents received by the application.
/// Uses TTerminal for scrollable text output with symbolic constant names.
/// Line-by-line port of evntview.h/evntview.cpp from upstream tvision.
/// </summary>
public class TEventViewer : TWindow
{
    private static readonly string[] Titles = ["Event Viewer", "Event Viewer (Stopped)"];

    private bool _stopped;
    private ulong _eventCount;
    private ushort _bufSize;
    private TTerminal? _interior;
    private TScrollBar? _scrollBar;
    private OTStream? _out;

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

        _out = new OTStream(_interior!);
    }

    public override void ShutDown()
    {
        _out?.Dispose();
        _interior = null;
        _scrollBar = null;
        _out = null;
        base.ShutDown();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Title = null; // So that TWindow doesn't delete it
        }
        base.Dispose(disposing);
    }

    public void Toggle()
    {
        _stopped = !_stopped;
        Title = Titles[_stopped ? 1 : 0];
        Frame?.DrawView();
    }

    public void Print(ref TEvent ev)
    {
        if (ev.What != EventConstants.evNothing && !_stopped && _out != null)
        {
            Lock();
            _out.Write($"Received event #{++_eventCount}\n");
            PrintEvent(_out, ref ev);
            _out.Flush();
            Unlock();
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == TvCmds.cmFndEventView)
        {
            ClearEvent(ref ev);
        }
    }

    private static void AppendConstantWithName(StringBuilder sb, ushort value,
        Func<ushort, string> printFunc)
    {
        sb.Append($"0x{value:X4}");
        string constStr = printFunc(value);
        if (constStr[0] != '0')
        {
            sb.Append($" ({constStr})");
        }
    }

    private static void PrintEvent(OTStream outStream, ref TEvent ev)
    {
        var sb = new StringBuilder();
        sb.Append("TEvent {\n");
        sb.Append("  .what = ");
        AppendConstantWithName(sb, ev.What, PrintConstants.PrintEventCode);
        sb.Append(",\n");

        if ((ev.What & EventConstants.evMouse) != 0)
        {
            sb.Append("  .mouse = MouseEventType {\n");
            sb.Append("    .where = TPoint {\n");
            sb.Append($"      .x = {ev.Mouse.Where.X}\n");
            sb.Append($"      .y = {ev.Mouse.Where.Y}\n");
            sb.Append("    },\n");
            sb.Append("    .eventFlags = ");
            AppendConstantWithName(sb, ev.Mouse.EventFlags, PrintConstants.PrintMouseEventFlags);
            sb.Append(",\n");
            sb.Append("    .controlKeyState = ");
            AppendConstantWithName(sb, ev.Mouse.ControlKeyState, PrintConstants.PrintControlKeyState);
            sb.Append(",\n");
            sb.Append("    .buttons = ");
            AppendConstantWithName(sb, ev.Mouse.Buttons, PrintConstants.PrintMouseButtonState);
            sb.Append(",\n");
            sb.Append("    .wheel = ");
            AppendConstantWithName(sb, ev.Mouse.Wheel, PrintConstants.PrintMouseWheelState);
            sb.Append("\n");
            sb.Append("  }\n");
        }

        if ((ev.What & EventConstants.evKeyboard) != 0)
        {
            byte charCode = ev.KeyDown.CharCode;
            sb.Append("  .keyDown = KeyDownEvent {\n");
            sb.Append("    .keyCode = ");
            AppendConstantWithName(sb, ev.KeyDown.KeyCode, PrintConstants.PrintKeyCode);
            sb.Append(",\n");
            sb.Append("    .charScan = CharScanType {\n");
            sb.Append($"      .charCode = {(int)charCode}");
            if (charCode >= 32 && charCode < 127)
                sb.Append($" ('{(char)charCode}')");
            sb.Append(",\n");
            sb.Append($"      .scanCode = {(int)ev.KeyDown.ScanCode}\n");
            sb.Append("    },\n");
            sb.Append("    .controlKeyState = ");
            AppendConstantWithName(sb, ev.KeyDown.ControlKeyState, PrintConstants.PrintControlKeyState);
            sb.Append(",\n");
            sb.Append("    .text = {");
            bool first = true;
            var text = ev.KeyDown.GetText();
            for (int i = 0; i < ev.KeyDown.TextLength; i++)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");
                sb.Append($"0x{(int)text[i]:X}");
            }
            sb.Append("},\n");
            sb.Append($"    .textLength = {ev.KeyDown.TextLength}\n");
            sb.Append("  }\n");
        }

        if ((ev.What & EventConstants.evCommand) != 0)
        {
            sb.Append("  .message = MessageEvent {\n");
            sb.Append($"    .command = {ev.Message.Command},\n");
            sb.Append($"    .infoPtr = {ev.Message.InfoPtr}\n");
            sb.Append("  }\n");
        }

        sb.Append("}\n");
        outStream.Write(sb.ToString());
    }

    public bool IsStopped => _stopped;
}
