using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Modal dialog (TWindow + modal behavior).
/// </summary>
public class TDialog : TWindow
{
    private static readonly byte[] DialogPalette =
    [
        0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
        0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
        0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
    ];

    // Dialog palette entries
    public const byte dpBlueDialog = 0;
    public const byte dpCyanDialog = 1;
    public const byte dpGrayDialog = 2;

    public TDialog(TRect bounds, string? title) : base(bounds, title, WindowConstants.wnNoNumber)
    {
        GrowMode = 0;
        Flags = WindowFlags.wfMove | WindowFlags.wfClose;
        Palette = dpGrayDialog;
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DialogPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evKeyDown:
                switch (ev.KeyDown.KeyCode)
                {
                    case KeyConstants.kbEsc:
                        ev.What = EventConstants.evCommand;
                        ev.Message.Command = CommandConstants.cmCancel;
                        ev.Message.InfoPtr = null;
                        PutEvent(ev);
                        ClearEvent(ref ev);
                        break;
                    case KeyConstants.kbEnter:
                        ev.What = EventConstants.evBroadcast;
                        ev.Message.Command = CommandConstants.cmDefault;
                        ev.Message.InfoPtr = null;
                        PutEvent(ev);
                        ClearEvent(ref ev);
                        break;
                }
                break;

            case EventConstants.evCommand:
                switch (ev.Message.Command)
                {
                    case CommandConstants.cmOK:
                    case CommandConstants.cmCancel:
                    case CommandConstants.cmYes:
                    case CommandConstants.cmNo:
                        if (GetState(StateFlags.sfModal))
                        {
                            EndModal(ev.Message.Command);
                            ClearEvent(ref ev);
                        }
                        break;
                }
                break;
        }
    }

    public override bool Valid(ushort command)
    {
        if (command == CommandConstants.cmCancel)
        {
            return true;
        }
        return base.Valid(command);
    }
}
