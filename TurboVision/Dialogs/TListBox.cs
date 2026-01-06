using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// List box with string items.
/// </summary>
public class TListBox : TListViewer
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TListBox";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    [JsonPropertyName("items")]
    public List<string>? Items { get; set; }

    public TListBox(TRect bounds, ushort numCols, TScrollBar? scrollBar)
        : base(bounds, numCols, null, scrollBar)
    {
        SetRange(0);
    }

    public override int DataSize()
    {
        // Size of list reference + selection index
        return sizeof(int) * 2;
    }

    public override void GetData(Span<byte> rec)
    {
        if (rec.Length >= sizeof(int) * 2)
        {
            // Store items pointer (as hash code for identification) and focused index
            BitConverter.TryWriteBytes(rec, Items?.GetHashCode() ?? 0);
            BitConverter.TryWriteBytes(rec.Slice(sizeof(int)), Focused);
        }
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        if (Items != null && item >= 0 && item < Items.Count)
        {
            var text = Items[item];
            int len = Math.Min(text.Length, Math.Min(maxLen, dest.Length - 1));
            text.AsSpan(0, len).CopyTo(dest);
            if (len < dest.Length)
            {
                dest[len] = '\0';
            }
        }
        else if (dest.Length > 0)
        {
            dest[0] = '\0';
        }
    }

    public virtual void NewList(List<string>? list)
    {
        Items = list;
        if (list != null)
        {
            SetRange((short)list.Count);
        }
        else
        {
            SetRange(0);
        }
        if (Range > 0)
        {
            FocusItem(0);
        }
        DrawView();
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length >= sizeof(int) * 2)
        {
            // The upstream uses a TListBoxRec structure with items pointer and selection
            // We can't restore the items pointer, but we can restore the selection
            short selection = (short)BitConverter.ToInt32(rec.Slice(sizeof(int)));
            if (Range > 0)
            {
                FocusItem(selection);
            }
            DrawView();
        }
    }

    public List<string>? List()
    {
        return Items;
    }
}
