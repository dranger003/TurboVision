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
        // TODO: Return appropriate palette based on AppPalette
        return null;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmQuit:
                    ClearEvent(ref ev);
                    EndModal(CommandConstants.cmQuit);
                    break;
            }
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            switch (ev.KeyDown.KeyCode)
            {
                case KeyConstants.kbAltX:
                    ClearEvent(ref ev);
                    EndModal(CommandConstants.cmQuit);
                    break;
            }
        }
    }

    public virtual void Idle()
    {
        if (StatusLine != null)
        {
            StatusLine.Update();
        }

        if (CommandSetChanged)
        {
            // TODO: Broadcast command set changed
            CommandSetChanged = false;
        }
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
        if (window != null && ValidView(window) != null)
        {
            DeskTop?.Insert(window);
            return window;
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
