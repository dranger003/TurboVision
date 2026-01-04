using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Menu item data structure.
/// </summary>
public class TMenuItem
{
    public TMenuItem? Next { get; set; }
    public string? Name { get; set; }
    public ushort Command { get; set; }
    public bool Disabled { get; set; }
    public TKey KeyCode { get; set; }
    public ushort HelpCtx { get; set; }

    // Union: either param string or submenu
    public string? Param { get; set; }
    public TMenu? SubMenu { get; set; }

    public bool IsSubMenu
    {
        get { return SubMenu != null; }
    }

    public bool IsSeparator
    {
        get { return Name == null && Command == 0; }
    }

    public TMenuItem(
        string? name,
        ushort command,
        TKey keyCode,
        ushort helpCtx = HelpContexts.hcNoContext,
        string? param = null,
        TMenuItem? next = null)
    {
        Name = name;
        Command = command;
        Disabled = !TView.CommandEnabled(command);
        KeyCode = keyCode;
        HelpCtx = helpCtx;
        Param = param;
        Next = next;
    }

    public TMenuItem(
        string? name,
        TKey keyCode,
        TMenu? subMenu,
        ushort helpCtx = HelpContexts.hcNoContext,
        TMenuItem? next = null)
    {
        Name = name;
        Command = 0;
        Disabled = !TView.CommandEnabled(0);
        KeyCode = keyCode;
        HelpCtx = helpCtx;
        SubMenu = subMenu;
        Next = next;
    }

    public void Append(TMenuItem item)
    {
        var last = this;
        while (last.Next != null)
        {
            last = last.Next;
        }
        last.Next = item;
    }

    /// <summary>
    /// Creates a separator line.
    /// </summary>
    public static TMenuItem NewLine()
    {
        return new TMenuItem(null, 0, default, HelpContexts.hcNoContext, null, null);
    }
}
