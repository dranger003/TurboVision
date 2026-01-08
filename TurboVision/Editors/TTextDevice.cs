using System.IO;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

// =============================================================================
// TTextDevice class
// Upstream: textview.h lines 37-56, textview.cpp lines 28-55
// =============================================================================

/// <summary>
/// Abstract base class for text output devices.
/// Provides stream-like output capabilities to a TScroller view.
/// Matches upstream TTextDevice (inherits from TScroller and streambuf).
/// </summary>
public abstract class TTextDevice(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar) : TScroller(bounds, aHScrollBar, aVScrollBar)
{

    /// <summary>
    /// Writes a string of characters to the device.
    /// Must be implemented by derived classes.
    /// Upstream: virtual int do_sputn(const char *s, int count) = 0;
    /// </summary>
    public abstract int DoSputn(ReadOnlySpan<char> s, int count);

    /// <summary>
    /// Writes a single character to the device.
    /// Upstream: virtual int overflow(int = EOF);
    /// </summary>
    public virtual int Overflow(int c)
    {
        if (c >= 0)
        {
            Span<char> buf = [(char)c];
            DoSputn(buf, 1);
        }
        return 1;
    }
}

/// <summary>
/// TextWriter wrapper for TTextDevice that allows using standard .NET I/O patterns.
/// This is a C# convenience class not present in the upstream C++ code.
/// </summary>
public class TTextDeviceWriter(TTextDevice device) : TextWriter
{
    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(char value)
    {
        device.Overflow(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null && count > 0)
        {
            device.DoSputn(buffer.AsSpan(index, count), count);
        }
    }

    public override void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            device.DoSputn(value.AsSpan(), value.Length);
        }
    }
}
