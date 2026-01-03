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
            do
            {
                var next = p.Next;
                p.ShutDown();
                p = next;
            } while (p != Last && p != null);
        }

        FreeBuffer();
        Current = null;
        Last = null;
        base.ShutDown();
    }

    // View management
    public void Insert(TView p)
    {
        InsertBefore(p, First());
    }

    public void InsertBefore(TView p, TView? target)
    {
        if (p == null || p.Owner != null)
        {
            return;
        }

        if (target != null)
        {
            InsertView(p, target);
        }
        else
        {
            InsertView(p, Last);
        }

        p.Owner = this;
        p.Awaken();

        if ((p.Options & OptionFlags.ofSelectable) != 0)
        {
            p.Select();
        }
    }

    public void InsertView(TView p, TView? target)
    {
        if (Last == null)
        {
            Last = p;
            p.Next = p;
        }
        else if (target == null)
        {
            p.Next = Last.Next;
            Last.Next = p;
            Last = p;
        }
        else
        {
            var prev = target.Prev();
            if (prev != null)
            {
                p.Next = target;
                prev.Next = p;
            }
        }
    }

    public void Remove(TView p)
    {
        RemoveView(p);
        p.Owner = null;
    }

    public void RemoveView(TView p)
    {
        if (Last == null)
        {
            return;
        }

        if (Last.Next == p && Last == p)
        {
            Last = null;
        }
        else
        {
            var prev = p.Prev();
            if (prev != null)
            {
                prev.Next = p.Next;
                if (Last == p)
                {
                    Last = prev;
                }
            }
        }

        if (Current == p)
        {
            ResetCurrent();
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
                if (mode != SelectMode.leaveSelect)
                {
                    Current = p;
                }
                FocusView(Current, true);
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
        if (p != null)
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
        }
        if (Buffer != null)
        {
            WriteBuf(0, 0, Size.X, Size.Y, Buffer);
        }
        else
        {
            Redraw();
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
        if ((Options & OptionFlags.ofBuffered) != 0 && Buffer == null)
        {
            Buffer = new TScreenCell[Size.X * Size.Y];
        }
    }

    public void FreeBuffer()
    {
        Buffer = null;
    }

    // Locking
    public void Lock()
    {
        LockFlag++;
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
                if (ev.What != EventConstants.evNothing && (p.Options & OptionFlags.ofPreProcess) != 0)
                {
                    p.HandleEvent(ref ev);
                }
                p = next;
            }

            Phase = PhaseType.phFocused;
            if (ev.What != EventConstants.evNothing && Current != null)
            {
                Current.HandleEvent(ref ev);
            }

            Phase = PhaseType.phPostProcess;
            p = First();
            while (p != null)
            {
                var next = p.NextView();
                if (ev.What != EventConstants.evNothing && (p.Options & OptionFlags.ofPostProcess) != 0)
                {
                    p.HandleEvent(ref ev);
                }
                p = next;
            }
        }
        else
        {
            var p = First();
            while (p != null)
            {
                var next = p.NextView();
                if (ev.What != EventConstants.evNothing && p.GetState(StateFlags.sfVisible))
                {
                    p.HandleEvent(ref ev);
                }
                p = next;
            }
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
        var saveCurrent = Current;
        var saveCommands = new TCommandSet(CurCommandSet);
        var saveTopView = TopView();

        Insert(p);
        p.Options |= OptionFlags.ofTopSelect;
        p.SetState(StateFlags.sfModal, true);

        SetCurrent(p, SelectMode.enterSelect);

        ushort result = p.Execute();

        SetCurrent(saveCurrent, SelectMode.leaveSelect);
        p.SetState(StateFlags.sfModal, false);
        p.Options = saveOptions;
        Remove(p);

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
        FreeBuffer();
        SetBounds(bounds);
        Clip = GetExtent();

        ForEach((view, _) =>
        {
            var r = new TRect();
            view.CalcBounds(ref r, delta);
            view.ChangeBounds(r);
        }, null);
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

        if ((aState & (StateFlags.sfActive | StateFlags.sfFocused | StateFlags.sfExposed)) != 0)
        {
            Lock();
            try
            {
                ForEach((view, _) =>
                {
                    if (view.GetState(StateFlags.sfVisible))
                    {
                        view.SetState(aState, enable);
                    }
                }, null);
            }
            finally
            {
                Unlock();
            }
        }
    }
}
