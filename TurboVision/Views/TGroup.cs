using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Container that owns and manages child views.
/// </summary>
public class TGroup : TView
{
    public TView? Last { get; set; }
    public TView? Current { get; set; }
    public TRect Clip { get; set; }
    public PhaseType Phase { get; set; }
    public TScreenCell[]? Buffer { get; set; }
    public byte LockFlag { get; set; }
    public ushort EndState { get; set; }

    public TGroup(TRect bounds) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable | OptionFlags.ofBuffered;
        Clip = GetExtent();
        EventMask = 0xFFFF;
    }

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

    // View management
    public void Insert(TView p)
    {
        InsertBefore(p, First());
    }

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

    public TView? First()
    {
        return Last?.Next;
    }

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

    // Focus and selection
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

    public void ResetCurrent()
    {
        SetCurrent(FirstMatch(StateFlags.sfVisible, OptionFlags.ofSelectable), SelectMode.normalSelect);
    }

    public void SelectNext(bool forwards)
    {
        var p = FindNext(forwards);
        if (p != null)
        {
            p.Select();
        }
    }

    public bool FocusNext(bool forwards)
    {
        var p = FindNext(forwards);
        return p != null && p.Focus();
    }

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

    private void FocusView(TView? p, bool enable)
    {
        // Only set focus on child if the group itself is focused
        if (GetState(StateFlags.sfFocused) && p != null)
        {
            p.SetState(StateFlags.sfFocused, enable);
        }
    }

    private void SelectView(TView? p, bool enable)
    {
        if (p != null)
        {
            p.SetState(StateFlags.sfSelected, enable);
        }
    }

    // Drawing
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

    public void Redraw()
    {
        DrawSubViews(First(), null);
    }

    public void DrawSubViews(TView? p, TView? bottom)
    {
        while (p != bottom && p != null)
        {
            p.DrawView();
            p = p.NextView();
        }
    }

    // Buffer management
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

    public void FreeBuffer()
    {
        // Don't free the screen buffer (owned by TScreen)
        if (Buffer != Platform.TScreen.ScreenBuffer)
        {
            Buffer = null;
        }
    }

    // Locking
    public void Lock()
    {
        // Only lock if buffer exists or already locked (matches C++ behavior)
        if (Buffer != null || LockFlag != 0)
        {
            LockFlag++;
        }
    }

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

    // Event handling
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
                var target = FirstThat((view, args) => view.ContainsMouse((TEvent)args!), ev);
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
    /// Matches C++ doHandleEvent behavior.
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

    public override void GetEvent(ref TEvent ev)
    {
        Owner?.GetEvent(ref ev);
    }

    public virtual void EventError(ref TEvent ev)
    {
    }

    // Modal execution
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

        // Only insert if view wasn't already owned (not already in hierarchy)
        if (saveOwner == null)
        {
            Insert(p);
        }

        p.Options &= unchecked((ushort)~OptionFlags.ofSelectable);
        p.SetState(StateFlags.sfModal, true);

        SetCurrent(p, SelectMode.enterSelect);

        ushort result = p.Execute();

        SetCurrent(saveCurrent, SelectMode.leaveSelect);
        p.SetState(StateFlags.sfModal, false);
        p.Options = saveOptions;

        // Only remove if view wasn't already owned
        if (saveOwner == null)
        {
            Remove(p);
        }

        SetCommands(saveCommands);

        return result;
    }

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
            } while (EndState == 0);
        } while (!Valid(EndState));

        return EndState;
    }

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

    // Bounds
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
            FreeBuffer();
            SetBounds(bounds);
            Clip = GetExtent();
            GetBuffer();
            Lock();
            ForEach((view, _) =>
            {
                var r = new TRect();
                view.CalcBounds(ref r, delta);
                view.ChangeBounds(r);
            }, null);
            Unlock();
        }
    }

    // Data
    public override int DataSize()
    {
        int size = 0;
        ForEach((view, _) => size += view.DataSize(), null);
        return size;
    }

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

    // Validation
    public override bool Valid(ushort command)
    {
        bool result = true;
        ForEach((view, _) =>
        {
            if (!view.Valid(command))
            {
                result = false;
            }
        }, null);
        return result;
    }

    // Activation
    public override void Awaken()
    {
        ForEach((view, _) => view.Awaken(), null);
    }

    // Help
    public override ushort GetHelpCtx()
    {
        var ctx = base.GetHelpCtx();
        if (ctx == HelpContexts.hcNoContext && Current != null)
        {
            ctx = Current.GetHelpCtx();
        }
        return ctx;
    }

    // Cursor
    public override void ResetCursor()
    {
        Current?.ResetCursor();
    }

    // State
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        // For sfActive or sfDragging: propagate to all children
        if ((aState & (StateFlags.sfActive | StateFlags.sfDragging)) != 0)
        {
            Lock();
            ForEach((view, _) => view.SetState(aState, enable), null);
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
            ForEach((view, _) =>
            {
                if (view.GetState(StateFlags.sfVisible))
                {
                    view.SetState(StateFlags.sfExposed, enable);
                }
            }, null);
            if (!enable)
            {
                FreeBuffer();
            }
        }
    }
}
