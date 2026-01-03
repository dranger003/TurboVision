using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Base class for grouped controls (checkboxes, radio buttons).
/// </summary>
public abstract class TCluster : TView
{
    private static readonly byte[] DefaultPalette = [0x10, 0x11, 0x12, 0x12, 0x1F];

    protected uint Value { get; set; }
    protected uint EnableMask { get; set; } = 0xFFFFFFFF;
    protected int Sel { get; set; }
    protected List<string> Strings { get; } = [];

    protected TCluster(TRect bounds, TSItem? strings) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofPreProcess | OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;

        while (strings != null)
        {
            Strings.Add(strings.Value ?? "");
            strings = strings.Next;
        }
    }

    public override int DataSize()
    {
        return sizeof(uint);
    }

    public void DrawBox(string icon, char marker)
    {
        // TODO: Draw cluster items with box icons
        var b = new TDrawBuffer();
        var color = GetColor(0x0101);

        b.MoveChar(0, ' ', color.Normal, Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public void DrawMultiBox(string icon, string marker)
    {
        // TODO: Draw cluster items with multi-state icons
    }

    public override void GetData(Span<byte> rec)
    {
        if (rec.Length >= sizeof(uint))
        {
            BitConverter.TryWriteBytes(rec, Value);
        }
    }

    public override ushort GetHelpCtx()
    {
        if (HelpCtx == HelpContexts.hcNoContext)
        {
            return (ushort)(HelpCtx + Sel);
        }
        return base.GetHelpCtx();
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
            // TODO: Handle mouse click on items
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle keyboard navigation
        }
    }

    public virtual bool Mark(int item)
    {
        return (Value & (1u << item)) != 0;
    }

    public virtual byte MultiMark(int item)
    {
        return Mark(item) ? (byte)1 : (byte)0;
    }

    public virtual void Press(int item)
    {
        // Override in derived classes
    }

    public virtual void MovedTo(int item)
    {
        Sel = item;
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length >= sizeof(uint))
        {
            Value = BitConverter.ToUInt32(rec);
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfSelected) != 0)
        {
            DrawView();
        }
    }

    public virtual void SetButtonState(uint mask, bool enable)
    {
        if (enable)
        {
            EnableMask |= mask;
        }
        else
        {
            EnableMask &= ~mask;
        }
        DrawView();
    }

    public bool ButtonState(int item)
    {
        return (EnableMask & (1u << item)) != 0;
    }

    protected int Column(int item)
    {
        // TODO: Calculate column for item
        return 0;
    }

    protected int Row(int item)
    {
        // TODO: Calculate row for item
        return item;
    }

    protected int FindSel(TPoint p)
    {
        // TODO: Find item at point
        return -1;
    }

    protected void MoveSel(int from, int to)
    {
        if (to != from)
        {
            Sel = to;
            MovedTo(to);
            DrawView();
        }
    }
}
