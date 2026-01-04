using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Checkbox group (multiple selection).
/// Each checkbox can be toggled independently.
/// </summary>
public class TCheckBoxes : TCluster
{
    private const string Button = " [ ] ";

    public TCheckBoxes(TRect bounds, TSItem? strings) : base(bounds, strings)
    {
    }

    public override void Draw()
    {
        DrawBox(Button, 'X');
    }

    public override bool Mark(int item)
    {
        return (Value & (1u << item)) != 0;
    }

    public override void Press(int item)
    {
        Value ^= (1u << item);
    }
}
