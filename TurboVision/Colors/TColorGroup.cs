namespace TurboVision.Colors;

/// <summary>
/// A group of color items.
/// </summary>
public class TColorGroup
{
    /// <summary>
    /// The display name of the group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The current selected item index within this group.
    /// </summary>
    public byte Index { get; set; }

    /// <summary>
    /// The first item in this group.
    /// </summary>
    public TColorItem? Items { get; set; }

    /// <summary>
    /// The next group in the chain, or null.
    /// </summary>
    public TColorGroup? Next { get; set; }

    public TColorGroup(string name, TColorItem? items = null, TColorGroup? next = null)
    {
        Name = name;
        Items = items;
        Next = next;
    }

    /// <summary>
    /// Adds an item to the last group in the chain.
    /// </summary>
    public static TColorGroup operator +(TColorGroup g, TColorItem i)
    {
        var grp = g;
        while (grp.Next != null)
        {
            grp = grp.Next;
        }

        if (grp.Items == null)
        {
            grp.Items = i;
        }
        else
        {
            var cur = grp.Items;
            while (cur.Next != null)
            {
                cur = cur.Next;
            }
            cur.Next = i;
        }
        return g;
    }

    /// <summary>
    /// Chains two groups together.
    /// </summary>
    public static TColorGroup operator +(TColorGroup g1, TColorGroup g2)
    {
        var cur = g1;
        while (cur.Next != null)
        {
            cur = cur.Next;
        }
        cur.Next = g2;
        return g1;
    }
}

/// <summary>
/// Stores color indices for persistence.
/// </summary>
public class TColorIndex
{
    public byte GroupIndex { get; set; }
    public byte ColorSize { get; set; }
    public byte[] ColorIndices { get; set; } = new byte[256];
}
