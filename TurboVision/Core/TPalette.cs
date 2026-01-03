namespace TurboVision.Core;

/// <summary>
/// Represents a color palette as an array of color attributes.
/// Views use palettes to map logical colors to actual display colors.
///
/// Following the C++ convention:
/// - Index 0 contains the length (number of color entries)
/// - Indices 1..length contain the actual color values (1-indexed)
/// </summary>
public class TPalette
{
    private TColorAttr[] _data;

    /// <summary>
    /// Creates a palette from byte color values.
    /// The length prefix is added automatically.
    /// </summary>
    public TPalette(ReadOnlySpan<byte> colors)
    {
        // Allocate length + 1 for the length prefix
        _data = new TColorAttr[colors.Length + 1];
        _data[0] = new TColorAttr((byte)colors.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            _data[i + 1] = new TColorAttr(colors[i]);
        }
    }

    /// <summary>
    /// Creates a palette from TColorAttr values.
    /// The length prefix is added automatically.
    /// </summary>
    public TPalette(ReadOnlySpan<TColorAttr> colors)
    {
        // Allocate length + 1 for the length prefix
        _data = new TColorAttr[colors.Length + 1];
        _data[0] = new TColorAttr((byte)colors.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            _data[i + 1] = colors[i];
        }
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public TPalette(TPalette other)
    {
        _data = new TColorAttr[other._data.Length];
        Array.Copy(other._data, _data, _data.Length);
    }

    /// <summary>
    /// Indexer for accessing palette entries.
    /// Index 0 returns the length; indices 1..length return color values.
    /// </summary>
    public TColorAttr this[int index]
    {
        get => _data[index];
        set => _data[index] = value;
    }

    /// <summary>
    /// Gets the total size of the internal array (includes length prefix).
    /// Use ColorCount for the number of actual color entries.
    /// </summary>
    public int Length => _data.Length;

    /// <summary>
    /// Gets the number of color entries (value stored at index 0).
    /// </summary>
    public int ColorCount => (byte)_data[0];

    /// <summary>
    /// Gets the raw data array.
    /// </summary>
    public ReadOnlySpan<TColorAttr> Data => _data;
}
