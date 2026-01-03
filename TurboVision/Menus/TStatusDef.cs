namespace TurboVision.Menus;

/// <summary>
/// Status line definition for a range of help contexts.
/// </summary>
public class TStatusDef
{
    public TStatusDef? Next { get; set; }
    public ushort Min { get; set; }
    public ushort Max { get; set; }
    public TStatusItem? Items { get; set; }

    public TStatusDef(ushort min, ushort max, TStatusItem? items = null, TStatusDef? next = null)
    {
        Min = min;
        Max = max;
        Items = items;
        Next = next;
    }

    /// <summary>
    /// Adds a status item to this definition.
    /// </summary>
    public TStatusDef Add(TStatusItem item)
    {
        if (Items == null)
        {
            Items = item;
        }
        else
        {
            var last = Items;
            while (last.Next != null)
            {
                last = last.Next;
            }
            last.Next = item;
        }
        return this;
    }

    /// <summary>
    /// Adds another definition to the chain.
    /// </summary>
    public TStatusDef Add(TStatusDef def)
    {
        if (Next == null)
        {
            Next = def;
        }
        else
        {
            var last = Next;
            while (last.Next != null)
            {
                last = last.Next;
            }
            last.Next = def;
        }
        return this;
    }
}
