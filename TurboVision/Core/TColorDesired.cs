namespace TurboVision.Core;

/// <summary>
/// Color type discriminators for TColorDesired.
/// </summary>
public static class ColorType
{
    public const byte ctDefault = 0x0;  // Terminal default color
    public const byte ctBIOS = 0x1;     // 4-bit BIOS color (0-15)
    public const byte ctRGB = 0x2;      // 24-bit RGB color (0xRRGGBB)
    public const byte ctXTerm = 0x3;    // 8-bit XTerm palette index (0-255)
}

/// <summary>
/// Represents a 24-bit RGB color.
/// </summary>
public readonly record struct TColorRGB
{
    private readonly uint _data;

    public TColorRGB(uint rgb)
    {
        _data = rgb & 0xFFFFFF;
    }

    public TColorRGB(byte r, byte g, byte b)
    {
        _data = ((uint)r << 16) | ((uint)g << 8) | b;
    }

    public byte R => (byte)((_data >> 16) & 0xFF);
    public byte G => (byte)((_data >> 8) & 0xFF);
    public byte B => (byte)(_data & 0xFF);

    public static implicit operator uint(TColorRGB rgb) => rgb._data;
    public static implicit operator TColorRGB(uint value) => new(value);
}

/// <summary>
/// Represents a 4-bit BIOS color (0-15).
/// </summary>
public readonly record struct TColorBIOS
{
    private readonly byte _data;

    public TColorBIOS(byte value)
    {
        _data = (byte)(value & 0x0F);
    }

    public static implicit operator byte(TColorBIOS bios) => bios._data;
    public static implicit operator TColorBIOS(byte value) => new(value);
}

/// <summary>
/// Represents an 8-bit XTerm palette index (0-255).
/// </summary>
public readonly record struct TColorXTerm
{
    private readonly byte _data;

    public TColorXTerm(byte index)
    {
        _data = index;
    }

    public byte Index => _data;

    public static implicit operator byte(TColorXTerm xterm) => xterm._data;
    public static implicit operator TColorXTerm(byte value) => new(value);
}

/// <summary>
/// A union type representing different kinds of colors: BIOS, RGB, XTerm, or Default.
/// Matches the upstream TColorDesired structure from colors.h.
///
/// The data is stored as a 32-bit value:
/// - Bits 0-23: Color value (depends on type)
/// - Bits 24-31: Color type (ctDefault, ctBIOS, ctRGB, ctXTerm)
///
/// Examples:
/// - BIOS color: TColorDesired bios = TColorDesired.FromBIOS(0x0F);
/// - RGB color:  TColorDesired rgb = TColorDesired.FromRGB(0x7F00BB);
/// - Default:    TColorDesired def = default; // or TColorDesired.Default
/// </summary>
public readonly record struct TColorDesired
{
    private readonly uint _data;

    private TColorDesired(uint data)
    {
        _data = data;
    }

    /// <summary>
    /// Creates a TColorDesired from raw bit-cast data.
    /// </summary>
    public static TColorDesired FromBitCast(uint data) => new(data);

    /// <summary>
    /// Gets the raw bit representation.
    /// </summary>
    public uint BitCast() => _data;

    /// <summary>
    /// Creates a default color (terminal default).
    /// </summary>
    public static TColorDesired Default => new(0);

    /// <summary>
    /// Creates a BIOS color (4-bit, 0-15).
    /// </summary>
    public static TColorDesired FromBIOS(byte bios)
    {
        return new((uint)((bios & 0x0F) | (ColorType.ctBIOS << 24)));
    }

    /// <summary>
    /// Creates an RGB color (24-bit).
    /// </summary>
    public static TColorDesired FromRGB(uint rgb)
    {
        return new((rgb & 0xFFFFFF) | ((uint)ColorType.ctRGB << 24));
    }

    /// <summary>
    /// Creates an RGB color from components.
    /// </summary>
    public static TColorDesired FromRGB(byte r, byte g, byte b)
    {
        return FromRGB(((uint)r << 16) | ((uint)g << 8) | b);
    }

    /// <summary>
    /// Creates an XTerm palette color (8-bit index).
    /// </summary>
    public static TColorDesired FromXTerm(byte index)
    {
        return new((uint)(index | (ColorType.ctXTerm << 24)));
    }

    /// <summary>
    /// Gets the color type.
    /// </summary>
    public byte Type => (byte)(_data >> 24);

    /// <summary>
    /// Returns true if this is the terminal default color.
    /// </summary>
    public bool IsDefault => Type == ColorType.ctDefault;

    /// <summary>
    /// Returns true if this is a BIOS color.
    /// </summary>
    public bool IsBIOS => Type == ColorType.ctBIOS;

    /// <summary>
    /// Returns true if this is an RGB color.
    /// </summary>
    public bool IsRGB => Type == ColorType.ctRGB;

    /// <summary>
    /// Returns true if this is an XTerm palette color.
    /// </summary>
    public bool IsXTerm => Type == ColorType.ctXTerm;

    /// <summary>
    /// Gets the BIOS color value. Only meaningful if IsBIOS is true.
    /// </summary>
    public TColorBIOS AsBIOS => new((byte)(_data & 0x0F));

    /// <summary>
    /// Gets the RGB color value. Only meaningful if IsRGB is true.
    /// </summary>
    public TColorRGB AsRGB => new(_data & 0xFFFFFF);

    /// <summary>
    /// Gets the XTerm palette index. Only meaningful if IsXTerm is true.
    /// </summary>
    public TColorXTerm AsXTerm => new((byte)(_data & 0xFF));

    /// <summary>
    /// Converts this color to a 4-bit BIOS color through quantization.
    /// </summary>
    /// <param name="isForeground">True if this is a foreground color (affects default behavior).</param>
    public TColorBIOS ToBIOS(bool isForeground)
    {
        return Type switch
        {
            ColorType.ctBIOS => AsBIOS,
            ColorType.ctRGB => ColorConversion.RGBtoBIOS(AsRGB),
            ColorType.ctXTerm => ColorConversion.XTermToBIOS(AsXTerm),
            _ => new TColorBIOS(isForeground ? (byte)0x7 : (byte)0x0)  // Default: light gray fg, black bg
        };
    }

    // Implicit conversions for common use cases
    public static implicit operator TColorDesired(byte bios) => FromBIOS(bios);
}

/// <summary>
/// Color conversion utilities matching upstream colors.h functions.
/// </summary>
public static class ColorConversion
{
    /// <summary>
    /// XTerm256 to XTerm16 lookup table.
    /// Maps all 256 XTerm colors to their closest 16-color equivalent.
    /// </summary>
    private static readonly byte[] XTerm256toXTerm16LUT =
    [
        // Standard colors 0-15 map to themselves
         0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15,
        // 216 colors (16-231): 6x6x6 color cube
         0,  4,  4,  4, 12, 12,  2,  6,  4,  4, 12, 12,  2,  2,  6,  4,
        12, 12,  2,  2,  2,  6, 12, 12, 10, 10, 10, 10, 14, 12, 10, 10,
        10, 10, 10, 14,  1,  5,  4,  4, 12, 12,  3,  8,  4,  4, 12, 12,
         2,  2,  6,  4, 12, 12,  2,  2,  2,  6, 12, 12, 10, 10, 10, 10,
        14, 12, 10, 10, 10, 10, 10, 14,  1,  1,  5,  4, 12, 12,  1,  1,
         5,  4, 12, 12,  3,  3,  8,  4, 12, 12,  2,  2,  2,  6, 12, 12,
        10, 10, 10, 10, 14, 12, 10, 10, 10, 10, 10, 14,  1,  1,  1,  5,
        12, 12,  1,  1,  1,  5, 12, 12,  1,  1,  1,  5, 12, 12,  3,  3,
         3,  7, 12, 12, 10, 10, 10, 10, 14, 12, 10, 10, 10, 10, 10, 14,
         9,  9,  9,  9, 13, 12,  9,  9,  9,  9, 13, 12,  9,  9,  9,  9,
        13, 12,  9,  9,  9,  9, 13, 12, 11, 11, 11, 11,  7, 12, 10, 10,
        10, 10, 10, 14,  9,  9,  9,  9,  9, 13,  9,  9,  9,  9,  9, 13,
         9,  9,  9,  9,  9, 13,  9,  9,  9,  9,  9, 13,  9,  9,  9,  9,
         9, 13, 11, 11, 11, 11, 11, 15,
        // Grayscale 232-255
         0,  0,  0,  0,  0,  8,  8,  8,  8,  8,  8,  8,  7,  7,  7,  7,
         7,  7,  7,  7, 15, 15, 15, 15
    ];

    /// <summary>
    /// XTerm256 to RGB lookup table (for indices 0-255).
    /// </summary>
    private static readonly uint[] XTerm256toRGBLUT = GenerateXTerm256toRGBLUT();

    private static uint[] GenerateXTerm256toRGBLUT()
    {
        var lut = new uint[256];

        // Standard ANSI colors (0-15) - using common terminal defaults
        uint[] ansiColors =
        [
            0x000000, 0x800000, 0x008000, 0x808000, 0x000080, 0x800080, 0x008080, 0xC0C0C0,
            0x808080, 0xFF0000, 0x00FF00, 0xFFFF00, 0x0000FF, 0xFF00FF, 0x00FFFF, 0xFFFFFF
        ];
        for (int i = 0; i < 16; i++)
            lut[i] = ansiColors[i];

        // 216 colors (16-231): 6x6x6 color cube
        // Channel values: 0, 95, 135, 175, 215, 255
        byte[] cubeValues = [0, 95, 135, 175, 215, 255];
        for (int r = 0; r < 6; r++)
        {
            for (int g = 0; g < 6; g++)
            {
                for (int b = 0; b < 6; b++)
                {
                    int idx = 16 + r * 36 + g * 6 + b;
                    lut[idx] = ((uint)cubeValues[r] << 16) | ((uint)cubeValues[g] << 8) | cubeValues[b];
                }
            }
        }

        // Grayscale (232-255): 24 shades from dark to light
        // Values: 8, 18, 28, ..., 238
        for (int i = 0; i < 24; i++)
        {
            byte gray = (byte)(8 + i * 10);
            lut[232 + i] = ((uint)gray << 16) | ((uint)gray << 8) | gray;
        }

        return lut;
    }

    /// <summary>
    /// Converts a BIOS color index to XTerm16 index.
    /// </summary>
    public static byte BIOStoXTerm16(TColorBIOS c)
    {
        // BIOS and XTerm16 differ in the order of RGB bits
        // BIOS: IRGB (Intensity, Red, Green, Blue)
        // XTerm: IBGR (Intensity, Blue, Green, Red)
        byte val = c;
        byte r = (byte)((val >> 2) & 1);
        byte b = (byte)(val & 1);
        byte g = (byte)((val >> 1) & 1);
        byte bright = (byte)((val >> 3) & 1);
        return (byte)((bright << 3) | (r << 2) | (g << 1) | b);
    }

    /// <summary>
    /// Converts an XTerm16 index to BIOS color.
    /// </summary>
    public static TColorBIOS XTerm16toBIOS(byte idx)
    {
        // Reverse of BIOStoXTerm16
        byte r = (byte)((idx >> 2) & 1);
        byte b = (byte)(idx & 1);
        byte g = (byte)((idx >> 1) & 1);
        byte bright = (byte)((idx >> 3) & 1);
        return new TColorBIOS((byte)((bright << 3) | (r << 2) | (g << 1) | b));
    }

    /// <summary>
    /// Converts an RGB color to its closest BIOS color.
    /// </summary>
    public static TColorBIOS RGBtoBIOS(TColorRGB c)
    {
        return XTerm16toBIOS(RGBtoXTerm16(c));
    }

    /// <summary>
    /// Converts an RGB color to its closest XTerm16 color.
    /// Uses simple threshold-based quantization.
    /// </summary>
    public static byte RGBtoXTerm16(TColorRGB c)
    {
        // Quantize each channel to 0 or 1
        // Use different thresholds for the color and brightness
        byte r = c.R >= 128 ? (byte)1 : (byte)0;
        byte g = c.G >= 128 ? (byte)1 : (byte)0;
        byte b = c.B >= 128 ? (byte)1 : (byte)0;

        // Check if this should be a bright color
        // A color is bright if any channel is very high
        byte bright = (byte)((c.R >= 192 || c.G >= 192 || c.B >= 192) ? 1 : 0);

        // Handle special cases for grays
        int max = Math.Max(c.R, Math.Max(c.G, c.B));
        int min = Math.Min(c.R, Math.Min(c.G, c.B));
        int chroma = max - min;

        if (chroma < 48) // Low saturation = grayscale
        {
            int lightness = (c.R + c.G + c.B) / 3;
            if (lightness < 48)
                return 0;  // Black
            if (lightness < 128)
                return 8;  // Dark gray
            if (lightness < 192)
                return 7;  // Light gray
            return 15;     // White
        }

        return (byte)((bright << 3) | (r << 2) | (g << 1) | b);
    }

    /// <summary>
    /// Converts an XTerm256 index to its closest XTerm16 color.
    /// </summary>
    public static byte XTerm256toXTerm16(byte idx)
    {
        return XTerm256toXTerm16LUT[idx];
    }

    /// <summary>
    /// Converts an XTerm palette index to BIOS color.
    /// </summary>
    public static TColorBIOS XTermToBIOS(TColorXTerm xterm)
    {
        byte idx = xterm.Index;
        if (idx < 16)
            return XTerm16toBIOS(idx);
        return XTerm16toBIOS(XTerm256toXTerm16(idx));
    }

    /// <summary>
    /// Converts an XTerm256 index to RGB (for indices 16-255).
    /// </summary>
    public static TColorRGB XTerm256toRGB(byte idx)
    {
        return new TColorRGB(XTerm256toRGBLUT[idx]);
    }
}
