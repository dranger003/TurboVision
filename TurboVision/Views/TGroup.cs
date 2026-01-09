using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Container that owns and manages child views.
/// </summary>
public class TGroup : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TGroup";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    // Runtime state - not serialized, reconstructed after deserialization
    [JsonIgnore]
    public TView? Last { get; set; }

    [JsonIgnore]
    public TView? Current { get; set; }

    [JsonIgnore]
    public TRect Clip { get; set; }

    [JsonIgnore]
    public PhaseType Phase { get; set; }

    [JsonIgnore]
    public TScreenCell[]? Buffer { get; set; }

    [JsonIgnore]
    public byte LockFlag { get; set; }

    [JsonIgnore]
    public ushort EndState { get; set; }

    // ============================================================================
    // SECTION 1: Constructor & Destructor
    // Upstream: tgroup.cpp lines 27-38, 652
    // ============================================================================

    /// <summary>
    /// Constructor with bounds.
    /// Upstream: tgroup.cpp lines 27-34
    /// </summary>
    public TGroup(TRect bounds) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofBuffered;
        Clip = GetExtent();
        EventMask = 0xFFFF;
    }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// Note: C++ has streamableInit constructor, C# uses [JsonConstructor]
    /// </summary>
    [JsonConstructor]
    protected TGroup() : base()
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofBuffered;
        EventMask = 0xFFFF;
    }

    // ============================================================================
    // SECTION 2: Lifecycle
    // Upstream: tgroup.cpp lines 40-76
    // ============================================================================

    /// <summary>
    /// Shut down and destroy all subviews.
    /// Upstream: tgroup.cpp lines 40-59
    /// </summary>
    public override void ShutDown()
    {
        var p = Last;
        if (p != null)
        {
            // First, hide all views
            do
            {
                p.Hide();
                p = p.Prev();
            } while (p != Last && p != null);

            // Then destroy all views - continue until Last is null
            do
            {
                var prev = p!.Prev();
                p.ShutDown();
                p = prev;
            } while (Last != null);
        }

        FreeBuffer();
        Current = null;
        base.ShutDown();
    }

    /// <summary>
    /// Initialize after deserialization.
    /// Upstream: tgroup.cpp lines 73-76
    /// </summary>
    public override void Awaken()
    {
        ForEach(DoAwaken, null);
    }

    // ============================================================================
    // SECTION 3: ChangeBounds
    // Upstream: tgroup.cpp lines 78-98
    // ============================================================================

    /// <summary>
    /// Handle bounds changes (position and/or size).
    /// Upstream: tgroup.cpp lines 78-98
    /// </summary>
    public override void ChangeBounds(TRect bounds)
    {
        var delta = new TPoint(bounds.B.X - bounds.A.X - Size.X, bounds.B.Y - bounds.A.Y - Size.Y);

        if (delta.X == 0 && delta.Y == 0)
        {
            // Just a position change, no size change
            SetBounds(bounds);
            DrawView();
        }
        else
        {
            // Size changed - reallocate buffer and update children
            SetBounds(bounds);
            Clip = GetExtent();
            GetBuffer();
            Lock();
            ForEach(DoCalcChange, delta);
            Unlock();
        }
    }

    // ============================================================================
    // SECTION 4: Data Management
    // Upstream: tgroup.cpp lines 100-315
    // ============================================================================

    /// <summary>
    /// Get total data size of all subviews.
    /// Upstream: tgroup.cpp lines 105-110
    /// </summary>
    public override int DataSize()
    {
        var box = new StrongBox<int>(0);
        ForEach(AddSubviewDataSize, box);
        return box.Value;
    }

    /// <summary>
    /// Get data from all subviews.
    /// Upstream: tgroup.cpp lines 303-315
    /// </summary>
    public override void GetData(Span<byte> rec)
    {
        int offset = 0;
        var p = First();
        while (p != null)
        {
            var next = p.NextView();
            int viewSize = p.DataSize();
            if (offset + viewSize <= rec.Length)
            {
                p.GetData(rec.Slice(offset, viewSize));
                offset += viewSize;
            }
            p = next;
        }
    }

    /// <summary>
    /// Set data to all subviews.
    /// Upstream: tgroup.cpp lines 495-507
    /// </summary>
    public override void SetData(ReadOnlySpan<byte> rec)
    {
        int offset = 0;
        var p = First();
        while (p != null)
        {
            var next = p.NextView();
            int viewSize = p.DataSize();
            if (offset + viewSize <= rec.Length)
            {
                p.SetData(rec.Slice(offset, viewSize));
                offset += viewSize;
            }
            p = next;
        }
    }

    // ============================================================================
    // SECTION 5: View Management - Remove
    // Upstream: tgroup.cpp lines 112-125
    // ============================================================================

    /// <summary>
    /// Remove a view from the group.
    /// Upstream: tgroup.cpp lines 112-125
    /// </summary>
    public void Remove(TView? p)
    {
        if (p != null)
        {
            var saveState = p.State;
            p.Hide();
            RemoveView(p);
            p.Owner = null;
            p.Next = null;
            if ((saveState & StateFlags.sfVisible) != 0)
            {
                p.Show();
            }
        }
    }

    /// <summary>
    /// Low-level view removal from circular list.
    /// Note: Not in upstream header, but used by Remove
    /// </summary>
    public void RemoveView(TView p)
    {
        if (Last == null)
        {
            return;
        }

        // Find the view before p in the circular list
        var s = Last;
        while (s.Next != p)
        {
            if (s.Next == Last)
            {
                // p is not in this group's list
                return;
            }
            s = s.Next!;
        }

        // Remove p from the list
        s.Next = p.Next;
        if (p == Last)
        {
            // p was the last view
            Last = (p == p.Next) ? null : s;
        }
    }

    // ============================================================================
    // SECTION 6: Drawing
    // Upstream: tgroup.cpp lines 128-157
    // ============================================================================

    /// <summary>
    /// Main draw method with buffer management.
    /// Upstream: tgroup.cpp lines 128-148
    /// </summary>
    public override void Draw()
    {
        if (Buffer == null)
        {
            GetBuffer();
            if (Buffer != null)
            {
                // Buffer was just created, populate it by redrawing children
                // Use direct increment/decrement to avoid triggering DrawView via Unlock
                LockFlag++;
                Redraw();
                if (LockFlag > 0) LockFlag--;
            }
        }
        if (Buffer != null)
        {
            WriteBuf(0, 0, Size.X, Size.Y, Buffer);
        }
        else
        {
            // No buffer, draw directly (matches upstream exactly)
            Clip = GetClipRect();
            Redraw();
            Clip = GetExtent();
        }
    }

    /// <summary>
    /// Draw range of subviews.
    /// Upstream: tgroup.cpp lines 150-157
    /// </summary>
    public void DrawSubViews(TView? p, TView? bottom)
    {
        while (p != bottom && p != null)
        {
            p.DrawView();
            p = p.NextView();
        }
    }

    /// <summary>
    /// Redraw all subviews.
    /// Upstream: tgroup.cpp lines 437-440
    /// </summary>
    public void Redraw()
    {
        DrawSubViews(First(), null);
    }

    // ============================================================================
    // SECTION 7: Modal Execution
    // Upstream: tgroup.cpp lines 159-214
    // ============================================================================

    /// <summary>
    /// End modal execution.
    /// Upstream: tgroup.cpp lines 159-165
    /// </summary>
    public override void EndModal(ushort command)
    {
        if (GetState(StateFlags.sfModal))
        {
            EndState = command;
        }
        else
        {
            base.EndModal(command);
        }
    }

    /// <summary>
    /// Handle unprocessed events.
    /// Upstream: tgroup.cpp lines 167-171
    /// </summary>
    public virtual void EventError(ref TEvent ev)
    {
    }

    /// <summary>
    /// Main event loop for modal execution.
    /// Upstream: tgroup.cpp lines 173-186
    /// </summary>
    public override ushort Execute()
    {
        do
        {
            EndState = 0;
            TEvent ev = default;
            do
            {
                GetEvent(ref ev);
                HandleEvent(ref ev);
                if (ev.What != EventConstants.evNothing)
                {
                    EventError(ref ev);
                }

                // INTERIM FIX: Reset cursor after each event to ensure it's positioned correctly.
                // This compensates for lack of DisplayBuffer system - in upstream, cursor positioning
                // happens after all drawing during DisplayBuffer::flushScreen(). In our C# port,
                // drawing happens immediately, so we must explicitly reset cursor after each event
                // to prevent it from being left at the last drawn position (e.g., end of HeapView).
                // TODO: Implement proper DisplayBuffer system (see dispbuff.cpp) for permanent fix.
                ResetCursor();
            } while (EndState == 0);
        } while (!Valid(EndState));

        return EndState;
    }

    /// <summary>
    /// Execute a view modally.
    /// Upstream: tgroup.cpp lines 188-214
    /// </summary>
    public ushort ExecView(TView? p)
    {
        if (p == null)
        {
            return CommandConstants.cmCancel;
        }

        var saveOptions = p.Options;
        var saveOwner = p.Owner;
        var saveCurrent = Current;
        var saveCommands = new TCommandSet(CurCommandSet);
        var saveTopView = TopView();

        // Clear ofSelectable and set sfModal BEFORE insert to prevent
        // resetCurrent() from being triggered when the view becomes visible.
        // This matches the upstream C++ order of operations.
        p.Options &= unchecked((ushort)~OptionFlags.ofSelectable);
        p.SetState(StateFlags.sfModal, true);
        SetCurrent(p, SelectMode.enterSelect);

        // Only insert if view wasn't already owned (not already in hierarchy)
        if (saveOwner == null)
        {
            Insert(p);
        }

        ushort result = p.Execute();

        // Only remove if view wasn't already owned
        if (saveOwner == null)
        {
            Remove(p);
        }

        SetCurrent(saveCurrent, SelectMode.leaveSelect);
        p.SetState(StateFlags.sfModal, false);
        p.Options = saveOptions;

        SetCommands(saveCommands);

        return result;
    }

    // ============================================================================
    // SECTION 8: Navigation
    // Upstream: tgroup.cpp lines 216-274
    // ============================================================================

    /// <summary>
    /// Get first view in circular list.
    /// Upstream: tgroup.cpp lines 216-222
    /// </summary>
    public TView? First()
    {
        return Last?.Next;
    }

    /// <summary>
    /// Find next visible, enabled, selectable view.
    /// Upstream: tgroup.cpp lines 224-245
    /// </summary>
    private TView? FindNext(bool forwards)
    {
        if (Current == null)
        {
            return null;
        }

        var p = Current;
        do
        {
            p = forwards ? p.NextView() : p.PrevView();
            if (p == null)
            {
                p = forwards ? First() : Last;
            }
        } while (p != Current && p != null && (!p.GetState(StateFlags.sfVisible) || (p.Options & OptionFlags.ofSelectable) == 0));

        return p != Current ? p : null;
    }

    /// <summary>
    /// Focus next selectable view.
    /// Upstream: tgroup.cpp lines 247-256
    /// </summary>
    public bool FocusNext(bool forwards)
    {
        var p = FindNext(forwards);
        return p != null && p.Focus();
    }

    /// <summary>
    /// Find first view matching state and options.
    /// Upstream: tgroup.cpp lines 258-274
    /// </summary>
    public TView? FirstMatch(ushort aState, ushort aOptions)
    {
        var p = First();
        while (p != null)
        {
            if ((p.State & aState) == aState && (p.Options & aOptions) == aOptions)
            {
                return p;
            }
            p = p.NextView();
        }
        return null;
    }

    /// <summary>
    /// Get view at index.
    /// Note: Not in upstream header at this position
    /// </summary>
    public TView? At(int index)
    {
        var p = First();
        while (p != null && index > 0)
        {
            p = p.NextView();
            index--;
        }
        return p;
    }

    /// <summary>
    /// Get index of view.
    /// Note: Not in upstream header at this position
    /// </summary>
    public int IndexOf(TView p)
    {
        var current = First();
        int index = 0;
        while (current != null)
        {
            if (current == p)
            {
                return index;
            }
            current = current.NextView();
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Find first view satisfying predicate.
    /// Note: Not in upstream header at this position
    /// </summary>
    public TView? FirstThat(Func<TView, object?, bool> func, object? args)
    {
        var p = First();
        while (p != null)
        {
            if (func(p, args))
            {
                return p;
            }
            p = p.NextView();
        }
        return null;
    }

    /// <summary>
    /// Apply action to all views.
    /// Note: Not in upstream header at this position
    /// </summary>
    public void ForEach(Action<TView, object?> action, object? args)
    {
        var p = First();
        while (p != null)
        {
            var next = p.NextView();
            action(p, args);
            p = next;
        }
    }

    // ============================================================================
    // SECTION 9: Buffer Management
    // Upstream: tgroup.cpp lines 276-301
    // ============================================================================

    /// <summary>
    /// Free off-screen buffer.
    /// Upstream: tgroup.cpp lines 276-283
    /// </summary>
    public void FreeBuffer()
    {
        // Don't free the screen buffer (owned by TScreen)
        if (Buffer != Platform.TScreen.ScreenBuffer)
        {
            Buffer = null;
        }
    }

    /// <summary>
    /// Allocate off-screen buffer.
    /// Upstream: tgroup.cpp lines 285-301
    /// </summary>
    public void GetBuffer()
    {
        // Only allocate buffer if exposed (matches C++ behavior)
        // Don't allocate if we already have a buffer (e.g., TProgram uses TScreen.ScreenBuffer)
        if ((State & StateFlags.sfExposed) != 0 &&
            (Options & OptionFlags.ofBuffered) != 0 &&
            Buffer == null)
        {
            Buffer = new TScreenCell[Size.X * Size.Y];
        }
    }

    // ============================================================================
    // SECTION 10: Event Handling
    // Upstream: tgroup.cpp lines 317-384
    // ============================================================================

    /// <summary>
    /// Route events through three phases to children.
    /// Upstream: tgroup.cpp lines 357-384
    /// </summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if ((ev.What & EventConstants.focusedEvents) != 0)
        {
            Phase = PhaseType.phPreProcess;
            var p = First();
            while (p != null)
            {
                var next = p.NextView();
                if (ev.What != EventConstants.evNothing)
                {
                    DoHandleEvent(p, ref ev);
                }
                p = next;
            }

            Phase = PhaseType.phFocused;
            if (ev.What != EventConstants.evNothing && Current != null)
            {
                DoHandleEvent(Current, ref ev);
            }

            Phase = PhaseType.phPostProcess;
            p = First();
            while (p != null)
            {
                var next = p.NextView();
                if (ev.What != EventConstants.evNothing)
                {
                    DoHandleEvent(p, ref ev);
                }
                p = next;
            }
        }
        else if (ev.What != EventConstants.evNothing)
        {
            Phase = PhaseType.phFocused;
            if ((ev.What & EventConstants.positionalEvents) != 0)
            {
                // For positional events (mouse), only handle by the view under the mouse
                var target = FirstThat(HasMouse, ev);
                if (target != null)
                {
                    DoHandleEvent(target, ref ev);
                }
            }
            else
            {
                // For other events (broadcasts), send to all views
                var p = First();
                while (p != null)
                {
                    var next = p.NextView();
                    if (ev.What != EventConstants.evNothing)
                    {
                        DoHandleEvent(p, ref ev);
                    }
                    p = next;
                }
            }
        }
    }

    /// <summary>
    /// Helper to handle events for a view, respecting disabled state, phase, and event mask.
    /// Upstream: doHandleEvent (tgroup.cpp lines 324-350)
    /// </summary>
    private void DoHandleEvent(TView? p, ref TEvent ev)
    {
        if (p == null)
            return;

        // Skip disabled views for positional and focused events
        if ((p.State & StateFlags.sfDisabled) != 0 &&
            (ev.What & (EventConstants.positionalEvents | EventConstants.focusedEvents)) != 0)
            return;

        // Check phase-specific options
        switch (Phase)
        {
            case PhaseType.phFocused:
                break;
            case PhaseType.phPreProcess:
                if ((p.Options & OptionFlags.ofPreProcess) == 0)
                    return;
                break;
            case PhaseType.phPostProcess:
                if ((p.Options & OptionFlags.ofPostProcess) == 0)
                    return;
                break;
        }

        // Only handle if event matches the view's event mask
        if ((ev.What & p.EventMask) != 0)
        {
            p.HandleEvent(ref ev);
        }
    }

    /// <summary>
    /// Get event from owner.
    /// Note: Delegates to owner
    /// </summary>
    public override void GetEvent(ref TEvent ev)
    {
        Owner?.GetEvent(ref ev);
    }

    // ============================================================================
    // SECTION 11: View Management - Insert
    // Upstream: tgroup.cpp lines 386-429
    // ============================================================================

    /// <summary>
    /// Insert view at beginning.
    /// Upstream: tgroup.cpp lines 386-389
    /// </summary>
    public void Insert(TView p)
    {
        InsertBefore(p, First());
    }

    /// <summary>
    /// Insert view before target with centering options.
    /// Upstream: tgroup.cpp lines 391-407
    /// </summary>
    public void InsertBefore(TView p, TView? target)
    {
        if (p == null || p.Owner != null || (target != null && target.Owner != this))
        {
            return;
        }

        // Handle centering options
        if ((p.Options & OptionFlags.ofCenterX) != 0)
        {
            p.Origin = new TPoint((Size.X - p.Size.X) / 2, p.Origin.Y);
        }
        if ((p.Options & OptionFlags.ofCenterY) != 0)
        {
            p.Origin = new TPoint(p.Origin.X, (Size.Y - p.Size.Y) / 2);
        }

        // Save state and hide before inserting
        var saveState = p.State;
        p.Hide();

        // Insert into the view list (this also sets the owner)
        InsertView(p, target);

        // Show if it was visible
        if ((saveState & StateFlags.sfVisible) != 0)
        {
            p.Show();
        }

        // Activate if the group is active
        if ((saveState & StateFlags.sfActive) != 0)
        {
            p.SetState(StateFlags.sfActive, true);
        }
    }

    /// <summary>
    /// Low-level view insertion into circular list.
    /// Upstream: tgroup.cpp lines 409-429
    /// </summary>
    public void InsertView(TView p, TView? target)
    {
        // Set owner first (matches C++ behavior)
        p.Owner = this;

        if (target != null)
        {
            // Insert before target
            var prev = target.Prev();
            if (prev != null)
            {
                p.Next = prev.Next;
                prev.Next = p;
            }
        }
        else
        {
            // Insert at end (after Last)
            if (Last == null)
            {
                p.Next = p;
            }
            else
            {
                p.Next = Last.Next;
                Last.Next = p;
            }
            Last = p;
        }
    }

    // ============================================================================
    // SECTION 12: Locking
    // Upstream: tgroup.cpp lines 431-441
    // ============================================================================

    /// <summary>
    /// Increment redraw lock counter.
    /// Upstream: tgroup.cpp lines 431-435
    /// </summary>
    public void Lock()
    {
        // Only lock if buffer exists or already locked (matches C++ behavior)
        if (Buffer != null || LockFlag != 0)
        {
            LockFlag++;
        }
    }

    /// <summary>
    /// Decrement redraw lock counter and redraw if zero.
    /// Upstream: tgroup.cpp lines 555-559
    /// </summary>
    public void Unlock()
    {
        if (LockFlag > 0)
        {
            LockFlag--;
            if (LockFlag == 0)
            {
                DrawView();
            }
        }
    }

    // ============================================================================
    // SECTION 13: Selection Management
    // Upstream: tgroup.cpp lines 442-493
    // ============================================================================

    /// <summary>
    /// Reset current to first selectable view.
    /// Upstream: tgroup.cpp lines 442-445
    /// </summary>
    public void ResetCurrent()
    {
        SetCurrent(FirstMatch(StateFlags.sfVisible, OptionFlags.ofSelectable), SelectMode.normalSelect);
        }

    /// <summary>
    /// Reset cursor to current view.
    /// Upstream: tgroup.cpp lines 447-451
    /// </summary>
    public override void ResetCursor()
    {
        if (Current != null)
        {
            Current.ResetCursor();
        }
        else
        {
            base.ResetCursor();
        }
    }

    /// <summary>
    /// Select next view in direction.
    /// Upstream: tgroup.cpp lines 453-460
    /// </summary>
    public void SelectNext(bool forwards)
    {
        var p = FindNext(forwards);
        if (p != null)
        {
            p.Select();
        }
    }

    /// <summary>
    /// Helper to select a view.
    /// Upstream: tgroup.cpp lines 462-466
    /// </summary>
    private void SelectView(TView? p, bool enable)
    {
        if (p != null)
        {
            p.SetState(StateFlags.sfSelected, enable);
        }
    }

    /// <summary>
    /// Helper to focus a view if group is focused.
    /// Upstream: tgroup.cpp lines 468-472
    /// </summary>
    private void FocusView(TView? p, bool enable)
    {
        // Only set focus on child if the group itself is focused
        if (GetState(StateFlags.sfFocused) && p != null)
        {
            p.SetState(StateFlags.sfFocused, enable);
        }
    }

    /// <summary>
    /// Set current focused/selected view.
    /// Upstream: tgroup.cpp lines 476-493
    /// Note: C# returns bool, C++ returns void
    /// </summary>
    public bool SetCurrent(TView? p, SelectMode mode)
    {
        if (Current != p)
        {
            Lock();
            try
            {
                FocusView(Current, false);
                // Deselect old current (unless entering selection)
                if (mode != SelectMode.enterSelect && Current != null)
                {
                    Current.SetState(StateFlags.sfSelected, false);
                }
                // Select new view (unless leaving selection)
                if (mode != SelectMode.leaveSelect && p != null)
                {
                    p.SetState(StateFlags.sfSelected, true);
                }
                // Focus new view if group is focused
                if (GetState(StateFlags.sfFocused) && p != null)
                {
                    p.SetState(StateFlags.sfFocused, true);
                }
                Current = p;
            }
            finally
            {
                Unlock();
            }
        }
        return true;
    }

    // ============================================================================
    // SECTION 14: State Propagation
    // Upstream: tgroup.cpp lines 495-553
    // ============================================================================

    /// <summary>
    /// Propagate state changes to children.
    /// Upstream: tgroup.cpp lines 526-553
    /// </summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        // For sfActive or sfDragging: propagate to all children
        if ((aState & (StateFlags.sfActive | StateFlags.sfDragging)) != 0)
        {
            var sb = new SetBlock { State = aState, Enable = enable };
            Lock();
            ForEach(DoSetState, sb);
            Unlock();
        }

        // For sfFocused: only propagate to current view
        if ((aState & StateFlags.sfFocused) != 0)
        {
            Current?.SetState(StateFlags.sfFocused, enable);
        }

        // For sfExposed: propagate to visible children and manage buffer
        if ((aState & StateFlags.sfExposed) != 0)
        {
            ForEach(DoExpose, enable);
            if (!enable)
            {
                FreeBuffer();
            }
        }
    }

    // ============================================================================
    // SECTION 15: Validation & Help
    // Upstream: tgroup.cpp lines 555-587
    // ============================================================================

    /// <summary>
    /// Validate all views for command.
    /// Upstream: tgroup.cpp lines 566-577
    /// </summary>
    public override bool Valid(ushort command)
    {
        // Special case: cmReleasedFocus only validates current view with ofValidate
        // Upstream: tgroup.cpp lines 568-574
        if (command == CommandConstants.cmReleasedFocus)
        {
            if (Current != null && (Current.Options & OptionFlags.ofValidate) != 0)
            {
                return Current.Valid(command);
            }
            return true;
        }

        // General case: all views must be valid
        // Upstream: tgroup.cpp line 576
        return FirstThat(IsInvalid, command) == null;
    }

    /// <summary>
    /// Get help context from current view or self.
    /// Upstream: tgroup.cpp lines 579-587
    /// </summary>
    public override ushort GetHelpCtx()
    {
        // Check current view first, then fall back to base
        ushort ctx = HelpContexts.hcNoContext;
        if (Current != null)
        {
            ctx = Current.GetHelpCtx();
        }
        if (ctx == HelpContexts.hcNoContext)
        {
            ctx = base.GetHelpCtx();
        }
        return ctx;
    }

    // ============================================================================
    // SECTION 16: JSON Serialization (C# Specific)
    // Note: Upstream uses binary streaming (tgroup.cpp lines 589-656)
    // C# implementation uses JSON with ViewHierarchyRebuilder
    // ============================================================================

    /// <summary>
    /// Gets or sets the child views for JSON serialization.
    /// On read, returns a list of all child views in Z-order.
    /// On write, stores the views to be reconstructed into the linked list.
    /// </summary>
    [JsonPropertyName("subViews")]
    public List<TView?>? SubViews
    {
        get => GetSubViewsForSerialization();
        set => _pendingSubViews = value;
    }

    /// <summary>
    /// Temporary storage for subviews during deserialization.
    /// </summary>
    [JsonIgnore]
    private List<TView?>? _pendingSubViews;

    /// <summary>
    /// Gets or sets the index of the current (focused) view for serialization.
    /// -1 if no current view.
    /// </summary>
    [JsonPropertyName("currentIndex")]
    public int CurrentIndex
    {
        get
        {
            if (Current == null || Last == null) return -1;
            var views = GetSubViewsForSerialization();
            return views?.IndexOf(Current) ?? -1;
        }
        set => _pendingCurrentIndex = value;
    }

    /// <summary>
    /// Temporary storage for current index during deserialization.
    /// </summary>
    [JsonIgnore]
    private int _pendingCurrentIndex = -1;

    /// <summary>
    /// Gets the pending subviews from deserialization.
    /// Used internally by ViewHierarchyRebuilder.
    /// </summary>
    internal List<TView?>? GetPendingSubViews() => _pendingSubViews;

    /// <summary>
    /// Gets the pending current index from deserialization.
    /// Used internally by ViewHierarchyRebuilder.
    /// </summary>
    internal int GetPendingCurrentIndex() => _pendingCurrentIndex;

    /// <summary>
    /// Clears the pending deserialization data after rebuilding.
    /// </summary>
    internal void ClearPendingData()
    {
        _pendingSubViews = null;
        _pendingCurrentIndex = -1;
    }

    /// <summary>
    /// Gets the child views as a list for serialization.
    /// Returns views in insertion order (first to last).
    /// </summary>
    public List<TView?>? GetSubViewsForSerialization()
    {
        if (Last == null) return null;

        var views = new List<TView?>();
        var view = First();
        if (view == null) return views;

        do
        {
            views.Add(view);
            view = view.Next;
        } while (view != First() && view != null);

        return views;
    }

    // ============================================================================
    // Static Helper Methods (Upstream Pattern)
    // These correspond to static functions in tgroup.cpp
    // ============================================================================

    /// <summary>
    /// Helper for Awaken - calls Awaken on each view.
    /// Upstream: doAwaken (tgroup.cpp line 68)
    /// </summary>
    private static void DoAwaken(TView view, object? args)
    {
        view.Awaken();
    }

    /// <summary>
    /// Helper for ChangeBounds - recalculates and applies bounds with delta.
    /// Upstream: doCalcChange (tgroup.cpp line 61)
    /// </summary>
    private static void DoCalcChange(TView view, object? delta)
    {
        var d = (TPoint)delta!;
        var r = new TRect();
        view.CalcBounds(ref r, d);
        view.ChangeBounds(r);
    }

    /// <summary>
    /// Helper for DataSize - accumulates data size from each view.
    /// Upstream: addSubviewDataSize (tgroup.cpp line 100)
    /// </summary>
    private static void AddSubviewDataSize(TView view, object? total)
    {
        var box = (StrongBox<int>)total!;
        box.Value += view.DataSize();
    }

    /// <summary>
    /// Helper for FirstThat - checks if view contains mouse position.
    /// Upstream: hasMouse (tgroup.cpp line 352)
    /// </summary>
    private static bool HasMouse(TView view, object? args)
    {
        return view.ContainsMouse((TEvent)args!);
    }

    /// <summary>
    /// Helper struct for SetState propagation.
    /// Upstream: setBlock (tgroup.cpp line 515)
    /// </summary>
    private struct SetBlock
    {
        public ushort State;
        public bool Enable;
    }

    /// <summary>
    /// Helper for SetState - propagates state to child view.
    /// Upstream: doSetState (tgroup.cpp line 521)
    /// </summary>
    private static void DoSetState(TView view, object? block)
    {
        var sb = (SetBlock)block!;
        view.SetState(sb.State, sb.Enable);
    }

    /// <summary>
    /// Helper for SetState - propagates sfExposed to visible children.
    /// Upstream: doExpose (tgroup.cpp line 509)
    /// </summary>
    private static void DoExpose(TView view, object? enable)
    {
        if ((view.State & StateFlags.sfVisible) != 0)
        {
            view.SetState(StateFlags.sfExposed, (bool)enable!);
        }
    }

    /// <summary>
    /// Helper for Valid - checks if view is invalid.
    /// Upstream: isInvalid (tgroup.cpp line 561)
    /// </summary>
    private static bool IsInvalid(TView view, object? command)
    {
        return !view.Valid((ushort)command!);
    }
}
