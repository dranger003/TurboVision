namespace TurboVision.Platform;

/// <summary>
/// Color conversion algorithms for terminal color palettes.
/// Matches upstream colors.h and colors.cpp color conversion functions.
/// </summary>
internal static class ColorConversion
{
    // ========================================
    // XTerm256 to XTerm16 Lookup Table
    // ========================================

    /// <summary>
    /// Lookup table for converting XTerm256 colors to XTerm16.
    /// Generated at compile time matching upstream initXTerm256toXTerm16LUT() in colors.cpp:102-128
    /// </summary>
    private static readonly byte[] XTerm256toXTerm16LUT = InitXTerm256toXTerm16LUT();

    /// <summary>
    /// Lookup table for converting XTerm256 colors to RGB.
    /// Generated at compile time matching upstream initXTerm256toRGBLUT() in colors.cpp:137-161
    /// </summary>
    private static readonly uint[] XTerm256toRGBLUT = InitXTerm256toRGBLUT();

    // ========================================
    // Main Conversion Functions
    // ========================================

    /// <summary>
    /// Converts BIOS color (0-15) to XTerm16 by swapping Red and Blue bits.
    /// Matches upstream BIOStoXTerm16() in colors.h:180-187
    ///
    /// BIOS colors: bit0=Blue, bit1=Green, bit2=Red, bit3=Bright
    /// XTerm colors: bit0=Red, bit1=Green, bit2=Blue, bit3=Bright
    /// </summary>
    public static byte BIOStoXTerm16(byte bios)
    {
        byte b = (byte)(bios & 0x1);        // Blue bit
        byte g = (byte)(bios & 0x2);        // Green bit (unchanged)
        byte r = (byte)(bios & 0x4);        // Red bit
        byte bright = (byte)(bios & 0x8);   // Bright bit (unchanged)

        // Swap Red and Blue: XTerm = (b→r) | g | (r→b) | bright
        return (byte)((b << 2) | g | (r >> 2) | bright);
    }

    /// <summary>
    /// Converts XTerm16 color to BIOS by swapping Red and Blue bits.
    /// Matches upstream XTerm16toBIOS() in colors.h:257-260
    /// </summary>
    public static byte XTerm16toBIOS(byte xterm)
    {
        return BIOStoXTerm16(xterm); // Same operation, bidirectional
    }

    /// <summary>
    /// Converts XTerm256 color (0-255) to XTerm16 (0-15).
    /// Matches upstream XTerm256toXTerm16() in colors.h:262-266
    /// </summary>
    public static byte XTerm256toXTerm16(byte idx)
    {
        return XTerm256toXTerm16LUT[idx];
    }

    /// <summary>
    /// Converts RGB color to XTerm16 using HSL-based algorithm.
    /// Matches upstream RGBtoXTerm16() in colors.cpp:71-99
    ///
    /// Algorithm:
    /// 1. Convert RGB to HCL (Hue, Chroma, Lightness)
    /// 2. If Chroma >= 12, it's a color:
    ///    - Use Hue to pick Red/Yellow/Green/Cyan/Blue/Magenta
    ///    - Use Lightness to pick dark/bright/white
    /// 3. If Chroma < 12, it's grayscale:
    ///    - Use Lightness to pick black/dark_gray/light_gray/white
    /// </summary>
    public static byte RGBtoXTerm16(byte r, byte g, byte b)
    {
        var c = RGBtoHCL(r, g, b);

        if (c.C >= 12) // Color if Chroma >= 12
        {
            // Map Hue to one of 6 colors: Red, Yellow, Green, Cyan, Blue, Magenta
            byte[] normal = [0x1, 0x3, 0x2, 0x6, 0x4, 0x5]; // Dark variants
            byte[] bright = [0x9, 0xB, 0xA, 0xE, 0xC, 0xD]; // Bright variants

            // Adjust Hue for proper bucketing, then divide into 6 buckets
            byte index = (byte)((c.H < HUE_MAX - HUE_PRECISION / 2
                ? c.H + HUE_PRECISION / 2
                : c.H - (HUE_MAX - HUE_PRECISION / 2)) / HUE_PRECISION);

            if (c.L < U8(0.5))
                return normal[index];  // Dark color
            if (c.L < U8(0.925))
                return bright[index];  // Bright color
            return 15;                 // Very bright → White
        }
        else // Grayscale if Chroma < 12
        {
            if (c.L < U8(0.25))
                return 0;   // Black
            if (c.L < U8(0.625))
                return 8;   // Dark Gray
            if (c.L < U8(0.875))
                return 7;   // Light Gray
            return 15;      // White
        }
    }

    /// <summary>
    /// Converts RGB color to BIOS color via XTerm16.
    /// Matches upstream RGBtoBIOS() in colors.h:189-192
    /// </summary>
    public static byte RGBtoBIOS(byte r, byte g, byte b)
    {
        return XTerm16toBIOS(RGBtoXTerm16(r, g, b));
    }

    /// <summary>
    /// Converts RGB color to XTerm256 using 6x6x6 cube + 24-level grayscale.
    /// Matches upstream RGBtoXTerm256() in colors.h:200-255
    ///
    /// The xterm-256color palette consists of:
    /// * [0..15]:    16 colors as in xterm-16color
    /// * [16..231]:  216 colors in a 6x6x6 cube
    /// * [232..255]: 24 grayscale colors
    ///
    /// This function returns indices in range [16..255] only.
    /// For [0..15], use RGBtoXTerm16() instead.
    /// </summary>
    public static byte RGBtoXTerm256(byte r, byte g, byte b)
    {
        // Scale RGB to 6x6x6 cube indices
        static byte ScaleColor(byte c)
        {
            // Compensate for dark color underrepresentation
            // Values [55..74] can map to either 0 or 1, we choose 1
            c = (byte)(c + (20 & -(c < 75 ? 1 : 0)));  // Add 20 if c < 75
            return (byte)((Math.Max(c, (byte)35) - 35) / 40);
        }

        byte rIdx = ScaleColor(r);
        byte gIdx = ScaleColor(g);
        byte bIdx = ScaleColor(b);
        byte cubeIdx = (byte)(16 + rIdx * 36 + gIdx * 6 + bIdx);

        // Convert cube index back to RGB to check accuracy
        uint cubeRGB = XTerm256toRGB(cubeIdx);
        byte cubeR = (byte)((cubeRGB >> 16) & 0xFF);
        byte cubeG = (byte)((cubeRGB >> 8) & 0xFF);
        byte cubeB = (byte)(cubeRGB & 0xFF);

        if (r != cubeR || g != cubeG || b != cubeB)
        {
            // Cube doesn't match exactly, check if grayscale would be better
            byte Xmin = Math.Min(Math.Min(r, g), b);
            byte Xmax = Math.Max(Math.Max(r, g), b);
            byte C = (byte)(Xmax - Xmin); // Chroma in HSL/HSV theory

            if (C < 12 || cubeIdx == 16) // Grayscale if Chroma < 12 or rounded to black
            {
                byte L = (byte)((ushort)(Xmax + Xmin) / 2); // Lightness, as in HSL

                // Map Lightness to grayscale indices [232..255]
                if (L < 8 - 5)
                    return 16;  // Too dark for grayscale, use black from cube
                if (L >= 238 + 5)
                    return 231; // Too bright for grayscale, use white from cube

                return (byte)(232 + (Math.Max(L, (byte)3) - 3) / 10);
            }
        }

        return cubeIdx;
    }

    /// <summary>
    /// Converts XTerm256 index to RGB.
    /// Matches upstream XTerm256toRGB() in colors.h:268-272
    /// Only meaningful for indices 16..255.
    /// </summary>
    public static uint XTerm256toRGB(byte idx)
    {
        return XTerm256toRGBLUT[idx];
    }

    // ========================================
    // HSL Color Space Conversion
    // ========================================

    private const byte HUE_PRECISION = 32;
    private const byte HUE_MAX = 6 * HUE_PRECISION; // 192

    private struct HCL
    {
        public byte H; // Hue [0..HUE_MAX)
        public byte C; // Chroma [0..255]
        public byte L; // Lightness [0..255]
    }

    /// <summary>
    /// Converts RGB to HCL (Hue, Chroma, Lightness).
    /// Matches upstream RGBtoHCL() in colors.cpp:39-63
    ///
    /// This is essentially RGB to HSL conversion, but we keep Chroma
    /// separate since it's used for determining color vs grayscale.
    /// </summary>
    private static HCL RGBtoHCL(byte R, byte G, byte B)
    {
        byte Xmin = Math.Min(Math.Min(R, G), B);
        byte Xmax = Math.Max(Math.Max(R, G), B);
        byte V = Xmax;
        byte L = (byte)((ushort)(Xmax + Xmin) / 2);
        byte C = (byte)(Xmax - Xmin);
        short H = 0;

        if (C != 0)
        {
            // Calculate Hue based on which component is max
            if (V == R)
                H = (short)(HUE_PRECISION * (G - B) / C);
            else if (V == G)
                H = (short)(HUE_PRECISION * (B - R) / C + 2 * HUE_PRECISION);
            else if (V == B)
                H = (short)(HUE_PRECISION * (R - G) / C + 4 * HUE_PRECISION);

            // Normalize Hue to [0..HUE_MAX)
            if (H < 0)
                H += HUE_MAX;
            else if (H >= HUE_MAX)
                H -= HUE_MAX;
        }

        return new HCL { H = (byte)H, C = C, L = L };
    }

    /// <summary>
    /// Converts double [0..1] to byte [0..255].
    /// Matches upstream u8() in colors.cpp:65-68
    /// </summary>
    private static byte U8(double d)
    {
        return (byte)(d * 255);
    }

    // ========================================
    // Lookup Table Initialization
    // ========================================

    /// <summary>
    /// Initializes the XTerm256 to XTerm16 lookup table.
    /// Matches upstream initXTerm256toXTerm16LUT() in colors.cpp:102-128
    /// </summary>
    private static byte[] InitXTerm256toXTerm16LUT()
    {
        var lut = new byte[256];

        // [0..15]: Identity mapping (XTerm16 colors)
        for (byte i = 0; i < 16; i++)
            lut[i] = i;

        // [16..231]: 6x6x6 color cube
        for (byte i = 0; i < 6; i++)
        {
            byte R = (byte)(i != 0 ? 55 + i * 40 : 0);
            for (byte j = 0; j < 6; j++)
            {
                byte G = (byte)(j != 0 ? 55 + j * 40 : 0);
                for (byte k = 0; k < 6; k++)
                {
                    byte B = (byte)(k != 0 ? 55 + k * 40 : 0);
                    byte idx16 = RGBtoXTerm16(R, G, B);
                    lut[16 + (i * 6 + j) * 6 + k] = idx16;
                }
            }
        }

        // [232..255]: 24-level grayscale
        for (byte i = 0; i < 24; i++)
        {
            byte L = (byte)(i * 10 + 8);
            byte idx16 = RGBtoXTerm16(L, L, L);
            lut[232 + i] = idx16;
        }

        return lut;
    }

    /// <summary>
    /// Initializes the XTerm256 to RGB lookup table.
    /// Matches upstream initXTerm256toRGBLUT() in colors.cpp:137-161
    /// </summary>
    private static uint[] InitXTerm256toRGBLUT()
    {
        var lut = new uint[256];

        // [0..15]: Not used (XTerm16 colors are not mapped to fixed RGB values)
        // [16..231]: 6x6x6 color cube
        for (byte i = 0; i < 6; i++)
        {
            byte R = (byte)(i != 0 ? 55 + i * 40 : 0);
            for (byte j = 0; j < 6; j++)
            {
                byte G = (byte)(j != 0 ? 55 + j * 40 : 0);
                for (byte k = 0; k < 6; k++)
                {
                    byte B = (byte)(k != 0 ? 55 + k * 40 : 0);
                    lut[16 + (i * 6 + j) * 6 + k] = Pack(R, G, B);
                }
            }
        }

        // [232..255]: 24-level grayscale
        for (byte i = 0; i < 24; i++)
        {
            byte L = (byte)(i * 10 + 8);
            lut[232 + i] = Pack(L, L, L);
        }

        return lut;
    }

    /// <summary>
    /// Packs RGB bytes into a single uint (0xRRGGBB format).
    /// Matches upstream pack() in colors.cpp:131-134
    /// </summary>
    private static uint Pack(byte r, byte g, byte b)
    {
        return (uint)((r << 16) | (g << 8) | b);
    }
}
