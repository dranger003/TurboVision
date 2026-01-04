namespace TurboVision.Core;

/// <summary>
/// Style flags for TColorAttr.
/// </summary>
public static class ColorStyle
{
    public const ushort slBold = 0x001;
    public const ushort slItalic = 0x002;
    public const ushort slUnderline = 0x004;
    public const ushort slBlink = 0x008;
    public const ushort slReverse = 0x010;
    public const ushort slStrike = 0x020;
    /// <summary>
    /// Don't draw window shadows over this cell.
    /// </summary>
    public const ushort slNoShadow = 0x200;
}

/// <summary>
/// Represents color attributes for a screen cell (foreground, background, and style).
/// Matches upstream TColorAttr structure with 64-bit storage:
///   - 10 bits for style flags
///   - 27 bits for foreground (TColorDesired)
///   - 27 bits for background (TColorDesired)
///
/// TColorDesired supports:
///   - BIOS colors (4-bit, indexed 0-15)
///   - RGB colors (24-bit, 0xRRGGBB)
///   - XTerm colors (8-bit, 0-255 palette index)
///   - Default color (terminal default)
///
/// Examples:
///   // BIOS attribute (backward compatible)
///   TColorAttr a = new TColorAttr(0x3D);  // Fg: BIOS 0xD, Bg: BIOS 0x3
///
///   // Full color model
///   TColorAttr b = new TColorAttr(
///       TColorDesired.FromRGB(0x7F00BB),
///       TColorDesired.FromBIOS(0x0),
///       ColorStyle.slBold);
/// </summary>
public record struct TColorAttr : IEquatable<TColorAttr>
{
    // 64-bit storage: _style (10 bits) | _fg (27 bits) | _bg (27 bits)
    // We use ulong and manually pack/unpack the fields
    private ulong _data;

    private const int StyleBits = 10;
    private const int ColorBits = 27;
    private const ulong StyleMask = (1UL << StyleBits) - 1;         // 0x3FF
    private const ulong ColorMask = (1UL << ColorBits) - 1;         // 0x7FFFFFF
    private const int FgShift = StyleBits;                          // 10
    private const int BgShift = StyleBits + ColorBits;              // 37

    /// <summary>
    /// Creates a default TColorAttr (both fg and bg are terminal default).
    /// </summary>
    public TColorAttr()
    {
        _data = 0;
    }

    /// <summary>
    /// Creates a TColorAttr from a BIOS attribute byte (backward compatible).
    /// Low nibble = foreground, high nibble = background.
    /// </summary>
    public TColorAttr(uint bios)
    {
        var fg = TColorDesired.FromBIOS((byte)(bios & 0x0F));
        var bg = TColorDesired.FromBIOS((byte)((bios >> 4) & 0x0F));
        _data = Pack(0, fg.BitCast(), bg.BitCast());
    }

    /// <summary>
    /// Creates a TColorAttr from BIOS foreground and background values (backward compatible).
    /// </summary>
    public TColorAttr(byte foreground, byte background)
    {
        var fg = TColorDesired.FromBIOS((byte)(foreground & 0x0F));
        var bg = TColorDesired.FromBIOS((byte)(background & 0x0F));
        _data = Pack(0, fg.BitCast(), bg.BitCast());
    }

    /// <summary>
    /// Creates a TColorAttr from BIOS foreground, background, and style (backward compatible).
    /// </summary>
    public TColorAttr(byte foreground, byte background, ushort style)
    {
        var fg = TColorDesired.FromBIOS((byte)(foreground & 0x0F));
        var bg = TColorDesired.FromBIOS((byte)(background & 0x0F));
        _data = Pack(style, fg.BitCast(), bg.BitCast());
    }

    /// <summary>
    /// Creates a TColorAttr from TColorDesired foreground and background.
    /// </summary>
    public TColorAttr(TColorDesired foreground, TColorDesired background, ushort style = 0)
    {
        _data = Pack(style, foreground.BitCast(), background.BitCast());
    }

    private static ulong Pack(ushort style, uint fg, uint bg)
    {
        return ((ulong)(style & StyleMask)) |
               ((ulong)(fg & ColorMask) << FgShift) |
               ((ulong)(bg & ColorMask) << BgShift);
    }

    /// <summary>
    /// Gets the style flags.
    /// </summary>
    public readonly ushort Style
    {
        get => (ushort)(_data & StyleMask);
    }

    /// <summary>
    /// Sets the style flags.
    /// </summary>
    public void SetStyle(ushort style)
    {
        _data = (_data & ~StyleMask) | (ulong)(style & StyleMask);
    }

    /// <summary>
    /// Gets the foreground color as TColorDesired.
    /// </summary>
    public readonly TColorDesired ForegroundColor
    {
        get => TColorDesired.FromBitCast((uint)((_data >> FgShift) & ColorMask));
    }

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    public void SetForeground(TColorDesired color)
    {
        _data = (_data & ~(ColorMask << FgShift)) | ((ulong)(color.BitCast() & ColorMask) << FgShift);
    }

    /// <summary>
    /// Gets the background color as TColorDesired.
    /// </summary>
    public readonly TColorDesired BackgroundColor
    {
        get => TColorDesired.FromBitCast((uint)((_data >> BgShift) & ColorMask));
    }

    /// <summary>
    /// Sets the background color.
    /// </summary>
    public void SetBackground(TColorDesired color)
    {
        _data = (_data & ~(ColorMask << BgShift)) | ((ulong)(color.BitCast() & ColorMask) << BgShift);
    }

    /// <summary>
    /// Gets the foreground as a BIOS color byte (for backward compatibility).
    /// Returns the quantized value if not a BIOS color.
    /// </summary>
    public readonly byte Foreground
    {
        get => ForegroundColor.ToBIOS(true);
    }

    /// <summary>
    /// Gets the background as a BIOS color byte (for backward compatibility).
    /// Returns the quantized value if not a BIOS color.
    /// </summary>
    public readonly byte Background
    {
        get => BackgroundColor.ToBIOS(false);
    }

    /// <summary>
    /// Gets the BIOS-compatible byte value (fg in low nibble, bg in high nibble).
    /// </summary>
    public readonly uint Value
    {
        get => (uint)((Background << 4) | Foreground);
    }

    /// <summary>
    /// Gets whether this attribute is a simple BIOS color (no style flags, both colors are BIOS).
    /// </summary>
    public readonly bool IsBIOS
    {
        get => ForegroundColor.IsBIOS && BackgroundColor.IsBIOS && Style == 0;
    }

    /// <summary>
    /// Gets the BIOS-compatible byte representation (quantizes if necessary).
    /// If this is not a BIOS attribute, returns 0x5F as a sentinel value.
    /// </summary>
    public readonly byte AsBIOS()
    {
        if (IsBIOS)
            return (byte)((Background << 4) | Foreground);
        return 0x5F;  // Sentinel for non-BIOS attributes
    }

    /// <summary>
    /// Gets the BIOS-compatible byte representation through quantization.
    /// </summary>
    public readonly byte ToBIOS()
    {
        return (byte)((Background << 4) | Foreground);
    }

    public static implicit operator TColorAttr(byte value)
    {
        return new TColorAttr(value);
    }

    public static implicit operator byte(TColorAttr attr)
    {
        return attr.AsBIOS();
    }

    /// <summary>
    /// Reverses the foreground and background colors.
    /// Matches upstream reverseAttribute() behavior.
    /// </summary>
    public static TColorAttr ReverseAttribute(TColorAttr attr)
    {
        var fg = attr.ForegroundColor;
        var bg = attr.BackgroundColor;
        var style = attr.Style;

        // If either color is default, toggle the slReverse flag instead of swapping
        if (fg.IsDefault || bg.IsDefault)
        {
            return new TColorAttr(fg, bg, (ushort)(style ^ ColorStyle.slReverse));
        }

        // Swap foreground and background
        return new TColorAttr(bg, fg, style);
    }
}
