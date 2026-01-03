namespace TurboVision.Core;

/// <summary>
/// Represents a keyboard event.
/// </summary>
public record struct KeyDownEvent
{
    public const int MaxTextLength = 4;

    private ushort _keyCode;
    private ushort _controlKeyState;
    private char[] _text;
    private byte _textLength;

    public KeyDownEvent()
    {
        _keyCode = 0;
        _controlKeyState = 0;
        _text = new char[MaxTextLength];
        _textLength = 0;
    }

    public ushort KeyCode
    {
        get { return _keyCode; }
        set { _keyCode = value; }
    }

    public ushort ControlKeyState
    {
        get { return _controlKeyState; }
        set { _controlKeyState = value; }
    }

    public byte CharCode
    {
        get { return (byte)(_keyCode & 0xFF); }
    }

    public byte ScanCode
    {
        get { return (byte)(_keyCode >> 8); }
    }

    public ReadOnlySpan<char> Text
    {
        get { return _text.AsSpan(0, _textLength); }
    }

    public void SetText(ReadOnlySpan<char> text)
    {
        _textLength = (byte)Math.Min(text.Length, MaxTextLength);
        text.Slice(0, _textLength).CopyTo(_text);
    }

    public TKey ToKey()
    {
        return new TKey(_keyCode, _controlKeyState);
    }
}
