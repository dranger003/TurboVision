using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Desktop background pattern view.
/// </summary>
public class TBackground : TView
{
    private static readonly byte[] DefaultPalette = [0x01];

    public char Pattern { get; set; }

    public TBackground(TRect bounds, char pattern) : base(bounds)
    {
        Pattern = pattern;
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(0x01);

        b.MoveChar(0, Pattern, color.Normal, Size.X);

        for (int y = 0; y < Size.Y; y++)
        {
            WriteLine(0, y, Size.X, 1, b);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }
}
