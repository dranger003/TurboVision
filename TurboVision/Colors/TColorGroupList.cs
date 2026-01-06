using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A list viewer for color groups.
/// </summary>
public class TColorGroupList : TListViewer
{
    private readonly TColorGroup? _groups;

    public TColorGroupList(TRect bounds, TScrollBar? scrollBar, TColorGroup? groups)
        : base(bounds, 1, null, scrollBar)
    {
        _groups = groups;

        int count = 0;
        var group = groups;
        while (group != null)
        {
            group = group.Next;
            count++;
        }
        SetRange((short)count);
    }

    public override void FocusItem(short item)
    {
        base.FocusItem(item);

        var curGroup = _groups;
        int i = item;
        while (i-- > 0 && curGroup != null)
        {
            curGroup = curGroup.Next;
        }

        if (curGroup != null)
        {
            SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmNewColorItem, curGroup);
        }
    }

    private static void SendMessage(TGroup? owner, ushort what, ushort command, object infoPtr)
    {
        if (owner == null) return;

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoPtr }
        };

        owner.HandleEvent(ref ev);
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        var curGroup = _groups;
        while (item-- > 0 && curGroup != null)
        {
            curGroup = curGroup.Next;
        }

        if (curGroup != null && dest.Length > 0)
        {
            int len = Math.Min(curGroup.Name.Length, Math.Min(maxLen, dest.Length - 1));
            curGroup.Name.AsSpan(0, len).CopyTo(dest);
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
            ev.Message.Command == ColorCommands.cmSaveColorIndex)
        {
            SetGroupIndex((byte)Focused, (byte)ev.Message.InfoInt);
        }
    }

    public void SetGroupIndex(byte groupNum, byte itemNum)
    {
        var g = GetGroup(groupNum);
        if (g != null)
        {
            g.Index = itemNum;
        }
    }

    public byte GetGroupIndex(byte groupNum)
    {
        var g = GetGroup(groupNum);
        return g?.Index ?? 0;
    }

    public TColorGroup? GetGroup(byte groupNum)
    {
        var g = _groups;
        while (groupNum-- > 0 && g != null)
        {
            g = g.Next;
        }
        return g;
    }

    public byte GetNumGroups()
    {
        byte n = 0;
        var g = _groups;
        while (g != null)
        {
            n++;
            g = g.Next;
        }
        return n;
    }
}
