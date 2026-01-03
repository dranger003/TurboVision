namespace TurboVision.Menus;

/// <summary>
/// Menu container holding a list of menu items.
/// </summary>
public class TMenu
{
    public TMenuItem? Items { get; set; }
    public TMenuItem? Default { get; set; }

    public TMenu()
    {
    }

    public TMenu(TMenuItem itemList)
    {
        Items = itemList;
        Default = itemList;
    }

    public TMenu(TMenuItem itemList, TMenuItem defaultItem)
    {
        Items = itemList;
        Default = defaultItem;
    }
}
