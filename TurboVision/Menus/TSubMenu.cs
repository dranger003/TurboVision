using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Submenu item (menu item that contains a dropdown menu).
/// </summary>
public class TSubMenu : TMenuItem
{
    public TSubMenu(string? name, TKey keyCode, ushort helpCtx = HelpContexts.hcNoContext)
        : base(name, keyCode, null, helpCtx, null)
    {
        SubMenu = new TMenu();
    }

    /// <summary>
    /// Creates a submenu with the specified items.
    /// </summary>
    public TSubMenu(string? name, TKey keyCode, params TMenuItem[] items)
        : base(name, keyCode, null, HelpContexts.hcNoContext, null)
    {
        SubMenu = new TMenu();
        foreach (var item in items)
        {
            Add(item);
        }
    }

    /// <summary>
    /// Creates a submenu with a single item.
    /// </summary>
    public TSubMenu(string? name, TKey keyCode, TMenuItem item)
        : base(name, keyCode, null, HelpContexts.hcNoContext, null)
    {
        SubMenu = new TMenu();
        Add(item);
    }

    /// <summary>
    /// Adds a menu item to this submenu.
    /// </summary>
    public TSubMenu Add(TMenuItem item)
    {
        if (SubMenu != null)
        {
            if (SubMenu.Items == null)
            {
                SubMenu.Items = item;
                SubMenu.Default = item;
            }
            else
            {
                SubMenu.Items.Append(item);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds another submenu to this submenu.
    /// </summary>
    public TSubMenu Add(TSubMenu subMenu)
    {
        return Add((TMenuItem)subMenu);
    }
}
