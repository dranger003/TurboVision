using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Menus;

namespace Hello;

/// <summary>
/// Minimal TurboVision application with a menu bar.
/// </summary>
public class HelloApp : TApplication
{
    public override TMenuBar? InitMenuBar(TRect r)
    {
        // Create new rect for menu bar (one row high)
        var menuRect = new TRect(r.A.X, r.A.Y, r.B.X, r.A.Y + 1);
        return new TMenuBar(menuRect,
            new TSubMenu("~F~ile", KeyConstants.kbAltF,
                new TMenuItem("~Q~uit", CommandConstants.cmQuit, KeyConstants.kbAltX)));
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
