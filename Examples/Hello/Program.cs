using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Menus;

namespace Hello;

/// <summary>
/// Minimal TurboVision application with a menu bar and status line.
/// </summary>
public class HelloApp : TApplication
{
    public override TMenuBar? InitMenuBar(TRect r)
    {
        var menuRect = new TRect(r.A.X, r.A.Y, r.B.X, r.A.Y + 1);
        return new TMenuBar(menuRect,
            new TSubMenu("~F~ile", KeyConstants.kbAltF,
                new TMenuItem("~Q~uit", CommandConstants.cmQuit, KeyConstants.kbAltX)));
    }

    public override TStatusLine? InitStatusLine(TRect r)
    {
        var statusRect = new TRect(r.A.X, r.B.Y - 1, r.B.X, r.B.Y);
        return new TStatusLine(statusRect,
            new TStatusDef(0, 0xFFFF,
                new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit,
                new TStatusItem(null, KeyConstants.kbF10, CommandConstants.cmMenu))));
    }
}

public static class Program
{
    public static void Main()
    {
        using var app = new HelloApp();
        app.Run();
    }
}
