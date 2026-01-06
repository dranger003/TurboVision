using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A monochrome color selector using radio buttons.
/// </summary>
public class TMonoSelector : TCluster
{
    private const string Button = " ( ) ";

    public TMonoSelector(TRect bounds) : base(bounds,
        new TSItem(MonoColors.Normal,
        new TSItem(MonoColors.Highlight,
        new TSItem(MonoColors.Underline,
        new TSItem(MonoColors.Inverse, null)))))
    {
        EventMask |= EventConstants.evBroadcast;
    }

    public override void Draw()
    {
        DrawBox(Button, '\u2022');
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == ColorCommands.cmColorSet)
        {
            Value = (uint)ev.Message.InfoInt;
            DrawView();
        }
    }

    public override bool Mark(int item)
    {
        return MonoColors.Values[item] == Value;
    }

    private void NewColor()
    {
        SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmColorForegroundChanged,
            (int)(Value & 0x0F));
        SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmColorBackgroundChanged,
            (int)((Value >> 4) & 0x0F));
    }

    public override void Press(int item)
    {
        Value = MonoColors.Values[item];
        NewColor();
    }

    public override void MovedTo(int item)
    {
        Value = MonoColors.Values[item];
        NewColor();
    }

    private static void SendMessage(TGroup? owner, ushort what, ushort command, int infoInt)
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
