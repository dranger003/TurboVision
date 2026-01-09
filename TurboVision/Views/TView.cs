/*------------------------------------------------------------*/
/* filename -       TView.cs                                  */
/*                                                            */
/* function(s)                                                */
/*                  TView member functions                    */
/*------------------------------------------------------------*/
/*
 *      Turbo Vision - C# Port
 *      Port of Turbo Vision 2.0
 *      Copyright (c) 1994 by Borland International
 *      All Rights Reserved.
 *
 *      Upstream: Reference/tvision/include/tvision/views.h (lines 322-530)
 *      Upstream: Reference/tvision/source/tvision/tview.cpp (943 lines)
 */

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
// Outline types
// TOutlineViewer is abstract - concrete types only
[JsonDerivedType(typeof(TOutline), "TOutline")]
public class TView : TObject, IStreamable
{
    // ============================================================================
    // SECTION 1: ENUMS
    // Upstream: views.h lines 344-345
    // ============================================================================

    // Upstream: enum phaseType { phFocused, phPreProcess, phPostProcess }
    public enum PhaseType { phFocused, phPreProcess, phPostProcess }

    // Upstream: enum selectMode { normalSelect, enterSelect, leaveSelect }
    public enum SelectMode { normalSelect, enterSelect, leaveSelect }

    // ============================================================================
    // SECTION 2: PUBLIC INSTANCE DATA MEMBERS
    // Upstream: views.h lines 434, 451-464
    // ============================================================================

    // Upstream: TPoint size (line 451)
    public TPoint Size { get; set; }

    // Upstream: ushort options (line 452)
    public ushort Options { get; set; }

    // Upstream: ushort eventMask (line 453)
    public ushort EventMask { get; set; } = EventConstants.evMouseDown | EventConstants.evKeyDown | EventConstants.evCommand;

    // Upstream: ushort state (line 454)
    // NOTE: C# uses a backing field _state with a property State for runtime access.
    // SerializedState handles JSON serialization with proper flag masking.
    private ushort _state = StateFlags.sfVisible;

    [JsonIgnore]
    public ushort State
    {
        get { return _state; }
        set { _state = value; }
    }

    // Upstream: TPoint origin (line 455)
    public TPoint Origin { get; set; }

    // Upstream: TPoint cursor (line 456)
    public TPoint Cursor { get; set; }

    // Upstream: uchar growMode (line 457)
    public byte GrowMode { get; set; }

    // Upstream: uchar dragMode (line 458)
    public byte DragMode { get; set; } = DragFlags.dmLimitLoY;

    // Upstream: ushort helpCtx (line 459)
    public ushort HelpCtx { get; set; }

    // Upstream: TView *next (line 434)
    // NOTE: Excluded from JSON serialization to avoid circular references.
    // Reconstructed by ViewHierarchyRebuilder after deserialization.
    [JsonIgnore]
    public TView? Next { get; set; }

    // Upstream: TGroup *owner (line 464)
    // NOTE: Excluded from JSON serialization to avoid circular references.
    // Reconstructed by ViewHierarchyRebuilder after deserialization.
    [JsonIgnore]
    public TGroup? Owner { get; set; }

    // ============================================================================
    // SECTION 3: PRIVATE DATA MEMBERS
    // Upstream: views.h line 487
    // ============================================================================

    // Upstream: TPoint resizeBalance (line 487)
    // NOTE: Runtime state - not serialized.
    [JsonIgnore]
    private TPoint ResizeBalance { get; set; }

    // ============================================================================
    // SECTION 4: STATIC DATA MEMBERS
    // Upstream: views.h lines 460-467, tview.cpp lines 34-38
    // ============================================================================

    // Upstream: Boolean _NEAR TView::commandSetChanged (line 460, tview.cpp line 38)
    public static bool CommandSetChanged { get; set; }

    // Upstream: TCommandSet _NEAR TView::curCommandSet (line 462, tview.cpp line 55)
    public static TCommandSet CurCommandSet { get; } = InitCommands();

    // Upstream: Boolean _NEAR TView::showMarkers (line 466, tview.cpp line 36)
    public static bool ShowMarkers { get; set; }

    // Upstream: uchar _NEAR TView::errorAttr (line 467, tview.cpp line 37)
    public static byte ErrorAttr { get; set; } = 0xCF;

    // Upstream: Global variables in tview.cpp (lines 34-35, 41)
    // NOTE: These are global in C++ but static members in C# for better encapsulation.
    public static TPoint ShadowSize { get; set; } = new TPoint(2, 1);
    public static byte ShadowAttr { get; set; } = 0x08;

    // Upstream: extern TView *TheTopView (tview.cpp line 41)
    public static TView? TheTopView { get; set; }

    // ============================================================================
    // SECTION 5: CONSTRUCTORS
    // Upstream: views.h lines 347-348, 494; tview.cpp lines 57-69
    // ============================================================================

    // Upstream: TView::TView(const TRect& bounds) noexcept (tview.cpp lines 57-65)
    public TView(TRect bounds)
    {
        Origin = bounds.A;
        Size = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
        Cursor = new TPoint(0, 0);
        ResizeBalance = new TPoint(0, 0);
    }

    // C# Specific: Parameterless constructor for JSON deserialization
    [JsonConstructor]
    protected TView()
    {
        Cursor = new TPoint(0, 0);
        ResizeBalance = new TPoint(0, 0);
    }

    // ============================================================================
    // SECTION 6: VIRTUAL METHODS
    // Upstream: views.h lines 350-427
    // ============================================================================

    // Upstream: virtual void sizeLimits(TPoint& min, TPoint& max) (tview.cpp lines 827-834)
    public virtual void SizeLimits(out TPoint min, out TPoint max)
    {
        min = new TPoint(0, 0);
        // If gfFixed is set or no owner, allow unlimited size
        if ((GrowMode & GrowFlags.gfFixed) == 0 && Owner != null)
            max = Owner.Size;
        else
            max = new TPoint(int.MaxValue, int.MaxValue);
    }

    // Upstream: virtual void dragView(TEvent& event, uchar mode, TRect& limits, TPoint minSize, TPoint maxSize) (tview.cpp lines 232-358)
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

    // Upstream: virtual void calcBounds(TRect& bounds, TPoint delta) (tview.cpp lines 134-160)
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

    // Upstream: virtual void changeBounds(const TRect& bounds) (tview.cpp lines 162-166)
    public virtual void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        DrawView();
    }

    // Upstream: virtual void draw() (tview.cpp lines 360-366)
    public virtual void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, ' ', MapColor(1), Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    // Upstream: virtual ushort getHelpCtx() (tview.cpp lines 524-529)
    public virtual ushort GetHelpCtx()
    {
        if (GetState(StateFlags.sfDragging))
        {
            return HelpContexts.hcDragging;
        }
        return HelpCtx;
    }

    // Upstream: virtual Boolean valid(ushort command) (tview.cpp lines 890-893)
    public virtual bool Valid(ushort command)
    {
        return true;
    }

    // Upstream: virtual ushort dataSize() (tview.cpp lines 179-182)
    public virtual int DataSize()
    {
        return 0;
    }

    // Upstream: virtual void getData(void *rec) (tview.cpp lines 499-501)
    public virtual void GetData(Span<byte> rec)
    {
    }

    // Upstream: virtual void setData(void *rec) (tview.cpp lines 765-767)
    public virtual void SetData(ReadOnlySpan<byte> rec)
    {
    }

    // Upstream: virtual void awaken() (tview.cpp lines 71-74)
    public virtual void Awaken()
    {
    }

    // Upstream: virtual void resetCursor() (see ResetCursor below)
    public virtual void ResetCursor()
    {
        new TVCursor(this).Reset();
    }

    // Upstream: virtual void getEvent(TEvent& event) (tview.cpp lines 503-507)
    public virtual void GetEvent(ref TEvent ev)
    {
        Owner?.GetEvent(ref ev);
    }

    // Upstream: virtual void handleEvent(TEvent& event) (tview.cpp lines 549-557)
    public virtual void HandleEvent(ref TEvent ev)
    {
        if (ev.What == EventConstants.evMouseDown)
        {
            if (!GetState(StateFlags.sfSelected | StateFlags.sfDisabled) && (Options & OptionFlags.ofSelectable) != 0)
            {
                if (!Focus() || (Options & OptionFlags.ofFirstClick) == 0)
                {
                    ClearEvent(ref ev);
                }
            }
        }
    }

    // Upstream: virtual void putEvent(TEvent& event) (tview.cpp lines 685-689)
    public virtual void PutEvent(TEvent ev)
    {
        Owner?.PutEvent(ev);
    }

    // Upstream: virtual ushort execute() (tview.cpp lines 445-448)
    public virtual ushort Execute()
    {
        return CommandConstants.cmCancel;
    }

    // Upstream: virtual void endModal(ushort command) (tview.cpp lines 425-429)
    public virtual void EndModal(ushort command)
    {
        if (Owner != null)
        {
            Owner.EndModal(command);
        }
    }

    // Upstream: virtual TPalette& getPalette() const (tview.cpp lines 531-536)
    public virtual TPalette? GetPalette()
    {
        return null;
    }

    // Upstream: virtual TColorAttr mapColor(uchar) noexcept (tview.cpp lines 482-492 + logic)
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

    // Upstream: virtual void setState(ushort aState, Boolean enable) (tview.cpp lines 769-807)
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

    // Upstream: virtual TTimerId setTimer(uint timeoutMs, int periodMs = -1) (tview.cpp lines 809-814)
    public virtual TTimerId SetTimer(int timeoutMs, int periodMs = -1)
    {
        return TimerQueue.SetTimer(timeoutMs, periodMs);
    }

    // Upstream: virtual void killTimer(TTimerId id) (tview.cpp lines 577-581)
    public virtual void KillTimer(TTimerId id)
    {
        TimerQueue.KillTimer(id);
    }

    // ============================================================================
    // SECTION 7: NON-VIRTUAL PUBLIC METHODS
    // Upstream: views.h lines 351-450
    // ============================================================================

    // Upstream: TRect getBounds() const noexcept (tview.cpp lines 440-443)
    public TRect GetBounds()
    {
        return new TRect(Origin, Origin + Size);
    }

    // Upstream: TRect getExtent() const noexcept (tview.cpp lines 519-522)
    public TRect GetExtent()
    {
        return new TRect(0, 0, Size.X, Size.Y);
    }

    // Upstream: TRect getClipRect() const noexcept (tview.cpp lines 473-480)
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

    // Upstream: Boolean mouseInView(TPoint mouse) noexcept (tview.cpp lines 643-648)
    public bool MouseInView(TPoint mouse)
    {
        var local = MakeLocal(mouse);
        return GetExtent().Contains(local);
    }

    // Upstream: Boolean containsMouse(TEvent& event) noexcept (tview.cpp lines 895-900)
    public bool ContainsMouse(TEvent ev)
    {
        return (State & StateFlags.sfVisible) != 0 && MouseInView(ev.Mouse.Where);
    }

    // Upstream: void locate(TRect& bounds) (tview.cpp lines 583-603)
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

    // Upstream: void growTo(short x, short y) (tview.cpp lines 543-547)
    public void GrowTo(int x, int y)
    {
        var r = GetBounds();
        r.B = new TPoint(r.A.X + x, r.A.Y + y);
        Locate(ref r);
    }

    // Upstream: void moveTo(short x, short y) (tview.cpp lines 650-654)
    public void MoveTo(int x, int y)
    {
        var r = GetBounds();
        r.Move(x - Origin.X, y - Origin.Y);
        Locate(ref r);
    }

    // Upstream: void setBounds(const TRect& bounds) noexcept (tview.cpp lines 737-741)
    public void SetBounds(TRect bounds)
    {
        Origin = bounds.A;
        Size = new TPoint(bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y);
    }

    // Upstream: void hide() (tview.cpp lines 559-563)
    public void Hide()
    {
        if (GetState(StateFlags.sfVisible))
        {
            SetState(StateFlags.sfVisible, false);
        }
    }

    // Upstream: void show() (tview.cpp lines 816-820)
    public void Show()
    {
        if (!GetState(StateFlags.sfVisible))
        {
            SetState(StateFlags.sfVisible, true);
        }
    }

    // Upstream: void drawView() noexcept (tview.cpp lines 402-409)
    public void DrawView()
    {
        if (Exposed())
        {
            Draw();
            DrawCursor();
        }
    }

    // Upstream: Boolean exposed() noexcept (uses TVExposed helper)
    public bool Exposed()
    {
        return new TVExposed(this).Check();
    }

    // Upstream: Boolean focus() (tview.cpp lines 450-471)
    public bool Focus()
    {
        bool result = true;

        if (!GetState(StateFlags.sfSelected | StateFlags.sfModal))
        {
            if (Owner != null)
            {
                result = Owner.Focus();
                if (result)
                {
                    if (Owner.Current == null ||
                        (Owner.Current.Options & OptionFlags.ofValidate) == 0 ||
                        Owner.Current.Valid(CommandConstants.cmReleasedFocus))
                    {
                        Select();
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        return result;
    }

    // Upstream: void hideCursor() (tview.cpp lines 565-568)
    public void HideCursor()
    {
        SetState(StateFlags.sfCursorVis, false);
    }

    // Upstream: void drawHide(TView *lastView) (tview.cpp lines 374-378)
    public void DrawHide(TView? lastView)
    {
        DrawCursor();
        DrawUnderView((State & StateFlags.sfShadow) != 0, lastView);
    }

    // Upstream: void drawShow(TView *lastView) (tview.cpp lines 380-385)
    public void DrawShow(TView? lastView)
    {
        DrawView();
        if ((State & StateFlags.sfShadow) != 0)
        {
            DrawUnderView(true, lastView);
        }
    }

    // Upstream: void drawUnderRect(TRect& r, TView *lastView) (tview.cpp lines 387-392)
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

    // Upstream: void drawUnderView(Boolean doShadow, TView *lastView) (tview.cpp lines 394-400)
    public void DrawUnderView(bool doShadow, TView? lastView)
    {
        var r = GetBounds();
        if (doShadow)
        {
            r.B = new TPoint(r.B.X + ShadowSize.X, r.B.Y + ShadowSize.Y);
        }
        DrawUnderRect(r, lastView);
    }

    // Upstream: void blockCursor() (tview.cpp lines 76-79)
    public void BlockCursor()
    {
        SetState(StateFlags.sfCursorIns, true);
    }

    // Upstream: void normalCursor() (tview.cpp lines 664-667)
    public void NormalCursor()
    {
        SetState(StateFlags.sfCursorIns, false);
    }

    // Upstream: void setCursor(int x, int y) noexcept (tview.cpp lines 758-763)
    public void SetCursor(int x, int y)
    {
        Cursor = new TPoint(x, y);
        DrawCursor();
    }

    // Upstream: void showCursor() (tview.cpp lines 822-825)
    public void ShowCursor()
    {
        SetState(StateFlags.sfCursorVis, true);
    }

    // Upstream: void drawCursor() noexcept (tview.cpp lines 368-372)
    public void DrawCursor()
    {
        if ((State & StateFlags.sfFocused) != 0)
        {
            ResetCursor();
        }
    }

    // Upstream: void clearEvent(TEvent& event) noexcept (tview.cpp lines 168-172)
    public void ClearEvent(ref TEvent ev)
    {
        ev.What = EventConstants.evNothing;
        ev.Message.InfoPtr = this;
    }

    // Upstream: Boolean eventAvail() (tview.cpp lines 431-438)
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

    // Upstream: void getEvent(TEvent& event, int timeoutMs) (tview.cpp lines 509-517)
    public virtual void GetEvent(ref TEvent ev, int timeoutMs)
    {
        int saveTimeout = Application.TProgram.EventTimeoutMs;
        Application.TProgram.EventTimeoutMs = timeoutMs;

        GetEvent(ref ev);

        Application.TProgram.EventTimeoutMs = saveTimeout;
    }

    // Upstream: void keyEvent(TEvent& event) (tview.cpp lines 570-575)
    public void KeyEvent(ref TEvent ev)
    {
        do
        {
            GetEvent(ref ev);
        } while (ev.What != EventConstants.evKeyDown);
    }

    // Upstream: Boolean mouseEvent(TEvent& event, ushort mask) (tview.cpp lines 634-641)
    public bool MouseEvent(ref TEvent ev, ushort mask)
    {
        do
        {
            GetEvent(ref ev);
        } while ((ev.What & (mask | EventConstants.evMouseUp)) == 0);

        return ev.What != EventConstants.evMouseUp;
    }

    // Upstream: Boolean textEvent(TEvent &event, TSpan<char> dest, size_t &length) (tview.cpp lines 855-875)
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

    // Upstream: TPoint makeGlobal(TPoint source) noexcept (tview.cpp lines 610-620)
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

    // Upstream: TPoint makeLocal(TPoint source) noexcept (tview.cpp lines 622-632)
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

    // Upstream: TView *nextView() noexcept (tview.cpp lines 656-662)
    public TView? NextView()
    {
        return (this == Owner?.Last) ? null : Next;
    }

    // Upstream: TView *prevView() noexcept (tview.cpp lines 677-683)
    public TView? PrevView()
    {
        return (this == Owner?.First()) ? null : Prev();
    }

    // Upstream: TView *prev() noexcept (tview.cpp lines 669-675)
    public TView? Prev()
    {
        var p = this;
        while (p?.Next != this)
        {
            p = p?.Next;
        }
        return p;
    }

    // Upstream: TView *TopView() noexcept (tview.cpp lines 877-888)
    public TView? TopView()
    {
        if (TheTopView != null)
        {
            return TheTopView;
        }
        else
        {
            TView? p = this;
            while (p != null && (p.State & StateFlags.sfModal) == 0)
            {
                p = p.Owner;
            }
            return p;
        }
    }

    // Upstream: void makeFirst() (tview.cpp lines 605-608)
    public void MakeFirst()
    {
        PutInFrontOf(Owner?.First());
    }

    // Upstream: void putInFrontOf(TView *Target) (tview.cpp lines 691-724)
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

    // Upstream: void writeBuf(short x, short y, short w, short h, const void _FAR* b) noexcept
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

    // Upstream: void writeBuf(short x, short y, short w, short h, const TDrawBuffer& b) noexcept
    public void WriteBuf(int x, int y, int w, int h, TDrawBuffer buf)
    {
        WriteBuf(x, y, w, h, buf.Data);
    }

    // Upstream: void writeChar(short x, short y, char c, uchar color, short count) noexcept
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

    // Upstream: void writeLine(short x, short y, short w, short h, const TDrawBuffer& b) noexcept
    public void WriteLine(int x, int y, int w, int h, TDrawBuffer buf)
    {
        WriteLine(x, y, w, h, buf.Data);
    }

    // Upstream: void writeLine(short x, short y, short w, short h, const void _FAR *b) noexcept
    public void WriteLine(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
    {
        // WriteLine writes the same buffer to multiple lines
        int count = Math.Min(w, buf.Length);
        for (int row = 0; row < h; row++)
        {
            WriteView(x, y + row, count, buf);
        }
    }

    // Upstream: void writeStr(short x, short y, const char *str, uchar color) noexcept
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

    // Upstream: TAttrPair getColor(ushort color) noexcept
    public TAttrPair GetColor(ushort color)
    {
        var lo = (byte)(color & 0xFF);
        var hi = (byte)((color >> 8) & 0xFF);
        return new TAttrPair(MapColor(lo), MapColor(hi));
    }

    // Upstream: void select() (tview.cpp lines 726-735)
    public void Select()
    {
        if ((Options & OptionFlags.ofSelectable) != 0 && Owner != null)
        {
            if ((Options & OptionFlags.ofTopSelect) != 0)
            {
                MakeFirst();
            }
            else
            {
                Owner.SetCurrent(this, SelectMode.normalSelect);
            }
        }
    }

    // Upstream: Boolean getState(ushort aState) const noexcept (tview.cpp lines 538-541)
    public bool GetState(ushort aState)
    {
        return (State & aState) == aState;
    }

    // ============================================================================
    // SECTION 8: STATIC COMMAND METHODS
    // Upstream: views.h lines 400-407
    // ============================================================================

    // Upstream: static Boolean commandEnabled(ushort command) noexcept (tview.cpp lines 174-177)
    public static bool CommandEnabled(ushort command)
    {
        // Commands > 255 are always enabled (not tracked in the command set)
        return command > 255 || CurCommandSet.Has(command);
    }

    // Upstream: static void disableCommands(TCommandSet& commands) noexcept (tview.cpp lines 184-189)
    public static void DisableCommands(TCommandSet commands)
    {
        // Only set CommandSetChanged if disabling commands that aren't all already disabled
        // This matches upstream behavior
        if (CurCommandSet.ContainsAny(commands))
        {
            CommandSetChanged = true;
        }
        CurCommandSet.DisableCmd(commands);
    }

    // Upstream: static void enableCommands(TCommandSet& commands) noexcept (tview.cpp lines 411-416)
    public static void EnableCommands(TCommandSet commands)
    {
        // Only set CommandSetChanged if enabling commands that aren't all already enabled
        // This matches upstream behavior
        if (!CurCommandSet.ContainsAll(commands))
        {
            CommandSetChanged = true;
        }
        CurCommandSet.EnableCmd(commands);
    }

    // Upstream: static void disableCommand(ushort command) noexcept (tview.cpp lines 191-196)
    public static void DisableCommand(ushort command)
    {
        // Only set CommandSetChanged if the command is currently enabled (will actually change)
        // This matches upstream: commandSetChanged = commandSetChanged || curCommandSet.has(command)
        if (CurCommandSet.Has(command))
        {
            CommandSetChanged = true;
        }
        CurCommandSet.DisableCmd(command);
    }

    // Upstream: static void enableCommand(ushort command) noexcept (tview.cpp lines 418-423)
    public static void EnableCommand(ushort command)
    {
        // Only set CommandSetChanged if the command is currently disabled (will actually change)
        // This matches upstream: commandSetChanged = commandSetChanged || !curCommandSet.has(command)
        if (!CurCommandSet.Has(command))
        {
            CommandSetChanged = true;
        }
        CurCommandSet.EnableCmd(command);
    }

    // Upstream: static void getCommands(TCommandSet& commands) noexcept (tview.cpp lines 494-497)
    public static void GetCommands(TCommandSet commands)
    {
        CurCommandSet.CopyTo(commands);
    }

    // Upstream: static void setCommands(TCommandSet& commands) noexcept (tview.cpp lines 751-756)
    public static void SetCommands(TCommandSet commands)
    {
        // Only set CommandSetChanged if the command set actually changes
        if (!CurCommandSet.Equals(commands))
        {
            CommandSetChanged = true;
        }
        CurCommandSet.CopyFrom(commands);
    }

    // Upstream: static void setCmdState(TCommandSet& commands, Boolean enable) noexcept (tview.cpp lines 743-749)
    public static void SetCmdState(TCommandSet commands, bool enable)
    {
        if (enable)
            EnableCommands(commands);
        else
            DisableCommands(commands);
    }

    // ============================================================================
    // SECTION 9: SHUTDOWN
    // Upstream: views.h line 469, tview.cpp lines 902-908
    // ============================================================================

    // Upstream: virtual void shutDown() (tview.cpp lines 902-908)
    public override void ShutDown()
    {
        Hide();
        Owner?.Remove(this);
        Owner = null;
        base.ShutDown();
    }

    // ============================================================================
    // SECTION 10: PRIVATE METHODS
    // Upstream: views.h lines 473-485
    // ============================================================================

    // Upstream: void moveGrow(TPoint p, TPoint s, TRect& limits, TPoint minSize, TPoint maxSize, uchar mode) (tview.cpp lines 198-222)
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

    // Upstream: void change(uchar mode, TPoint delta, TPoint& p, TPoint& s, ushort ctrlState) noexcept (tview.cpp lines 224-230)
    private static void Change(byte mode, TPoint delta, ref TPoint p, ref TPoint s, ushort ctrlState)
    {
        // Check for either left or right shift key
        const ushort shiftMask = KeyConstants.kbRightShift | KeyConstants.kbLeftShift;
        bool shiftPressed = (ctrlState & shiftMask) != 0;

        if ((mode & DragFlags.dmDragMove) != 0 && !shiftPressed)
        {
            p = p + delta;
        }
        else if ((mode & DragFlags.dmDragGrow) != 0 && shiftPressed)
        {
            s = s + delta;
        }
    }

    // Upstream: static void writeView(write_args) and void writeView(...) (private overloads)
    private void WriteView(int x, int y, int count, ReadOnlySpan<TScreenCell> buf)
    {
        var writer = new TVWrite(buf);
        writer.L0(this, x, y, count);
    }

    // ============================================================================
    // SECTION 11: PRIVATE HELPER FUNCTIONS (C# SPECIFIC)
    // Not in upstream - C# implementations of inline helper functions
    // ============================================================================

    // Upstream: static inline int range(int val, int min, int max) (tview.cpp lines 81-90)
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

    // Upstream: static int balancedRange(int val, int min, int max, int &balance) noexcept (tview.cpp lines 92-108)
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

    // Upstream: static void fitToLimits(int a, int& b, int min, int max, int &balance) (tview.cpp lines 110-113)
    private static void FitToLimits(int a, ref int b, int min, int max, ref int balance)
    {
        b = a + BalancedRange(b - a, min, max, ref balance);
    }

    // Upstream: inline grow() helper (tview.cpp lines 115-132)
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

    // Upstream: static bool getEventText(TEvent &ev, TSpan<char> dest, size_t &length) (tview.cpp lines 836-853)
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

    // ============================================================================
    // SECTION 12: STREAMING
    // Upstream: views.h lines 489-504, tview.cpp lines 911-943
    // ============================================================================

    // Upstream: static const char * const _NEAR name (line 498)
    public const string TypeName = "TView";

    // Upstream: virtual const char *streamableName() const (line 489-490)
    [JsonIgnore]
    public virtual string StreamableName
    {
        get { return TypeName; }
    }

    // ============================================================================
    // SECTION 13: C# SERIALIZATION SUPPORT
    // C# Specific - JSON serialization extensions not in upstream
    // ============================================================================

    // C# Specific: State property for JSON serialization with runtime flag masking
    // Upstream behavior: tview.cpp lines 913-921 masks out sfActive | sfSelected | sfFocused | sfExposed
    [JsonPropertyName("state")]
    public ushort SerializedState
    {
        get { return (ushort)(_state & ~(StateFlags.sfActive | StateFlags.sfSelected | StateFlags.sfFocused | StateFlags.sfExposed)); }
        set { _state = value; }
    }

    // C# Specific: Static initializer method for CurCommandSet
    // Upstream: tview.cpp lines 42-53 (initCommands function)
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

    // C# Specific: Timer queue support (not in upstream)
    private static TTimerQueue? _timerQueue;
    internal static TTimerQueue TimerQueue
    {
        get { return _timerQueue ??= new TTimerQueue(); }
    }
}
