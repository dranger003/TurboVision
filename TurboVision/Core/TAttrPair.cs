namespace TurboVision.Core;

/// <summary>
/// Represents a pair of color attributes (normal and highlight).
/// Used for text rendering with shortcut highlighting.
/// </summary>
public readonly record struct TAttrPair(TColorAttr Normal, TColorAttr Highlight) : IEquatable<TAttrPair>
{
    public TAttrPair(byte normal, byte highlight)
        : this(new TColorAttr(normal), new TColorAttr(highlight))
    {
    }

    /// <summary>
    /// Gets the normal (first) color attribute.
    /// </summary>
    public TColorAttr this[int index]
    {
        get { return index == 0 ? Normal : Highlight; }
    }
}
