using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Scrollbar widget.
/// </summary>
public class TScrollBar : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TScrollBar";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x04, 0x05, 0x05];

    // Scroll bar character sets - matching upstream tvtext1.cpp
    // vChars: {'\x1E', '\x1F', '\xB1', '\xFE', '\xB2'} = {▲, ▼, ▒, ■, ▓}
    // hChars: {'\x11', '\x10', '\xB1', '\xFE', '\xB2'} = {◄, ►, ▒, ■, ▓}
    // CP437: 0xB0=░ LIGHT SHADE, 0xB1=▒ MEDIUM SHADE, 0xB2=▓ DARK SHADE
    public static char[] VChars { get; set; } = ['\u25B2', '\u25BC', '\u2592', '\u25A0', '\u2593'];
    public static char[] HChars { get; set; } = ['\u25C4', '\u25BA', '\u2592', '\u25A0', '\u2593'];

    public int Value { get; set; }
    public int MinVal { get; set; }
    public int MaxVal { get; set; }
    public int PgStep { get; set; } = 1;
    public int ArStep { get; set; } = 1;

    /// <summary>
    /// Scroll bar character set. Determined by orientation, not serialized.
    /// </summary>
    [JsonIgnore]
    public char[] Chars { get; set; }

    public TScrollBar(TRect bounds) : base(bounds)
    {
        if (Size.X == 1)
        {
            Chars = (char[])VChars.Clone();
            GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        }
        else
        {
            Chars = (char[])HChars.Clone();
            GrowMode = GrowFlags.gfGrowLoY | GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        }

        // Note: ofPostProcess is NOT set by default (matching upstream).
        // It can be enabled via StandardScrollBar with sbHandleKeyboard flag.
        EventMask |= EventConstants.evMouseWheel;
    }

    public override void Draw()
    {
        DrawPos(GetPos());
    }

    public void DrawPos(int pos)
    {
        var b = new TDrawBuffer();

        int s = GetSize() - 1;
        b.MoveChar(0, Chars[0], GetColor(2).Normal, 1);
        if (MaxVal == MinVal)
        {
            b.MoveChar(1, Chars[4], GetColor(1).Normal, s - 1);
        }
        else
        {
            b.MoveChar(1, Chars[2], GetColor(1).Normal, s - 1);
            b.MoveChar(pos, Chars[3], GetColor(3).Normal, 1);
        }

        b.MoveChar(s, Chars[1], GetColor(2).Normal, 1);
        WriteBuf(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public int GetPos()
    {
        int r = MaxVal - MinVal;
        if (r == 0)
        {
            return 1;
        }
        else
        {
            return (int)((((long)(Value - MinVal) * (GetSize() - 3)) + (r >> 1)) / r) + 1;
        }
    }

    public int GetSize()
    {
        int s;

        if (Size.X == 1)
        {
            s = Size.Y;
        }
        else
        {
            s = Size.X;
        }

        return Math.Max(3, s);
    }

    // Static variables for mouse tracking during drag
    private TPoint _mouse;
    private int _p;
    private int _s;
    private TRect _extent;

    private int GetPartCode()
    {
        int part = -1;
        if (_extent.Contains(_mouse))
        {
            int mark = (Size.X == 1) ? _mouse.Y : _mouse.X;

            if (mark == _p)
            {
                part = ScrollBarParts.sbIndicator;
            }
            else
            {
                if (mark < 1)
                {
                    part = ScrollBarParts.sbLeftArrow;
                }
                else if (mark < _p)
                {
                    part = ScrollBarParts.sbPageLeft;
                }
                else if (mark < _s)
                {
                    part = ScrollBarParts.sbPageRight;
                }
                else
                {
                    part = ScrollBarParts.sbRightArrow;
                }

                if (Size.X == 1)
                {
                    part += 4;
                }
            }
        }
        return part;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        int i, clickPart, step = 0;

        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case EventConstants.evMouseWheel:
                if ((State & StateFlags.sfVisible) != 0)
                {
                    if (Size.X == 1)
                    {
                        switch (ev.Mouse.Wheel)
                        {
                            case EventConstants.mwUp: step = -ArStep; break;
                            case EventConstants.mwDown: step = ArStep; break;
                        }
                    }
                    else
                    {
                        switch (ev.Mouse.Wheel)
                        {
                            case EventConstants.mwLeft: step = -ArStep; break;
                            case EventConstants.mwRight: step = ArStep; break;
                        }
                    }
                }
                if (step != 0)
                {
                    Message(Owner, EventConstants.evBroadcast, CommandConstants.cmScrollBarClicked, this);
                    SetValue(Value + 3 * step);
                    ClearEvent(ref ev);
                }
                break;

            case EventConstants.evMouseDown:
                Message(Owner, EventConstants.evBroadcast, CommandConstants.cmScrollBarClicked, this);
                _mouse = MakeLocal(ev.Mouse.Where);
                _extent = GetExtent();
                _extent.Grow(1, 1);
                _p = GetPos();
                _s = GetSize() - 1;
                clickPart = GetPartCode();
                switch (clickPart)
                {
                    case ScrollBarParts.sbLeftArrow:
                    case ScrollBarParts.sbRightArrow:
                    case ScrollBarParts.sbUpArrow:
                    case ScrollBarParts.sbDownArrow:
                        do
                        {
                            _mouse = MakeLocal(ev.Mouse.Where);
                            if (GetPartCode() == clickPart)
                            {
                                SetValue(Value + ScrollStep(clickPart));
                            }
                        } while (MouseEvent(ref ev, EventConstants.evMouseAuto));
                        break;

                    default:
                        do
                        {
                            _mouse = MakeLocal(ev.Mouse.Where);
                            if (Size.X == 1)
                            {
                                i = _mouse.Y;
                            }
                            else
                            {
                                i = _mouse.X;
                            }
                            i = Math.Max(i, 1);
                            i = Math.Min(i, _s - 1);
                            _p = i;
                            if (_s > 2)
                            {
                                SetValue((int)(((long)(_p - 1) * (MaxVal - MinVal) + ((_s - 2) >> 1)) / (_s - 2)) + MinVal);
                            }
                            DrawPos(_p);
                        } while (MouseEvent(ref ev, EventConstants.evMouseMove));
                        break;
                }
                ClearEvent(ref ev);
                break;

            case EventConstants.evKeyDown:
                if ((State & StateFlags.sfVisible) != 0)
                {
                    clickPart = ScrollBarParts.sbIndicator;
                    i = Value;
                    if (Size.Y == 1)
                    {
                        switch (TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode))
                        {
                            case KeyConstants.kbLeft:
                                clickPart = ScrollBarParts.sbLeftArrow;
                                break;
                            case KeyConstants.kbRight:
                                clickPart = ScrollBarParts.sbRightArrow;
                                break;
                            case KeyConstants.kbCtrlLeft:
                                clickPart = ScrollBarParts.sbPageLeft;
                                break;
                            case KeyConstants.kbCtrlRight:
                                clickPart = ScrollBarParts.sbPageRight;
                                break;
                            case KeyConstants.kbCtrlUp:
                                clickPart = ScrollBarParts.sbPageUp;
                                break;
                            case KeyConstants.kbCtrlDown:
                                clickPart = ScrollBarParts.sbPageDown;
                                break;
                            case KeyConstants.kbHome:
                                i = MinVal;
                                break;
                            case KeyConstants.kbEnd:
                                i = MaxVal;
                                break;
                            default:
                                return;
                        }
                    }
                    else
                    {
                        switch (TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode))
                        {
                            case KeyConstants.kbUp:
                                clickPart = ScrollBarParts.sbUpArrow;
                                break;
                            case KeyConstants.kbDown:
                                clickPart = ScrollBarParts.sbDownArrow;
                                break;
                            case KeyConstants.kbPgUp:
                                clickPart = ScrollBarParts.sbPageUp;
                                break;
                            case KeyConstants.kbPgDn:
                                clickPart = ScrollBarParts.sbPageDown;
                                break;
                            case KeyConstants.kbCtrlPgUp:
                                i = MinVal;
                                break;
                            case KeyConstants.kbCtrlPgDn:
                                i = MaxVal;
                                break;
                            default:
                                return;
                        }
                    }
                    Message(Owner, EventConstants.evBroadcast, CommandConstants.cmScrollBarClicked, this);
                    if (clickPart != ScrollBarParts.sbIndicator)
                    {
                        i = Value + ScrollStep(clickPart);
                    }
                    SetValue(i);
                    ClearEvent(ref ev);
                }
                break;
        }
    }

    public virtual void ScrollDraw()
    {
        Message(Owner, EventConstants.evBroadcast, CommandConstants.cmScrollBarChanged, this);
    }

    public virtual int ScrollStep(int part)
    {
        int step;

        if ((part & 2) == 0)
        {
            step = ArStep;
        }
        else
        {
            step = PgStep;
        }
        if ((part & 1) == 0)
        {
            return -step;
        }
        else
        {
            return step;
        }
    }

    public void SetParams(int aValue, int aMin, int aMax, int aPgStep, int aArStep)
    {
        int sValue;

        aMax = Math.Max(aMax, aMin);
        aValue = Math.Max(aMin, aValue);
        aValue = Math.Min(aMax, aValue);
        sValue = Value;
        if (sValue != aValue || MinVal != aMin || MaxVal != aMax)
        {
            Value = aValue;
            MinVal = aMin;
            MaxVal = aMax;
            DrawView();
            if (sValue != aValue)
            {
                ScrollDraw();
            }
        }
        PgStep = aPgStep;
        ArStep = aArStep;
    }

    public void SetRange(int aMin, int aMax)
    {
        SetParams(Value, aMin, aMax, PgStep, ArStep);
    }

    public void SetStep(int aPgStep, int aArStep)
    {
        SetParams(Value, MinVal, MaxVal, aPgStep, aArStep);
    }

    public void SetValue(int aValue)
    {
        SetParams(aValue, MinVal, MaxVal, PgStep, ArStep);
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
