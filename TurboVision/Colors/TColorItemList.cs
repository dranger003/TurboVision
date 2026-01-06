using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A list viewer for color items within a group.
/// </summary>
public class TColorItemList : TListViewer
{
    private TColorItem? _items;

    public TColorItemList(TRect bounds, TScrollBar? scrollBar, TColorItem? items)
        : base(bounds, 1, null, scrollBar)
    {
        EventMask |= EventConstants.evBroadcast;
        _items = items;

        int count = 0;
        var item = items;
        while (item != null)
        {
            item = item.Next;
            count++;
        }
        SetRange((short)count);
    }

    public override void FocusItem(short item)
    {
        base.FocusItem(item);
        SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmSaveColorIndex, (int)item);

        var curItem = _items;
        int i = item;
        while (i-- > 0 && curItem != null)
        {
            curItem = curItem.Next;
        }

        if (curItem != null)
        {
            SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmNewColorIndex, (int)curItem.Index);
        }
    }

    private static void SendMessage(TGroup? owner, ushort what, ushort command, int infoInt)
    {
        if (owner == null) return;

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoInt }
        };

        owner.HandleEvent(ref ev);
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        var curItem = _items;
        while (item-- > 0 && curItem != null)
        {
            curItem = curItem.Next;
        }

        if (curItem != null && dest.Length > 0)
        {
            int len = Math.Min(curItem.Name.Length, Math.Min(maxLen, dest.Length - 1));
            curItem.Name.AsSpan(0, len).CopyTo(dest);
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

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == ColorCommands.cmNewColorItem)
        {
            if (ev.Message.InfoPtr is TColorGroup group)
            {
                _items = group.Items;

                int count = 0;
                var curItem = _items;
                while (curItem != null)
                {
                    curItem = curItem.Next;
                    count++;
                }
                SetRange((short)count);
                FocusItem((short)group.Index);
                DrawView();
            }
        }
    }
}
