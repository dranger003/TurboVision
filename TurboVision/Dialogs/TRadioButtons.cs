using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Radio button group (mutually exclusive selection).
/// </summary>
public class TRadioButtons : TCluster
{
    private const string Button = " ( ) ";

    public TRadioButtons(TRect bounds, TSItem? strings) : base(bounds, strings)
    {
    }

    public override void Draw()
    {
        DrawBox(Button, '●');
    }

    public override bool Mark(int item)
    {
        return item == (int)Value;
    }

    public override void MovedTo(int item)
    {
        base.MovedTo(item);
    }

    public override void Press(int item)
    {
        Value = (uint)item;
        DrawView();
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        base.SetData(rec);
        Sel = (int)Value;
    }
}
