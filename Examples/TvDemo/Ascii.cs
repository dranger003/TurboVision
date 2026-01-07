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
/// Provides CP437 to Unicode character mapping.
/// </summary>
internal static class Cp437
{
    /// <summary>
    /// Maps CP437 byte values (0-255) to their Unicode equivalents.
    /// </summary>
    public static readonly char[] ToUnicode =
    [
        // 0x00-0x1F: Control characters displayed as symbols in CP437
        '\u0000', '\u263A', '\u263B', '\u2665', '\u2666', '\u2663', '\u2660', '\u2022',
        '\u25D8', '\u25CB', '\u25D9', '\u2642', '\u2640', '\u266A', '\u266B', '\u263C',
        '\u25BA', '\u25C4', '\u2195', '\u203C', '\u00B6', '\u00A7', '\u25AC', '\u21A8',
        '\u2191', '\u2193', '\u2192', '\u2190', '\u221F', '\u2194', '\u25B2', '\u25BC',
        // 0x20-0x7E: Standard ASCII (space through tilde)
        ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
        '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
        '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
        'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u2302',
        // 0x80-0x9F: Extended ASCII - international characters
        '\u00C7', '\u00FC', '\u00E9', '\u00E2', '\u00E4', '\u00E0', '\u00E5', '\u00E7',
        '\u00EA', '\u00EB', '\u00E8', '\u00EF', '\u00EE', '\u00EC', '\u00C4', '\u00C5',
        '\u00C9', '\u00E6', '\u00C6', '\u00F4', '\u00F6', '\u00F2', '\u00FB', '\u00F9',
        '\u00FF', '\u00D6', '\u00DC', '\u00A2', '\u00A3', '\u00A5', '\u20A7', '\u0192',
        // 0xA0-0xBF: Extended ASCII - more characters and box drawing
        '\u00E1', '\u00ED', '\u00F3', '\u00FA', '\u00F1', '\u00D1', '\u00AA', '\u00BA',
        '\u00BF', '\u2310', '\u00AC', '\u00BD', '\u00BC', '\u00A1', '\u00AB', '\u00BB',
        '\u2591', '\u2592', '\u2593', '\u2502', '\u2524', '\u2561', '\u2562', '\u2556',
        '\u2555', '\u2563', '\u2551', '\u2557', '\u255D', '\u255C', '\u255B', '\u2510',
        // 0xC0-0xDF: Box drawing characters
        '\u2514', '\u2534', '\u252C', '\u251C', '\u2500', '\u253C', '\u255E', '\u255F',
        '\u255A', '\u2554', '\u2569', '\u2566', '\u2560', '\u2550', '\u256C', '\u2567',
        '\u2568', '\u2564', '\u2565', '\u2559', '\u2558', '\u2552', '\u2553', '\u256B',
        '\u256A', '\u2518', '\u250C', '\u2588', '\u2584', '\u258C', '\u2590', '\u2580',
        // 0xE0-0xFF: Greek letters and mathematical symbols
        '\u03B1', '\u00DF', '\u0393', '\u03C0', '\u03A3', '\u03C3', '\u00B5', '\u03C4',
        '\u03A6', '\u0398', '\u03A9', '\u03B4', '\u221E', '\u03C6', '\u03B5', '\u2229',
        '\u2261', '\u00B1', '\u2265', '\u2264', '\u2320', '\u2321', '\u00F7', '\u2248',
        '\u00B0', '\u2219', '\u00B7', '\u221A', '\u207F', '\u00B2', '\u25A0', '\u00A0',
    ];
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
                int charIndex = 32 * y + x;
                buf.MoveChar(x, Cp437.ToUnicode[charIndex], color, 1);
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

        char displayChar = _asciiChar == 0 ? ' ' : Cp437.ToUnicode[_asciiChar];
        string str = $"  Char: {displayChar} Decimal: {_asciiChar,3} Hex {_asciiChar,2:X}     ";

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
