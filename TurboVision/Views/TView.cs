using TurboVision.Core;
using TurboVision.Platform;

namespace TurboVision.Views;

/// <summary>
/// The foundation view class. All visual elements derive from TView.
/// </summary>
public class TView : TObject
{
    // Phase type for event handling
    public enum PhaseType { phFocused, phPreProcess, phPostProcess }

    // Selection mode
    public enum SelectMode { normalSelect, enterSelect, leaveSelect }

    // Static members
    public static bool CommandSetChanged { get; set; }
    public static TCommandSet CurCommandSet { get; } = new();
    public static bool ShowMarkers { get; set; }
    public static byte ErrorAttr { get; set; } = 0xCF;

    // Instance members
    public TView? Next { get; set; }
    public TGroup? Owner { get; set; }
    public TPoint Size { get; set; }
    public TPoint Origin { get; set; }
    public TPoint Cursor { get; set; }
    public ushort Options { get; set; }
    public ushort EventMask { get; set; } = EventConstants.evMouseDown | EventConstants.evKeyDown | EventConstants.evCommand;
    public ushort State { get; set; } = StateFlags.sfVisible;
    public byte GrowMode { get; set; }
    public byte DragMode { get; set; }
    public ushort HelpCtx { get; set; }

    public TView(TRect bounds)
    {
        Origin = bounds.A;
        Size = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
    }

    // Bounds and geometry
    public TRect GetBounds()
    {
        return new TRect(Origin, Origin + Size);
    }

    public TRect GetExtent()
    {
        return new TRect(0, 0, Size.X, Size.Y);
    }

    public TRect GetClipRect()
    {
        var clip = GetBounds();
        if (Owner != null)
        {
            clip.Intersect(Owner.Clip);
        }
        clip.Move(-Origin.X, -Origin.Y);
        return clip;
    }

    public void SetBounds(TRect bounds)
    {
        Origin = bounds.A;
        Size = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
    }

    public virtual void SizeLimits(out TPoint min, out TPoint max)
    {
        min = new TPoint(0, 0);
        max = Owner != null ? Owner.Size : new TPoint(int.MaxValue, int.MaxValue);
    }

    public virtual void CalcBounds(ref TRect bounds, TPoint delta)
    {
        bounds = GetBounds();
        // TODO: Implement grow mode calculations
    }

    public virtual void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        DrawView();
    }

    public void GrowTo(int x, int y)
    {
        var r = GetBounds();
        r.B = new TPoint(r.A.X + x, r.A.Y + y);
        Locate(ref r);
    }

    public void MoveTo(int x, int y)
    {
        var r = GetBounds();
        r.Move(x - Origin.X, y - Origin.Y);
        Locate(ref r);
    }

    public void Locate(ref TRect bounds)
    {
        SizeLimits(out var min, out var max);
        var size = new TPoint(
            Math.Max(min.X, Math.Min(max.X, bounds.B.X - bounds.A.X)),
            Math.Max(min.Y, Math.Min(max.Y, bounds.B.Y - bounds.A.Y))
        );
        bounds.B = new TPoint(bounds.A.X + size.X, bounds.A.Y + size.Y);

        if (!bounds.Equals(GetBounds()))
        {
            ChangeBounds(bounds);
        }
    }

    // State management
    public bool GetState(ushort aState)
    {
        return (State & aState) != 0;
    }

    public virtual void SetState(ushort aState, bool enable)
    {
        if (enable)
        {
            State |= aState;
        }
        else
        {
            State &= (ushort)~aState;
        }

        if ((aState & StateFlags.sfVisible) != 0)
        {
            if (Owner != null)
            {
                if (enable)
                {
                    DrawShow(null);
                }
                else
                {
                    DrawHide(null);
                }
            }
        }
    }

    // Visibility
    public void Show()
    {
        if (!GetState(StateFlags.sfVisible))
        {
            SetState(StateFlags.sfVisible, true);
        }
    }

    public void Hide()
    {
        if (GetState(StateFlags.sfVisible))
        {
            SetState(StateFlags.sfVisible, false);
        }
    }

    public bool Exposed()
    {
        // TODO: Implement exposure check
        return GetState(StateFlags.sfExposed);
    }

    // Drawing
    public virtual void Draw()
    {
        // Override in derived classes
    }

    public void DrawView()
    {
        if (Exposed())
        {
            Draw();
        }
    }

    public void DrawHide(TView? lastView)
    {
        // TODO: Implement
    }

    public void DrawShow(TView? lastView)
    {
        // TODO: Implement
    }

    public void DrawUnderRect(TRect r, TView? lastView)
    {
        // TODO: Implement
    }

    public void DrawUnderView(bool doShadow, TView? lastView)
    {
        // TODO: Implement
    }

    // Cursor
    public void SetCursor(int x, int y)
    {
        Cursor = new TPoint(x, y);
    }

    public void ShowCursor()
    {
        SetState(StateFlags.sfCursorVis, true);
    }

    public void HideCursor()
    {
        SetState(StateFlags.sfCursorVis, false);
    }

    public void NormalCursor()
    {
        SetState(StateFlags.sfCursorIns, false);
    }

    public void BlockCursor()
    {
        SetState(StateFlags.sfCursorIns, true);
    }

    public virtual void ResetCursor()
    {
        // TODO: Implement
    }

    public void DrawCursor()
    {
        // TODO: Implement
    }

    // Event handling
    public virtual void HandleEvent(ref TEvent ev)
    {
        if (ev.What == EventConstants.evMouseDown)
        {
            if (!GetState(StateFlags.sfSelected) && (Options & OptionFlags.ofSelectable) != 0)
            {
                Select();
            }
        }
    }

    public virtual void GetEvent(ref TEvent ev)
    {
        Owner?.GetEvent(ref ev);
    }

    public virtual void PutEvent(TEvent ev)
    {
        Owner?.PutEvent(ev);
    }

    public void ClearEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;
    }

    public bool EventAvail()
    {
        TEvent ev = default;
        GetEvent(ref ev);
        if (ev.What != EventConstants.evNothing)
        {
            PutEvent(ev);
            return true;
        }
        return false;
    }

    // Focus and selection
    public void Select()
    {
        if ((Options & OptionFlags.ofTopSelect) != 0)
        {
            MakeFirst();
        }
        else if (Owner != null)
        {
            Owner.SetCurrent(this, SelectMode.normalSelect);
        }
    }

    public bool Focus()
    {
        if (GetState(StateFlags.sfFocused))
        {
            return true;
        }
        if (Owner != null)
        {
            return Owner.Focus() && Owner.SetCurrent(this, SelectMode.normalSelect);
        }
        return false;
    }

    // Z-order
    public void MakeFirst()
    {
        PutInFrontOf(Owner?.First());
    }

    public void PutInFrontOf(TView? target)
    {
        Owner?.InsertView(this, target);
    }

    // Navigation
    public TView? NextView()
    {
        return (this == Owner?.Last) ? null : Next;
    }

    public TView? PrevView()
    {
        return (this == Owner?.First()) ? null : Prev();
    }

    public TView? Prev()
    {
        var p = this;
        while (p?.Next != this)
        {
            p = p?.Next;
        }
        return p;
    }

    public TView? TopView()
    {
        if (Owner != null && !GetState(StateFlags.sfModal))
        {
            return Owner.TopView();
        }
        return this;
    }

    // Coordinate conversion
    public TPoint MakeGlobal(TPoint source)
    {
        var result = source + Origin;
        var p = Owner;
        while (p != null)
        {
            result = result + p.Origin;
            p = p.Owner;
        }
        return result;
    }

    public TPoint MakeLocal(TPoint source)
    {
        var result = source - Origin;
        var p = Owner;
        while (p != null)
        {
            result = result - p.Origin;
            p = p.Owner;
        }
        return result;
    }

    public bool MouseInView(TPoint mouse)
    {
        var local = MakeLocal(mouse);
        return GetExtent().Contains(local);
    }

    public bool MouseEvent(ref TEvent ev, ushort mask)
    {
        do
        {
            GetEvent(ref ev);
        } while ((ev.What & (mask | EventConstants.evMouseUp)) == 0);

        return ev.What != EventConstants.evMouseUp;
    }

    public bool ContainsMouse(TEvent ev)
    {
        return (ev.What & EventConstants.evMouse) != 0 && MouseInView(ev.Mouse.Where);
    }

    // Writing to screen
    public void WriteBuf(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
    {
        // Get the clipping rectangle for this view
        var clip = GetClipRect();
        if (w <= 0 || h <= 0)
        {
            return;
        }

        // Convert local coordinates to global
        var globalOrigin = MakeGlobal(new TPoint(x, y));

        // Clip against the view's clip rectangle
        int srcX = 0;
        int srcY = 0;
        int dstX = globalOrigin.X;
        int dstY = globalOrigin.Y;
        int clipW = w;
        int clipH = h;

        // Left clipping
        if (x < clip.A.X)
        {
            srcX = clip.A.X - x;
            dstX = MakeGlobal(new TPoint(clip.A.X, 0)).X;
            clipW -= srcX;
        }

        // Top clipping
        if (y < clip.A.Y)
        {
            srcY = clip.A.Y - y;
            dstY = MakeGlobal(new TPoint(0, clip.A.Y)).Y;
            clipH -= srcY;
        }

        // Right clipping
        if (x + w > clip.B.X)
        {
            clipW = clip.B.X - x - srcX;
        }

        // Bottom clipping
        if (y + h > clip.B.Y)
        {
            clipH = clip.B.Y - y - srcY;
        }

        if (clipW <= 0 || clipH <= 0)
        {
            return;
        }

        var driver = TScreen.Driver;
        if (driver == null)
        {
            return;
        }

        // Write each clipped line
        for (int row = 0; row < clipH; row++)
        {
            int srcOffset = (srcY + row) * w + srcX;
            if (srcOffset + clipW <= buf.Length)
            {
                driver.WriteBuffer(dstX, dstY + row, clipW, 1, buf.Slice(srcOffset, clipW));
            }
        }
    }

    public void WriteBuf(int x, int y, int w, int h, TDrawBuffer buf)
    {
        WriteBuf(x, y, w, h, buf.Data);
    }

    public void WriteChar(int x, int y, char c, byte color, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var attr = MapColor(color);
        Span<TScreenCell> cells = stackalloc TScreenCell[count];
        for (int i = 0; i < count; i++)
        {
            cells[i] = new TScreenCell(c, attr);
        }
        WriteBuf(x, y, count, 1, cells);
    }

    public void WriteStr(int x, int y, string str, byte color)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        var attr = MapColor(color);
        Span<TScreenCell> cells = stackalloc TScreenCell[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            cells[i] = new TScreenCell(str[i], attr);
        }
        WriteBuf(x, y, str.Length, 1, cells);
    }

    public void WriteLine(int x, int y, int w, int h, TDrawBuffer buf)
    {
        WriteLine(x, y, w, h, buf.Data);
    }

    public void WriteLine(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
    {
        // WriteLine writes the same buffer to multiple lines
        for (int row = 0; row < h; row++)
        {
            WriteBuf(x, y + row, w, 1, buf.Slice(0, Math.Min(w, buf.Length)));
        }
    }

    // Commands
    public static bool CommandEnabled(ushort command)
    {
        return CurCommandSet.Has(command);
    }

    public static void EnableCommand(ushort command)
    {
        CurCommandSet.EnableCmd(command);
        CommandSetChanged = true;
    }

    public static void DisableCommand(ushort command)
    {
        CurCommandSet.DisableCmd(command);
        CommandSetChanged = true;
    }

    public static void EnableCommands(TCommandSet commands)
    {
        CurCommandSet.EnableCmd(commands);
        CommandSetChanged = true;
    }

    public static void DisableCommands(TCommandSet commands)
    {
        CurCommandSet.DisableCmd(commands);
        CommandSetChanged = true;
    }

    public static void SetCommands(TCommandSet commands)
    {
        // TODO: Implement
        CommandSetChanged = true;
    }

    public static void GetCommands(TCommandSet commands)
    {
        // TODO: Implement copy
    }

    // Modal execution
    public virtual ushort Execute()
    {
        return CommandConstants.cmCancel;
    }

    public virtual void EndModal(ushort command)
    {
        if (Owner != null)
        {
            Owner.EndModal(command);
        }
    }

    // Validation
    public virtual bool Valid(ushort command)
    {
        return true;
    }

    // Data exchange
    public virtual int DataSize()
    {
        return 0;
    }

    public virtual void GetData(Span<byte> rec)
    {
    }

    public virtual void SetData(ReadOnlySpan<byte> rec)
    {
    }

    // Activation
    public virtual void Awaken()
    {
    }

    // Help
    public virtual ushort GetHelpCtx()
    {
        if (GetState(StateFlags.sfDragging))
        {
            return HelpContexts.hcDragging;
        }
        return HelpCtx;
    }

    // Colors
    public virtual TPalette? GetPalette()
    {
        return null;
    }

    public virtual TColorAttr MapColor(byte index)
    {
        var p = GetPalette();
        byte color;

        if (p != null && p[0] != 0)
        {
            // If palette has entries and index is within range
            if (index > 0 && index <= (byte)p[0])
            {
                color = (byte)p[index];
            }
            else
            {
                return new TColorAttr(ErrorAttr);
            }
        }
        else
        {
            // No palette - pass through index unchanged
            color = index;
        }

        if (color == 0)
        {
            return new TColorAttr(ErrorAttr);
        }

        // Propagate to owner for further lookup
        if (Owner != null)
        {
            return Owner.MapColor(color);
        }

        return new TColorAttr(color);
    }

    public TAttrPair GetColor(ushort color)
    {
        var lo = (byte)(color & 0xFF);
        var hi = (byte)((color >> 8) & 0xFF);
        return new TAttrPair(MapColor(lo), MapColor(hi));
    }

    // Dragging
    public virtual void DragView(TEvent ev, byte mode, TRect limits, TPoint minSize, TPoint maxSize)
    {
        // TODO: Implement dragging
    }

    public override void ShutDown()
    {
        Hide();
        Owner?.Remove(this);
        Owner = null;
        base.ShutDown();
    }
}
