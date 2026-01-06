using System.Text.Json.Serialization;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Editors;
using TurboVision.Menus;
using TurboVision.Platform;
using TurboVision.Streaming;

namespace TurboVision.Views;

/// <summary>
/// The foundation view class. All visual elements derive from TView.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// Base and Group types
[JsonDerivedType(typeof(TView), "TView")]
[JsonDerivedType(typeof(TGroup), "TGroup")]
[JsonDerivedType(typeof(TFrame), "TFrame")]
[JsonDerivedType(typeof(TWindow), "TWindow")]
[JsonDerivedType(typeof(TDialog), "TDialog")]
// View types
[JsonDerivedType(typeof(TScrollBar), "TScrollBar")]
[JsonDerivedType(typeof(TScroller), "TScroller")]
// TListViewer is abstract - concrete types only
[JsonDerivedType(typeof(TBackground), "TBackground")]
// Dialog controls
[JsonDerivedType(typeof(TButton), "TButton")]
[JsonDerivedType(typeof(TStaticText), "TStaticText")]
[JsonDerivedType(typeof(TParamText), "TParamText")]
[JsonDerivedType(typeof(TLabel), "TLabel")]
[JsonDerivedType(typeof(TInputLine), "TInputLine")]
// TCluster is abstract - concrete types only
[JsonDerivedType(typeof(TCheckBoxes), "TCheckBoxes")]
[JsonDerivedType(typeof(TRadioButtons), "TRadioButtons")]
[JsonDerivedType(typeof(TMultiCheckBoxes), "TMultiCheckBoxes")]
[JsonDerivedType(typeof(TListBox), "TListBox")]
[JsonDerivedType(typeof(TSortedListBox), "TSortedListBox")]
[JsonDerivedType(typeof(THistory), "THistory")]
[JsonDerivedType(typeof(THistoryViewer), "THistoryViewer")]
[JsonDerivedType(typeof(THistoryWindow), "THistoryWindow")]
// File dialog types
[JsonDerivedType(typeof(TFileInputLine), "TFileInputLine")]
[JsonDerivedType(typeof(TFileInfoPane), "TFileInfoPane")]
[JsonDerivedType(typeof(TFileList), "TFileList")]
[JsonDerivedType(typeof(TDirListBox), "TDirListBox")]
[JsonDerivedType(typeof(TFileDialog), "TFileDialog")]
[JsonDerivedType(typeof(TChDirDialog), "TChDirDialog")]
// Menu types
[JsonDerivedType(typeof(TMenuView), "TMenuView")]
[JsonDerivedType(typeof(TMenuBar), "TMenuBar")]
[JsonDerivedType(typeof(TMenuBox), "TMenuBox")]
[JsonDerivedType(typeof(TMenuPopup), "TMenuPopup")]
[JsonDerivedType(typeof(TStatusLine), "TStatusLine")]
// Editor types
[JsonDerivedType(typeof(TEditor), "TEditor")]
[JsonDerivedType(typeof(TMemo), "TMemo")]
[JsonDerivedType(typeof(TFileEditor), "TFileEditor")]
[JsonDerivedType(typeof(TIndicator), "TIndicator")]
[JsonDerivedType(typeof(TEditWindow), "TEditWindow")]
public class TView : TObject, IStreamable
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public const string TypeName = "TView";

    /// <inheritdoc/>
    [JsonIgnore]
    public virtual string StreamableName => TypeName;

    // Phase type for event handling
    public enum PhaseType { phFocused, phPreProcess, phPostProcess }

    // Selection mode
    public enum SelectMode { normalSelect, enterSelect, leaveSelect }

    // Static members
    public static bool CommandSetChanged { get; set; }
    public static TCommandSet CurCommandSet { get; } = InitCommands();
    public static bool ShowMarkers { get; set; }
    public static byte ErrorAttr { get; set; } = 0xCF;
    public static TPoint ShadowSize { get; set; } = new TPoint(2, 1);
    public static byte ShadowAttr { get; set; } = 0x08;

    private static TCommandSet InitCommands()
    {
        var temp = new TCommandSet();
        // Enable all commands by default
        for (int i = 0; i < 256; i++)
        {
            temp.EnableCmd(i);
        }
        // Disable window-specific commands (enabled when windows are present)
        temp.DisableCmd(CommandConstants.cmZoom);
        temp.DisableCmd(CommandConstants.cmClose);
        temp.DisableCmd(CommandConstants.cmResize);
        temp.DisableCmd(CommandConstants.cmNext);
        temp.DisableCmd(CommandConstants.cmPrev);
        return temp;
    }

    // Instance members - Next and Owner are excluded from serialization to avoid circular references.
    // They are reconstructed by ViewHierarchyRebuilder after deserialization.
    [JsonIgnore]
    public TView? Next { get; set; }

    [JsonIgnore]
    public TGroup? Owner { get; set; }

    public TPoint Size { get; set; }
    public TPoint Origin { get; set; }
    public TPoint Cursor { get; set; }
    public ushort Options { get; set; }
    public ushort EventMask { get; set; } = EventConstants.evMouseDown | EventConstants.evKeyDown | EventConstants.evCommand;

    /// <summary>
    /// Backing field for State property.
    /// </summary>
    private ushort _state = StateFlags.sfVisible;

    /// <summary>
    /// Runtime state flags. Use SerializedState for JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ushort State
    {
        get => _state;
        set => _state = value;
    }

    /// <summary>
    /// State flags for serialization. On write, masks out runtime-only flags
    /// (sfActive, sfSelected, sfFocused, sfExposed) to match upstream behavior.
    /// </summary>
    [JsonPropertyName("state")]
    public ushort SerializedState
    {
        get => (ushort)(_state & ~(StateFlags.sfActive | StateFlags.sfSelected | StateFlags.sfFocused | StateFlags.sfExposed));
        set => _state = value;
    }

    public byte GrowMode { get; set; }
    public byte DragMode { get; set; } = DragFlags.dmLimitLoY;
    public ushort HelpCtx { get; set; }

    /// <summary>
    /// Balance values for resize operations. Tracks remainders when views
    /// hit size limits, allowing them to recover original sizes when possible.
    /// Runtime state - not serialized.
    /// </summary>
    [JsonIgnore]
    public TPoint ResizeBalance { get; set; }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TView()
    {
        Cursor = new TPoint(0, 0);
        ResizeBalance = new TPoint(0, 0);
    }

    public TView(TRect bounds)
    {
        Origin = bounds.A;
        Size = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
        Cursor = new TPoint(0, 0);
        ResizeBalance = new TPoint(0, 0);
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
        // If gfFixed is set or no owner, allow unlimited size
        if ((GrowMode & GrowFlags.gfFixed) == 0 && Owner != null)
            max = Owner.Size;
        else
            max = new TPoint(int.MaxValue, int.MaxValue);
    }

    /// <summary>
    /// Constrains a value to a range.
    /// </summary>
    private static int Range(int val, int min, int max)
    {
        if (min > max)
            min = max;
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }

    /// <summary>
    /// Fits val into a range while tracking remainders in balance.
    /// This allows views to recover their original sizes when constraints are relaxed.
    /// </summary>
    private static int BalancedRange(int val, int min, int max, ref int balance)
    {
        if (min > max)
            max = min;
        if (val < min)
        {
            balance += val - min;
            return min;
        }
        else if (val > max)
        {
            balance += val - max;
            return max;
        }
        else
        {
            int offset = Range(val + balance, min, max) - val;
            balance -= offset;
            return val + offset;
        }
    }

    /// <summary>
    /// Adjusts size (a to b) to fit within min/max limits using balanced ranging.
    /// </summary>
    private static void FitToLimits(int a, ref int b, int min, int max, ref int balance)
    {
        b = a + BalancedRange(b - a, min, max, ref balance);
    }

    /// <summary>
    /// Grows or moves a coordinate based on grow mode and size change.
    /// </summary>
    private void Grow(int s, int d, ref int i)
    {
        if ((GrowMode & GrowFlags.gfGrowRel) != 0)
        {
            if (s != d)
                i = (i * s + ((s - d) >> 1)) / (s - d);
        }
        else
        {
            i += d;
        }
    }

    public virtual void CalcBounds(ref TRect bounds, TPoint delta)
    {
        bounds = GetBounds();

        if (Owner == null)
            return;

        int s = Owner.Size.X;
        int d = delta.X;

        int ax = bounds.A.X;
        int bx = bounds.B.X;
        int ay = bounds.A.Y;
        int by = bounds.B.Y;

        if ((GrowMode & GrowFlags.gfGrowLoX) != 0)
            Grow(s, d, ref ax);

        if ((GrowMode & GrowFlags.gfGrowHiX) != 0)
            Grow(s, d, ref bx);

        s = Owner.Size.Y;
        d = delta.Y;

        if ((GrowMode & GrowFlags.gfGrowLoY) != 0)
            Grow(s, d, ref ay);

        if ((GrowMode & GrowFlags.gfGrowHiY) != 0)
            Grow(s, d, ref by);

        SizeLimits(out var minLim, out var maxLim);

        int balanceX = ResizeBalance.X;
        int balanceY = ResizeBalance.Y;

        FitToLimits(ax, ref bx, minLim.X, maxLim.X, ref balanceX);
        FitToLimits(ay, ref by, minLim.Y, maxLim.Y, ref balanceY);

        ResizeBalance = new TPoint(balanceX, balanceY);

        bounds = new TRect(ax, ay, bx, by);
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

        var oldBounds = GetBounds();
        if (!bounds.Equals(oldBounds))
        {
            ChangeBounds(bounds);
            // Redraw views underneath the old position to erase the "ghost"
            if (Owner != null && (State & StateFlags.sfVisible) != 0)
            {
                if ((State & StateFlags.sfShadow) != 0)
                {
                    oldBounds = oldBounds.Union(bounds);
                    oldBounds.B = new TPoint(oldBounds.B.X + ShadowSize.X, oldBounds.B.Y + ShadowSize.Y);
                }
                DrawUnderRect(oldBounds, null);
            }
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

        if (Owner == null)
        {
            return;
        }

        switch (aState)
        {
            case StateFlags.sfVisible:
                // Propagate sfExposed if owner is exposed
                if ((Owner.State & StateFlags.sfExposed) != 0)
                {
                    SetState(StateFlags.sfExposed, enable);
                }
                if (enable)
                {
                    DrawShow(null);
                }
                else
                {
                    DrawHide(null);
                }
                // Reset current selection when a selectable view's visibility changes
                if ((Options & OptionFlags.ofSelectable) != 0)
                {
                    Owner.ResetCurrent();
                }
                break;

            case StateFlags.sfCursorVis:
            case StateFlags.sfCursorIns:
                DrawCursor();
                break;

            case StateFlags.sfShadow:
                DrawUnderView(true, null);
                break;

            case StateFlags.sfFocused:
                ResetCursor();
                // Broadcast cmReceivedFocus/cmReleasedFocus to linked views
                if (Owner != null)
                {
                    var ev = new TEvent
                    {
                        What = EventConstants.evBroadcast
                    };
                    ev.Message.Command = enable ? CommandConstants.cmReceivedFocus : CommandConstants.cmReleasedFocus;
                    ev.Message.InfoPtr = this;
                    Owner.HandleEvent(ref ev);
                }
                break;
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
        return new TVExposed(this).Check();
    }

    /// <summary>
    /// Helper struct for calculating view exposure (visibility).
    /// Port of TVExposd from tvexposd.cpp.
    /// </summary>
    private ref struct TVExposed
    {
        private int _y;        // Current Y coordinate (eax in C++)
        private int _left;     // Left X bound (ebx in C++)
        private int _right;    // Right X bound (ecx in C++)
        private int _temp;     // Temporary bound (esi in C++)
        private TView? _target; // Current target view
        private readonly TView _view;

        public TVExposed(TView view)
        {
            _view = view;
            _y = 0;
            _left = 0;
            _right = 0;
            _temp = 0;
            _target = null;
        }

        public bool Check()
        {
            // L0: Check initial conditions
            if ((_view.State & StateFlags.sfExposed) == 0)
                return false;
            if (_view.Size.X <= 0 || _view.Size.Y <= 0)
                return false;

            // L1: For each row, check if any part is exposed
            for (int row = 0; row < _view.Size.Y; row++)
            {
                _y = row;
                _left = 0;
                _right = _view.Size.X;
                if (!CheckRow(_view))
                    return true;
            }
            return false;
        }

        // L11: Transform coordinates to owner's space and check against clip rect
        private bool CheckRow(TView dest)
        {
            _target = dest;
            _y += dest.Origin.Y;
            _left += dest.Origin.X;
            _right += dest.Origin.X;

            var owner = dest.Owner;
            if (owner == null)
                return false;

            // Check if Y is within owner's clip rect
            if (_y < owner.Clip.A.Y)
                return true;
            if (_y >= owner.Clip.B.Y)
                return true;

            // Clamp X range to owner's clip rect
            if (_left < owner.Clip.A.X)
                _left = owner.Clip.A.X;
            if (_right > owner.Clip.B.X)
                _right = owner.Clip.B.X;

            // L13: Check if range is valid
            if (_left >= _right)
                return true;

            // L20: Start checking siblings from owner's last view
            return CheckSiblings(owner.Last);
        }

        // L10: Check if we can recurse up to owner's owner
        private bool CheckOwner(TView dest)
        {
            var owner = dest.Owner;
            if (owner == null)
                return false;
            if (owner.Buffer != null || owner.LockFlag != 0)
                return false;
            return CheckRow(owner);
        }

        // L20: Walk through siblings checking for occlusion
        private bool CheckSiblings(TView? last)
        {
            if (last == null)
                return true;

            var next = last.Next;
            if (next == null)
                return true;
            if (next == _target)
                return CheckOwner(next);
            return CheckSiblingVisibility(next);
        }

        // L21: Check if a sibling occludes the current range
        private bool CheckSiblingVisibility(TView? next)
        {
            if (next == null)
                return true;

            // Skip invisible siblings
            if ((next.State & StateFlags.sfVisible) == 0)
                return CheckSiblings(next);

            // Check Y overlap
            _temp = next.Origin.Y;
            if (_y < _temp)
                return CheckSiblings(next);
            _temp += next.Size.Y;
            if (_y >= _temp)
                return CheckSiblings(next);

            // Check X overlap - sibling's left edge
            _temp = next.Origin.X;
            if (_left < _temp)
                return CheckPartialOcclusion(next);

            // Full left side covered, check right edge
            _temp += next.Size.X;
            if (_left >= _temp)
                return CheckSiblings(next);

            // Left side is covered by sibling, move left bound to sibling's right
            _left = _temp;
            if (_left < _right)
                return CheckSiblings(next);
            return true;
        }

        // L22: Handle partial occlusion (sibling covers middle of range)
        private bool CheckPartialOcclusion(TView? next)
        {
            if (next == null)
                return true;

            // If right edge is beyond sibling's left, need to check further
            if (_right <= _temp)
                return CheckSiblings(next);

            // Check if sibling's right edge is within our range
            _temp += next.Size.X;
            if (_right > _temp)
                return CheckSplitRange(next);

            // Sibling covers right portion, shrink our range to the left
            _right = next.Origin.X;
            return CheckSiblings(next);
        }

        // L23: Handle split range (sibling in middle creates two regions to check)
        private bool CheckSplitRange(TView? next)
        {
            if (next == null)
                return true;

            // Save state for right portion
            var savedTarget = _target;
            var savedTemp = _temp;
            var savedRight = _right;
            var savedY = _y;

            // Check left portion (from _left to sibling's left edge)
            _right = next.Origin.X;
            bool result = CheckSiblings(next);

            // Restore state and check right portion
            _y = savedY;
            _right = savedRight;
            _left = savedTemp;
            _target = savedTarget;

            if (result)
                return CheckSiblings(next);
            return false;
        }
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
        DrawCursor();
        DrawUnderView((State & StateFlags.sfShadow) != 0, lastView);
    }

    public void DrawShow(TView? lastView)
    {
        DrawView();
        if ((State & StateFlags.sfShadow) != 0)
        {
            DrawUnderView(true, lastView);
        }
    }

    public void DrawUnderRect(TRect r, TView? lastView)
    {
        if (Owner == null)
        {
            return;
        }
        // Temporarily clip to the specified rectangle and redraw views under this one
        var saveClip = Owner.Clip;
        Owner.Clip = Owner.Clip.Intersect(r);
        Owner.DrawSubViews(NextView(), lastView);
        Owner.Clip = saveClip;
    }

    public void DrawUnderView(bool doShadow, TView? lastView)
    {
        var r = GetBounds();
        if (doShadow)
        {
            r.B = new TPoint(r.B.X + ShadowSize.X, r.B.Y + ShadowSize.Y);
        }
        DrawUnderRect(r, lastView);
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
        new TVCursor(this).Reset();
    }

    public void DrawCursor()
    {
        // DrawCursor delegates to ResetCursor which handles all cursor positioning
        ResetCursor();
    }

    /// <summary>
    /// Helper struct for cursor positioning and visibility.
    /// Port of TVCursor from tvcursor.cpp.
    /// </summary>
    private ref struct TVCursor
    {
        private readonly TView _self;
        private int _x;
        private int _y;

        public TVCursor(TView self)
        {
            _self = self;
            _x = self.Cursor.X;
            _y = self.Cursor.Y;
        }

        public void Reset()
        {
            int caretSize = ComputeCaretSize();
            if (caretSize > 0)
            {
                TScreen.Driver?.SetCursorPosition(_x, _y);
            }
            TScreen.Driver?.SetCursorType((ushort)caretSize);
        }

        private int ComputeCaretSize()
        {
            // Check all required flags: sfVisible, sfCursorVis, sfFocused
            // The condition !(~state & (flags)) checks if all flags are set
            ushort requiredFlags = StateFlags.sfVisible | StateFlags.sfCursorVis | StateFlags.sfFocused;
            if ((~_self.State & requiredFlags) != 0)
                return 0;

            var v = _self;
            while (_y >= 0 && _y < v.Size.Y && _x >= 0 && _x < v.Size.X)
            {
                _y += v.Origin.Y;
                _x += v.Origin.X;

                if (v.Owner != null)
                {
                    if ((v.Owner.State & StateFlags.sfVisible) != 0)
                    {
                        if (CaretCovered(v))
                            break;
                        v = v.Owner;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // Reached top of hierarchy - cursor is visible
                    return DecideCaretSize();
                }
            }
            return 0;
        }

        private bool CaretCovered(TView v)
        {
            if (v.Owner?.Last == null)
                return false;

            var u = v.Owner.Last.Next;
            while (u != null && u != v)
            {
                if ((u.State & StateFlags.sfVisible) != 0 &&
                    u.Origin.Y <= _y && _y < u.Origin.Y + u.Size.Y &&
                    u.Origin.X <= _x && _x < u.Origin.X + u.Size.X)
                {
                    return true;
                }
                u = u.Next;
            }
            return false;
        }

        private int DecideCaretSize()
        {
            if ((_self.State & StateFlags.sfCursorIns) != 0)
                return 100; // Block cursor
            return TScreen.CursorLines & 0x0F; // Normal cursor
        }
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

    /// <summary>
    /// Gets an event with a specified timeout. The timeout temporarily overrides
    /// the default event wait timeout in TProgram.
    /// </summary>
    public virtual void GetEvent(ref TEvent ev, int timeoutMs)
    {
        int saveTimeout = Application.TProgram.EventTimeoutMs;
        Application.TProgram.EventTimeoutMs = timeoutMs;

        GetEvent(ref ev);

        Application.TProgram.EventTimeoutMs = saveTimeout;
    }

    /// <summary>
    /// Helper function to extract text from a keyboard event.
    /// Returns true if text was extracted and added to the buffer.
    /// </summary>
    private static bool GetEventText(ref TEvent ev, Span<char> dest, ref int length)
    {
        if (ev.What == EventConstants.evKeyDown)
        {
            ReadOnlySpan<char> text;

            if (ev.KeyDown.TextLength > 0)
            {
                text = ev.KeyDown.GetText();
            }
            else if (ev.KeyDown.KeyCode == KeyConstants.kbEnter)
            {
                text = "\n";
            }
            else if (ev.KeyDown.KeyCode == KeyConstants.kbTab)
            {
                text = "\t";
            }
            else
            {
                return false;
            }

            // Check if we have space in the destination buffer
            var remaining = dest.Slice(length);
            if (!text.IsEmpty && text.Length <= remaining.Length)
            {
                text.CopyTo(remaining);
                length += text.Length;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Fills the destination buffer with text from consecutive keyboard events.
    /// If the passed event is an evKeyDown, its text is also included.
    /// Returns true if any characters were written to the buffer.
    /// On exit, ev.What is evNothing.
    /// </summary>
    public bool TextEvent(ref TEvent ev, Span<char> dest, out int length)
    {
        length = 0;

        // Try to get text from the initial event
        GetEventText(ref ev, dest, ref length);

        // Keep polling for more keyboard events with zero timeout
        do
        {
            GetEvent(ref ev, 0);
        } while (GetEventText(ref ev, dest, ref length));

        // If the last event wasn't consumed, put it back
        if (ev.What != EventConstants.evNothing)
        {
            PutEvent(ev);
        }

        ClearEvent(ref ev);
        return length != 0;
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
        if (Owner != null && target != this && target != NextView() &&
            (target == null || target.Owner == Owner))
        {
            if ((State & StateFlags.sfVisible) == 0)
            {
                // Not visible - simple remove and reinsert
                Owner.RemoveView(this);
                Owner.InsertView(this, target);
            }
            else
            {
                // Visible - need to handle drawing
                var lastView = NextView();
                var p = target;
                while (p != null && p != this)
                {
                    p = p.NextView();
                }
                if (p == null)
                {
                    lastView = target;
                }
                State &= unchecked((ushort)~StateFlags.sfVisible);
                if (lastView == target)
                {
                    DrawHide(lastView);
                }
                Owner.RemoveView(this);
                Owner.InsertView(this, target);
                State |= StateFlags.sfVisible;
                if (lastView != target)
                {
                    DrawShow(lastView);
                }
                if ((Options & OptionFlags.ofSelectable) != 0)
                {
                    Owner.ResetCurrent();
                }
            }
        }
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
    /// <summary>
    /// Writes a single line to the view using the hierarchical write system.
    /// This is the core write function that handles occlusion and shadow rendering.
    /// </summary>
    private void WriteView(int x, int y, int count, ReadOnlySpan<TScreenCell> buf)
    {
        var writer = new TVWrite(buf);
        writer.L0(this, x, y, count);
    }

    /// <summary>
    /// Writes a buffer of cells to the view at the specified position.
    /// Uses hierarchical write system with occlusion detection and shadow support.
    /// </summary>
    public void WriteBuf(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        // Write each line using the hierarchical write system
        for (int row = 0; row < h; row++)
        {
            int srcOffset = row * w;
            if (srcOffset + w <= buf.Length)
            {
                WriteView(x, y + row, w, buf.Slice(srcOffset, w));
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
        WriteView(x, y, count, cells);
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
        WriteView(x, y, str.Length, cells);
    }

    public void WriteLine(int x, int y, int w, int h, TDrawBuffer buf)
    {
        WriteLine(x, y, w, h, buf.Data);
    }

    public void WriteLine(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
    {
        // WriteLine writes the same buffer to multiple lines
        int count = Math.Min(w, buf.Length);
        for (int row = 0; row < h; row++)
        {
            WriteView(x, y + row, count, buf);
        }
    }

    // Commands
    public static bool CommandEnabled(ushort command)
    {
        // Commands > 255 are always enabled (not tracked in the command set)
        return command > 255 || CurCommandSet.Has(command);
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
        CurCommandSet.CopyFrom(commands);
        CommandSetChanged = true;
    }

    public static void GetCommands(TCommandSet commands)
    {
        CurCommandSet.CopyTo(commands);
    }

    /// <summary>
    /// Enables or disables a set of commands based on the enable flag.
    /// This is a convenience method that calls EnableCommands or DisableCommands.
    /// </summary>
    public static void SetCmdState(TCommandSet commands, bool enable)
    {
        if (enable)
            EnableCommands(commands);
        else
            DisableCommands(commands);
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

    /// <summary>
    /// Waits for a key event by polling GetEvent until evKeyDown is received.
    /// </summary>
    public void KeyEvent(ref TEvent ev)
    {
        do
        {
            GetEvent(ref ev);
        } while (ev.What != EventConstants.evKeyDown);
    }

    /// <summary>
    /// Helper method for keyboard-based dragging. Modifies position or size based on mode and shift state.
    /// </summary>
    private static void Change(byte mode, TPoint delta, ref TPoint p, ref TPoint s, ushort ctrlState)
    {
        if ((mode & DragFlags.dmDragMove) != 0 && (ctrlState & KeyConstants.kbShift) == 0)
        {
            p = p + delta;
        }
        else if ((mode & DragFlags.dmDragGrow) != 0 && (ctrlState & KeyConstants.kbShift) != 0)
        {
            s = s + delta;
        }
    }

    /// <summary>
    /// Constrains position and size within limits, then calls Locate.
    /// </summary>
    private void MoveGrow(TPoint p, TPoint s, TRect limits, TPoint minSize, TPoint maxSize, byte mode)
    {
        // Constrain size
        s = new TPoint(
            Math.Min(Math.Max(s.X, minSize.X), maxSize.X),
            Math.Min(Math.Max(s.Y, minSize.Y), maxSize.Y)
        );

        // Constrain position - allow partial visibility
        p = new TPoint(
            Math.Min(Math.Max(p.X, limits.A.X - s.X + 1), limits.B.X - 1),
            Math.Min(Math.Max(p.Y, limits.A.Y - s.Y + 1), limits.B.Y - 1)
        );

        // Apply limit flags
        if ((mode & DragFlags.dmLimitLoX) != 0)
            p = new TPoint(Math.Max(p.X, limits.A.X), p.Y);
        if ((mode & DragFlags.dmLimitLoY) != 0)
            p = new TPoint(p.X, Math.Max(p.Y, limits.A.Y));
        if ((mode & DragFlags.dmLimitHiX) != 0)
            p = new TPoint(Math.Min(p.X, limits.B.X - s.X), p.Y);
        if ((mode & DragFlags.dmLimitHiY) != 0)
            p = new TPoint(p.X, Math.Min(p.Y, limits.B.Y - s.Y));

        var r = new TRect(p.X, p.Y, p.X + s.X, p.Y + s.Y);
        Locate(ref r);
    }

    /// <summary>
    /// Implements drag to move/resize functionality via mouse or keyboard.
    /// </summary>
    public virtual void DragView(TEvent ev, byte mode, TRect limits, TPoint minSize, TPoint maxSize)
    {
        TRect saveBounds;
        TPoint p, s;

        SetState(StateFlags.sfDragging, true);

        if (ev.What == EventConstants.evMouseDown)
        {
            // Mouse-based dragging
            if ((mode & DragFlags.dmDragMove) != 0)
            {
                // Drag to move
                p = Origin - ev.Mouse.Where;
                do
                {
                    var newPos = ev.Mouse.Where + p;
                    MoveGrow(newPos, Size, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, EventConstants.evMouseMove));
            }
            else if ((mode & DragFlags.dmDragGrow) != 0)
            {
                // Drag to resize (bottom-right corner)
                p = Size - ev.Mouse.Where;
                do
                {
                    var newSize = ev.Mouse.Where + p;
                    MoveGrow(Origin, newSize, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, EventConstants.evMouseMove));
            }
            else // dmDragGrowLeft
            {
                // Drag to resize (bottom-left corner)
                var bounds = GetBounds();
                s = Origin;
                s = new TPoint(s.X, s.Y + Size.Y);
                p = s - ev.Mouse.Where;
                do
                {
                    var mousePos = ev.Mouse.Where + p;
                    bounds = new TRect(
                        Math.Min(Math.Max(mousePos.X, bounds.B.X - maxSize.X), bounds.B.X - minSize.X),
                        bounds.A.Y,
                        bounds.B.X,
                        mousePos.Y
                    );
                    var newOrigin = new TPoint(bounds.A.X, bounds.A.Y);
                    var newSize = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
                    MoveGrow(newOrigin, newSize, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, EventConstants.evMouseMove));
            }
        }
        else
        {
            // Keyboard-based dragging
            var goLeft = new TPoint(-1, 0);
            var goRight = new TPoint(1, 0);
            var goUp = new TPoint(0, -1);
            var goDown = new TPoint(0, 1);
            var goCtrlLeft = new TPoint(-8, 0);
            var goCtrlRight = new TPoint(8, 0);
            var goCtrlUp = new TPoint(0, -4);
            var goCtrlDown = new TPoint(0, 4);

            saveBounds = GetBounds();
            do
            {
                p = Origin;
                s = Size;
                KeyEvent(ref ev);
                switch (ev.KeyDown.KeyCode & 0xFF00)
                {
                    case KeyConstants.kbLeft:
                        Change(mode, goLeft, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbRight:
                        Change(mode, goRight, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbUp:
                        Change(mode, goUp, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbDown:
                        Change(mode, goDown, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbCtrlLeft:
                        Change(mode, goCtrlLeft, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbCtrlRight:
                        Change(mode, goCtrlRight, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbCtrlUp:
                        Change(mode, goCtrlUp, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbCtrlDown:
                        Change(mode, goCtrlDown, ref p, ref s, ev.KeyDown.ControlKeyState);
                        break;
                    case KeyConstants.kbHome:
                        p = new TPoint(limits.A.X, p.Y);
                        break;
                    case KeyConstants.kbEnd:
                        p = new TPoint(limits.B.X - s.X, p.Y);
                        break;
                    case KeyConstants.kbPgUp:
                        p = new TPoint(p.X, limits.A.Y);
                        break;
                    case KeyConstants.kbPgDn:
                        p = new TPoint(p.X, limits.B.Y - s.Y);
                        break;
                }
                MoveGrow(p, s, limits, minSize, maxSize, mode);
            } while (ev.KeyDown.KeyCode != KeyConstants.kbEsc && ev.KeyDown.KeyCode != KeyConstants.kbEnter);

            if (ev.KeyDown.KeyCode == KeyConstants.kbEsc)
            {
                Locate(ref saveBounds);
            }
        }

        SetState(StateFlags.sfDragging, false);
    }

    // Timer support
    private static TTimerQueue? _timerQueue;
    internal static TTimerQueue TimerQueue => _timerQueue ??= new TTimerQueue();

    /// <summary>
    /// Sets a timer that fires after timeoutMs milliseconds.
    /// If periodMs is negative (default), it's a one-shot timer.
    /// Returns a TTimerId that can be used to identify the timer in cmTimerExpired events.
    /// </summary>
    public TTimerId SetTimer(int timeoutMs, int periodMs = -1)
    {
        return TimerQueue.SetTimer(timeoutMs, periodMs);
    }

    /// <summary>
    /// Kills a timer by its ID.
    /// </summary>
    public void KillTimer(TTimerId id)
    {
        TimerQueue.KillTimer(id);
    }

    public override void ShutDown()
    {
        Hide();
        Owner?.Remove(this);
        Owner = null;
        base.ShutDown();
    }
}
