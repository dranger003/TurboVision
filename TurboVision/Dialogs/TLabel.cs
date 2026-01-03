using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Label linked to another control.
/// </summary>
public class TLabel : TStaticText
{
    private static readonly byte[] DefaultPalette = [0x07, 0x08, 0x09, 0x09];

    protected TView? Link { get; set; }
    protected bool Light { get; set; }

    public TLabel(TRect bounds, string? text, TView? link) : base(bounds, text)
    {
        Link = link;
        Options |= OptionFlags.ofPreProcess | OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        byte colorIndex = (byte)(Light ? 2 : 1);
        var color = GetColor((ushort)((colorIndex << 8) | colorIndex));
        var scOff = GetColor(0x0304);

        b.MoveChar(0, ' ', color.Normal, Size.X);

        if (!string.IsNullOrEmpty(Text))
        {
            b.MoveCStr(0, Text, new TAttrPair(color.Normal, scOff.Normal));
        }

        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            if (Link != null)
            {
                Link.Select();
                ClearEvent(ref ev);
            }
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Check for shortcut key
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == CommandConstants.cmReceivedFocus ||
                ev.Message.Command == CommandConstants.cmReleasedFocus)
            {
                Light = ev.Message.Command == CommandConstants.cmReceivedFocus &&
                        ev.Message.InfoPtr == (nint)(Link?.GetHashCode() ?? 0);
                DrawView();
            }
        }
    }

    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }
}
