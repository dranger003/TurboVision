using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Base class for grouped controls (checkboxes, radio buttons).
/// </summary>
public abstract class TCluster : TView
{
    private static readonly byte[] DefaultPalette = [0x10, 0x11, 0x12, 0x12, 0x1F];

    protected uint Value { get; set; }
    protected uint EnableMask { get; set; } = 0xFFFFFFFF;
    protected int Sel { get; set; }
    protected List<string> Strings { get; } = [];

    protected TCluster(TRect bounds, TSItem? strings) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofPreProcess | OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;

        while (strings != null)
        {
            Strings.Add(strings.Value ?? "");
            strings = strings.Next;
        }

        SetCursor(2, 0);
        ShowCursor();
    }

    public override int DataSize()
    {
        return sizeof(ushort);
    }

    public void DrawBox(string icon, char marker)
    {
        // DrawBox uses a simple two-character marker string: " " + marker
        DrawMultiBox(icon, $" {marker}");
    }

    public void DrawMultiBox(string icon, string marker)
    {
        var b = new TDrawBuffer();

        var cNorm = GetColor(0x0301);
        var cSel = GetColor(0x0402);
        var cDis = GetColor(0x0505);

        for (int row = 0; row < Size.Y; row++)
        {
            b.MoveChar(0, ' ', cNorm.Normal, Size.X);

            // Calculate how many columns we have
            int numCols = (Strings.Count + Size.Y - 1) / Size.Y;

            for (int col = 0; col < numCols; col++)
            {
                int item = col * Size.Y + row;
                if (item < Strings.Count)
                {
                    int colX = Column(item);
                    if (colX < Size.X)
                    {
                        TAttrPair color;
                        if (!ButtonState(item))
                        {
                            color = cDis;
                        }
                        else if (item == Sel && GetState(StateFlags.sfSelected))
                        {
                            color = cSel;
                        }
                        else
                        {
                            color = cNorm;
                        }

                        // Clear from column to end with the color
                        b.MoveChar(colX, ' ', color.Normal, Size.X - colX);

                        // Draw the icon (e.g., " [ ] " or " ( ) ")
                        b.MoveCStr(colX, icon, color);

                        // Draw the marker character at position +2 in the icon
                        int markIdx = MultiMark(item);
                        if (markIdx < marker.Length)
                        {
                            b.PutChar(colX + 2, marker[markIdx]);
                        }

                        // Draw the string label
                        b.MoveCStr(colX + 5, Strings[item], color);

                        // Show markers for focused item if ShowMarkers is enabled
                        if (ShowMarkers && GetState(StateFlags.sfSelected) && item == Sel)
                        {
                            b.PutChar(colX, '\u00BB'); // »
                            int nextCol = Column(item + Size.Y);
                            if (nextCol > colX)
                            {
                                b.PutChar(nextCol - 1, '\u00AB'); // «
                            }
                        }
                    }
                }
            }

            WriteBuf(0, row, Size.X, 1, b);
        }

        SetCursor(Column(Sel) + 2, Row(Sel));
    }

    public override void GetData(Span<byte> rec)
    {
        if (rec.Length >= sizeof(ushort))
        {
            BitConverter.TryWriteBytes(rec, (ushort)Value);
        }
        DrawView();
    }

    public override ushort GetHelpCtx()
    {
        if (HelpCtx == HelpContexts.hcNoContext)
        {
            return HelpCtx;
        }
        return (ushort)(HelpCtx + Sel);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if ((Options & OptionFlags.ofSelectable) == 0)
        {
            return;
        }

        if (ev.What == EventConstants.evMouseDown)
        {
            var mouse = MakeLocal(ev.Mouse.Where);
            int i = FindSel(mouse);
            if (i != -1 && ButtonState(i))
            {
                Sel = i;
            }
            DrawView();

            do
            {
                mouse = MakeLocal(ev.Mouse.Where);
                if (FindSel(mouse) == Sel && ButtonState(Sel))
                {
                    ShowCursor();
                }
                else
                {
                    HideCursor();
                }
            } while (MouseEvent(ref ev, EventConstants.evMouseMove));

            ShowCursor();
            mouse = MakeLocal(ev.Mouse.Where);
            if (FindSel(mouse) == Sel)
            {
                Press(Sel);
                DrawView();
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            int s = Sel;
            ushort keyCode = TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode);

            switch (keyCode)
            {
                case KeyConstants.kbUp:
                    if (GetState(StateFlags.sfFocused))
                    {
                        int i = 0;
                        do
                        {
                            i++;
                            s--;
                            if (s < 0)
                            {
                                s = Strings.Count - 1;
                            }
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;

                case KeyConstants.kbDown:
                    if (GetState(StateFlags.sfFocused))
                    {
                        int i = 0;
                        do
                        {
                            i++;
                            s++;
                            if (s >= Strings.Count)
                            {
                                s = 0;
                            }
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;

                case KeyConstants.kbRight:
                    if (GetState(StateFlags.sfFocused))
                    {
                        int i = 0;
                        do
                        {
                            i++;
                            s += Size.Y;
                            if (s >= Strings.Count)
                            {
                                s = 0;
                            }
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;

                case KeyConstants.kbLeft:
                    if (GetState(StateFlags.sfFocused))
                    {
                        int i = 0;
                        do
                        {
                            i++;
                            if (s > 0)
                            {
                                s -= Size.Y;
                                if (s < 0)
                                {
                                    s = ((Strings.Count + Size.Y - 1) / Size.Y) * Size.Y + s - 1;
                                    if (s >= Strings.Count)
                                    {
                                        s = Strings.Count - 1;
                                    }
                                }
                            }
                            else
                            {
                                s = Strings.Count - 1;
                            }
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;

                default:
                    // Check for hotkey or Alt+letter
                    for (int i = 0; i < Strings.Count; i++)
                    {
                        char c = TStringUtils.HotKey(Strings[i]);
                        if (ev.KeyDown.KeyCode != 0 &&
                            (TStringUtils.GetAltCode(c) == ev.KeyDown.KeyCode ||
                             ((Owner?.Phase == PhaseType.phPostProcess || GetState(StateFlags.sfFocused)) &&
                              c != '\0' &&
                              c == char.ToUpperInvariant((char)ev.KeyDown.CharCode))))
                        {
                            if (ButtonState(i))
                            {
                                if (Focus())
                                {
                                    Sel = i;
                                    MovedTo(Sel);
                                    Press(Sel);
                                    DrawView();
                                }
                                ClearEvent(ref ev);
                            }
                            return;
                        }
                    }

                    // Space toggles current selection
                    if (ev.KeyDown.CharCode == ' ' && GetState(StateFlags.sfFocused))
                    {
                        Press(Sel);
                        DrawView();
                        ClearEvent(ref ev);
                    }
                    break;
            }
        }
    }

    public virtual bool Mark(int item)
    {
        return false;
    }

    public virtual byte MultiMark(int item)
    {
        return Mark(item) ? (byte)1 : (byte)0;
    }

    public virtual void Press(int item)
    {
        // Override in derived classes
    }

    public virtual void MovedTo(int item)
    {
        Sel = item;
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length >= sizeof(ushort))
        {
            Value = BitConverter.ToUInt16(rec);
        }
        DrawView();
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfSelected) != 0)
        {
            DrawView();
        }
    }

    public virtual void SetButtonState(uint mask, bool enable)
    {
        if (!enable)
        {
            EnableMask &= ~mask;
        }
        else
        {
            EnableMask |= mask;
        }

        int n = Strings.Count;
        if (n < 32)
        {
            uint testMask = (1u << n) - 1;
            if ((EnableMask & testMask) != 0)
            {
                Options |= OptionFlags.ofSelectable;
            }
            else
            {
                Options &= unchecked((ushort)~OptionFlags.ofSelectable);
            }
        }
    }

    public bool ButtonState(int item)
    {
        if (item < 32)
        {
            return (EnableMask & (1u << item)) != 0;
        }
        return false;
    }

    protected int Column(int item)
    {
        if (item < Size.Y)
        {
            return 0;
        }

        int width = 0;
        int col = -6;

        for (int i = 0; i <= item; i++)
        {
            if (i % Size.Y == 0)
            {
                col += width + 6;
                width = 0;
            }

            if (i < Strings.Count)
            {
                int l = TStringUtils.CstrLen(Strings[i]);
                if (l > width)
                {
                    width = l;
                }
            }
        }
        return col;
    }

    protected int Row(int item)
    {
        return item % Size.Y;
    }

    protected int FindSel(TPoint p)
    {
        var r = GetExtent();
        if (!r.Contains(p))
        {
            return -1;
        }

        int i = 0;
        while (p.X >= Column(i + Size.Y))
        {
            i += Size.Y;
        }

        int s = i + p.Y;
        if (s >= Strings.Count)
        {
            return -1;
        }
        return s;
    }

    protected void MoveSel(int iterations, int newSel)
    {
        if (iterations <= Strings.Count)
        {
            Sel = newSel;
            MovedTo(Sel);
            DrawView();
        }
    }
}
