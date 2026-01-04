using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// A static text control that supports printf-style formatted text.
/// The text can be dynamically updated using SetText() with format arguments.
/// </summary>
public class TParamText : TStaticText
{
    private const int MaxTextLength = 256;

    private string _formattedText;

    /// <summary>
    /// Creates a new TParamText control with an empty initial string.
    /// </summary>
    /// <param name="bounds">The bounding rectangle.</param>
    public TParamText(TRect bounds) : base(bounds, null)
    {
        _formattedText = "";
    }

    /// <summary>
    /// Gets the current formatted text into the destination buffer.
    /// </summary>
    /// <param name="dest">Destination buffer for the text.</param>
    public override void GetText(Span<char> dest)
    {
        if (_formattedText != null)
        {
            int len = Math.Min(_formattedText.Length, dest.Length - 1);
            _formattedText.AsSpan(0, len).CopyTo(dest);
            if (len < dest.Length)
            {
                dest[len] = '\0';
            }
        }
        else if (dest.Length > 0)
        {
            dest[0] = '\0';
        }
    }

    /// <summary>
    /// Gets the length of the current formatted text.
    /// </summary>
    /// <returns>The length of the text in characters.</returns>
    public virtual int GetTextLen()
    {
        return _formattedText?.Length ?? 0;
    }

    /// <summary>
    /// Sets the text using a format string and arguments.
    /// The format follows C# string formatting conventions.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public virtual void SetText(string format, params object[] args)
    {
        string result;
        if (args.Length > 0)
        {
            result = string.Format(format, args);
        }
        else
        {
            result = format;
        }

        // Truncate to maximum length
        if (result.Length > MaxTextLength)
        {
            result = result.Substring(0, MaxTextLength);
        }

        _formattedText = result;
        DrawView();
    }
}
