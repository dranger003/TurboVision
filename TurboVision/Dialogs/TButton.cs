using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Platform;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Clickable push button.
/// </summary>
public class TButton : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TButton";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette =
    [
        0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0E, 0x0E, 0x0F
    ];

    // Shadow characters for button rendering
    private const char ShadowTopRight = '\u2584'; // ▄ or use space
    private const char ShadowRight = '\u2588';    // █ or use space
    private const char ShadowBottom = '\u2580';   // ▀ or use space

    // Internal command constants
    private const ushort cmGrabDefault = 61;
    private const ushort cmReleaseDefault = 62;
    private const int AnimationDurationMs = 100;

    public string? Title { get; set; }

    [JsonPropertyName("command")]
    public ushort Command { get; set; }

    [JsonPropertyName("flags")]
    public byte Flags { get; set; }

    [JsonPropertyName("amDefault")]
    public bool AmDefault { get; set; }

    [JsonIgnore]
    private TTimerId _animationTimer;

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TButton() : base()
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofPreProcess | OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;
    }

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
        TAttrPair cButton;
        TColorAttr cShadow;

        if (GetState(StateFlags.sfDisabled))
        {
            cButton = GetColor(0x0404);
        }
        else
        {
            cButton = GetColor(0x0501);
            if (GetState(StateFlags.sfActive))
            {
                if (GetState(StateFlags.sfSelected))
                {
                    cButton = GetColor(0x0703);
                }
                else if (AmDefault)
                {
                    cButton = GetColor(0x0602);
                }
            }
        }

        cShadow = GetColor(8).Normal;

        int s = Size.X - 1;
        int titleY = Size.Y / 2 - 1;
        char ch = ' ';

        // Draw each row of the button
        for (int y = 0; y <= Size.Y - 2; y++)
        {
            int i;
            b.MoveChar(0, ' ', cButton.Normal, Size.X);
            b.PutAttribute(0, cShadow);

            if (down)
            {
                // When pressed, shift button right
                b.PutAttribute(1, cShadow);
                ch = ' ';
                i = 2;
            }
            else
            {
                // Normal state - shadow on right side
                b.PutAttribute(s, cShadow);
                if (ShowMarkers)
                {
                    ch = ' ';
                }
                else
                {
                    if (y == 0)
                    {
                        b.PutChar(s, ShadowTopRight);
                    }
                    else
                    {
                        b.PutChar(s, ShadowRight);
                    }
                    ch = ShadowBottom;
                }
                i = 1;
            }

            if (y == titleY && Title != null)
            {
                DrawTitle(b, s, i, cButton, down);
            }

            if (ShowMarkers && !down)
            {
                b.PutChar(1, '[');
                b.PutChar(s - 1, ']');
            }

            WriteLine(0, y, Size.X, 1, b);
        }

        // Draw bottom shadow row
        b.MoveChar(0, ' ', cShadow, 2);
        b.MoveChar(2, ch, cShadow, s - 1);
        WriteLine(0, Size.Y - 1, Size.X, 1, b);
    }

    private void DrawTitle(TDrawBuffer b, int s, int indent, TAttrPair cButton, bool down)
    {
        if (Title == null)
        {
            return;
        }

        int l;
        if ((Flags & CommandConstants.bfLeftJust) != 0)
        {
            l = 1;
        }
        else
        {
            l = (s - TStringUtils.CstrLen(Title) - 1) / 2;
            if (l < 1)
            {
                l = 1;
            }
        }

        b.MoveCStr(indent + l, Title, cButton);

        // Draw selection markers if ShowMarkers is enabled and not pressed
        if (ShowMarkers && !down)
        {
            int scOff;
            if (GetState(StateFlags.sfSelected))
            {
                scOff = 0;
            }
            else if (AmDefault)
            {
                scOff = 2;
            }
            else
            {
                scOff = 4;
            }

            // Special marker characters at position 0 and s
            char leftMarker = scOff == 0 ? '\u00BB' : (scOff == 2 ? '\u00AB' : ' '); // » «
            char rightMarker = scOff == 0 ? '\u00AB' : (scOff == 2 ? '\u00BB' : ' ');
            b.PutChar(0, leftMarker);
            b.PutChar(s, rightMarker);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        var clickRect = GetExtent();
        clickRect = new TRect(clickRect.A.X + 1, clickRect.A.Y, clickRect.B.X - 1, clickRect.B.Y - 1);

        if (ev.What == EventConstants.evMouseDown)
        {
            var mouse = MakeLocal(ev.Mouse.Where);
            if (!clickRect.Contains(mouse))
            {
                ClearEvent(ref ev);
            }
        }

        if ((Flags & CommandConstants.bfGrabFocus) != 0)
        {
            base.HandleEvent(ref ev);
        }

        char c = Title != null ? TStringUtils.HotKey(Title) : '\0';

        switch (ev.What)
        {
            case var _ when ev.What == EventConstants.evMouseDown:
                if (!GetState(StateFlags.sfDisabled))
                {
                    clickRect = new TRect(clickRect.A.X, clickRect.A.Y, clickRect.B.X + 1, clickRect.B.Y);
                    bool down = false;

                    do
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        if (down != clickRect.Contains(mouse))
                        {
                            down = !down;
                            DrawState(down);
                        }
                    } while (MouseEvent(ref ev, EventConstants.evMouseMove));

                    if (down)
                    {
                        Press();
                        DrawState(false);
                    }
                }
                ClearEvent(ref ev);
                break;

            case var _ when ev.What == EventConstants.evKeyDown:
                // Check for Alt+hotkey, space when focused, or direct letter match
                if (ev.KeyDown.KeyCode != 0 &&
                    (ev.KeyDown.KeyCode == TStringUtils.GetAltCode(c) ||
                     (Owner?.Phase == PhaseType.phPostProcess &&
                      c != '\0' &&
                      c == char.ToUpperInvariant((char)ev.KeyDown.CharCode)) ||
                     (GetState(StateFlags.sfFocused) && ev.KeyDown.CharCode == ' ')))
                {
                    // Start animation with timer
                    DrawState(true);
                    if (_animationTimer == default)
                    {
                        _animationTimer = SetTimer(AnimationDurationMs);
                    }
                    ClearEvent(ref ev);
                }
                break;

            case var _ when ev.What == EventConstants.evBroadcast:
                switch (ev.Message.Command)
                {
                    case CommandConstants.cmDefault:
                        if (AmDefault && !GetState(StateFlags.sfDisabled))
                        {
                            // Start animation with timer
                            DrawState(true);
                            if (_animationTimer == default)
                            {
                                _animationTimer = SetTimer(AnimationDurationMs);
                            }
                            ClearEvent(ref ev);
                        }
                        break;

                    case cmGrabDefault:
                    case cmReleaseDefault:
                        if ((Flags & CommandConstants.bfDefault) != 0)
                        {
                            AmDefault = ev.Message.Command == cmReleaseDefault;
                            DrawView();
                        }
                        break;

                    case CommandConstants.cmCommandSetChanged:
                        SetState(StateFlags.sfDisabled, !CommandEnabled(Command));
                        DrawView();
                        break;

                    case CommandConstants.cmTimerExpired:
                        // Compare timer IDs properly - InfoPtr contains a boxed TTimerId
                        if (_animationTimer != default &&
                            ev.Message.InfoPtr is TTimerId timerId &&
                            timerId == _animationTimer)
                        {
                            _animationTimer = default;
                            DrawState(false);
                            Press();
                            ClearEvent(ref ev);
                        }
                        break;
                }
                break;
        }
    }

    public void MakeDefault(bool enable)
    {
        if ((Flags & CommandConstants.bfDefault) == 0)
        {
            // Broadcast to other buttons to grab/release default status
            Message(Owner, EventConstants.evBroadcast,
                enable ? cmGrabDefault : cmReleaseDefault, 0);
            AmDefault = enable;
            DrawView();
        }
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

    public virtual void Press()
    {
        Message(Owner, EventConstants.evBroadcast, CommandConstants.cmRecordHistory, 0);

        if ((Flags & CommandConstants.bfBroadcast) != 0)
        {
            Message(Owner, EventConstants.evBroadcast, Command, this);
        }
        else
        {
            TEvent ev = TEvent.Command(Command, this);
            PutEvent(ev);
        }
    }

    private TRect GetActiveRect()
    {
        return GetExtent();
    }

    // Helper to send a message to a target
    private static object? Message(TGroup? target, ushort what, ushort command, object? infoPtr)
    {
        if (target != null)
        {
            TEvent ev = new()
            {
                What = what
            };
            ev.Message.Command = command;
            ev.Message.InfoPtr = infoPtr;
            target.HandleEvent(ref ev);
            if (ev.What == EventConstants.evNothing)
            {
                return ev.Message.InfoPtr;
            }
        }
        return null;
    }

    public override void ShutDown()
    {
        if (_animationTimer != default)
        {
            KillTimer(_animationTimer);
            _animationTimer = default;
        }
        base.ShutDown();
    }
}
