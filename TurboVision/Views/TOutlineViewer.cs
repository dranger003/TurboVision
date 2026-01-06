using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Visitor callback type for outline tree traversal.
/// </summary>
/// <param name="viewer">The outline viewer being traversed.</param>
/// <param name="node">The current node.</param>
/// <param name="level">The depth level of the node (0-based).</param>
/// <param name="position">The display position of the node.</param>
/// <param name="lines">Bitmask indicating which levels have continuation lines.</param>
/// <param name="flags">Node flags (ovExpanded, ovChildren, ovLast).</param>
/// <returns>True to stop traversal, false to continue.</returns>
public delegate bool OutlineVisitor(TOutlineViewer viewer, TNode node, int level, int position, long lines, ushort flags);

/// <summary>
/// Abstract base class for hierarchical outline views.
/// Displays tree-structured data with expand/collapse functionality.
/// </summary>
/// <remarks>
/// Palette layout:
///   1 = Normal color
///   2 = Focus color
///   3 = Select color
///   4 = Not expanded color
/// </remarks>
public abstract class TOutlineViewer : TScroller
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TOutlineViewer";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x06, 0x07, 0x03, 0x08];

    // Tree line drawing characters
    // Index: 0=level filler, 1=level mark, 2=end first (not last), 3=end first (last),
    //        4=end filler, 5=end child, 6=retracted (+), 7=expanded (-)
    private static readonly string GraphChars = " \u2502\u251C\u2514\u2500\u2500+\u2500";
    // " │├└──+─" in Unicode box drawing characters

    private const int LevelWidth = 3;
    private const int EndWidth = 3;

    /// <summary>
    /// The currently focused item position.
    /// </summary>
    public int Foc { get; protected set; }

    // Thread-local state for drawing traversal
    [ThreadStatic]
    private static TDrawBuffer? _drawBuffer;
    [ThreadStatic]
    private static int _auxPos;

    // Thread-local state for focused item query
    [ThreadStatic]
    private static int _focLevel;
    [ThreadStatic]
    private static long _focLines;
    [ThreadStatic]
    private static ushort _focFlags;

    // Thread-local state for update count
    [ThreadStatic]
    private static int _updateCount;
    [ThreadStatic]
    private static int _updateMaxX;

    protected TOutlineViewer(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar)
        : base(bounds, hScrollBar, vScrollBar)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        Foc = 0;
    }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TOutlineViewer() : base()
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        Foc = 0;
    }

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Adjusts the expansion state of a node.
    /// </summary>
    public abstract void Adjust(TNode node, bool expand);

    /// <summary>
    /// Gets the root node of the outline tree.
    /// </summary>
    public abstract TNode? GetRoot();

    /// <summary>
    /// Gets the next sibling of a node.
    /// </summary>
    public abstract TNode? GetNext(TNode node);

    /// <summary>
    /// Gets the i-th child of a node.
    /// </summary>
    public abstract TNode? GetChild(TNode node, int i);

    /// <summary>
    /// Gets the number of children of a node.
    /// </summary>
    public abstract int GetNumChildren(TNode node);

    /// <summary>
    /// Gets the display text for a node.
    /// </summary>
    public abstract string GetText(TNode node);

    /// <summary>
    /// Returns whether a node is expanded.
    /// </summary>
    public abstract bool IsExpanded(TNode node);

    /// <summary>
    /// Returns whether a node has children.
    /// </summary>
    public abstract bool HasChildren(TNode node);

    #endregion

    #region Virtual Methods - Can be overridden

    /// <summary>
    /// Called when a node receives focus.
    /// </summary>
    public virtual void Focused(int i)
    {
        Foc = i;
    }

    /// <summary>
    /// Returns whether the item at position i is selected.
    /// By default, returns true if the item is focused (single selection).
    /// </summary>
    public virtual bool IsSelected(int i)
    {
        return Foc == i;
    }

    /// <summary>
    /// Called when a node is selected (double-click or Enter).
    /// </summary>
    public virtual void Selected(int i)
    {
        // Default: no action. Override to handle selection.
    }

    /// <summary>
    /// Creates the graph string (tree lines) for a node.
    /// Can be overridden to customize appearance.
    /// </summary>
    public virtual string GetGraph(int level, long lines, ushort flags)
    {
        return CreateGraph(level, lines, flags, LevelWidth, EndWidth, GraphChars);
    }

    #endregion

    #region Drawing

    public override void Draw()
    {
        var nrmColor = GetColor(0x0401);
        var dBuf = new TDrawBuffer();
        _drawBuffer = dBuf;
        _auxPos = -1;

        FirstThat(DrawTree);

        dBuf.MoveChar(0, ' ', nrmColor.Normal, Size.X);
        int remainingLines = Size.Y - (_auxPos - Delta.Y);
        if (remainingLines > 0 && _auxPos + 1 < Delta.Y + Size.Y)
        {
            for (int y = _auxPos + 1 - Delta.Y; y < Size.Y; y++)
            {
                WriteLine(0, y, Size.X, 1, dBuf);
            }
        }
    }

    private static bool DrawTree(TOutlineViewer viewer, TNode node, int level, int position, long lines, ushort flags)
    {
        var dBuf = _drawBuffer!;
        TAttrPair color;

        if (position >= viewer.Delta.Y)
        {
            if (position >= viewer.Delta.Y + viewer.Size.Y)
            {
                return true; // Stop traversal
            }

            if (position == viewer.Foc && viewer.GetState(StateFlags.sfFocused))
            {
                color = viewer.GetColor(0x0202);
            }
            else if (viewer.IsSelected(position))
            {
                color = viewer.GetColor(0x0303);
            }
            else
            {
                color = viewer.GetColor(0x0401);
            }

            dBuf.MoveChar(0, ' ', color.Normal, viewer.Size.X);

            // Draw the graph (tree lines)
            string graph = viewer.GetGraph(level, lines, flags);
            int graphWidth = TStringUtils.StrWidth(graph);
            int x = graphWidth - viewer.Delta.X;

            if (x > 0)
            {
                // Handle horizontal scrolling for graph
                if (viewer.Delta.X < graph.Length)
                {
                    dBuf.MoveStr(0, graph.AsSpan(viewer.Delta.X), color.Normal);
                }
            }

            // Draw the text
            string text = viewer.GetText(node);
            var textColor = (flags & OutlineFlags.ovExpanded) != 0 ? color.Normal : color.Highlight;
            int textX = Math.Max(0, x);
            int textOffset = Math.Max(0, -x);

            if (textOffset < text.Length)
            {
                dBuf.MoveStr(textX, text.AsSpan(textOffset), textColor);
            }

            viewer.WriteLine(0, position - viewer.Delta.Y, viewer.Size.X, 1, dBuf);
            _auxPos = position;
        }

        return false; // Continue traversal
    }

    #endregion

    #region Graph Generation

    /// <summary>
    /// Creates a graph string for displaying tree lines.
    /// </summary>
    /// <param name="level">The depth level of the node.</param>
    /// <param name="lines">Bitmask of levels that have continuation lines.</param>
    /// <param name="flags">Node flags (ovExpanded, ovChildren, ovLast).</param>
    /// <param name="levWidth">Characters per level indent.</param>
    /// <param name="endWidth">Characters for the end section.</param>
    /// <param name="chars">The 8-character drawing set.</param>
    /// <returns>The graph string to display before the node text.</returns>
    /// <remarks>
    /// Character layout in chars:
    ///   0: Level Filler (space)
    ///   1: Level Mark (vertical bar)
    ///   2: End First - not last child (T shape)
    ///   3: End First - last child (L shape)
    ///   4: End Filler (horizontal line)
    ///   5: End Child position (unused, typically horizontal)
    ///   6: Retracted character (+)
    ///   7: Expanded character (-)
    /// </remarks>
    public string CreateGraph(int level, long lines, ushort flags, int levWidth, int endWidth, string chars)
    {
        const int FillerOrBar = 0;
        const int YorL = 2;
        const int StraightOrTee = 4;
        const int Retracted = 6;

        bool expanded = (flags & OutlineFlags.ovExpanded) != 0;
        bool children = (flags & OutlineFlags.ovChildren) != 0;
        bool last = (flags & OutlineFlags.ovLast) != 0;

        var graph = new char[level * levWidth + endWidth];
        int p = 0;

        // Build level marks
        for (int lev = level; lev > 0; lev--, lines >>= 1)
        {
            graph[p++] = (lines & 1) != 0 ? chars[FillerOrBar + 1] : chars[FillerOrBar];
            for (int i = 1; i < levWidth; i++)
            {
                graph[p++] = chars[FillerOrBar];
            }
        }

        // Build end section
        int ew = endWidth - 1;
        if (ew > 0)
        {
            graph[p++] = last ? chars[YorL + 1] : chars[YorL];
            ew--;

            if (ew > 0)
            {
                ew--;
                // Fill with horizontal lines
                while (ew > 0)
                {
                    graph[p++] = chars[StraightOrTee];
                    ew--;
                }
                graph[p++] = children ? chars[StraightOrTee + 1] : chars[StraightOrTee];
            }

            graph[p++] = expanded ? chars[Retracted + 1] : chars[Retracted];
        }

        return new string(graph, 0, p);
    }

    #endregion

    #region Tree Traversal

    /// <summary>
    /// Iterates over the outline tree, calling the visitor for each node.
    /// Stops when the visitor returns true.
    /// </summary>
    public TNode? FirstThat(OutlineVisitor test)
    {
        return Iterate(test, checkResult: true);
    }

    /// <summary>
    /// Iterates over all nodes in the outline tree.
    /// </summary>
    public TNode? ForEach(OutlineVisitor action)
    {
        return Iterate(action, checkResult: false);
    }

    private TNode? Iterate(OutlineVisitor action, bool checkResult)
    {
        int position = -1;
        var root = GetRoot();
        if (root != null)
        {
            return TraverseTree(action, ref position, checkResult, root, 0, 0, GetNext(root) == null);
        }
        return null;
    }

    private TNode? TraverseTree(OutlineVisitor action, ref int position, bool checkResult,
        TNode cur, int level, long lines, bool lastChild)
    {
        bool children = HasChildren(cur);

        ushort flags = 0;
        if (lastChild)
            flags |= OutlineFlags.ovLast;
        if (children && IsExpanded(cur))
            flags |= OutlineFlags.ovChildren;
        if (!children || IsExpanded(cur))
            flags |= OutlineFlags.ovExpanded;

        position++;

        bool result = action(this, cur, level, position, lines, flags);
        if (checkResult && result)
            return cur;

        // Traverse children if expanded
        if (children && IsExpanded(cur))
        {
            long childLines = lines;
            if (!lastChild)
                childLines |= 1L << level;

            var child = GetChild(cur, 0);
            while (child != null)
            {
                var next = GetNext(child);
                var ret = TraverseTree(action, ref position, checkResult, child, level + 1, childLines, next == null);
                child = next;
                if (ret != null)
                    return ret;
            }
        }

        // Traverse siblings at root level
        if (cur == GetRoot())
        {
            var next = cur;
            while ((next = GetNext(next)) != null)
            {
                var ret = TraverseTree(action, ref position, checkResult, next, level, lines, GetNext(next) == null);
                if (ret != null)
                    return ret;
            }
        }

        return null;
    }

    #endregion

    #region Node Access

    /// <summary>
    /// Gets the node at display position i.
    /// </summary>
    public TNode? GetNode(int i)
    {
        _auxPos = i;
        return FirstThat(IsNodeAtPosition);
    }

    private static bool IsNodeAtPosition(TOutlineViewer viewer, TNode node, int level, int position, long lines, ushort flags)
    {
        return _auxPos == position;
    }

    #endregion

    #region Focus Management

    private void AdjustFocus(int newFocus)
    {
        if (newFocus < 0)
            newFocus = 0;
        else if (newFocus >= Limit.Y)
            newFocus = Limit.Y - 1;

        if (Foc != newFocus)
            Focused(newFocus);

        // Scroll to keep focus visible
        if (newFocus < Delta.Y)
        {
            ScrollTo(Delta.X, newFocus);
        }
        else if (newFocus - Size.Y >= Delta.Y)
        {
            ScrollTo(Delta.X, newFocus - Size.Y + 1);
        }
    }

    #endregion

    #region Event Handling

    public override void HandleEvent(ref TEvent ev)
    {
        const int mouseAutoToSkip = 3;

        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evMouseDown:
                HandleMouseDown(ref ev, mouseAutoToSkip);
                break;

            case EventConstants.evKeyDown:
                HandleKeyDown(ref ev);
                break;
        }
    }

    private void HandleMouseDown(ref TEvent ev, int mouseAutoToSkip)
    {
        int count = 0;
        byte dragged = 0;
        int newFocus = Foc;

        do
        {
            if (dragged < 2)
                dragged++;

            var mouse = MakeLocal(ev.Mouse.Where);

            if (MouseInView(ev.Mouse.Where))
            {
                int i = Delta.Y + mouse.Y;
                newFocus = i < Limit.Y ? i : Foc;
            }
            else
            {
                if (ev.What == EventConstants.evMouseAuto)
                    count++;

                if (count == mouseAutoToSkip)
                {
                    count = 0;
                    if (mouse.Y < 0)
                        newFocus--;
                    if (mouse.Y >= Size.Y)
                        newFocus++;
                }
            }

            if (Foc != newFocus)
            {
                AdjustFocus(newFocus);
                DrawView();
            }
        } while (MouseEvent(ref ev, EventConstants.evMouseMove | EventConstants.evMouseAuto));

        if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0)
        {
            Selected(Foc);
        }
        else
        {
            if (dragged < 2)
            {
                var cur = FirstThat(IsFocusedNode);
                if (cur != null)
                {
                    string graph = GetGraph(_focLevel, _focLines, _focFlags);
                    var mouse = MakeLocal(ev.Mouse.Where);
                    if (mouse.X < TStringUtils.StrWidth(graph))
                    {
                        Adjust(cur, !IsExpanded(cur));
                        Update();
                        DrawView();
                    }
                }
            }
        }

        ClearEvent(ref ev);
    }

    private static bool IsFocusedNode(TOutlineViewer viewer, TNode node, int level, int position, long lines, ushort flags)
    {
        if (position == viewer.Foc)
        {
            _focLevel = level;
            _focLines = lines;
            _focFlags = flags;
            return true;
        }
        return false;
    }

    private void HandleKeyDown(ref TEvent ev)
    {
        int newFocus = Foc;
        bool handled = true;

        switch (TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode))
        {
            case KeyConstants.kbUp:
            case KeyConstants.kbLeft:
                newFocus--;
                break;

            case KeyConstants.kbDown:
            case KeyConstants.kbRight:
                newFocus++;
                break;

            case KeyConstants.kbPgDn:
                newFocus += Size.Y - 1;
                break;

            case KeyConstants.kbPgUp:
                newFocus -= Size.Y - 1;
                break;

            case KeyConstants.kbHome:
                newFocus = Delta.Y;
                break;

            case KeyConstants.kbEnd:
                newFocus = Delta.Y + Size.Y - 1;
                break;

            case KeyConstants.kbCtrlPgUp:
                newFocus = 0;
                break;

            case KeyConstants.kbCtrlPgDn:
                newFocus = Limit.Y - 1;
                break;

            case KeyConstants.kbEnter:
                Selected(newFocus);
                break;

            default:
                // Check for +, -, * characters
                byte charCode = (byte)(ev.KeyDown.KeyCode & 0xFF);
                switch ((char)charCode)
                {
                    case '-':
                    case '+':
                        var cur = GetNode(newFocus);
                        if (cur != null)
                        {
                            Adjust(cur, charCode == '+');
                        }
                        Update();
                        break;

                    case '*':
                        var curNode = GetNode(newFocus);
                        if (curNode != null)
                        {
                            ExpandAll(curNode);
                        }
                        Update();
                        break;

                    default:
                        handled = false;
                        break;
                }
                break;
        }

        if (handled)
        {
            ClearEvent(ref ev);
            AdjustFocus(newFocus);
            DrawView();
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfFocused) != 0)
        {
            DrawView();
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates the limits of the outline viewer. Should be called whenever
    /// the data changes.
    /// </summary>
    public void Update()
    {
        _updateCount = 0;
        _updateMaxX = 0;
        FirstThat(CountNode);
        SetLimit(_updateMaxX, _updateCount);
        AdjustFocus(Foc);
    }

    private static bool CountNode(TOutlineViewer viewer, TNode node, int level, int position, long lines, ushort flags)
    {
        _updateCount++;
        string graph = viewer.GetGraph(level, lines, flags);
        int len = TStringUtils.StrWidth(viewer.GetText(node)) + TStringUtils.StrWidth(graph);
        if (_updateMaxX < len)
            _updateMaxX = len;
        return false;
    }

    /// <summary>
    /// Expands a node and all its children recursively.
    /// </summary>
    public void ExpandAll(TNode node)
    {
        if (HasChildren(node))
        {
            Adjust(node, true);
            int n = GetNumChildren(node);
            for (int i = 0; i < n; i++)
            {
                var child = GetChild(node, i);
                if (child != null)
                {
                    ExpandAll(child);
                }
            }
        }
    }

    #endregion

    #region Node Disposal

    /// <summary>
    /// Recursively disposes of a node tree.
    /// </summary>
    protected static void DisposeNode(TNode? node)
    {
        if (node != null)
        {
            if (node.ChildList != null)
                DisposeNode(node.ChildList);
            if (node.Next != null)
                DisposeNode(node.Next);
            // In C#, GC handles memory, but this clears references
            node.ChildList = null;
            node.Next = null;
        }
    }

    #endregion

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }
}
