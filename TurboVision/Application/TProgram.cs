using TurboVision.Core;
using TurboVision.Menus;
using TurboVision.Platform;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Application skeleton and event loop.
/// </summary>
public class TProgram : TGroup
{
    // Application palette entries
    public const int apColor = 0;
    public const int apBlackWhite = 1;
    public const int apMonochrome = 2;

    // Application color palettes (128 bytes each + 8 help colors)
    // Translated from Reference/tvision/include/tvision/app.h
    private static readonly byte[] AppColorPalette =
    [
        0x71, 0x70, 0x78, 0x74, 0x20, 0x28, 0x24, 0x17, 0x1F, 0x1A, 0x31, 0x31, 0x1E, 0x71, 0x1F, // 1-15
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x3E, 0x21, 0x3F, 0x70, 0x7F, 0x7A, 0x13, 0x13, 0x70, 0x7F, 0x7E, // 16-31
        0x70, 0x7F, 0x7A, 0x13, 0x13, 0x70, 0x70, 0x7F, 0x7E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x70, 0x30, // 32-47
        0x3F, 0x3E, 0x1F, 0x2F, 0x1A, 0x20, 0x72, 0x31, 0x31, 0x30, 0x2F, 0x3E, 0x31, 0x13, 0x38, 0x00, // 48-63
        0x17, 0x1F, 0x1A, 0x71, 0x71, 0x1E, 0x17, 0x1F, 0x1E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x10, 0x30, // 64-79
        0x3F, 0x3E, 0x70, 0x2F, 0x7A, 0x20, 0x12, 0x31, 0x31, 0x30, 0x2F, 0x3E, 0x31, 0x13, 0x38, 0x00, // 80-95
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x3E, 0x30, 0x3F, 0x3E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x30, 0x70, // 96-111
        0x7F, 0x7E, 0x1F, 0x2F, 0x1A, 0x20, 0x32, 0x31, 0x71, 0x70, 0x2F, 0x7E, 0x71, 0x13, 0x78, 0x00, // 112-127
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x30, 0x3E, 0x1E // 128-135 (help colors)
    ];

    private static readonly byte[] AppBlackWhitePalette =
    [
        0x70, 0x70, 0x78, 0x7F, 0x07, 0x07, 0x0F, 0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x0F,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x07, 0x0F,
        0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x70, 0x7F, 0x7F, 0x07, 0x0F, 0x0F, 0x78, 0x0F, 0x78, 0x07,
        0x0F, 0x0F, 0x0F, 0x70, 0x0F, 0x07, 0x70, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x08, 0x00,
        0x07, 0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x0F, 0x0F, 0x70, 0x78, 0x7F, 0x08, 0x7F, 0x08, 0x70,
        0x7F, 0x7F, 0x7F, 0x0F, 0x70, 0x70, 0x07, 0x70, 0x70, 0x70, 0x07, 0x7F, 0x70, 0x07, 0x78, 0x00,
        0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x70, 0x7F, 0x7F, 0x07, 0x0F, 0x0F, 0x78, 0x0F, 0x78, 0x07,
        0x0F, 0x0F, 0x0F, 0x70, 0x0F, 0x07, 0x70, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x08, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x0F, 0x70
    ];

    private static readonly byte[] AppMonochromePalette =
    [
        0x70, 0x07, 0x07, 0x0F, 0x70, 0x70, 0x70, 0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x00, 0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x07, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x70, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x01, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x01, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x0F, 0x70
    ];

    // Cached palette instances
    private static TPalette? _colorPalette;
    private static TPalette? _blackWhitePalette;
    private static TPalette? _monochromePalette;

    // Static members
    public static TProgram? Application { get; set; }
    public static TStatusLine? StatusLine { get; set; }
    public static TMenuBar? MenuBar { get; set; }
    public static TDeskTop? DeskTop { get; set; }
    public static int AppPalette { get; set; } = apColor;
    public static int EventTimeoutMs { get; set; } = 50;

    protected static TEvent _pending;

    public TProgram() : base(new TRect(0, 0, TScreen.ScreenWidth, TScreen.ScreenHeight))
    {
        Application = this;
        State = StateFlags.sfVisible | StateFlags.sfSelected | StateFlags.sfFocused |
                StateFlags.sfModal | StateFlags.sfExposed;

        // Disable buffering at program level - we draw directly to screen
        Options &= unchecked((ushort)~OptionFlags.ofBuffered);

        InitScreen();

        // After InitScreen, screen dimensions may have changed. Update our bounds and clip.
        var screenRect = new TRect(0, 0, TScreen.ScreenWidth, TScreen.ScreenHeight);
        SetBounds(screenRect);
        Clip = GetExtent();

        var r = GetExtent();

        StatusLine = InitStatusLine(new TRect(r.A.X, r.B.Y - 1, r.B.X, r.B.Y));
        if (StatusLine != null)
        {
            Insert(StatusLine);
        }

        MenuBar = InitMenuBar(new TRect(r.A.X, r.A.Y, r.B.X, r.A.Y + 1));
        if (MenuBar != null)
        {
            Insert(MenuBar);
        }

        DeskTop = InitDeskTop(new TRect(r.A.X, r.A.Y + 1, r.B.X, r.B.Y - 1));
        if (DeskTop != null)
        {
            Insert(DeskTop);
        }
    }

    public virtual bool CanMoveFocus()
    {
        return DeskTop?.Valid(CommandConstants.cmReleasedFocus) ?? true;
    }

    public virtual ushort ExecuteDialog(TDialog? dialog, object? data = null)
    {
        if (dialog == null)
        {
            return CommandConstants.cmCancel;
        }

        if (data != null)
        {
            // TODO: SetData from object
        }

        ushort result = DeskTop?.ExecView(dialog) ?? CommandConstants.cmCancel;

        if (result != CommandConstants.cmCancel && data != null)
        {
            // TODO: GetData to object
        }

        dialog.Dispose();
        return result;
    }

    public override void GetEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;

        if (_pending.What != EventConstants.evNothing)
        {
            ev = _pending;
            _pending.What = EventConstants.evNothing;
        }
        else
        {
            // Wait for events before polling (like upstream tvision)
            TEventQueue.WaitForEvents(EventTimeoutMs);

            TEventQueue.GetMouseEvent(ref ev);
            if (ev.What == EventConstants.evNothing)
            {
                TEventQueue.GetKeyEvent(ref ev);
                if (ev.What == EventConstants.evNothing)
                {
                    Idle();
                }
            }
        }

        // Handle screen resize events
        if (ev.What == EventConstants.evCommand && ev.Message.Command == CommandConstants.cmScreenChanged)
        {
            SetScreenMode(TDisplay.smUpdate);
            ClearEvent(ref ev);
        }
    }

    public override TPalette? GetPalette()
    {
        return AppPalette switch
        {
            apBlackWhite => _blackWhitePalette ??= new TPalette(AppBlackWhitePalette),
            apMonochrome => _monochromePalette ??= new TPalette(AppMonochromePalette),
            _ => _colorPalette ??= new TPalette(AppColorPalette)
        };
    }

    public override void HandleEvent(ref TEvent ev)
    {
        // Handle Alt+1-9 window selection
        if (ev.What == EventConstants.evKeyDown)
        {
            char c = TStringUtils.GetAltChar(ev.KeyDown.KeyCode);
            if (c >= '1' && c <= '9')
            {
                if (CanMoveFocus())
                {
                    var result = Message(DeskTop, EventConstants.evBroadcast,
                        CommandConstants.cmSelectWindowNum, c - '0');
                    if (result != null)
                    {
                        ClearEvent(ref ev);
                    }
                }
                else
                {
                    ClearEvent(ref ev);
                }
            }
        }

        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand && ev.Message.Command == CommandConstants.cmQuit)
        {
            EndModal(CommandConstants.cmQuit);
            ClearEvent(ref ev);
        }
    }

    /// <summary>
    /// Helper to send a message to a target and return the result.
    /// </summary>
    protected static object? Message(TGroup? target, ushort what, ushort command, object? infoPtr)
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

    /// <summary>
    /// Helper to send a message with an integer info parameter.
    /// </summary>
    protected static object? Message(TGroup? target, ushort what, ushort command, int infoInt)
    {
        return Message(target, what, command, (object)infoInt);
    }

    public virtual void Idle()
    {
        if (StatusLine != null)
        {
            StatusLine.Update();
        }

        if (CommandSetChanged)
        {
            Message(this, EventConstants.evBroadcast, CommandConstants.cmCommandSetChanged, null);
            CommandSetChanged = false;
        }

        // Process expired timers
        TView.TimerQueue.ProcessTimers(HandleTimerExpired);
    }

    private void HandleTimerExpired(TTimerId id)
    {
        // Broadcast cmTimerExpired to all views
        TEvent ev = new()
        {
            What = EventConstants.evBroadcast
        };
        ev.Message.Command = CommandConstants.cmTimerExpired;
        ev.Message.InfoPtr = id;
        HandleEvent(ref ev);
    }

    public virtual void InitScreen()
    {
        // TODO: Initialize screen driver
    }

    public virtual void OutOfMemory()
    {
        // Override to handle out of memory
    }

    public override void PutEvent(TEvent ev)
    {
        _pending = ev;
    }

    public virtual void Run()
    {
        // Clear screen and trigger initial draw
        TScreen.ClearScreen();
        Redraw();
        Execute();
    }

    public virtual TWindow? InsertWindow(TWindow? window)
    {
        if (ValidView(window) != null)
        {
            if (CanMoveFocus())
            {
                DeskTop?.Insert(window!);
                return window;
            }
            else
            {
                window?.Dispose();
            }
        }
        return null;
    }

    public void SetScreenMode(ushort mode)
    {
        TScreen.SetVideoMode(mode);
        InitScreen();
        var r = new TRect(0, 0, TScreen.ScreenWidth, TScreen.ScreenHeight);
        ChangeBounds(r);
        SetState(StateFlags.sfExposed, false);
        SetState(StateFlags.sfExposed, true);
        Redraw();
    }

    public TView? ValidView(TView? p)
    {
        if (p == null)
        {
            return null;
        }

        if (!p.Valid(CommandConstants.cmValid))
        {
            p.Dispose();
            return null;
        }

        return p;
    }

    public virtual TStatusLine? InitStatusLine(TRect r)
    {
        return null; // Override in derived class
    }

    public virtual TMenuBar? InitMenuBar(TRect r)
    {
        return null; // Override in derived class
    }

    public virtual TDeskTop? InitDeskTop(TRect r)
    {
        return new TDeskTop(r);
    }

    public virtual void Suspend()
    {
    }

    public virtual void Resume()
    {
    }

    public override void ShutDown()
    {
        StatusLine = null;
        MenuBar = null;
        DeskTop = null;
        Application = null;
        base.ShutDown();
    }
}
