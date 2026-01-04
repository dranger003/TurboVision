using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Base class for menu views.
/// </summary>
public class TMenuView : TView
{
    private static readonly byte[] DefaultPalette = [0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

    // Alt key scan code to character mapping
    private static readonly string AltCodes1 = "QWERTYUIOP\0\0\0\0ASDFGHJKL\0\0\0\0\0ZXCVBNM";
    private static readonly string AltCodes2 = "1234567890-=";

    private enum MenuAction { DoNothing, DoSelect, DoReturn }

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
        if (Menu == null)
        {
            return 0;
        }

        bool autoSelect = false;
        bool firstEvent = true;
        MenuAction action;
        ushort result = 0;
        TMenuItem? itemShown = null;
        TMenuItem? lastTargetItem = null;
        bool mouseActive = false;

        Current = Menu.Default;
        TEvent e = default;

        do
        {
            action = MenuAction.DoNothing;
            GetEvent(ref e);

            switch (e.What)
            {
                case EventConstants.evMouseDown:
                    if (MouseInView(e.Mouse.Where) || MouseInOwner(e))
                    {
                        TrackMouse(e, ref mouseActive);
                        // autoSelect makes it possible to open the selected submenu directly
                        // on a MouseDown event. This should be avoided when said submenu was
                        // just closed by clicking on its name, or when this is not a menu bar.
                        if (Size.Y == 1)
                        {
                            autoSelect = Current == null || lastTargetItem != Current;
                        }
                        // A submenu will close if the MouseDown event takes place on the
                        // parent menu, except when this submenu has just been opened.
                        else if (!firstEvent && MouseInOwner(e))
                        {
                            action = MenuAction.DoReturn;
                        }
                    }
                    else
                    {
                        // Menu gets closed by a click outside its bounds.
                        // Let the event reach the view recovering focus.
                        if (PutClickEventOnExit)
                        {
                            PutEvent(e);
                        }
                        action = MenuAction.DoReturn;
                    }
                    break;

                case EventConstants.evMouseUp:
                    TrackMouse(e, ref mouseActive);
                    if (MouseInOwner(e))
                    {
                        Current = Menu.Default;
                    }
                    else if (Current != null)
                    {
                        if (Current.Name != null)
                        {
                            if (Current != lastTargetItem)
                            {
                                action = MenuAction.DoSelect;
                            }
                            else if (Size.Y == 1)
                            {
                                // If a menu bar entry was closed, exit and stop listening for events.
                                action = MenuAction.DoReturn;
                            }
                            else
                            {
                                // MouseUp won't open up a submenu that was just closed by clicking on its name.
                                action = MenuAction.DoNothing;
                                // But the next one will.
                                lastTargetItem = null;
                            }
                        }
                    }
                    else if (mouseActive && !MouseInView(e.Mouse.Where))
                    {
                        action = MenuAction.DoReturn;
                    }
                    else if (Size.Y != 1)
                    {
                        // When MouseUp happens inside the Box but not on a highlightable entry
                        // either the default or the first entry will be automatically highlighted.
                        Current = Menu.Default;
                        if (Current == null)
                        {
                            Current = Menu.Items;
                        }
                        action = MenuAction.DoNothing;
                    }
                    break;

                case EventConstants.evMouseMove:
                    if (e.Mouse.Buttons != 0)
                    {
                        TrackMouse(e, ref mouseActive);
                        if (!(MouseInView(e.Mouse.Where) || MouseInOwner(e)) && MouseInMenus(e))
                        {
                            action = MenuAction.DoReturn;
                        }
                        // A menu bar entry closed by clicking on its name stays highlighted
                        // until MouseUp. If mouse drag is then performed and a different
                        // entry is selected, it will open up automatically.
                        else if (Size.Y == 1 && mouseActive && Current != lastTargetItem)
                        {
                            autoSelect = true;
                        }
                    }
                    break;

                case EventConstants.evKeyDown:
                    switch (e.KeyDown.KeyCode)
                    {
                        case KeyConstants.kbUp:
                        case KeyConstants.kbDown:
                            if (Size.Y != 1)
                            {
                                TrackKey(e.KeyDown.KeyCode == KeyConstants.kbDown);
                            }
                            else if (e.KeyDown.KeyCode == KeyConstants.kbDown)
                            {
                                autoSelect = true;
                            }
                            break;

                        case KeyConstants.kbLeft:
                        case KeyConstants.kbRight:
                            if (Size.Y == 1)
                            {
                                TrackKey(e.KeyDown.KeyCode == KeyConstants.kbRight);
                            }
                            else if (ParentMenu != null)
                            {
                                action = MenuAction.DoReturn;
                            }
                            break;

                        case KeyConstants.kbHome:
                        case KeyConstants.kbEnd:
                            if (Size.Y != 1)
                            {
                                Current = Menu.Items;
                                if (e.KeyDown.KeyCode == KeyConstants.kbEnd)
                                {
                                    TrackKey(false);
                                }
                            }
                            break;

                        case KeyConstants.kbEnter:
                            if (Size.Y == 1)
                            {
                                autoSelect = true;
                            }
                            action = MenuAction.DoSelect;
                            break;

                        case KeyConstants.kbEsc:
                            action = MenuAction.DoReturn;
                            if (ParentMenu == null || ParentMenu.Size.Y != 1)
                            {
                                ClearEvent(ref e);
                            }
                            break;

                        default:
                            var target = this;
                            TMenuItem? p;
                            char altChar = GetAltChar(e.KeyDown.KeyCode);

                            if (altChar != '\0')
                            {
                                target = TopMenu()!;
                                p = target.FindItem(altChar);
                            }
                            else
                            {
                                // Try to find item by the typed character
                                var text = GetKeyText(e.KeyDown.KeyCode);
                                p = !string.IsNullOrEmpty(text) ? FindItem(text) : null;
                            }

                            if (p == null)
                            {
                                p = TopMenu()!.HotKey(e.KeyDown.ToKey());
                                if (p != null && CommandEnabled(p.Command))
                                {
                                    result = p.Command;
                                    action = MenuAction.DoReturn;
                                }
                            }
                            else if (target == this)
                            {
                                if (Size.Y == 1)
                                {
                                    autoSelect = true;
                                }
                                action = MenuAction.DoSelect;
                                Current = p;
                            }
                            else if (ParentMenu != target || ParentMenu.Current != p)
                            {
                                action = MenuAction.DoReturn;
                            }
                            break;
                    }
                    break;

                case EventConstants.evCommand:
                    if (e.Message.Command == CommandConstants.cmMenu)
                    {
                        autoSelect = false;
                        lastTargetItem = null;
                        if (ParentMenu != null)
                        {
                            action = MenuAction.DoReturn;
                        }
                    }
                    else
                    {
                        action = MenuAction.DoReturn;
                    }
                    break;
            }

            // If a submenu was closed by clicking on its name, and the mouse is dragged
            // to another menu entry, then the submenu will be opened the next time it
            // is hovered over.
            if (lastTargetItem != Current)
            {
                lastTargetItem = null;
            }

            if (itemShown != Current)
            {
                itemShown = Current;
                DrawView();
            }

            if ((action == MenuAction.DoSelect || (action == MenuAction.DoNothing && autoSelect)) &&
                Current != null &&
                Current.Name != null)
            {
                if (Current.Command == 0 && !Current.Disabled)
                {
                    // Open submenu
                    if ((e.What & (EventConstants.evMouseDown | EventConstants.evMouseMove)) != 0)
                    {
                        PutEvent(e);
                    }

                    var r = GetItemRect(Current);
                    r = new TRect(
                        r.A.X + Origin.X,
                        r.B.Y + Origin.Y,
                        Owner?.Size.X ?? r.B.X,
                        Owner?.Size.Y ?? r.B.Y
                    );
                    if (Size.Y == 1)
                    {
                        r = new TRect(r.A.X - 1, r.A.Y, r.B.X, r.B.Y);
                    }

                    var subView = TopMenu()!.NewSubView(r, Current.SubMenu, this);
                    if (subView != null && Owner != null)
                    {
                        result = Owner.ExecView(subView);
                        subView.Dispose();
                    }
                    lastTargetItem = Current;
                    Menu.Default = Current;
                }
                else if (action == MenuAction.DoSelect)
                {
                    result = Current.Command;
                }
            }

            if (result != 0 && CommandEnabled(result))
            {
                action = MenuAction.DoReturn;
                ClearEvent(ref e);
            }
            else
            {
                result = 0;
            }

            firstEvent = false;

        } while (action != MenuAction.DoReturn);

        // Pass unhandled events back up (upstream behavior)
        if (e.What != EventConstants.evNothing &&
            (ParentMenu != null || e.What == EventConstants.evCommand))
        {
            PutEvent(e);
        }

        if (Current != null)
        {
            Menu.Default = Current;
            Current = null;
            DrawView();
        }

        return result;
    }

    public TMenuItem? FindItem(char shortcut)
    {
        var p = Menu?.Items;
        while (p != null)
        {
            if (p.Name != null && !p.Disabled)
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
        var c = this;
        while (c != null && (c.Current == null || c.Current.HelpCtx == HelpContexts.hcNoContext || c.Current.Name == null))
        {
            c = c.ParentMenu;
        }
        if (c != null)
        {
            return c.Current!.HelpCtx;
        }
        return HelpCtx;
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (Menu == null)
        {
            return;
        }

        switch (ev.What)
        {
            case EventConstants.evMouseDown:
                DoSelect(ref ev);
                break;

            case EventConstants.evKeyDown:
                {
                    // Check for Alt+letter shortcuts
                    char altChar = GetAltChar(ev.KeyDown.KeyCode);
                    if (altChar != '\0' && FindItem(altChar) != null)
                    {
                        DoSelect(ref ev);
                    }
                    else
                    {
                        // Check for hotkeys (e.g., Alt-X for Quit)
                        var p = HotKey(ev.KeyDown.ToKey());
                        if (p != null && CommandEnabled(p.Command))
                        {
                            ev.What = EventConstants.evCommand;
                            ev.Message.Command = p.Command;
                            ev.Message.InfoPtr = 0;
                            PutEvent(ev);
                            ClearEvent(ref ev);
                        }
                    }
                }
                break;

            case EventConstants.evCommand:
                if (ev.Message.Command == CommandConstants.cmMenu)
                {
                    DoSelect(ref ev);
                }
                break;

            case EventConstants.evBroadcast:
                if (ev.Message.Command == CommandConstants.cmCommandSetChanged)
                {
                    if (UpdateMenu(Menu))
                    {
                        DrawView();
                    }
                }
                break;
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
            if (p.Name != null)
            {
                if (p.Command == 0)
                {
                    // Has submenu - search recursively
                    var found = FindHotKey(p.SubMenu?.Items, key);
                    if (found != null)
                    {
                        return found;
                    }
                }
                else if (!p.Disabled && p.KeyCode.KeyCode != KeyConstants.kbNoKey && p.KeyCode == key)
                {
                    return p;
                }
            }
            p = p.Next;
        }
        return null;
    }

    private void NextItem()
    {
        if (Menu == null)
        {
            return;
        }

        Current = Current?.Next;
        if (Current == null)
        {
            Current = Menu.Items;
        }
    }

    private void PrevItem()
    {
        if (Menu == null)
        {
            return;
        }

        TMenuItem? p = Current == Menu.Items ? null : Current;
        do
        {
            NextItem();
        } while (Current?.Next != p);
    }

    private void TrackKey(bool findNext)
    {
        if (Menu == null)
        {
            return;
        }

        if (Current == null)
        {
            Current = Menu.Items;
            if (!findNext)
            {
                PrevItem();
            }
            if (Current?.Name != null)
            {
                return;
            }
        }

        do
        {
            if (findNext)
            {
                NextItem();
            }
            else
            {
                PrevItem();
            }
        } while (Current?.Name == null);
    }

    private bool MouseInOwner(TEvent ev)
    {
        if (ParentMenu == null)
        {
            return false;
        }

        var mouse = ParentMenu.MakeLocal(ev.Mouse.Where);
        var r = ParentMenu.GetItemRect(ParentMenu.Current);
        return r.Contains(mouse);
    }

    private bool MouseInMenus(TEvent ev)
    {
        var p = ParentMenu;
        while (p != null && !p.MouseInView(ev.Mouse.Where))
        {
            p = p.ParentMenu;
        }
        return p != null;
    }

    private void TrackMouse(TEvent ev, ref bool mouseActive)
    {
        if (Menu == null)
        {
            return;
        }

        var mouse = MakeLocal(ev.Mouse.Where);
        for (Current = Menu.Items; Current != null; Current = Current.Next)
        {
            var r = GetItemRect(Current);
            if (r.Contains(mouse))
            {
                mouseActive = true;
                return;
            }
        }
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
        if (menu == null)
        {
            return false;
        }

        bool res = false;
        for (var p = menu.Items; p != null; p = p.Next)
        {
            if (p.Name != null)
            {
                if (p.Command == 0)
                {
                    // Has submenu
                    if (UpdateMenu(p.SubMenu))
                    {
                        res = true;
                    }
                }
                else
                {
                    bool commandState = CommandEnabled(p.Command);
                    if (p.Disabled == commandState)
                    {
                        p.Disabled = !commandState;
                        res = true;
                    }
                }
            }
        }
        return res;
    }

    private void DoSelect(ref TEvent ev)
    {
        PutEvent(ev);
        if (Owner != null)
        {
            ev.Message.Command = Owner.ExecView(this);
            if (ev.Message.Command != 0 && CommandEnabled(ev.Message.Command))
            {
                ev.What = EventConstants.evCommand;
                ev.Message.InfoPtr = 0;
                PutEvent(ev);
            }
        }
        ClearEvent(ref ev);
    }

    /// <summary>
    /// Gets the character for an Alt+key combination from the key code.
    /// </summary>
    private static char GetAltChar(ushort keyCode)
    {
        byte scanCode = (byte)(keyCode >> 8);
        byte charCode = (byte)(keyCode & 0xFF);

        // Alt keys have charCode == 0 and specific scan codes
        if (charCode != 0)
        {
            return '\0';
        }

        // Alt+Q through Alt+M (scan codes 0x10-0x32)
        if (scanCode >= 0x10 && scanCode < 0x10 + AltCodes1.Length)
        {
            return AltCodes1[scanCode - 0x10];
        }

        // Alt+1 through Alt+= (scan codes 0x78-0x83)
        if (scanCode >= 0x78 && scanCode < 0x78 + AltCodes2.Length)
        {
            return AltCodes2[scanCode - 0x78];
        }

        return '\0';
    }

    /// <summary>
    /// Gets the text character from a key code (for non-Alt keys).
    /// </summary>
    private static string GetKeyText(ushort keyCode)
    {
        byte charCode = (byte)(keyCode & 0xFF);
        if (charCode >= ' ' && charCode < 127)
        {
            return char.ToUpperInvariant((char)charCode).ToString();
        }
        return string.Empty;
    }
}
