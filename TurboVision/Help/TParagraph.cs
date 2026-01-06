namespace TurboVision.Help;

/// <summary>
/// A paragraph of help text. Forms a linked list of paragraphs within a topic.
/// </summary>
public class TParagraph
{
    /// <summary>
    /// The next paragraph in the chain, or null if this is the last.
    /// </summary>
    public TParagraph? Next { get; set; }

    /// <summary>
    /// Whether this paragraph should wrap at the viewer width.
    /// </summary>
    public bool Wrap { get; set; }

    /// <summary>
    /// The text content of this paragraph.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The size (length) of the text.
    /// </summary>
    public int Size
    {
        get { return Text.Length; }
    }

    public TParagraph()
    {
    }

    public TParagraph(string text, bool wrap = true)
    {
        Text = text;
        Wrap = wrap;
    }
}
