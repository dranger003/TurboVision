namespace TurboVision.Colors;

/// <summary>
/// A single color item in a color group.
/// </summary>
public class TColorItem
{
    /// <summary>
    /// The display name of the color item.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The palette index this item affects.
    /// </summary>
    public byte Index { get; }

    /// <summary>
    /// The next item in the chain, or null.
    /// </summary>
    public TColorItem? Next { get; set; }

    public TColorItem(string name, byte index, TColorItem? next = null)
    {
        Name = name;
        Index = index;
        Next = next;
    }

    /// <summary>
    /// Chains two color items together.
    /// </summary>
    public static TColorItem operator +(TColorItem i1, TColorItem i2)
    {
        var cur = i1;
        while (cur.Next != null)
        {
            cur = cur.Next;
        }
        cur.Next = i2;
        return i1;
    }
}
