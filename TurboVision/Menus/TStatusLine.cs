using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Status bar at bottom of screen.
/// </summary>
public class TStatusLine : TView
{
    private static readonly byte[] DefaultPalette = [0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
    private const string HintSeparator = " │ ";

    protected TStatusItem? Items { get; set; }
    protected TStatusDef? Defs { get; set; }

    public TStatusLine(TRect bounds, TStatusDef defs) : base(bounds)
    {
        Defs = defs;
        GrowMode = GrowFlags.gfGrowLoY | GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        Options |= OptionFlags.ofPreProcess;
        EventMask |= EventConstants.evBroadcast;

        FindItems();
    }

    public override void Draw()
    {
        DrawSelect(null);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evMouseDown:
                {
                    TStatusItem? selected = null;
                    do
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        var newItem = ItemMouseIsIn(mouse);
                        if (newItem != selected)
                        {
                            selected = newItem;
                            DrawSelect(selected);
                        }
                    } while (MouseEvent(ref ev, EventConstants.evMouseMove));

                    if (selected != null && CommandEnabled(selected.Command))
                    {
                        ev.What = EventConstants.evCommand;
                        ev.Message.Command = selected.Command;
                        ev.Message.InfoPtr = 0;
                        PutEvent(ev);
                    }
                    ClearEvent(ref ev);
                    DrawView();
                }
                break;

            case EventConstants.evKeyDown:
                if (ev.KeyDown.KeyCode != KeyConstants.kbNoKey)
                {
                    var eventKey = ev.KeyDown.ToKey();
                    for (var item = Items; item != null; item = item.Next)
                    {
                        if (eventKey == item.KeyCode && CommandEnabled(item.Command))
                        {
                            ev.What = EventConstants.evCommand;
                            ev.Message.Command = item.Command;
                            ev.Message.InfoPtr = 0;
                            return;
                        }
                    }
                }
                break;

            case EventConstants.evBroadcast:
                if (ev.Message.Command == CommandConstants.cmCommandSetChanged)
                {
                    DrawView();
                }
                break;
        }
    }

    public virtual string? Hint(ushort helpCtx)
    {
        return null;
    }

    public void Update()
    {
        var topView = TopView();
        ushort h = topView != null ? topView.GetHelpCtx() : HelpContexts.hcNoContext;
        if (HelpCtx != h)
        {
            HelpCtx = h;
            FindItems();
            DrawView();
        }
    }

    private void DrawSelect(TStatusItem? selected)
    {
        var b = new TDrawBuffer();
        var cNormal = GetColor(0x0301);
        var cDisabled = GetColor(0x0202);
        var cSelect = GetColor(0x0604);
        var cSelectDisabled = GetColor(0x0505);

        b.MoveChar(0, ' ', cNormal.Normal, Size.X);

        int x = 0;
        var item = Items;

        while (item != null && x < Size.X)
        {
            if (item.Text != null)
            {
                bool disabled = !CommandEnabled(item.Command);
                var color = disabled ? cDisabled : cNormal;
                if (item == selected)
                {
                    color = disabled ? cSelectDisabled : cSelect;
                }

                b.MoveChar(x, ' ', color.Normal, 1);
                x++;
                x += b.MoveCStr(x, item.Text, new TAttrPair(color.Normal, color.Highlight));
                b.MoveChar(x, ' ', color.Normal, 1);
                x++;
            }
            item = item.Next;
        }

        // TODO: Add hint text if space available

        WriteBuf(0, 0, Size.X, Size.Y, b);
    }

    private void FindItems()
    {
        var p = Defs;
        while (p != null && (HelpCtx < p.Min || HelpCtx > p.Max))
        {
            p = p.Next;
        }
        Items = p?.Items;
    }

    private TStatusItem? ItemMouseIsIn(TPoint p)
    {
        if (p.Y != 0)
        {
            return null;
        }

        int x = 0;
        var item = Items;

        while (item != null)
        {
            if (item.Text != null)
            {
                int width = item.Text.Length - item.Text.Count(c => c == '~') + 2;
                if (p.X >= x && p.X < x + width)
                {
                    return item;
                }
                x += width;
            }
            item = item.Next;
        }

        return null;
    }

    private void DisposeItems(TStatusItem? item)
    {
        // Items are managed by GC
    }
}
