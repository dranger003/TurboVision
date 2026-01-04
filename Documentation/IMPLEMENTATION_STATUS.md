# Implementation Status

This document tracks the porting progress of magiblot/tvision to C# 14 / .NET 10.

**Overall Progress: ~85% of core framework complete**

> **Note:** The hierarchical WriteBuf/TVWrite system is implemented with the full TColorAttr/TColorDesired color model.
> However, critical bugs remain in shadow rendering and event handling that prevent full upstream parity.

---

## CRITICAL BUGS (User Reported)

The following bugs were reported during testing and need to be fixed for full parity:

### Bug 1: Shadow Missing Bottom Part (Menu/Dialog)
**Severity:** High
**Symptoms:** Menu shadows and dialog window shadows are missing the bottom portion.
**Root Cause:** Logic flaw in `TVWrite.L20()` - the bottom shadow region handling at lines 161-173 incorrectly uses `goto L20End` before checking if we're in the shadow region and incrementing `_shadowDepth`.

**Analysis:**
In upstream `tvwrite.cpp` lines 168-178, after handling the bottom shadow left edge split, the code falls through to line 180 which checks `if (X < esi)` and increments `edx` (shadow depth). In the C# version, lines 167-173 always jump to `L20End` via `goto`, skipping the shadow depth increment logic at lines 180-191.

**Fix Required:** Restructure L20 logic so that bottom shadow regions properly increment `_shadowDepth`.

---

### Bug 2: Button Stays Pressed / Command Not Triggered (Spacebar)
**Severity:** High
**Symptoms:** When pressing spacebar on a focused button, it stays visually pressed until Tab moves focus, and the command event is never triggered.
**Root Cause:** Timer comparison issue in `TButton.HandleEvent()`.

**Analysis:**
In `TButton.cs` line 287:
```csharp
if (_animationTimer != default && Equals(ev.Message.InfoPtr, (nint)_animationTimer))
```
The `ev.Message.InfoPtr` is an `object`, but `(nint)_animationTimer` creates a boxed `IntPtr`. The `Equals` call will fail because it compares boxed value types by reference, not value.

In upstream `tbutton.cpp` line 264:
```cpp
if( animationTimer != 0 && event.message.infoPtr == animationTimer )
```
This is a direct pointer comparison that works correctly.

**Fix Required:** Change the timer comparison to properly extract and compare the timer ID values.

---

### Bug 3: Dialog Labels Not Visible
**Severity:** High
**Symptoms:** TLabel controls in dialogs are not visible (wrong color or not rendered).
**Suspected Cause:** Palette cascade issue - colors may not be properly resolving through the dialog/window palette chain.

**Analysis:**
The TLabel palette `[0x07, 0x08, 0x09, 0x09]` matches upstream `"\x07\x08\x09\x09"`. The issue is likely in:
1. How `GetColor()` resolves palette indices through the owner chain
2. The dialog/window base palette not matching upstream
3. Possible issue with `TAttrPair` handling in `MoveCStr()`

**Fix Required:** Debug palette cascade to verify colors are being resolved correctly from TLabel -> TDialog -> TApplication.

---

### Bug 4: Dialog Frame Close Button Wrong Color
**Severity:** Medium
**Symptoms:** Close button icon `[■]` should be green but appears white.
**Suspected Cause:** Palette mapping issue in `TFrame.Draw()`.

**Analysis:**
In `TFrame.Draw()`, the close icon is drawn using:
```csharp
b.MoveCStr(2, CloseIcon, cFrame);
```
The `cFrame` color comes from `GetColor(0x0503)` when active. The close button icon in upstream uses special character highlighting that may require different color handling.

**Fix Required:** Verify the window palette and frame color indices match upstream values.

---

### Bug 5: Window Content Disappears on Titlebar Click
**Severity:** High
**Symptoms:** When clicking the window titlebar, the window background appears to get redrawn over the child views, making the window appear "empty". Tabbing makes buttons reappear one by one.
**Root Cause:** Buffer management issue during state changes.

**Analysis:**
When clicking causes state changes (sfActive, sfSelected), `TGroup.SetState()` calls `Lock()/Unlock()` and propagates to children. The issue may be:
1. Buffer being cleared/reallocated during state change
2. Draw order issue where parent draws after children
3. `Lock()` condition at line 399 may not properly prevent intermediate draws

**Fix Required:** Review `TGroup.SetState()` and buffer management during focus changes.

---

### Bug 6: Strong Flashing During Window Drag
**Severity:** Medium
**Symptoms:** Significant visual flashing when dragging a dialog window.
**Root Cause:** Improper locking/buffering during drag operations.

**Analysis:**
During drag operations, `sfDragging` state is set which triggers redraws. The window should be properly locked to prevent multiple screen updates. The issue may be in:
1. `TView.DragView()` not properly locking the parent during drag
2. Each mouse move event causing a full redraw instead of batched updates
3. Buffer propagation happening too frequently

**Fix Required:** Ensure proper locking during drag operations and batch screen updates.

---

## View Writing / Buffering Pipeline ⚠️ Partial Implementation

The rendering layer implements the upstream hierarchical buffer system via the `TVWrite` class, matching the `tvwrite.cpp` architecture. However, critical bugs remain.

### Current Status: ⚠️ Core Working, Critical Bugs

| Feature | Upstream | C# Port | Status |
|---------|----------|---------|--------|
| WriteBuf to screen | ✅ | ✅ | Working |
| WriteBuf to parent buffer | ✅ | ✅ | Working |
| Hierarchical buffer propagation | ✅ | ✅ | Working |
| Shadow rendering (right side) | ✅ | ✅ | Working |
| Shadow rendering (bottom) | ✅ | ❌ | **BUG** - Missing (see Bug 1) |
| Clip-aware view occlusion | ✅ | ✅ | Working |
| Lock/Unlock buffer management | ✅ | ⚠️ | Partial (see Bug 5, 6) |
| TGroup buffer allocation | ✅ | ✅ | Working |
| TColorAttr full color model | ✅ | ✅ | Working |
| Legacy ushort buffer support | ✅ | ❌ | Intentionally omitted |

### Implementation Details

The `TVWrite` class (`TurboVision/Views/TVWrite.cs`) implements the full hierarchical write system:
- **L0**: Entry point - clips against view bounds, initializes shadow counter
- **L10**: Owner propagation - converts to owner coordinates, clips against owner's clip rect
- **L20**: View occlusion check - Z-order traversal, shadow detection, recursive splitting (**HAS BUG**)
- **L30**: Recursive split - saves state, limits region, recurses for partial occlusion
- **L40**: Buffer write + propagation - writes to owner's buffer, propagates up if unlocked
- **L50**: Buffer copy - actual memory copy with shadow application, flushes to screen

### L20 Shadow Bug Details

**Upstream code flow (tvwrite.cpp lines 168-188):**
```cpp
else if ((next->state & sfShadow) && Y < esi + shadowSize.y)
{
    esi = next->origin.x + shadowSize.x;
    if (X < esi)
    {
        if (Count > esi)
            L30(next);
        else break;  // breaks to L190
    }
    esi += next->size.x;  // <-- Always executed if X >= esi
}
// Falls through to line 180:
if (X < esi)
{
    edx++;  // <-- Shadow depth increment for bottom shadow
    if (Count > esi)
    {
        L30(next);
        edx--;
    }
}
```

**C# code flow (TVWrite.cs lines 161-191):**
```csharp
else if ((next.State & StateFlags.sfShadow) != 0 &&
         _y < _tempPos + TView.ShadowSize.Y)
{
    _tempPos = next.Origin.X + TView.ShadowSize.X;
    if (_x < _tempPos)
    {
        if (_count > _tempPos)
            L30(next);
        goto L20End;  // <-- BUG: Jumps over shadow depth logic!
    }
    _tempPos += next.Size.X;
}
else
{
    goto L20End;  // Another exit
}

// Lines 180-191 only reached from Y < _tempPos branch, not bottom shadow
if (_x < _tempPos)
{
    _shadowDepth++;
    ...
}
```

The C# `goto L20End` at line 171 bypasses the shadow depth increment that should occur for bottom shadows.

---

## Priority Status Summary

### Priority 1: Hierarchical WriteBuf ⚠️ MOSTLY COMPLETE (1 BUG)
TVWrite class implements core hierarchical write system. Remaining work:
- [x] L0-L50 structure matches upstream
- [x] Buffer propagation works
- [ ] **FIX BUG**: L20 bottom shadow region handling skips shadow depth increment

### Priority 2: Shadow Rendering ⚠️ PARTIAL (BUG)
Shadow rendering works for right-side shadows but fails for bottom shadows:
- [x] Right-side shadow rendering works
- [x] ApplyShadow uses TColorDesired.ToBIOS(false) correctly
- [x] slNoShadow style flag prevents double-shadowing
- [ ] **FIX BUG**: Bottom shadow not rendered (L20 logic flaw)

### Priority 3: Re-enable Window Buffering ✅ COMPLETE
TWindow now uses full buffering with hierarchical write support:
- [x] TGroup constructor sets `Options |= ofBuffered`
- [x] TWindow does NOT remove ofBuffered flag
- [x] Buffer allocation, locking, and propagation work

### Priority 4: TColorAttr Full Color Model ✅ COMPLETE
Full upstream-compatible color system implemented:
- [x] `TColorDesired` union type (BIOS/RGB/XTerm/Default)
- [x] `TColorAttr` uses 64-bit storage with 27-bit fg/bg fields
- [x] `TColorBIOS`, `TColorRGB`, `TColorXTerm` types
- [x] Color conversion functions (RGBtoBIOS, XTermToBIOS, etc.)
- [x] `ApplyShadow` uses `TColorDesired.ToBIOS()`
- [x] `ReverseAttribute` handles default colors correctly

---

## Verified Implementation Claims

### Claim: "TVWrite L0-L50 matches upstream" - ✅ VERIFIED (with caveat)
The overall structure matches. L0, L10, L30, L40, L50 are correctly implemented.
**Caveat:** L20 has a logic flaw causing bottom shadows to be missed.

### Claim: "TColorAttr 64-bit storage" - ✅ VERIFIED
`TColorAttr.cs` uses `ulong _data` with correct bit packing:
- Bits 0-9: Style (10 bits)
- Bits 10-36: Foreground (27 bits)
- Bits 37-63: Background (27 bits)

### Claim: "TColorDesired union type" - ✅ VERIFIED
`TColorDesired.cs` implements all color types:
- ctDefault (0x0), ctBIOS (0x1), ctRGB (0x2), ctXTerm (0x3)
- Proper `ToBIOS()` quantization for all types
- Correct bitcast storage format

### Claim: "TGroup buffer management matches upstream" - ⚠️ PARTIALLY VERIFIED
Buffer allocation and basic locking work. Issues remain with:
- State change handling (Bug 5)
- Drag operation locking (Bug 6)

### Claim: "Window buffering enabled" - ✅ VERIFIED
`TWindow.cs` does not disable `ofBuffered`. Windows use buffering correctly.

---

## Phase Summary

| Phase | Component | Status | Completion |
|-------|-----------|--------|------------|
| 1 | Core Primitives | ✅ Complete | 100% (TColorAttr/TColorDesired done) |
| 2 | Event System | ⚠️ Bug | 95% (Timer comparison bug) |
| 3 | Platform Layer | ✅ Complete | 100% (Windows) |
| 4 | View Hierarchy | ⚠️ Bugs | 85% (Shadow, buffer bugs) |
| 5 | Application Framework | ✅ Complete | 100% |
| 6 | Dialog Controls | ⚠️ Bug | 95% (TButton timer bug) |
| 7 | Menu System | ✅ Complete | 100% |
| 8 | Editor Module | ❌ Not Started | 0% |

**Build Status:** ✅ Clean
**Test Status:** ✅ 88 tests passing
**Hello Example:** ⚠️ Visual bugs present

---

## Phase 1: Core Primitives ✅ Complete

Core types implemented with test coverage.

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TPoint | Core/TPoint.cs | ✅ | 2D coordinates with operators |
| TRect | Core/TRect.cs | ✅ | Rectangle geometry |
| TColorAttr | Core/TColorAttr.cs | ✅ | 64-bit storage, full color model |
| TColorDesired | Core/TColorDesired.cs | ✅ | BIOS/RGB/XTerm/Default support |
| TColorBIOS | Core/TColorDesired.cs | ✅ | 4-bit BIOS color |
| TColorRGB | Core/TColorDesired.cs | ✅ | 24-bit RGB color |
| TColorXTerm | Core/TColorDesired.cs | ✅ | 8-bit XTerm palette |
| ColorConversion | Core/TColorDesired.cs | ✅ | Full conversion utilities |
| TScreenCell | Core/TScreenCell.cs | ✅ | Character + attribute pair |
| TAttrPair | Core/TAttrPair.cs | ✅ | Normal/highlight attribute pairs |
| TDrawBuffer | Core/TDrawBuffer.cs | ✅ | All buffer operations |
| TPalette | Core/TPalette.cs | ✅ | Color palette wrapper |
| TCommandSet | Core/TCommandSet.cs | ✅ | Command bitset |
| TStringView | Core/TStringView.cs | ✅ | String utilities |

---

## Phase 2: Event System ⚠️ Bug in Timer Handling

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TEvent | Core/TEvent.cs | ✅ | Event structure |
| KeyDownEvent | Core/KeyDownEvent.cs | ✅ | Keyboard events |
| MouseEvent | Core/MouseEvent.cs | ✅ | Mouse events |
| MessageEvent | Core/MessageEvent.cs | ⚠️ | Timer ID comparison issue |
| Timer System | — | ⚠️ | Timer ID matching broken |

---

## Phase 3: Platform Layer ✅ Complete (Windows)

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

## Phase 4: View Hierarchy ⚠️ Critical Bugs

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TView | Views/TView.cs | ✅ | Core view class |
| TGroup | Views/TGroup.cs | ⚠️ | Buffer locking issues |
| TVWrite | Views/TVWrite.cs | ⚠️ | **L20 shadow bug** |
| TFrame | Views/TFrame.cs | ⚠️ | Close button color issue |
| TScrollBar | Views/TScrollBar.cs | ✅ | — |
| TScroller | Views/TScroller.cs | ✅ | — |
| TListViewer | Views/TListViewer.cs | ✅ | — |
| TBackground | Views/TBackground.cs | ✅ | — |

---

## Phase 5: Application Framework ✅ Complete

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TProgram | Application/TProgram.cs | ✅ | Event loop, screen buffer |
| TApplication | Application/TApplication.cs | ✅ | Win32 driver init |
| TDeskTop | Application/TDeskTop.cs | ✅ | Window management |
| TDialog | Application/TDialog.cs | ✅ | Modal execution |
| TWindow | Application/TWindow.cs | ✅ | Full buffering support |

---

## Phase 6: Dialog Controls ⚠️ Timer Bug

| Class | File | Status | Notes |
|-------|------|--------|-------|
| TButton | Dialogs/TButton.cs | ⚠️ | **Timer comparison bug** |
| TStaticText | Dialogs/TStaticText.cs | ✅ | — |
| TLabel | Dialogs/TLabel.cs | ⚠️ | Visibility issue (palette?) |
| TInputLine | Dialogs/TInputLine.cs | ✅ | — |
| TCluster | Dialogs/TCluster.cs | ✅ | — |
| TCheckBoxes | Dialogs/TCheckBoxes.cs | ✅ | — |
| TRadioButtons | Dialogs/TRadioButtons.cs | ✅ | — |
| TListBox | Dialogs/TListBox.cs | ✅ | — |
| THistory | Dialogs/THistory.cs | ✅ | — |

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

### Priority 1: Fix Critical Bugs (BLOCKING)
1. [ ] **TVWrite.L20 shadow bug** - Restructure bottom shadow handling
2. [ ] **TButton timer comparison** - Fix `InfoPtr` timer ID matching
3. [ ] **TGroup buffer management** - Fix state change redraw issues

### Priority 2: Fix Visual Bugs
4. [ ] **TLabel visibility** - Debug palette cascade
5. [ ] **TFrame close button color** - Verify palette indices
6. [ ] **Drag flashing** - Improve locking during drag

### Priority 3: Standard Dialogs
- messageBox(), inputBox()

### Priority 4: Editor Module
- TEditor, TMemo, TFileEditor

### Priority 5: File Dialogs
- TFileDialog, TChDirDialog

### Priority 6: Advanced Features
- Validators, Help system, Collections

### Priority 7: Cross-Platform
- Linux driver (ncurses-based)
- macOS support

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
