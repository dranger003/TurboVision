using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A 4x4 color selector grid.
/// </summary>
public class TColorSelector : TView
{
    public enum ColorSel { csBackground, csForeground }

    private const char Icon = '\u00DB'; // Full block character

    public byte Color { get; protected set; }
    public ColorSel SelType { get; }

    public TColorSelector(TRect bounds, ColorSel selType) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick | OptionFlags.ofFramed;
        EventMask |= EventConstants.evBroadcast;
        SelType = selType;
        Color = 0;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, ' ', new TColorAttr(0x70), Size.X);

        for (int i = 0; i <= Size.Y; i++)
        {
            if (i < 4)
            {
                for (int j = 0; j < 4; j++)
                {
                    int c = i * 4 + j;
                    b.MoveChar(j * 3, Icon, new TColorAttr((byte)c), 3);
                    if (c == Color)
                    {
                        b.PutChar(j * 3 + 1, (char)8); // Mark selected
                        if (c == 0)
                        {
                            b.PutAttribute(j * 3 + 1, new TColorAttr(0x70));
                        }
                    }
                }
            }
            WriteLine(0, i, Size.X, 1, b);
        }
    }

    private void ColorChanged()
    {
        ushort msg = SelType == ColorSel.csForeground
            ? ColorCommands.cmColorForegroundChanged
            : ColorCommands.cmColorBackgroundChanged;
        SendMessage(Owner, EventConstants.evBroadcast, msg, (int)Color);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        const int width = 4;

        base.HandleEvent(ref ev);

        byte oldColor = Color;
        int maxCol = (SelType == ColorSel.csBackground) ? 7 : 15;

        switch (ev.What)
        {
            case EventConstants.evMouseDown:
                do
                {
                    if (MouseInView(ev.Mouse.Where))
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        Color = (byte)(mouse.Y * 4 + mouse.X / 3);
                    }
                    else
                    {
                        Color = oldColor;
                    }
                    ColorChanged();
                    DrawView();
                } while (MouseEvent(ref ev, EventConstants.evMouseMove));
                break;

            case EventConstants.evKeyDown:
                switch (TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode))
                {
                    case KeyConstants.kbLeft:
                        if (Color > 0)
                            Color--;
                        else
                            Color = (byte)maxCol;
                        break;

                    case KeyConstants.kbRight:
                        if (Color < maxCol)
                            Color++;
                        else
                            Color = 0;
                        break;

                    case KeyConstants.kbUp:
                        if (Color > width - 1)
                            Color -= width;
                        else if (Color == 0)
                            Color = (byte)maxCol;
                        else
                            Color += (byte)(maxCol - width);
                        break;

                    case KeyConstants.kbDown:
                        if (Color < maxCol - (width - 1))
                            Color += width;
                        else if (Color == maxCol)
                            Color = 0;
                        else
                            Color -= (byte)(maxCol - width);
                        break;

                    default:
                        return;
                }
                break;

            case EventConstants.evBroadcast:
                if (ev.Message.Command == ColorCommands.cmColorSet)
                {
                    if (SelType == ColorSel.csBackground)
                        Color = (byte)(ev.Message.InfoInt >> 4);
                    else
                        Color = (byte)(ev.Message.InfoInt & 0x0F);
                    DrawView();
                    return;
                }
                else
                {
                    return;
                }

            default:
                return;
        }

        DrawView();
        ColorChanged();
        ClearEvent(ref ev);
    }

    private static void SendMessage(TGroup? owner, ushort what, ushort command, int infoInt)
    {
        if (owner == null) return;

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoInt }
        };

        owner.HandleEvent(ref ev);
    }
}
