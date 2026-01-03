using TurboVision.Core;
using TurboVision.Platform;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Full application with menus and status line.
/// </summary>
public class TApplication : TProgram
{
    public TApplication() : base()
    {
        // Initialize platform
        // TODO: Create and initialize platform drivers
    }

    public override void Suspend()
    {
        TScreen.Suspend();
        TEventQueue.Suspend();
    }

    public override void Resume()
    {
        TEventQueue.Resume();
        TScreen.Resume();
        Redraw();
    }

    public void Cascade()
    {
        if (DeskTop != null)
        {
            DeskTop.Cascade(GetTileRect());
        }
    }

    public void Tile()
    {
        if (DeskTop != null)
        {
            DeskTop.Tile(GetTileRect());
        }
    }

    public virtual TRect GetTileRect()
    {
        return DeskTop?.GetExtent() ?? GetExtent();
    }

    public void DosShell()
    {
        Suspend();
        WriteShellMsg();
        // TODO: Execute shell
        Resume();
    }

    public virtual void WriteShellMsg()
    {
        Console.WriteLine("Type EXIT to return...");
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmTile:
                    ClearEvent(ref ev);
                    Tile();
                    break;
                case CommandConstants.cmCascade:
                    ClearEvent(ref ev);
                    Cascade();
                    break;
                case CommandConstants.cmDosShell:
                    ClearEvent(ref ev);
                    DosShell();
                    break;
            }
        }
    }
}
