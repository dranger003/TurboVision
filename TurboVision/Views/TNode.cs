namespace TurboVision.Views;

/// <summary>
/// Represents a node in an outline tree structure.
/// Each node can have children and siblings forming a hierarchical tree.
/// </summary>
public class TNode
{
    /// <summary>
    /// Pointer to the next sibling node in the linked list.
    /// </summary>
    public TNode? Next { get; set; }

    /// <summary>
    /// The text displayed for this node.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Pointer to the first child node.
    /// </summary>
    public TNode? ChildList { get; set; }

    /// <summary>
    /// Whether this node is expanded (children visible).
    /// </summary>
    public bool Expanded { get; set; }

    /// <summary>
    /// Creates a new node with the specified text, initially expanded with no children.
    /// </summary>
    public TNode(string text)
    {
        Text = text;
        Next = null;
        ChildList = null;
        Expanded = true;
    }

    /// <summary>
    /// Creates a new node with full initialization.
    /// </summary>
    /// <param name="text">The text to display for this node.</param>
    /// <param name="children">The first child node, or null if no children.</param>
    /// <param name="next">The next sibling node, or null if last sibling.</param>
    /// <param name="expanded">Initial expanded state (default true).</param>
    public TNode(string text, TNode? children, TNode? next, bool expanded = true)
    {
        Text = text;
        ChildList = children;
        Next = next;
        Expanded = expanded;
    }
}
