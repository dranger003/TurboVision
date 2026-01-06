using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Concrete outline implementation that displays a tree of TNode objects.
/// </summary>
/// <remarks>
/// Palette layout:
///   1 = Normal color
///   2 = Focus color
///   3 = Select color
///   4 = Not expanded color
/// </remarks>
public class TOutline : TOutlineViewer
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TOutline";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    /// <summary>
    /// The root node of the outline tree. Not directly serialized;
    /// the tree is serialized separately.
    /// </summary>
    [JsonIgnore]
    public TNode? Root { get; set; }

    /// <summary>
    /// Creates a new outline view with the specified bounds and scrollbars.
    /// </summary>
    public TOutline(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar, TNode? root)
        : base(bounds, hScrollBar, vScrollBar)
    {
        Root = root;
        Update();
    }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TOutline() : base()
    {
    }

    /// <summary>
    /// Disposes the outline and its node tree.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeNode(Root);
            Root = null;
        }
        base.Dispose(disposing);
    }

    #region Abstract Method Implementations

    /// <inheritdoc/>
    public override void Adjust(TNode node, bool expand)
    {
        node.Expanded = expand;
    }

    /// <inheritdoc/>
    public override TNode? GetRoot()
    {
        return Root;
    }

    /// <inheritdoc/>
    public override TNode? GetNext(TNode node)
    {
        return node.Next;
    }

    /// <inheritdoc/>
    public override TNode? GetChild(TNode node, int i)
    {
        var p = node.ChildList;
        while (i != 0 && p != null)
        {
            i--;
            p = p.Next;
        }
        return p;
    }

    /// <inheritdoc/>
    public override int GetNumChildren(TNode node)
    {
        int count = 0;
        var p = node.ChildList;
        while (p != null)
        {
            count++;
            p = p.Next;
        }
        return count;
    }

    /// <inheritdoc/>
    public override string GetText(TNode node)
    {
        return node.Text;
    }

    /// <inheritdoc/>
    public override bool IsExpanded(TNode node)
    {
        return node.Expanded;
    }

    /// <inheritdoc/>
    public override bool HasChildren(TNode node)
    {
        return node.ChildList != null;
    }

    #endregion

    #region JSON Serialization Support

    /// <summary>
    /// Serialized representation of the node tree for JSON.
    /// </summary>
    [JsonPropertyName("rootNode")]
    public SerializedNode? SerializedRoot
    {
        get { return Root != null ? SerializeNode(Root) : null; }
        set { Root = value != null ? DeserializeNode(value) : null; }
    }

    /// <summary>
    /// Helper class for JSON serialization of tree nodes.
    /// </summary>
    public class SerializedNode
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("expanded")]
        public bool Expanded { get; set; } = true;

        [JsonPropertyName("children")]
        public List<SerializedNode>? Children { get; set; }

        [JsonPropertyName("siblings")]
        public List<SerializedNode>? Siblings { get; set; }
    }

    private static SerializedNode SerializeNode(TNode node)
    {
        var serialized = new SerializedNode
        {
            Text = node.Text,
            Expanded = node.Expanded
        };

        // Serialize children
        if (node.ChildList != null)
        {
            serialized.Children = [];
            var child = node.ChildList;
            while (child != null)
            {
                serialized.Children.Add(SerializeNode(child));
                child = child.Next;
            }
        }

        // Serialize siblings (only for root level, handled separately in reconstruction)
        if (node.Next != null)
        {
            serialized.Siblings = [];
            var sibling = node.Next;
            while (sibling != null)
            {
                serialized.Siblings.Add(SerializeNode(sibling));
                sibling = sibling.Next;
            }
        }

        return serialized;
    }

    private static TNode DeserializeNode(SerializedNode serialized)
    {
        var node = new TNode(serialized.Text)
        {
            Expanded = serialized.Expanded
        };

        // Deserialize children
        if (serialized.Children != null && serialized.Children.Count > 0)
        {
            TNode? prevChild = null;
            foreach (var childData in serialized.Children)
            {
                var child = DeserializeNode(childData);
                if (prevChild == null)
                {
                    node.ChildList = child;
                }
                else
                {
                    prevChild.Next = child;
                }
                prevChild = child;
            }
        }

        // Deserialize siblings
        if (serialized.Siblings != null && serialized.Siblings.Count > 0)
        {
            TNode current = node;
            foreach (var siblingData in serialized.Siblings)
            {
                var sibling = DeserializeNode(siblingData);
                current.Next = sibling;
                current = sibling;
            }
        }

        return node;
    }

    #endregion
}
