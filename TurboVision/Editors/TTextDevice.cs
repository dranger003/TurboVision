using System.IO;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// Abstract base class for text output devices.
/// Provides stream-like output capabilities to a TScroller view.
/// </summary>
public abstract class TTextDevice : TScroller
{
    protected TTextDevice(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar)
        : base(bounds, hScrollBar, vScrollBar)
    {
    }

    /// <summary>
    /// Writes a string of characters to the device.
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract int DoSputn(ReadOnlySpan<char> s);

    /// <summary>
    /// Writes a single character to the device.
    /// </summary>
    public virtual int Overflow(int c)
    {
        if (c >= 0)
        {
            Span<char> buf = stackalloc char[1];
            buf[0] = (char)c;
            DoSputn(buf);
        }
        return 1;
    }

    /// <summary>
    /// Writes a string to the device.
    /// </summary>
    public void Write(string s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            DoSputn(s.AsSpan());
        }
    }

    /// <summary>
    /// Writes a line to the device (with newline).
    /// </summary>
    public void WriteLine(string s)
    {
        Write(s);
        Write("\n");
    }
}

/// <summary>
/// TextWriter wrapper for TTextDevice that allows using standard .NET I/O patterns.
/// </summary>
public class TTextDeviceWriter : TextWriter
{
    private readonly TTextDevice _device;

    public TTextDeviceWriter(TTextDevice device)
    {
        _device = device;
    }

    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(char value)
    {
        _device.Overflow(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null && count > 0)
        {
            _device.DoSputn(buffer.AsSpan(index, count));
        }
    }

    public override void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _device.DoSputn(value.AsSpan());
        }
    }
}
