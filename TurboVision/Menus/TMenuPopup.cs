using TurboVision.Core;

namespace TurboVision.Menus;

/// <summary>
/// Context/popup menu.
/// </summary>
public class TMenuPopup : TMenuBox
{
    public TMenuPopup(TRect bounds, TMenu? menu, TMenuView? parent = null)
        : base(bounds, menu, parent)
    {
    }

    public override ushort Execute()
    {
        // TODO: Implement popup menu execution
        return base.Execute();
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            // TODO: Handle mouse outside popup (dismiss)
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            if (ev.KeyDown.KeyCode == KeyConstants.kbEsc)
            {
                ClearEvent(ref ev);
                // TODO: Dismiss popup
            }
        }
    }
}
