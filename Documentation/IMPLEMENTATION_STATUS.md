# Implementation Status

This document tracks the porting progress of magiblot/tvision to C# 14 / .NET 10.

**Overall Progress: ~85% of core framework complete**

---

## Critical Gap: View Writing / Buffering Pipeline

The rendering layer has significant architectural differences from the upstream C++ implementation that prevent full parity. The current implementation uses a simplified direct-to-screen approach instead of the hierarchical buffer system used in the original Turbo Vision.

### Current Status: ⚠️ Partial Implementation

| Feature | Upstream | C# Port | Status |
|---------|----------|---------|--------|
| WriteBuf to screen | ✅ | ✅ | Working |
| WriteBuf to parent buffer | ✅ | ❌ | **Missing** |
| Hierarchical buffer propagation | ✅ | ❌ | **Missing** |
| Shadow rendering during write | ✅ | ❌ | **Missing** |
| Clip-aware view occlusion | ✅ | ❌ | **Missing** |
| Lock/Unlock buffer management | ✅ | 🟡 | Partial |
| TGroup buffer allocation | ✅ | ✅ | Working (disabled for TWindow) |

### Workaround Applied

To avoid black dialog rendering, `TWindow` (and by inheritance `TDialog`) currently disables buffering:
```csharp
Options &= unchecked((ushort)~OptionFlags.ofBuffered);
```

This is a **temporary workaround**, not a proper fix. It causes:
- No shadow rendering for windows/dialogs
- Potential flicker during complex redraws
- Suboptimal performance for complex view hierarchies

---

## Upstream WriteBuf Architecture (tvwrite.cpp)

The upstream C++ implementation uses a sophisticated hierarchical writing system in `tvwrite.cpp`. Understanding this is essential for full parity.

### TVWrite Class Structure

```cpp
struct TVWrite {
    short X, Y, Count, wOffset;
    const void *Buffer;
    TView *Target;
    int edx, esi;  // Shadow tracking

    void L0(TView*, short, short, short, const void*);  // Entry point
    void L10(TView*);  // Owner propagation
    void L20(TView*);  // View occlusion check
    void L30(TView*);  // Recursive split
    void L40(TView*);  // Buffer write + propagation
    void L50(TGroup*); // Actual buffer copy

    static TColorAttr applyShadow(TColorAttr attr);
};
```

### Key Functions

#### L0 - Entry Point
```cpp
void TVWrite::L0(TView* dest, short x, short y, short count, const void* b)
{
    // Set up coordinates and buffer
    // Clip against view bounds
    // Call L10 if visible
}
```

#### L10 - Owner Propagation
```cpp
void TVWrite::L10(TView* dest)
{
    TGroup* owner = dest->owner;
    if ((dest->state & sfVisible) && owner)
    {
        // Convert to owner coordinates
        // Clip against owner->clip
        // Start occlusion check at owner->last
        L20(owner->last);
    }
}
```

#### L20 - View Occlusion Check
This is the critical function that handles:
1. **Z-order traversal**: Walks through sibling views in front of the target
2. **Occlusion detection**: Checks if other views cover the write area
3. **Shadow detection**: Tracks when writing through shadow regions (`edx` counter)
4. **Recursive splitting**: Splits write regions around occluding views

```cpp
void TVWrite::L20(TView* dest)
{
    TView* next = dest->next;
    if (next == Target)
        L40(next);  // Reached target, do the write
    else
    {
        // Check if 'next' occludes the write area
        // Handle shadow regions (sfShadow state)
        // Recursively split if partially occluded (L30)
        // Continue to next sibling
        L20(next);
    }
}
```

#### L40 - Buffer Write + Propagation
```cpp
void TVWrite::L40(TView* dest)
{
    TGroup* owner = dest->owner;
    if (owner->buffer)
    {
        L50(owner);  // Write to owner's buffer
    }
    if (owner->lockFlag == 0)
        L10(owner);  // Propagate up to screen
}
```

**Key insight**: When a child view writes, it writes to the parent's buffer AND (if not locked) propagates up the hierarchy to eventually reach the screen.

#### L50 - Buffer Copy with Shadow
```cpp
void TVWrite::L50(TGroup* owner)
{
    TScreenCell* dst = &owner->buffer[Y * owner->size.x + X];
    if (edx == 0)
        memcpy(dst, src, ...);  // Normal copy
    else
        for (i = 0; i < Count - X; ++i)
        {
            auto c = src[i];
            setAttr(c, applyShadow(getAttr(c)));  // Apply shadow
            dst[i] = c;
        }
    if (owner->buffer == TScreen::screenBuffer)
        THardwareInfo::screenWrite(X, Y, dst, Count - X);  // Flush to screen
}
```

### Shadow Implementation

Shadows are NOT drawn explicitly. Instead:
1. Views with `sfShadow` state have an extended collision region (`bounds + shadowSize`)
2. When L20 traverses views, it detects shadow regions and increments `edx`
3. When `edx > 0`, L50 applies `applyShadow()` to darken colors
4. The shadow effect appears because views behind the window get their colors modified

```cpp
static TColorAttr applyShadow(TColorAttr attr)
{
    auto style = getStyle(attr);
    if (!(style & slNoShadow))
    {
        if (getBack(attr).toBIOS(false) != 0)
            attr = shadowAttr;  // Dark gray
        else
            attr = reverseAttribute(shadowAttr);  // Reverse on black
        setStyle(attr, style | slNoShadow);  // Mark as shadowed
    }
    return attr;
}
```

---

## C# Port Requirements for Full Parity

### Phase 1: Hierarchical WriteBuf (Critical)

Rewrite `TView.WriteBuf` to match the upstream `TVWrite` architecture:

```csharp
// Pseudocode for required implementation
public void WriteBuf(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> buf)
{
    var writer = new TVWrite(this, x, y, w, buf);
    writer.Execute();
}

internal class TVWrite
{
    private int X, Y, Count, WOffset;
    private ReadOnlySpan<TScreenCell> Buffer;
    private TView Target;
    private int ShadowDepth;  // Replaces 'edx'

    public void Execute()
    {
        L0(Target, X, Y, Count, Buffer);
    }

    private void L10(TView dest)
    {
        var owner = dest.Owner as TGroup;
        if ((dest.State & StateFlags.sfVisible) != 0 && owner != null)
        {
            // Convert to owner coordinates
            Y += dest.Origin.Y;
            X += dest.Origin.X;
            Count += dest.Origin.X;
            WOffset += dest.Origin.X;

            // Clip against owner's clip rectangle
            if (owner.Clip.A.Y <= Y && Y < owner.Clip.B.Y)
            {
                X = Math.Max(X, owner.Clip.A.X);
                Count = Math.Min(Count, owner.Clip.B.X);
                if (X < Count)
                    L20(owner.Last);
            }
        }
    }

    private void L20(TView dest)
    {
        var next = dest.Next;
        if (next == Target)
            L40(next);
        else
        {
            // Check occlusion and shadow
            // Recursive split for partial occlusion
            // Track shadow depth
            L20(next);
        }
    }

    private void L40(TView dest)
    {
        var owner = dest.Owner as TGroup;
        if (owner?.Buffer != null)
        {
            L50(owner);  // Write to parent buffer
        }
        if (owner != null && owner.LockFlag == 0)
            L10(owner);  // Propagate up
    }

    private void L50(TGroup owner)
    {
        // Copy to owner's buffer with shadow application
        var dstOffset = Y * owner.Size.X + X;
        for (int i = 0; i < Count - X; i++)
        {
            var cell = Buffer[X - WOffset + i];
            if (ShadowDepth > 0)
                cell.Attr = ApplyShadow(cell.Attr);
            owner.Buffer[dstOffset + i] = cell;
        }

        // If this is the screen buffer, flush to driver
        if (owner.Buffer == TScreen.ScreenBuffer)
            TScreen.Driver.WriteBuffer(X, Y, Count - X, 1, ...);
    }
}
```

### Phase 2: TGroup Buffer Management

Update `TGroup` to properly manage buffers:

```csharp
public class TGroup : TView
{
    public TScreenCell[]? Buffer { get; private set; }
    public int LockFlag { get; private set; }

    public void GetBuffer()
    {
        if ((State & StateFlags.sfExposed) != 0 &&
            (Options & OptionFlags.ofBuffered) != 0 &&
            Buffer == null)
        {
            Buffer = new TScreenCell[Size.X * Size.Y];
            // Initialize to prevent garbage
            Array.Fill(Buffer, new TScreenCell(' ', default));
        }
    }

    public void FreeBuffer()
    {
        if ((Options & OptionFlags.ofBuffered) != 0)
            Buffer = null;
    }

    public override void Draw()
    {
        if (Buffer == null)
        {
            GetBuffer();
            if (Buffer != null)
            {
                LockFlag++;
                Redraw();  // Children write to Buffer
                LockFlag--;
            }
        }
        if (Buffer != null)
            WriteBuf(0, 0, Size.X, Size.Y, Buffer);
        else
        {
            var saveClip = Clip;
            Clip = GetClipRect();
            Redraw();
            Clip = saveClip;
        }
    }
}
```

### Phase 3: Shadow Rendering

Implement shadow attribute application:

```csharp
public static class ShadowHelper
{
    public static TColorAttr ApplyShadow(TColorAttr attr)
    {
        // Check if already shadowed (using style flags if available)
        if (attr.Background != 0)
            return new TColorAttr(TView.ShadowAttr);
        else
            return new TColorAttr(ReverseAttribute(TView.ShadowAttr));
    }

    private static byte ReverseAttribute(byte attr)
    {
        return (byte)(((attr & 0x0F) << 4) | ((attr >> 4) & 0x0F));
    }
}
```

### Phase 4: Re-enable Window Buffering

Once the hierarchical WriteBuf is implemented:

```csharp
// Remove this workaround from TWindow constructor:
// Options &= unchecked((ushort)~OptionFlags.ofBuffered);
```

---

## Quick Reference

| Phase | Component | Status | Completion |
|-------|-----------|--------|------------|
| 1 | Core Primitives | ✅ Complete | 100% |
| 2 | Event System | ✅ Complete | 100% |
| 3 | Platform Layer | ✅ Complete | 100% (Windows) |
| 4 | View Hierarchy | ⚠️ Partial | 85% |
| 5 | Application Framework | ✅ Complete | 100% |
| 6 | Dialog Controls | ✅ Complete | 100% |
| 7 | Menu System | ✅ Complete | 100% |
| 8 | Editor Module | ❌ Not Started | 0% |

**Build Status:** ✅ Clean
**Test Status:** ✅ 88 tests passing
**Hello Example:** ⚠️ Functional but missing shadow rendering

---

## Phase 1: Core Primitives ✅ Complete

All core types are fully implemented with comprehensive test coverage.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TPoint | Core/TPoint.cs | ✅ | 2D coordinates with operators |
| TRect | Core/TRect.cs | ✅ | Rectangle geometry (Move, Grow, Intersect, Union, Contains) |
| TColorAttr | Core/TColorAttr.cs | ✅ | Foreground/background colors |
| TScreenCell | Core/TScreenCell.cs | ✅ | Character + attribute pair |
| TAttrPair | Core/TAttrPair.cs | ✅ | Normal/highlight attribute pairs |
| TDrawBuffer | Core/TDrawBuffer.cs | ✅ | MoveBuf, MoveChar, MoveStr, MoveCStr, PutChar |
| TPalette | Core/TPalette.cs | ✅ | Color palette array wrapper |
| TCommandSet | Core/TCommandSet.cs | ✅ | Command enable/disable bitset |
| TStringView | Core/TStringView.cs | ✅ | String utilities |

---

## Phase 2: Event System ✅ Complete

Full event system matching upstream behavior.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TEvent | Core/TEvent.cs | ✅ | Union-like event structure |
| KeyDownEvent | Core/KeyDownEvent.cs | ✅ | Keyboard events with TKey normalization |
| MouseEvent | Core/MouseEvent.cs | ✅ | Mouse position, buttons, wheel |
| MessageEvent | Core/MessageEvent.cs | ✅ | Command messages |
| KeyConstants | Core/KeyConstants.cs | ✅ | kbEnter, kbEsc, kbAltX, etc. |
| CommandConstants | Core/CommandConstants.cs | ✅ | cmQuit, cmClose, cmZoom, etc. |
| EventConstants | Core/EventConstants.cs | ✅ | evKeyDown, evMouseDown, evCommand, etc. |

---

## Phase 3: Platform Layer ✅ Complete (Windows)

Windows Console API fully implemented.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| IScreenDriver | Platform/IScreenDriver.cs | ✅ | Screen rendering interface |
| IEventSource | Platform/IEventSource.cs | ✅ | Input events interface |
| Win32ConsoleDriver | Platform/Win32ConsoleDriver.cs | ✅ | Full P/Invoke implementation |
| TScreen | Platform/TScreen.cs | ✅ | Static screen state |
| TDisplay | Platform/TDisplay.cs | ✅ | Display capabilities |
| TEventQueue | Platform/TEventQueue.cs | ✅ | Event polling |
| THardwareInfo | Platform/THardwareInfo.cs | ✅ | Platform detection |

---

## Phase 4: View Hierarchy ⚠️ Partial (85%)

Core view system functional but missing hierarchical buffer writes.

| Class | File | Status | Gap |
|-------|------|--------|-----|
| TView | Views/TView.cs | ⚠️ | WriteBuf needs hierarchy support |
| TGroup | Views/TGroup.cs | ⚠️ | Buffer writes bypass hierarchy |
| TFrame | Views/TFrame.cs | ✅ | — |
| TScrollBar | Views/TScrollBar.cs | ✅ | — |
| TScroller | Views/TScroller.cs | ✅ | — |
| TListViewer | Views/TListViewer.cs | ✅ | — |
| TBackground | Views/TBackground.cs | ✅ | — |

### Missing Features in TView.WriteBuf

| Feature | Required For | Status |
|---------|--------------|--------|
| Write to parent buffer | Buffered groups | ❌ Missing |
| Propagate up when unlocked | Screen output | ❌ Missing |
| Z-order occlusion check | Overlapping views | ❌ Missing |
| Shadow depth tracking | Shadow rendering | ❌ Missing |
| View intersection/split | Partial occlusion | ❌ Missing |

---

## Phase 5: Application Framework ✅ Complete

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TProgram | Application/TProgram.cs | ✅ | Event loop, command sets |
| TApplication | Application/TApplication.cs | ✅ | Win32 driver init |
| TDeskTop | Application/TDeskTop.cs | ✅ | Window management |
| TDialog | Application/TDialog.cs | ✅ | Modal execution |
| TWindow | Application/TWindow.cs | ⚠️ | Buffering disabled as workaround |

---

## Phase 6: Dialog Controls ✅ Complete

All dialog controls fully functional.

| Class | File | Status |
|-------|------|--------|
| TButton | Dialogs/TButton.cs | ✅ |
| TStaticText | Dialogs/TStaticText.cs | ✅ |
| TLabel | Dialogs/TLabel.cs | ✅ |
| TInputLine | Dialogs/TInputLine.cs | ✅ |
| TCluster | Dialogs/TCluster.cs | ✅ |
| TCheckBoxes | Dialogs/TCheckBoxes.cs | ✅ |
| TRadioButtons | Dialogs/TRadioButtons.cs | ✅ |
| TListBox | Dialogs/TListBox.cs | ✅ |
| THistory | Dialogs/THistory.cs | ✅ |

---

## Phase 7: Menu System ✅ Complete

| Class | File | Status |
|-------|------|--------|
| TMenuItem | Menus/TMenuItem.cs | ✅ |
| TMenu | Menus/TMenu.cs | ✅ |
| TMenuView | Menus/TMenuView.cs | ✅ |
| TMenuBar | Menus/TMenuBar.cs | ✅ |
| TMenuBox | Menus/TMenuBox.cs | ✅ |
| TStatusLine | Menus/TStatusLine.cs | ✅ |

---

## Phase 8: Editor Module ❌ Not Started

| Class | Status | Description |
|-------|--------|-------------|
| TIndicator | ❌ | Line/column display |
| TEditor | ❌ | Core text editing |
| TMemo | ❌ | In-memory editor |
| TFileEditor | ❌ | File-based editor |
| TEditWindow | ❌ | Window wrapper |

---

## Additional Components Not Yet Ported

### File Dialogs
- TFileInputLine, TFileList, TFileInfoPane
- TFileDialog (Open/Save)
- TDirCollection, TDirListBox
- TChDirDialog

### Validators
- TValidator, TPXPictureValidator, TFilterValidator
- TRangeValidator, TLookupValidator

### Collections
- TCollection, TSortedCollection
- TStringCollection, TFileCollection

### Help System
- THelpFile, THelpTopic, THelpViewer

### Utilities
- messageBox(), inputBox() dialogs
- Clipboard integration

---

## Test Coverage

| Category | Tests | Status |
|----------|-------|--------|
| TKey Normalization | 1 | ✅ |
| Endian/Aliasing | 5 | ✅ |
| TRect Geometry | 14 | ✅ |
| TPoint Arithmetic | 8 | ✅ |
| TColorAttr | 10 | ✅ |
| TScreenCell | 5 | ✅ |
| TAttrPair | 3 | ✅ |
| TDrawBuffer | 27 | ✅ |
| TStatusLine | 5 | ✅ |
| TGroup/ExecView | 10 | ✅ |

**Total: 88 tests (all passing)**

---

## Prioritized Next Steps

### Priority 1: Hierarchical WriteBuf (CRITICAL)
1. Implement `TVWrite` class with L0-L50 methods
2. Add Z-order traversal and occlusion detection
3. Implement shadow depth tracking
4. Add recursive region splitting for partial occlusion
5. Update `TView.WriteBuf` to use new implementation

### Priority 2: Shadow Rendering
1. Implement `ApplyShadow()` color transformation
2. Add shadow collision detection in L20
3. Track shadow state to prevent double-shadow
4. Re-enable `sfShadow` state for TWindow

### Priority 3: Re-enable Window Buffering
1. Remove `ofBuffered` disable from TWindow
2. Test buffer population via child writes
3. Verify lock/unlock behavior

### Priority 4: Standard Dialogs
- messageBox(), inputBox()

### Priority 5: Editor Module
- TEditor, TMemo, TFileEditor

### Priority 6: File Dialogs
- TFileDialog, TChDirDialog

### Priority 7: Advanced Features
- Validators, Help system, Collections

### Priority 8: Cross-Platform
- Linux driver (ncurses-based)
- macOS support
