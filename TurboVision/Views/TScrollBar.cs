using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Scrollbar widget.
/// </summary>
public class TScrollBar : TView
{
    private static readonly byte[] DefaultPalette = [0x04, 0x05, 0x05];

    // Scroll bar character sets
    public static char[] VChars { get; set; } = ['▲', '▼', '░', '░', '█'];
    public static char[] HChars { get; set; } = ['◄', '►', '░', '░', '█'];

    public int Value { get; set; }
    public int MinVal { get; set; }
    public int MaxVal { get; set; }
    public int PgStep { get; set; } = 1;
    public int ArStep { get; set; } = 1;
    public char[] Chars { get; set; }

    public TScrollBar(TRect bounds) : base(bounds)
    {
        if (Size.X == 1)
        {
            Chars = (char[])VChars.Clone();
            GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        }
        else
        {
            Chars = (char[])HChars.Clone();
            GrowMode = GrowFlags.gfGrowLoY | GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        }

        Options |= OptionFlags.ofPostProcess;
    }

    public override void Draw()
    {
        // TODO: Implement scrollbar drawing
        var b = new TDrawBuffer();
        var color = GetColor(0x0301);

        b.MoveChar(0, '░', color.Normal, Size.X * Size.Y);
        WriteBuf(0, 0, Size.X, Size.Y, b);
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
            // TODO: Handle mouse click on scrollbar parts
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle keyboard navigation
        }
    }

    public void SetParams(int aValue, int aMin, int aMax, int aPgStep, int aArStep)
    {
        MaxVal = aMax;
        MinVal = aMin;
        PgStep = aPgStep;
        ArStep = aArStep;
        SetValue(aValue);
    }

    public void SetRange(int aMin, int aMax)
    {
        SetParams(Value, aMin, aMax, PgStep, ArStep);
    }

    public void SetStep(int aPgStep, int aArStep)
    {
        SetParams(Value, MinVal, MaxVal, aPgStep, aArStep);
    }

    public void SetValue(int aValue)
    {
        Value = Math.Max(MinVal, Math.Min(MaxVal, aValue));
        DrawView();
        // TODO: Notify owner of change
    }

    public virtual void ScrollDraw()
    {
        // TODO: Implement scroll indicator drawing
    }

    public virtual int ScrollStep(int part)
    {
        return part switch
        {
            ScrollBarParts.sbUpArrow or ScrollBarParts.sbLeftArrow => -ArStep,
            ScrollBarParts.sbDownArrow or ScrollBarParts.sbRightArrow => ArStep,
            ScrollBarParts.sbPageUp or ScrollBarParts.sbPageLeft => -PgStep,
            ScrollBarParts.sbPageDown or ScrollBarParts.sbPageRight => PgStep,
            _ => 0
        };
    }

    public void DrawPos(int pos)
    {
        // TODO: Implement position indicator drawing
    }

    public int GetPos()
    {
        // TODO: Calculate position based on value
        return 0;
    }

    public int GetSize()
    {
        return Size.X == 1 ? Size.Y : Size.X;
    }

    private int GetPartCode()
    {
        // TODO: Determine which part was clicked
        return 0;
    }
}
