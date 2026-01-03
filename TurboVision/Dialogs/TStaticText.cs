using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Read-only static text label.
/// </summary>
public class TStaticText : TView
{
    private static readonly byte[] DefaultPalette = [0x06];

    protected string? Text { get; set; }

    public TStaticText(TRect bounds, string? text) : base(bounds)
    {
        Text = text;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(0x01);

        b.MoveChar(0, ' ', color.Normal, Size.X);

        if (!string.IsNullOrEmpty(Text))
        {
            b.MoveCStr(0, Text, new TAttrPair(color.Normal, color.Normal));
        }

        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public virtual void GetText(Span<char> dest)
    {
        if (Text != null)
        {
            int len = Math.Min(Text.Length, dest.Length);
            Text.AsSpan(0, len).CopyTo(dest);
        }
    }
}
