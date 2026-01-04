using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Abstract base class for list display.
/// </summary>
public abstract class TListViewer : TView
{
    private static readonly byte[] DefaultPalette = [0x1A, 0x1A, 0x1B, 0x1C, 0x1D];

    // Special characters for markers (matching upstream specialChars)
    // { 175, 174, 26, 27, ' ', ' ' } = { », «, →, ←, ' ', ' ' }
    private static readonly char[] SpecialChars = ['\u00BB', '\u00AB', '\u001A', '\u001B', ' ', ' '];

    // Empty list text
    private const string EmptyText = "<empty>";

    public TScrollBar? HScrollBar { get; set; }
    public TScrollBar? VScrollBar { get; set; }
    public short NumCols { get; set; }
    public short TopItem { get; set; }
    public short Focused { get; set; }
    public short Range { get; protected set; }

    protected TListViewer(TRect bounds, ushort numCols, TScrollBar? hScrollBar, TScrollBar? vScrollBar)
        : base(bounds)
    {
        short arStep, pgStep;

        NumCols = (short)numCols;
        Options |= OptionFlags.ofFirstClick | OptionFlags.ofSelectable;
        EventMask |= EventConstants.evBroadcast;

        if (vScrollBar != null)
        {
            if (numCols == 1)
            {
                pgStep = (short)(Size.Y - 1);
                arStep = 1;
            }
            else
            {
                pgStep = (short)(Size.Y * numCols);
                arStep = (short)Size.Y;
            }
            vScrollBar.SetStep(pgStep, arStep);
        }

        if (hScrollBar != null)
        {
            hScrollBar.SetStep(Size.X / numCols, 1);
        }

        HScrollBar = hScrollBar;
        VScrollBar = vScrollBar;
    }

    public override void ChangeBounds(TRect bounds)
    {
        base.ChangeBounds(bounds);
        if (HScrollBar != null)
        {
            HScrollBar.SetStep(Size.X / NumCols, HScrollBar.ArStep);
        }
        if (VScrollBar != null)
        {
            VScrollBar.SetStep(Size.Y, VScrollBar.ArStep);
        }
    }

    public override void Draw()
    {
        short i, j, item;
        TColorAttr normalColor, selectedColor, focusedColor, color;
        short colWidth, curCol, indent;
        var b = new TDrawBuffer();
        byte scOff;
        bool focusedVis;

        if ((State & (StateFlags.sfSelected | StateFlags.sfActive)) == (StateFlags.sfSelected | StateFlags.sfActive))
        {
            normalColor = GetColor(1).Normal;
            focusedColor = GetColor(3).Normal;
            selectedColor = GetColor(4).Normal;
        }
        else
        {
            normalColor = GetColor(2).Normal;
            selectedColor = GetColor(4).Normal;
            focusedColor = default; // Unused, but silence warning
        }

        if (HScrollBar != null)
        {
            indent = (short)HScrollBar.Value;
        }
        else
        {
            indent = 0;
        }

        focusedVis = false;
        colWidth = (short)(Size.X / NumCols + 1);
        Span<char> text = stackalloc char[256]; // Moved outside loop to avoid stack overflow
        for (i = 0; i < Size.Y; i++)
        {
            for (j = 0; j < NumCols; j++)
            {
                item = (short)(j * Size.Y + i + TopItem);
                curCol = (short)(j * colWidth);
                if ((State & (StateFlags.sfSelected | StateFlags.sfActive)) == (StateFlags.sfSelected | StateFlags.sfActive) &&
                    Focused == item &&
                    Range > 0)
                {
                    color = focusedColor;
                    SetCursor(curCol + 1, i);
                    scOff = 0;
                    focusedVis = true;
                }
                else if (item < Range && IsSelected(item))
                {
                    color = selectedColor;
                    scOff = 2;
                }
                else
                {
                    color = normalColor;
                    scOff = 4;
                }

                b.MoveChar(curCol, ' ', color, colWidth);
                if (item < Range)
                {
                    if (indent < 255)
                    {
                        text.Clear();
                        GetText(text, item, 255);
                        int len = text.IndexOf('\0');
                        if (len < 0) len = text.Length;
                        if (indent < len)
                        {
                            b.MoveStr(curCol + 1, text.Slice(indent, len - indent), color, colWidth - 1);
                        }
                    }
                    if (ShowMarkers)
                    {
                        b.PutChar(curCol, SpecialChars[scOff]);
                        b.PutChar(curCol + colWidth - 2, SpecialChars[scOff + 1]);
                    }
                }
                else if (i == 0 && j == 0)
                {
                    b.MoveStr(curCol + 1, EmptyText, GetColor(1).Normal, colWidth - 1);
                }

                b.MoveChar(curCol + colWidth - 1, '\u2502', GetColor(5).Normal, 1); // │
            }
            WriteLine(0, i, Size.X, 1, b);
        }

        if (!focusedVis)
        {
            SetCursor(-1, -1);
        }
    }

    public virtual void FocusItem(short item)
    {
        Focused = item;
        if (VScrollBar != null)
        {
            VScrollBar.SetValue(item);
        }
        else
        {
            DrawView();
        }
        if (Size.Y > 0)
        {
            if (item < TopItem)
            {
                if (NumCols == 1)
                {
                    TopItem = item;
                }
                else
                {
                    TopItem = (short)(item - item % Size.Y);
                }
            }
            else if (item >= TopItem + Size.Y * NumCols)
            {
                if (NumCols == 1)
                {
                    TopItem = (short)(item - Size.Y + 1);
                }
                else
                {
                    TopItem = (short)(item - item % Size.Y - (Size.Y * (NumCols - 1)));
                }
            }
        }
    }

    public virtual void FocusItemNum(short item)
    {
        if (item < 0)
        {
            item = 0;
        }
        else if (item >= Range && Range > 0)
        {
            item = (short)(Range - 1);
        }

        if (Range != 0)
        {
            FocusItem(item);
        }
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
        TPoint mouse;
        ushort colWidth;
        short oldItem, newItem = 0;
        ushort count;
        int mouseAutosToSkip = 4;

        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            colWidth = (ushort)(Size.X / NumCols + 1);
            oldItem = Focused;
            count = 0;
            do
            {
                mouse = MakeLocal(ev.Mouse.Where);
                if (MouseInView(ev.Mouse.Where))
                {
                    newItem = (short)(mouse.Y + (Size.Y * (mouse.X / colWidth)) + TopItem);
                }
                else
                {
                    if (NumCols == 1)
                    {
                        if (ev.What == EventConstants.evMouseAuto)
                        {
                            count++;
                        }
                        if (count == mouseAutosToSkip)
                        {
                            count = 0;
                            if (mouse.Y < 0)
                            {
                                newItem = (short)(Focused - 1);
                            }
                            else if (mouse.Y >= Size.Y)
                            {
                                newItem = (short)(Focused + 1);
                            }
                        }
                    }
                    else
                    {
                        if (ev.What == EventConstants.evMouseAuto)
                        {
                            count++;
                        }
                        if (count == mouseAutosToSkip)
                        {
                            count = 0;
                            if (mouse.X < 0)
                            {
                                newItem = (short)(Focused - Size.Y);
                            }
                            else if (mouse.X >= Size.X)
                            {
                                newItem = (short)(Focused + Size.Y);
                            }
                            else if (mouse.Y < 0)
                            {
                                newItem = (short)(Focused - Focused % Size.Y);
                            }
                            else if (mouse.Y > Size.Y)
                            {
                                newItem = (short)(Focused - Focused % Size.Y + Size.Y - 1);
                            }
                        }
                    }
                }
                if (newItem != oldItem)
                {
                    FocusItemNum(newItem);
                    DrawView();
                }
                oldItem = newItem;
                if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0)
                {
                    break;
                }
            } while (MouseEvent(ref ev, EventConstants.evMouseMove | EventConstants.evMouseAuto));
            FocusItemNum(newItem);
            DrawView();
            if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0 && Range > newItem)
            {
                SelectItem(newItem);
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            if ((ev.KeyDown.KeyCode & 0xFF) == ' ' && Focused < Range)
            {
                SelectItem(Focused);
                newItem = Focused;
            }
            else
            {
                switch (TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode))
                {
                    case KeyConstants.kbUp:
                        newItem = (short)(Focused - 1);
                        break;
                    case KeyConstants.kbDown:
                        newItem = (short)(Focused + 1);
                        break;
                    case KeyConstants.kbRight:
                        if (NumCols > 1)
                        {
                            newItem = (short)(Focused + Size.Y);
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case KeyConstants.kbLeft:
                        if (NumCols > 1)
                        {
                            newItem = (short)(Focused - Size.Y);
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case KeyConstants.kbPgDn:
                        newItem = (short)(Focused + Size.Y * NumCols);
                        break;
                    case KeyConstants.kbPgUp:
                        newItem = (short)(Focused - Size.Y * NumCols);
                        break;
                    case KeyConstants.kbHome:
                        newItem = TopItem;
                        break;
                    case KeyConstants.kbEnd:
                        newItem = (short)(TopItem + (Size.Y * NumCols) - 1);
                        break;
                    case KeyConstants.kbCtrlPgDn:
                        newItem = (short)(Range - 1);
                        break;
                    case KeyConstants.kbCtrlPgUp:
                        newItem = 0;
                        break;
                    default:
                        return;
                }
            }
            FocusItemNum(newItem);
            DrawView();
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if ((Options & OptionFlags.ofSelectable) != 0)
            {
                if (ev.Message.Command == CommandConstants.cmScrollBarClicked &&
                    (ReferenceEquals(ev.Message.InfoPtr, HScrollBar) || ReferenceEquals(ev.Message.InfoPtr, VScrollBar)))
                {
                    Select();
                }
                else if (ev.Message.Command == CommandConstants.cmScrollBarChanged)
                {
                    if (ReferenceEquals(ev.Message.InfoPtr, VScrollBar) && VScrollBar != null)
                    {
                        FocusItemNum((short)VScrollBar.Value);
                        DrawView();
                    }
                    else if (ReferenceEquals(ev.Message.InfoPtr, HScrollBar))
                    {
                        DrawView();
                    }
                }
            }
        }
    }

    public virtual void SelectItem(short item)
    {
        Message(Owner, EventConstants.evBroadcast, CommandConstants.cmListItemSelected, this);
    }

    public void SetRange(short aRange)
    {
        Range = aRange;
        if (Focused >= aRange)
        {
            Focused = 0;
        }
        if (VScrollBar != null)
        {
            VScrollBar.SetParams(Focused, 0, aRange - 1, VScrollBar.PgStep, VScrollBar.ArStep);
        }
        else
        {
            DrawView();
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & (StateFlags.sfSelected | StateFlags.sfActive | StateFlags.sfVisible)) != 0)
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

    /// <summary>
    /// Sends a message to a view via its owner.
    /// </summary>
    private static void Message(TGroup? owner, ushort what, ushort command, object? infoPtr)
    {
        if (owner == null) return;

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoPtr }
        };

        owner.HandleEvent(ref ev);
    }
}
