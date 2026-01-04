using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Menus;
using TurboVision.Views;

namespace Hello;

/// <summary>
/// Turbo Vision Hello World Demo - C# port
/// </summary>
public class HelloApp : TApplication
{
    public const ushort GreetThemCmd = 100;

    public override TMenuBar? InitMenuBar(TRect r)
    {
        var menuRect = new TRect(r.A.X, r.A.Y, r.B.X, r.A.Y + 1);

        // Build menu items: Greeting -> separator -> Exit
        var exitItem = new TMenuItem("E~x~it", CommandConstants.cmQuit, KeyConstants.kbAltX,
            HelpContexts.hcNoContext, "Alt-X");
        var separator = TMenuItem.NewLine();
        separator.Next = exitItem;
        var greetingItem = new TMenuItem("~G~reeting...", GreetThemCmd, KeyConstants.kbAltG,
            HelpContexts.hcNoContext, null, separator);

        return new TMenuBar(menuRect,
            new TSubMenu("~H~ello", KeyConstants.kbAltH, greetingItem));
    }

    public override TStatusLine? InitStatusLine(TRect r)
    {
        var statusRect = new TRect(r.A.X, r.B.Y - 1, r.B.X, r.B.Y);
        return new TStatusLine(statusRect,
            new TStatusDef(0, 0xFFFF,
                new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit,
                new TStatusItem(null, KeyConstants.kbF10, CommandConstants.cmMenu))));
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case GreetThemCmd:
                    GreetingBox();
                    ClearEvent(ref ev);
                    break;
            }
        }
    }

    private void GreetingBox()
    {
        var d = new TDialog(new TRect(25, 5, 55, 16), "Hello, World!");

        d.Insert(new TStaticText(new TRect(3, 5, 15, 6), "How are you?"));
        d.Insert(new TButton(new TRect(16, 2, 28, 4), "Terrific", CommandConstants.cmCancel, CommandConstants.bfNormal));
        d.Insert(new TButton(new TRect(16, 4, 28, 6), "Ok", CommandConstants.cmCancel, CommandConstants.bfNormal));
        d.Insert(new TButton(new TRect(16, 6, 28, 8), "Lousy", CommandConstants.cmCancel, CommandConstants.bfNormal));
        d.Insert(new TButton(new TRect(16, 8, 28, 10), "Cancel", CommandConstants.cmCancel, CommandConstants.bfNormal));

        DeskTop?.ExecView(d);
        d.Dispose();
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
