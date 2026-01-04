# Implementation Status

This document tracks the porting progress of magiblot/tvision to C# 14 / .NET 10.

**Overall Progress: ~85% of core framework complete**

> **Note:** The hierarchical WriteBuf/TVWrite system is implemented and working for standard BIOS colors.
> Full upstream parity requires implementing the complete TColorAttr/TColorDesired color model
> which supports RGB, XTerm-256, and default terminal colors.

---

## View Writing / Buffering Pipeline ⚠️ Partial Implementation

The rendering layer implements the upstream hierarchical buffer system via the `TVWrite` class, matching the `tvwrite.cpp` architecture. However, several gaps remain for full parity.

### Current Status: ⚠️ Core Working, Gaps Remain

| Feature | Upstream | C# Port | Status |
|---------|----------|---------|--------|
| WriteBuf to screen | ✅ | ✅ | Working |
| WriteBuf to parent buffer | ✅ | ✅ | Working |
| Hierarchical buffer propagation | ✅ | ✅ | Working |
| Shadow rendering during write | ✅ | ⚠️ | Partial - BIOS colors only |
| Clip-aware view occlusion | ✅ | ✅ | Working |
| Lock/Unlock buffer management | ✅ | ✅ | Working |
| TGroup buffer allocation | ✅ | ✅ | Working |
| TColorAttr full color model | ✅ | ❌ | BIOS only (see gaps) |
| Legacy ushort buffer support | ✅ | ❌ | Intentionally omitted |

### Implementation Details

The `TVWrite` class (`TurboVision/Views/TVWrite.cs`) implements the full hierarchical write system:
- **L0**: Entry point - clips against view bounds, initializes shadow counter
- **L10**: Owner propagation - converts to owner coordinates, clips against owner's clip rect
- **L20**: View occlusion check - Z-order traversal, shadow detection, recursive splitting
- **L30**: Recursive split - saves state, limits region, recurses for partial occlusion
- **L40**: Buffer write + propagation - writes to owner's buffer, propagates up if unlocked
- **L50**: Buffer copy - actual memory copy with shadow application, flushes to screen

### TColorAttr Style Flags

`TColorAttr` now supports style flags including `slNoShadow` (0x200) to prevent double-shadowing of cells.

### Remaining Gaps for Full Parity

#### Gap 1: TColorAttr Data Model (Critical)

**Upstream** (`colors.h` lines 496-524):
```cpp
struct TColorAttr {
    uint64_t
        _style : 10,  // Style flags
        _fg    : 27,  // Foreground (TColorDesired - BIOS/RGB/XTerm/Default)
        _bg    : 27;  // Background (TColorDesired - BIOS/RGB/XTerm/Default)
};
```

**C# Port** (`TColorAttr.cs`):
```csharp
private byte _fg;      // 4-bit BIOS color only
private byte _bg;      // 4-bit BIOS color only
private ushort _style; // Style flags (correct)
```

**Impact**: The C# port only supports 4-bit BIOS colors. Upstream supports:
- BIOS colors (4-bit, indexed 0-15)
- RGB colors (24-bit, 0xRRGGBB)
- XTerm colors (8-bit, 0-255 palette index)
- Default color (terminal default)

**To fix**: Implement `TColorDesired` union type and update `TColorAttr` to use 27-bit fg/bg fields.

#### Gap 2: Shadow ApplyShadow Color Handling

**Upstream** (`tvwrite.cpp` lines 59-84):
```cpp
static TColorAttr applyShadow(TColorAttr attr) noexcept {
    auto style = ::getStyle(attr);
    if (!(style & slNoShadow)) {
        if (::getBack(attr).toBIOS(false) != 0)  // Uses TColorDesired.toBIOS()
            attr = shadowAttr;
        else
            attr = reverseAttribute(shadowAttr);
        ::setStyle(attr, style | slNoShadow);
    }
    return attr;
}
```

**C# Port** (`TVWrite.cs` lines 308-333):
```csharp
if (attr.Background != 0)  // Direct byte comparison, not TColorDesired.toBIOS()
```

**Impact**: Works correctly for BIOS colors, but incorrect behavior for RGB/XTerm colors where Background might be non-zero but would quantize to 0 in BIOS.

#### Gap 3: L20 Shadow Region Logic Structure

**Upstream** uses a `do { } while (0)` idiom for complex branching with shadow region detection. The C# implementation restructures this logic. While functionally similar for common cases, edge cases with:
- Views with `sfShadow` flag
- Overlapping shadow regions from multiple windows
- Shadow regions at exact boundary positions

...may behave differently. Needs comprehensive testing with:
1. Multiple overlapping windows with shadows
2. Windows at screen edges
3. Shadow regions spanning multiple sibling views

#### Gap 4: Missing writeView Overloads

**Upstream** (`tvwrite.cpp` lines 87-97):
```cpp
void TView::writeView(short x, short y, short count, const void _FAR* b);
void TView::writeView(short x, short y, short count, const TScreenCell* b);
```

**C# Port**: Only has `TScreenCell` version.

**Impact**: Low - Legacy ushort buffer format intentionally not supported. All C# code should use `TScreenCell`.

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
| 1 | Core Primitives | ⚠️ Partial | 85% (TColorAttr needs full model) |
| 2 | Event System | ✅ Complete | 100% |
| 3 | Platform Layer | ✅ Complete | 100% (Windows) |
| 4 | View Hierarchy | ⚠️ Mostly Complete | 90% (TVWrite edge cases) |
| 5 | Application Framework | ✅ Complete | 100% |
| 6 | Dialog Controls | ✅ Complete | 100% |
| 7 | Menu System | ✅ Complete | 100% |
| 8 | Editor Module | ❌ Not Started | 0% |

**Build Status:** ✅ Clean
**Test Status:** ✅ 88 tests passing
**Hello Example:** ✅ Functional with BIOS color shadows

### Known Limitations
- **Color System**: Only 4-bit BIOS colors supported (not RGB/XTerm/Default)
- **Shadow Rendering**: Works for BIOS colors; may misbehave with extended colors
- **TVWrite L20**: Edge cases with overlapping shadow regions need testing

---

## Phase 1: Core Primitives ⚠️ Mostly Complete

Core types implemented with test coverage. TColorAttr needs full color model for parity.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TPoint | Core/TPoint.cs | ✅ | 2D coordinates with operators |
| TRect | Core/TRect.cs | ✅ | Rectangle geometry (Move, Grow, Intersect, Union, Contains) |
| TColorAttr | Core/TColorAttr.cs | ⚠️ | BIOS only - needs TColorDesired |
| TColorDesired | — | ❌ | Not implemented (required for full color model) |
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

## Phase 4: View Hierarchy ⚠️ Mostly Complete

Core view system with hierarchical buffer writes and basic shadow support. Color model needs work.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TView | Views/TView.cs | ✅ | WriteBuf hierarchy via TVWrite |
| TGroup | Views/TGroup.cs | ✅ | Buffer management complete |
| TVWrite | Views/TVWrite.cs | ⚠️ | Core working, edge cases untested |
| TFrame | Views/TFrame.cs | ✅ | — |
| TScrollBar | Views/TScrollBar.cs | ✅ | — |
| TScroller | Views/TScroller.cs | ✅ | — |
| TListViewer | Views/TListViewer.cs | ✅ | — |
| TBackground | Views/TBackground.cs | ✅ | — |

### Implemented Features in TView.WriteBuf

| Feature | Required For | Status |
|---------|--------------|--------|
| Write to parent buffer | Buffered groups | ✅ Working |
| Propagate up when unlocked | Screen output | ✅ Working |
| Z-order occlusion check | Overlapping views | ✅ Working |
| Shadow depth tracking | Shadow rendering | ⚠️ Basic working |
| View intersection/split | Partial occlusion | ✅ Working |
| Full color model (RGB/XTerm) | True color support | ❌ Not implemented |

---

## Phase 5: Application Framework ✅ Complete

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TProgram | Application/TProgram.cs | ✅ | Event loop, command sets, screen buffer management |
| TApplication | Application/TApplication.cs | ✅ | Win32 driver init |
| TDeskTop | Application/TDeskTop.cs | ✅ | Window management |
| TDialog | Application/TDialog.cs | ✅ | Modal execution |
| TWindow | Application/TWindow.cs | ✅ | Full buffering with shadow support |

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

### Priority 1: Hierarchical WriteBuf ⚠️ MOSTLY COMPLETE
TVWrite class implements core hierarchical write system. Remaining work:
- [ ] Test L20 shadow region logic with overlapping windows
- [ ] Verify shadow rendering at screen boundaries

### Priority 2: Shadow Rendering ⚠️ PARTIAL
Basic shadow support works with slNoShadow style flag. Remaining work:
- [ ] Full TColorAttr/TColorDesired implementation (Gap 1)
- [ ] Fix ApplyShadow for non-BIOS colors (Gap 2)

### Priority 3: Re-enable Window Buffering ✅ COMPLETE
TWindow now uses full buffering with hierarchical write support.

### Priority 4: TColorAttr Full Color Model (CRITICAL for full parity)
Implement upstream-compatible color system:
- [ ] `TColorDesired` union type (BIOS/RGB/XTerm/Default)
- [ ] Update `TColorAttr` to use 27-bit fg/bg fields
- [ ] Update `ApplyShadow` to use `TColorDesired.toBIOS()`
- [ ] Color conversion functions (RGBtoBIOS, XTermtoBIOS, etc.)

### Priority 5: Standard Dialogs
- messageBox(), inputBox()

### Priority 6: Editor Module
- TEditor, TMemo, TFileEditor

### Priority 7: File Dialogs
- TFileDialog, TChDirDialog

### Priority 8: Advanced Features
- Validators, Help system, Collections

### Priority 9: Cross-Platform
- Linux driver (ncurses-based)
- macOS support
