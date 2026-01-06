using TurboVision.Core;

namespace TurboVision.Help;

/// <summary>
/// A help topic containing paragraphs of text and cross-references.
/// </summary>
public class THelpTopic
{
    /// <summary>
    /// The linked list of paragraphs in this topic.
    /// </summary>
    public TParagraph? Paragraphs { get; private set; }

    /// <summary>
    /// The array of cross-references in this topic.
    /// </summary>
    public List<TCrossRef> CrossRefs { get; } = [];

    private int _width;
    private int _lastOffset;
    private int _lastLine = int.MaxValue;
    private TParagraph? _lastParagraph;

    public THelpTopic()
    {
    }

    /// <summary>
    /// Adds a cross-reference to this topic.
    /// </summary>
    public void AddCrossRef(TCrossRef crossRef)
    {
        CrossRefs.Add(crossRef);
    }

    /// <summary>
    /// Adds a paragraph to the end of this topic.
    /// </summary>
    public void AddParagraph(TParagraph paragraph)
    {
        if (Paragraphs == null)
        {
            Paragraphs = paragraph;
        }
        else
        {
            var p = Paragraphs;
            while (p.Next != null)
            {
                p = p.Next;
            }
            p.Next = paragraph;
        }
        paragraph.Next = null;
    }

    /// <summary>
    /// Gets information about a cross-reference.
    /// </summary>
    /// <param name="index">Zero-based index of the cross-reference.</param>
    /// <param name="loc">Output: location (x=column, y=line) of the cross-ref.</param>
    /// <param name="length">Output: display width of the cross-ref text.</param>
    /// <param name="refTopic">Output: the target topic context.</param>
    public void GetCrossRef(int index, out TPoint loc, out byte length, out int refTopic)
    {
        loc = new TPoint(0, 0);
        length = 0;
        refTopic = 0;

        if (index < 0 || index >= CrossRefs.Count)
        {
            return;
        }

        var crossRef = CrossRefs[index];
        int paraOffset = 0;
        int curOffset = 0;
        int line = 0;
        int offset = crossRef.Offset;
        var p = Paragraphs;

        while (p != null)
        {
            while (curOffset < p.Size)
            {
                int lineOffset = curOffset;
                WrapText(p.Text, p.Size, ref curOffset, p.Wrap);
                line++;

                if (offset <= paraOffset + curOffset)
                {
                    int refOffset = offset - (paraOffset + lineOffset) - 1;
                    if (refOffset < 0) refOffset = 0;

                    string textBefore = p.Text.Substring(lineOffset, Math.Min(refOffset, p.Text.Length - lineOffset));
                    loc = new TPoint(StringWidth(textBefore), line);
                    length = crossRef.Length;
                    refTopic = crossRef.Ref;
                    return;
                }
            }

            paraOffset += p.Size;
            p = p.Next;
            curOffset = 0;
        }
    }

    /// <summary>
    /// Gets a line of text from the topic.
    /// </summary>
    /// <param name="line">One-based line number.</param>
    /// <returns>The text for that line, or empty if out of range.</returns>
    public string GetLine(int line)
    {
        int offset;
        TParagraph? p;

        if (_lastLine < line)
        {
            int i = line;
            line -= _lastLine;
            _lastLine = i;
            offset = _lastOffset;
            p = _lastParagraph;
        }
        else
        {
            p = Paragraphs;
            offset = 0;
            _lastLine = line;
        }

        while (p != null)
        {
            while (offset < p.Size)
            {
                line--;
                string lineText = WrapText(p.Text, p.Size, ref offset, p.Wrap);
                if (line == 0)
                {
                    _lastOffset = offset;
                    _lastParagraph = p;
                    return lineText;
                }
            }
            p = p.Next;
            offset = 0;
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the number of cross-references in this topic.
    /// </summary>
    public int GetNumCrossRefs()
    {
        return CrossRefs.Count;
    }

    /// <summary>
    /// Calculates the width of the longest line.
    /// </summary>
    public int LongestLineWidth()
    {
        int maxWidth = 0;
        int lineCount = NumLines();

        for (int i = 1; i <= lineCount; i++)
        {
            string line = GetLine(i);
            int lineWidth = StringWidth(line);
            if (lineWidth > maxWidth)
            {
                maxWidth = lineWidth;
            }
        }

        return maxWidth;
    }

    /// <summary>
    /// Counts the total number of lines in this topic.
    /// </summary>
    public int NumLines()
    {
        int offset = 0;
        int lines = 0;
        var p = Paragraphs;

        while (p != null)
        {
            offset = 0;
            while (offset < p.Size)
            {
                lines++;
                WrapText(p.Text, p.Size, ref offset, p.Wrap);
            }
            p = p.Next;
        }

        return lines;
    }

    /// <summary>
    /// Sets the width for word wrapping.
    /// </summary>
    public void SetWidth(int width)
    {
        _width = width;
        _lastLine = int.MaxValue;
    }

    /// <summary>
    /// Wraps text at the current width.
    /// </summary>
    private string WrapText(string text, int textSize, ref int offset, bool wrap)
    {
        string line = GetLineAtOffset(text, textSize, offset);

        if (wrap && _width > 0)
        {
            int wrappedSize = ScrollText(line, _width);
            if (wrappedSize > 0 && wrappedSize < line.Length)
            {
                int newSize = wrappedSize;

                // Omit the last word if it was cut off by wrapping
                while (newSize > 0 && !char.IsWhiteSpace(line[newSize]))
                {
                    newSize--;
                }

                // Unless it fills the whole line
                if (newSize == 0)
                {
                    newSize = wrappedSize;
                }

                // If a space follows, keep it so offset points past it
                if (newSize < textSize - offset && newSize < line.Length && char.IsWhiteSpace(line[newSize]))
                {
                    newSize++;
                }

                line = line[..newSize];
            }
        }

        offset += line.Length;
        return DiscardTrailingWhitespace(line);
    }

    private static string GetLineAtOffset(string text, int textSize, int offset)
    {
        if (offset >= text.Length)
        {
            return string.Empty;
        }

        int newlineIndex = text.IndexOf('\n', offset);
        int lineEnd;
        if (newlineIndex < 0)
        {
            lineEnd = textSize;
        }
        else
        {
            lineEnd = newlineIndex + 1; // Include the newline
        }

        return text[offset..lineEnd];
    }

    private static string DiscardTrailingWhitespace(string str)
    {
        return str.TrimEnd();
    }

    private static int ScrollText(string text, int width)
    {
        // Simple implementation: count characters up to width
        int count = 0;
        foreach (char c in text)
        {
            if (c == '\n')
            {
                break;
            }
            count++;
            if (count >= width)
            {
                break;
            }
        }
        return count;
    }

    private static int StringWidth(string text)
    {
        // Simple implementation: count printable characters
        int width = 0;
        foreach (char c in text)
        {
            if (c != '\n' && c != '\r')
            {
                width++;
            }
        }
        return width;
    }
}
