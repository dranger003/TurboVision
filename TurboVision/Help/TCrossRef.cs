namespace TurboVision.Help;

/// <summary>
/// A cross-reference (hyperlink) within help text.
/// </summary>
public class TCrossRef
{
    /// <summary>
    /// The target topic context number to jump to.
    /// </summary>
    public int Ref { get; set; }

    /// <summary>
    /// The character offset within the topic where this cross-ref begins.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The length of the cross-reference text in characters.
    /// </summary>
    public byte Length { get; set; }

    public TCrossRef()
    {
    }

    public TCrossRef(int refTopic, int offset, byte length)
    {
        Ref = refTopic;
        Offset = offset;
        Length = length;
    }
}
