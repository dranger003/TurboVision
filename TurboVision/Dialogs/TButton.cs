using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Clickable push button.
/// </summary>
public class TButton : TView
{
    private static readonly byte[] DefaultPalette =
    [
        0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0E, 0x0E, 0x0F
    ];

    public string? Title { get; set; }
    protected ushort Command { get; set; }
    protected byte Flags { get; set; }
    protected bool AmDefault { get; set; }

    public TButton(TRect bounds, string? title, ushort command, ushort flags) : base(bounds)
    {
        Title = title;
        Command = command;
        Flags = (byte)flags;
        AmDefault = (flags & CommandConstants.bfDefault) != 0;

        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofPreProcess | OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;

        if (!CommandEnabled(command))
        {
            State |= StateFlags.sfDisabled;
        }
    }

    public override void Draw()
    {
        DrawState(false);
    }

    public void DrawState(bool down)
    {
        var b = new TDrawBuffer();
        byte colorIndex;

        if (GetState(StateFlags.sfDisabled))
        {
            colorIndex = 4;
        }
        else if (GetState(StateFlags.sfActive))
        {
            if (GetState(StateFlags.sfSelected))
            {
                colorIndex = 3;
            }
            else if (AmDefault)
            {
                colorIndex = 2;
            }
            else
            {
                colorIndex = 1;
            }
        }
        else
        {
            colorIndex = 1;
        }

        var color = GetColor((ushort)((colorIndex << 8) | colorIndex));
        var scOff = GetColor(0x0504);

        b.MoveChar(0, ' ', color.Normal, Size.X);

        if (!string.IsNullOrEmpty(Title))
        {
            int offset = (Size.X - Title.Length - 4) / 2;
            if (offset < 0) offset = 0;

            b.MoveStr(offset, "[ ", color.Normal);
            b.MoveCStr(offset + 2, Title, new TAttrPair(color.Normal, scOff.Normal));
            b.MoveStr(offset + 2 + Title.Length, " ]", color.Normal);
        }

        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What == EventConstants.evMouseDown)
        {
            if (!GetState(StateFlags.sfDisabled))
            {
                PressButton(ref ev);
            }
            ClearEvent(ref ev);
            return;
        }

        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evKeyDown)
        {
            if (ev.KeyDown.KeyCode == KeyConstants.kbEnter && AmDefault)
            {
                Press();
                ClearEvent(ref ev);
            }
            // TODO: Handle shortcut key
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == CommandConstants.cmCommandSetChanged)
            {
                SetState(StateFlags.sfDisabled, !CommandEnabled(Command));
                DrawView();
            }
        }
    }

    public void MakeDefault(bool enable)
    {
        if ((Flags & CommandConstants.bfDefault) == 0)
        {
            AmDefault = enable;
            DrawView();
        }
    }

    public virtual void Press()
    {
        TEvent ev = TEvent.Command(Command);

        if ((Flags & CommandConstants.bfBroadcast) != 0)
        {
            ev.What = EventConstants.evBroadcast;
        }

        PutEvent(ev);
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & (StateFlags.sfSelected | StateFlags.sfActive)) != 0)
        {
            DrawView();
        }

        if ((aState & StateFlags.sfFocused) != 0)
        {
            MakeDefault(enable);
        }
    }

    private void PressButton(ref TEvent ev)
    {
        // TODO: Implement button press animation
        Press();
    }

    private TRect GetActiveRect()
    {
        return GetExtent();
    }
}
