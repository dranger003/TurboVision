using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Abstract base class for list display.
/// </summary>
public abstract class TListViewer : TView
{
    private static readonly byte[] DefaultPalette = [0x1A, 0x1A, 0x1B, 0x1C, 0x1D];

    public TScrollBar? HScrollBar { get; set; }
    public TScrollBar? VScrollBar { get; set; }
    public short NumCols { get; set; }
    public short TopItem { get; set; }
    public short Focused { get; set; }
    public short Range { get; protected set; }

    protected TListViewer(TRect bounds, ushort numCols, TScrollBar? hScrollBar, TScrollBar? vScrollBar)
        : base(bounds)
    {
        NumCols = (short)numCols;
        HScrollBar = hScrollBar;
        VScrollBar = vScrollBar;
        Options |= OptionFlags.ofFirstClick | OptionFlags.ofSelectable;
        EventMask |= EventConstants.evBroadcast;
    }

    public override void ChangeBounds(TRect bounds)
    {
        base.ChangeBounds(bounds);
        // TODO: Update scrollbars
    }

    public override void Draw()
    {
        // TODO: Implement list drawing
        var b = new TDrawBuffer();
        var color = GetColor(0x0101);
        b.MoveChar(0, ' ', color.Normal, Size.X);

        for (int y = 0; y < Size.Y; y++)
        {
            WriteLine(0, y, Size.X, 1, b);
        }
    }

    public virtual void FocusItem(short item)
    {
        Focused = item;
        // TODO: Scroll to make item visible
        DrawView();
    }

    public virtual void FocusItemNum(short item)
    {
        FocusItem(item);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public abstract void GetText(Span<char> dest, short item, short maxLen);

    public virtual bool IsSelected(short item)
    {
        return item == Focused;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            // TODO: Handle mouse selection
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle keyboard navigation
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == CommandConstants.cmScrollBarChanged)
            {
                // TODO: Handle scrollbar changes
            }
        }
    }

    public virtual void SelectItem(short item)
    {
        // TODO: Post item selected message
    }

    public void SetRange(short aRange)
    {
        Range = aRange;
        VScrollBar?.SetParams(Focused, 0, aRange - 1, Size.Y - 1, 1);
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & (StateFlags.sfSelected | StateFlags.sfActive)) != 0)
        {
            if (HScrollBar != null)
            {
                if (GetState(StateFlags.sfActive) && GetState(StateFlags.sfVisible))
                {
                    HScrollBar.Show();
                }
                else
                {
                    HScrollBar.Hide();
                }
            }

            if (VScrollBar != null)
            {
                if (GetState(StateFlags.sfActive) && GetState(StateFlags.sfVisible))
                {
                    VScrollBar.Show();
                }
                else
                {
                    VScrollBar.Hide();
                }
            }
            DrawView();
        }
    }

    public override void ShutDown()
    {
        HScrollBar = null;
        VScrollBar = null;
        base.ShutDown();
    }
}
