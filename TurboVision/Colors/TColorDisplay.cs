using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A view that displays text with the current color selection.
/// </summary>
public class TColorDisplay : TView
{
    private byte _colorValue;
    private readonly string _text;

    public TColorDisplay(TRect bounds, string text) : base(bounds)
    {
        EventMask |= EventConstants.evBroadcast;
        _text = text;
        _colorValue = 0;
    }

    public override void Draw()
    {
        byte c = _colorValue;
        if (c == 0)
        {
            c = ErrorAttr;
        }

        var b = new TDrawBuffer();
        int len = _text.Length;
        if (len == 0) len = 1;

        for (int i = 0; i <= Size.X / len; i++)
        {
            b.MoveStr(i * len, _text, new TColorAttr(c));
        }

        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast)
        {
            switch (ev.Message.Command)
            {
                case ColorCommands.cmColorBackgroundChanged:
                    _colorValue = (byte)((_colorValue & 0x0F) | ((ev.Message.InfoInt << 4) & 0xF0));
                    DrawView();
                    break;

                case ColorCommands.cmColorForegroundChanged:
                    _colorValue = (byte)((_colorValue & 0xF0) | (ev.Message.InfoInt & 0x0F));
                    DrawView();
                    break;
            }
        }
    }

    /// <summary>
    /// Sets the color from a palette entry and broadcasts the change.
    /// </summary>
    public void SetColor(byte colorValue)
    {
        _colorValue = colorValue;
        SendMessage(Owner, EventConstants.evBroadcast, ColorCommands.cmColorSet, (int)_colorValue);
        DrawView();
    }

    /// <summary>
    /// Gets the current color value.
    /// </summary>
    public byte GetColorValue()
    {
        return _colorValue;
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
