using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TurboVision.Core;

/// <summary>
/// Fixed-size buffer for keyboard event text (UTF-8 character up to 4 chars).
/// </summary>
[InlineArray(KeyDownEvent.MaxTextLength)]
public struct KeyDownTextBuffer
{
    private char _element;

    /// <summary>
    /// Gets a span over the buffer contents.
    /// </summary>
    public Span<char> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element, KeyDownEvent.MaxTextLength);
    }

    /// <summary>
    /// Gets a read-only span over the buffer contents.
    /// </summary>
    public ReadOnlySpan<char> AsReadOnlySpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element, KeyDownEvent.MaxTextLength);
    }
}

/// <summary>
/// Represents a keyboard event.
/// </summary>
public struct KeyDownEvent
{
    public const int MaxTextLength = 4;

    private ushort _keyCode;
    private ushort _controlKeyState;
    private KeyDownTextBuffer _text;
    private byte _textLength;

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

    public byte TextLength
    {
        get { return _textLength; }
    }

    public ReadOnlySpan<char> GetText()
    {
        return _text.AsReadOnlySpan().Slice(0, _textLength);
    }

    public void SetText(ReadOnlySpan<char> text)
    {
        _textLength = (byte)Math.Min(text.Length, MaxTextLength);
        text.Slice(0, _textLength).CopyTo(_text.AsSpan());
    }

    public TKey ToKey()
    {
        return new TKey(_keyCode, _controlKeyState);
    }
}
