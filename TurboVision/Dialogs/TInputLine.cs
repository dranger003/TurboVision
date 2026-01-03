using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Single-line text input field.
/// </summary>
public class TInputLine : TView
{
    private static readonly byte[] DefaultPalette = [0x13, 0x13, 0x14, 0x15];

    // Limit modes
    public const ushort ilMaxBytes = 0;
    public const ushort ilMaxWidth = 1;
    public const ushort ilMaxChars = 2;

    public string Data { get; set; } = "";
    public int MaxLen { get; set; }
    public int MaxWidth { get; set; }
    public int MaxChars { get; set; }
    public int CurPos { get; set; }
    public int FirstPos { get; set; }
    public int SelStart { get; set; }
    public int SelEnd { get; set; }

    public TInputLine(TRect bounds, int limit, ushort limitMode = ilMaxBytes) : base(bounds)
    {
        MaxLen = limit;
        MaxWidth = limit;
        MaxChars = limit;

        State |= StateFlags.sfCursorVis;
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofValidate;
    }

    public override int DataSize()
    {
        return MaxLen + 1;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        byte colorIndex = GetState(StateFlags.sfFocused) ? (byte)2 : (byte)1;
        var color = GetColor((ushort)((colorIndex << 8) | colorIndex));

        b.MoveChar(0, ' ', color.Normal, Size.X);
        b.MoveStr(1, Data, color.Normal);

        // TODO: Draw selection, arrows if scrolled

        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override void GetData(Span<byte> rec)
    {
        // TODO: Copy string data to rec
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            // TODO: Handle mouse selection
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle text editing
        }
    }

    public void SelectAll(bool enable, bool scroll = true)
    {
        if (enable)
        {
            SelStart = 0;
            SelEnd = Data.Length;
        }
        else
        {
            SelStart = 0;
            SelEnd = 0;
        }
        CurPos = SelEnd;
        if (scroll)
        {
            FirstPos = 0;
        }
        DrawView();
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        // TODO: Set string data from rec
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfFocused) != 0)
        {
            DrawView();
        }
    }

    public override bool Valid(ushort cmd)
    {
        // TODO: Validate input
        return true;
    }
}
