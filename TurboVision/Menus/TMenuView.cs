using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Base class for menu views.
/// </summary>
public class TMenuView : TView
{
    private static readonly byte[] DefaultPalette = [0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

    protected TMenuView? ParentMenu { get; set; }
    protected TMenu? Menu { get; set; }
    protected TMenuItem? Current { get; set; }
    protected bool PutClickEventOnExit { get; set; } = true;

    public TMenuView(TRect bounds, TMenu? menu, TMenuView? parent = null) : base(bounds)
    {
        ParentMenu = parent;
        Menu = menu;
        EventMask |= EventConstants.evBroadcast;
    }

    public TMenuView(TRect bounds) : base(bounds)
    {
        EventMask |= EventConstants.evBroadcast;
    }

    public override ushort Execute()
    {
        // TODO: Implement menu execution
        return 0;
    }

    public TMenuItem? FindItem(char shortcut)
    {
        var p = Menu?.Items;
        while (p != null)
        {
            if (p.Name != null)
            {
                int idx = p.Name.IndexOf('~');
                if (idx >= 0 && idx + 1 < p.Name.Length)
                {
                    if (char.ToUpperInvariant(p.Name[idx + 1]) == char.ToUpperInvariant(shortcut))
                    {
                        return p;
                    }
                }
            }
            p = p.Next;
        }
        return null;
    }

    public TMenuItem? FindItem(string shortcut)
    {
        return shortcut.Length > 0 ? FindItem(shortcut[0]) : null;
    }

    public virtual TRect GetItemRect(TMenuItem? item)
    {
        return new TRect();
    }

    public override ushort GetHelpCtx()
    {
        return Current?.HelpCtx ?? base.GetHelpCtx();
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
            // TODO: Handle mouse click
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle keyboard navigation
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            // TODO: Handle broadcasts
        }
    }

    public TMenuItem? HotKey(TKey key)
    {
        return FindHotKey(Menu?.Items, key);
    }

    public TMenuView? NewSubView(TRect bounds, TMenu? menu, TMenuView? parent)
    {
        return new TMenuBox(bounds, menu, parent);
    }

    protected TMenuItem? FindHotKey(TMenuItem? p, TKey key)
    {
        while (p != null)
        {
            if (p.KeyCode.KeyCode == key.KeyCode)
            {
                return p;
            }
            if (p.SubMenu != null)
            {
                var result = FindHotKey(p.SubMenu.Items, key);
                if (result != null)
                {
                    return result;
                }
            }
            p = p.Next;
        }
        return null;
    }

    private void NextItem()
    {
        // TODO: Move to next item
    }

    private void PrevItem()
    {
        // TODO: Move to previous item
    }

    private void TrackKey(bool findNext)
    {
        // TODO: Track keyboard input
    }

    private bool MouseInOwner(TEvent ev)
    {
        // TODO: Check if mouse is in owner
        return false;
    }

    private bool MouseInMenus(TEvent ev)
    {
        // TODO: Check if mouse is in menus
        return false;
    }

    private void TrackMouse(TEvent ev, ref bool mouseActive)
    {
        // TODO: Track mouse movement
    }

    private TMenuView? TopMenu()
    {
        var p = this;
        while (p.ParentMenu != null)
        {
            p = p.ParentMenu;
        }
        return p;
    }

    private bool UpdateMenu(TMenu? menu)
    {
        // TODO: Update menu state
        return false;
    }

    private void DoSelect(TEvent ev)
    {
        // TODO: Handle selection
    }
}
