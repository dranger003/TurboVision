using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// Command constants for the ASCII chart.
/// </summary>
public static class AsciiCommands
{
    public const ushort cmAsciiTableCmdBase = 910;
    public const ushort cmCharFocused = 0;
}

/// <summary>
/// A view that displays a 32x8 ASCII character table.
/// </summary>
public class TTable : TView
{
    public TTable(TRect bounds) : base(bounds)
    {
        EventMask |= EventConstants.evKeyboard;
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var color = GetColor(6).Normal;

        for (short y = 0; y < Size.Y; y++)
        {
            buf.MoveChar(0, ' ', color, Size.X);
            for (short x = 0; x < Size.X; x++)
            {
                buf.MoveChar(x, (char)(32 * y + x), color, 1);
            }
            WriteLine(0, y, Size.X, 1, buf);
        }
        ShowCursor();
    }

    private void CharFocused()
    {
        Message(Owner, EventConstants.evBroadcast,
            (ushort)(AsciiCommands.cmAsciiTableCmdBase + AsciiCommands.cmCharFocused),
            Cursor.X + 32 * Cursor.Y);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            do
            {
                if (MouseInView(ev.Mouse.Where))
                {
                    var spot = MakeLocal(ev.Mouse.Where);
                    SetCursor(spot.X, spot.Y);
                    CharFocused();
                }
            } while (MouseEvent(ref ev, EventConstants.evMouseMove));
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            switch (ev.KeyDown.KeyCode)
            {
                case KeyConstants.kbHome:
                    SetCursor(0, 0);
                    break;
                case KeyConstants.kbEnd:
                    SetCursor(Size.X - 1, Size.Y - 1);
                    break;
                case KeyConstants.kbUp:
                    if (Cursor.Y > 0)
                        SetCursor(Cursor.X, Cursor.Y - 1);
                    break;
                case KeyConstants.kbDown:
                    if (Cursor.Y < Size.Y - 1)
                        SetCursor(Cursor.X, Cursor.Y + 1);
                    break;
                case KeyConstants.kbLeft:
                    if (Cursor.X > 0)
                        SetCursor(Cursor.X - 1, Cursor.Y);
                    break;
                case KeyConstants.kbRight:
                    if (Cursor.X < Size.X - 1)
                        SetCursor(Cursor.X + 1, Cursor.Y);
                    break;
                default:
                    int c = ev.KeyDown.CharCode;
                    SetCursor(c % 32, c / 32);
                    break;
            }
            CharFocused();
            ClearEvent(ref ev);
        }
    }

    private static void Message(TGroup? owner, ushort what, ushort command, int infoInt)
    {
        if (owner == null) return;

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoInt }
        };

        owner.HandleEvent(ref ev);
    }
}

/// <summary>
/// A view that displays information about the currently selected ASCII character.
/// </summary>
public class TReport : TView
{
    private byte _asciiChar;

    public TReport(TRect bounds) : base(bounds)
    {
        _asciiChar = 0;
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var color = GetColor(6).Normal;

        char displayChar = _asciiChar == 0 ? (char)0x20 : (char)_asciiChar;
        string str = $"  Char: {displayChar} Decimal: {_asciiChar,3} Hex {_asciiChar:X2}     ";

        buf.MoveStr(0, str, color);
        WriteLine(0, 0, 32, 1, buf);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == AsciiCommands.cmAsciiTableCmdBase + AsciiCommands.cmCharFocused)
            {
                _asciiChar = (byte)ev.Message.InfoInt;
                DrawView();
            }
        }
    }
}

/// <summary>
/// A window containing the ASCII character chart and report.
/// </summary>
public class TAsciiChart : TWindow
{
    public TAsciiChart()
        : base(new TRect(0, 0, 34, 12), "ASCII Chart", WindowConstants.wnNoNumber)
    {
        Flags &= unchecked((byte)~(WindowFlags.wfGrow | WindowFlags.wfZoom));
        GrowMode = 0;
        Palette = WindowPalettes.wpGrayWindow;

        var r = GetExtent();
        r.Grow(-1, -1);
        r.A = new TPoint(r.A.X, r.B.Y - 1);
        var report = new TReport(r);
        report.Options |= OptionFlags.ofFramed;
        report.EventMask |= EventConstants.evBroadcast;
        Insert(report);

        r = GetExtent();
        r.Grow(-1, -1);
        r.B = new TPoint(r.B.X, r.B.Y - 2);
        var table = new TTable(r);
        table.Options |= OptionFlags.ofFramed | OptionFlags.ofSelectable;
        table.BlockCursor();
        Insert(table);
        table.Select();
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
    }
}
