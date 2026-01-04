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
/// Matches upstream TColorAttr structure with support for style flags.
/// </summary>
public record struct TColorAttr : IEquatable<TColorAttr>
{
    // Store as BIOS-compatible byte for foreground/background, plus style
    private byte _fg;
    private byte _bg;
    private ushort _style;

    public TColorAttr(byte foreground, byte background)
    {
        _fg = (byte)(foreground & 0x0F);
        _bg = (byte)(background & 0x0F);
        _style = 0;
    }

    public TColorAttr(byte foreground, byte background, ushort style)
    {
        _fg = (byte)(foreground & 0x0F);
        _bg = (byte)(background & 0x0F);
        _style = style;
    }

    public TColorAttr(uint value)
    {
        // Legacy BIOS-style constructor (low nibble = fg, high nibble = bg)
        _fg = (byte)(value & 0x0F);
        _bg = (byte)((value >> 4) & 0x0F);
        _style = 0;
    }

    public byte Foreground
    {
        readonly get { return _fg; }
        set { _fg = (byte)(value & 0x0F); }
    }

    public byte Background
    {
        readonly get { return _bg; }
        set { _bg = (byte)(value & 0x0F); }
    }

    public ushort Style
    {
        readonly get { return _style; }
        set { _style = value; }
    }

    /// <summary>
    /// Gets the BIOS-compatible byte value (fg in low nibble, bg in high nibble).
    /// </summary>
    public readonly uint Value
    {
        get { return (uint)((_bg << 4) | _fg); }
    }

    /// <summary>
    /// Gets whether this attribute is a simple BIOS color (no style flags).
    /// </summary>
    public readonly bool IsBIOS
    {
        get { return _style == 0; }
    }

    /// <summary>
    /// Gets the BIOS-compatible byte representation.
    /// </summary>
    public readonly byte ToBIOS()
    {
        return (byte)((_bg << 4) | _fg);
    }

    public static implicit operator TColorAttr(byte value)
    {
        return new TColorAttr(value);
    }

    public static implicit operator byte(TColorAttr attr)
    {
        return attr.ToBIOS();
    }

    /// <summary>
    /// Reverses the foreground and background colors.
    /// </summary>
    public static TColorAttr ReverseAttribute(TColorAttr attr)
    {
        return new TColorAttr(attr._bg, attr._fg, attr._style);
    }
}
