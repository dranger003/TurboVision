namespace TurboVision.Core;

/// <summary>
/// Represents a color palette as an array of color attributes.
/// Views use palettes to map logical colors to actual display colors.
/// </summary>
public class TPalette
{
    private TColorAttr[] _data;

    public TPalette(ReadOnlySpan<byte> colors)
    {
        _data = new TColorAttr[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            _data[i] = new TColorAttr(colors[i]);
        }
    }

    public TPalette(ReadOnlySpan<TColorAttr> colors)
    {
        _data = colors.ToArray();
    }

    public TPalette(TPalette other)
    {
        _data = new TColorAttr[other._data.Length];
        Array.Copy(other._data, _data, _data.Length);
    }

    public TColorAttr this[int index]
    {
        get { return _data[index]; }
        set { _data[index] = value; }
    }

    public int Length
    {
        get { return _data.Length; }
    }

    public ReadOnlySpan<TColorAttr> Data
    {
        get { return _data; }
    }
}
