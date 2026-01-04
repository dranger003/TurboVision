using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Label linked to another control.
/// </summary>
public class TLabel : TStaticText
{
    private static readonly byte[] DefaultPalette = [0x07, 0x08, 0x09, 0x09];

    // Special characters for showMarkers mode: », «, →, ←, ' ', ' '
    private static readonly char[] SpecialChars = ['\u00BB', '\u00AB', '\u001A', '\u001B', ' ', ' '];

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
        TAttrPair color;
        int scOff;

        if (Light)
        {
            color = GetColor(0x0402);
            scOff = 0;
        }
        else
        {
            color = GetColor(0x0301);
            scOff = 4;
        }

        b.MoveChar(0, ' ', color.Normal, Size.X);

        if (!string.IsNullOrEmpty(Text))
        {
            b.MoveCStr(1, Text, color);
        }

        if (ShowMarkers)
        {
            b.PutChar(0, SpecialChars[scOff]);
        }

        WriteLine(0, 0, Size.X, 1, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    private void FocusLink(ref TEvent ev)
    {
        if (Link != null && (Link.Options & OptionFlags.ofSelectable) != 0)
        {
            Link.Focus();
        }
        ClearEvent(ref ev);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            FocusLink(ref ev);
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            char c = TStringUtils.HotKey(Text ?? "");
            if (ev.KeyDown.KeyCode != 0 &&
                (TStringUtils.GetAltCode(c) == ev.KeyDown.KeyCode ||
                 (c != '\0' && Owner?.Phase == PhaseType.phPostProcess &&
                  c == char.ToUpperInvariant((char)ev.KeyDown.CharCode))))
            {
                FocusLink(ref ev);
            }
        }
        else if (ev.What == EventConstants.evBroadcast && Link != null &&
                 (ev.Message.Command == CommandConstants.cmReceivedFocus ||
                  ev.Message.Command == CommandConstants.cmReleasedFocus))
        {
            Light = (Link.State & StateFlags.sfFocused) != 0;
            DrawView();
        }
    }

    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }
}
