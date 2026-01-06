using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Outline view state flags used during tree traversal and drawing.
/// </summary>
public static class OutlineFlags
{
    /// <summary>
    /// Node is expanded (children are visible).
    /// </summary>
    public const ushort ovExpanded = 0x01;

    /// <summary>
    /// Node has children that are expanded.
    /// </summary>
    public const ushort ovChildren = 0x02;

    /// <summary>
    /// Node is the last child of its parent.
    /// </summary>
    public const ushort ovLast = 0x04;
}

/// <summary>
/// Commands specific to outline views.
/// </summary>
public static class OutlineCommands
{
    /// <summary>
    /// Broadcast when an outline item is selected.
    /// </summary>
    public const ushort cmOutlineItemSelected = 301;
}
