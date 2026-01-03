namespace TurboVision.Core;

/// <summary>
/// Represents a single cell on the screen containing a character and its attributes.
/// </summary>
public record struct TScreenCell : IEquatable<TScreenCell>
{
    private char _char;
    private TColorAttr _attr;
    private bool _isWide;
    private bool _isWideTrail;

    public TScreenCell()
    {
        _char = ' ';
        _attr = default;
        _isWide = false;
        _isWideTrail = false;
    }

    public TScreenCell(char ch, TColorAttr attr)
    {
        _char = ch;
        _attr = attr;
        _isWide = false;
        _isWideTrail = false;
    }

    public char Char
    {
        get { return _char; }
        set { _char = value; }
    }

    public TColorAttr Attr
    {
        get { return _attr; }
        set { _attr = value; }
    }

    public bool IsWide
    {
        get { return _isWide; }
        set { _isWide = value; }
    }

    public bool IsWideCharTrail
    {
        get { return _isWideTrail; }
        set { _isWideTrail = value; }
    }

    public void SetCell(char ch, TColorAttr attr)
    {
        _char = ch;
        _attr = attr;
        _isWide = false;
        _isWideTrail = false;
    }
}
