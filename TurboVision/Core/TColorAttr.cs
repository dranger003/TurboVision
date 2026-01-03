namespace TurboVision.Core;

/// <summary>
/// Represents color attributes for a screen cell (foreground, background, and style).
/// </summary>
public readonly record struct TColorAttr : IEquatable<TColorAttr>
{
    private readonly uint _value;

    public TColorAttr(byte foreground, byte background)
    {
        _value = (uint)((background << 4) | (foreground & 0x0F));
    }

    public TColorAttr(uint value)
    {
        _value = value;
    }

    public byte Foreground
    {
        get { return (byte)(_value & 0x0F); }
    }

    public byte Background
    {
        get { return (byte)((_value >> 4) & 0x0F); }
    }

    public uint Value
    {
        get { return _value; }
    }

    public static implicit operator TColorAttr(byte value)
    {
        return new TColorAttr(value);
    }

    public static implicit operator byte(TColorAttr attr)
    {
        return (byte)attr._value;
    }
}
