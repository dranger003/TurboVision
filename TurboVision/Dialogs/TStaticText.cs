using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Read-only static text label.
/// </summary>
public class TStaticText : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TStaticText";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x06];

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    public TStaticText(TRect bounds, string? text) : base(bounds)
    {
        Text = text;
        GrowMode |= GrowFlags.gfFixed;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(1).Normal;

        Span<char> buf = stackalloc char[256];
        GetText(buf);

        // Find actual length (null-terminated or full span)
        int textLen = buf.IndexOf('\0');
        if (textLen < 0) textLen = buf.Length;

        // Use original Text if possible to avoid truncation
        ReadOnlySpan<char> s = Text != null && Text.AsSpan().StartsWith(buf.Slice(0, Math.Min(textLen, Text.Length)))
            ? Text.AsSpan()
            : buf.Slice(0, textLen);

        int l = s.Length;
        int p = 0;
        int y = 0;
        bool center = false;

        while (y < Size.Y)
        {
            b.MoveChar(0, ' ', color, Size.X);

            if (p < l)
            {
                // Check for centering control character (ASCII 3)
                if (s[p] == '\x03')
                {
                    center = true;
                    p++;
                }

                int lineStart = p;

                // Find how much text fits on this line
                int lineEnd = lineStart;
                int lastWordBreak = lineStart;

                while (lineEnd < l && s[lineEnd] != '\n')
                {
                    // Track word boundaries
                    if (lineEnd > lineStart && s[lineEnd - 1] == ' ' && s[lineEnd] != ' ')
                    {
                        lastWordBreak = lineEnd;
                    }

                    // Check if we've exceeded the line width
                    if (lineEnd - lineStart >= Size.X)
                    {
                        // Break at last word boundary if possible
                        if (lastWordBreak > lineStart)
                        {
                            lineEnd = lastWordBreak;
                        }
                        break;
                    }
                    lineEnd++;
                }

                // Calculate display width
                int width = lineEnd - lineStart;
                int indent = center ? (Size.X - width) / 2 : 0;
                if (indent < 0) indent = 0;

                // Draw the text segment
                b.MoveStr(indent, s.Slice(lineStart, width), color);

                // Skip trailing spaces
                p = lineEnd;
                while (p < l && s[p] == ' ')
                    p++;

                // Handle newline
                if (p < l && s[p] == '\n')
                {
                    center = false;
                    p++;
                }
            }

            WriteLine(0, y++, Size.X, 1, b);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public virtual void GetText(Span<char> dest)
    {
        if (Text != null)
        {
            int len = Math.Min(Text.Length, dest.Length - 1);
            Text.AsSpan(0, len).CopyTo(dest);
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
}
