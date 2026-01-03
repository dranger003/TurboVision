using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// List box with string items.
/// </summary>
public class TListBox : TListViewer
{
    protected List<string>? Items { get; set; }

    public TListBox(TRect bounds, ushort numCols, TScrollBar? scrollBar)
        : base(bounds, numCols, null, scrollBar)
    {
    }

    public override int DataSize()
    {
        // Size of list reference + selection index
        return sizeof(int);
    }

    public override void GetData(Span<byte> rec)
    {
        if (rec.Length >= sizeof(int))
        {
            BitConverter.TryWriteBytes(rec, Focused);
        }
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        if (Items != null && item >= 0 && item < Items.Count)
        {
            var text = Items[item];
            int len = Math.Min(text.Length, Math.Min(maxLen, dest.Length));
            text.AsSpan(0, len).CopyTo(dest);
        }
    }

    public virtual void NewList(List<string>? list)
    {
        Items = list;
        SetRange((short)(Items?.Count ?? 0));
        if (Range > 0)
        {
            FocusItem(0);
        }
        DrawView();
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length >= sizeof(int))
        {
            FocusItem((short)BitConverter.ToInt32(rec));
        }
    }

    public List<string>? List()
    {
        return Items;
    }
}
