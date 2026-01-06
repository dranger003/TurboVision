# Implementation Status

This document tracks the comprehensive porting progress of magiblot/tvision to C# 14 / .NET 10.

**Overall Progress: ~40% of full upstream feature parity**

> **Note:** This assessment is based on a thorough file-by-file comparison between the upstream C++ source (~70 header files, ~163 source files) and the C# port (~74 source files). While core rendering, event handling, and basic UI work, major modules remain unimplemented.

---

## Executive Summary

| Category | Status | Completion |
|----------|--------|------------|
| Core Primitives | Complete | 95% |
| Event System | Mostly Complete | 75% |
| Platform Layer | Partial | 50% |
| View Hierarchy | Mostly Complete | 85% |
| Application Framework | Mostly Complete | 80% |
| Dialog Controls | Mostly Complete | 85% |
| Menu System | Mostly Complete | 85% |
| Message Box/Input | Complete | 100% |
| Validators | Complete | 95% |
| **Editor Module** | **Not Started** | **0%** |
| **File/Directory Dialogs** | **Not Started** | **0%** |
| **Collections Framework** | **Not Started** | **0%** |
| **Outline Views** | **Not Started** | **0%** |
| **Color Selector** | **Not Started** | **0%** |
| **Help System** | **Not Started** | **0%** |
| **Streaming/Serialization** | **In Progress** | **70%** |

---

## Detailed Module Analysis

### Core Primitives - 95% Complete

Core types are well-implemented with minor gaps.

| Class | File | Status | Missing |
|-------|------|--------|---------|
| TPoint | Core/TPoint.cs | Complete | Streaming operators |
| TRect | Core/TRect.cs | Complete | Streaming operators |
| TColorAttr | Core/TColorAttr.cs | Complete | - |
| TColorDesired | Core/TColorDesired.cs | Complete | - |
| TColorRGB/BIOS/XTerm | Core/TColorDesired.cs | Complete | - |
| TScreenCell | Core/TScreenCell.cs | Complete | - |
| TAttrPair | Core/TAttrPair.cs | Complete | - |
| TDrawBuffer | Core/TDrawBuffer.cs | Complete | - |
| TPalette | Core/TPalette.cs | Complete | - |
| TCommandSet | Core/TCommandSet.cs | Complete | - |
| TKey | Core/KeyDownEvent.cs | Complete | - |
| EventConstants | Core/EventConstants.cs | Complete | - |
| CommandConstants | Core/CommandConstants.cs | Complete | - |
| KeyConstants | Core/KeyConstants.cs | Complete | - |

---

### Event System - 75% Complete

Event structures and basic handling work but some methods are missing.

| Component | Status | Missing Features |
|-----------|--------|------------------|
| TEvent struct | Complete | - |
| KeyDownEvent | Complete | - |
| MouseEvent | Complete | - |
| MessageEvent | Complete | - |
| TEventQueue | Partial | EventWaiter architecture |
| TKey | Complete | - |

**Missing Methods in TView:**
- `GetEvent(ref TEvent ev, int timeoutMs)` - Timeout-based event waiting
- `TextEvent(ref TEvent ev, Span<char> dest, out int length)` - Text accumulation
- `SetCmdState(TCommandSet commands, bool enable)` - Bulk command state

**Implemented:**
- `GetEvent()`, `KeyEvent()`, `MouseEvent()`, `ClearEvent()`, `EventAvail()`, `PutEvent()`

---

### Platform Layer - 50% Complete

Windows driver exists but lacks sophisticated features.

| Component | File | Status | Missing Features |
|-----------|------|--------|------------------|
| IScreenDriver | Platform/IScreenDriver.cs | Complete | - |
| IEventSource | Platform/IEventSource.cs | Complete | - |
| Win32ConsoleDriver | Platform/Win32ConsoleDriver.cs | Partial | See below |
| TScreen | Platform/TScreen.cs | Partial | Mode detection |
| TDisplay | Platform/TDisplay.cs | Partial | Screen mode calculation |
| TEventQueue | Platform/TEventQueue.cs | Partial | Event multiplexing |
| THardwareInfo | Platform/THardwareInfo.cs | Minimal | Platform detection only |
| TTimerQueue | Platform/TTimerQueue.cs | Partial | Needs event loop integration |
| TClipboard | Platform/TClipboard.cs | Minimal | No system clipboard |

**Win32ConsoleDriver Missing Features:**
- Color mode detection (legacy vs VT terminal)
- Wine detection and fallback
- Damage tracking (row-based dirty rectangles)
- Wide character overlap handling
- UTF-16 surrogate pair handling

**Platform Singleton Missing:**
- Unified Platform abstraction (upstream: platform.cpp)
- Console adapter pattern
- Display buffer manager

**Missing Platform Implementations:**
- Linux/ncurses driver
- ANSI terminal driver
- Unix clipboard integration

---

### View Hierarchy - 85% Complete

Core view classes are well-implemented with specific gaps.

| Class | File | Status | Missing Methods |
|-------|------|--------|-----------------|
| TObject | Views/TObject.cs | Complete | - |
| TView | Views/TView.cs | 90% | `GetEvent(timeout)`, `TextEvent()`, `SetCmdState()` |
| TGroup | Views/TGroup.cs | 95% | Streaming only |
| TFrame | Views/TFrame.cs | Complete | Streaming only |
| TScrollBar | Views/TScrollBar.cs | Complete | Streaming only |
| TScroller | Views/TScroller.cs | Complete | Streaming only |
| TListViewer | Views/TListViewer.cs | Complete | Streaming only |
| TBackground | Views/TBackground.cs | Complete | Streaming only |
| TVWrite | Views/TVWrite.cs | Complete | - |

**TView Implemented Methods (~50 methods):**
- Geometry: `GetBounds()`, `SetBounds()`, `GetExtent()`, `GetClipRect()`, `SizeLimits()`, `CalcBounds()`, `ChangeBounds()`, `Locate()`, `MoveTo()`, `GrowTo()`
- State: `GetState()`, `SetState()`, `Show()`, `Hide()`
- Events: `HandleEvent()`, `GetEvent()`, `PutEvent()`, `KeyEvent()`, `MouseEvent()`, `ClearEvent()`
- Drawing: `Draw()`, `DrawView()`, `DrawHide()`, `DrawShow()`, `WriteBuf()`, `WriteChar()`, `WriteStr()`, `WriteLine()`
- Focus: `Select()`, `Focus()`, `MakeFirst()`, `PutInFrontOf()`
- Navigation: `NextView()`, `PrevView()`, `Prev()`, `TopView()`
- Cursor: `SetCursor()`, `ShowCursor()`, `HideCursor()`, `BlockCursor()`, `NormalCursor()`, `ResetCursor()`
- Commands: `CommandEnabled()`, `EnableCommand()`, `DisableCommand()`, `EnableCommands()`, `DisableCommands()`, `GetCommands()`, `SetCommands()`
- Modal: `Execute()`, `EndModal()`, `Valid()`
- Data: `DataSize()`, `GetData()`, `SetData()`
- Colors: `GetPalette()`, `MapColor()`, `GetColor()`
- Timers: `SetTimer()`, `KillTimer()`
- Dragging: `DragView()` with keyboard/mouse support

**TGroup Implemented Methods:**
- View Management: `Insert()`, `InsertBefore()`, `InsertView()`, `Remove()`, `RemoveView()`, `At()`, `IndexOf()`, `First()`, `Last()`
- Selection: `SetCurrent()`, `ResetCurrent()`, `SelectNext()`, `FocusNext()`
- Drawing: `Draw()`, `Redraw()`, `DrawSubViews()`, `Lock()`, `Unlock()`
- Events: `HandleEvent()` with 3-phase routing
- Modal: `ExecView()`, `Execute()`, `EndModal()`, `Valid()`
- Iteration: `ForEach()`, `FirstMatch()`, `FirstThat()`
- Buffer: `GetBuffer()`, `FreeBuffer()`

---

### Application Framework - 80% Complete

Core application classes work but missing some features.

| Class | File | Status | Missing Features |
|-------|------|--------|------------------|
| TProgram | Application/TProgram.cs | Partial | `eventWaitTimeout()`, memory pressure handling |
| TApplication | Application/TApplication.cs | Partial | `cmDosShell` handler is TODO |
| TDialog | Application/TDialog.cs | Complete | Streaming only |
| TWindow | Application/TWindow.cs | Complete | Streaming only |
| TDeskTop | Application/TDeskTop.cs | Complete | Streaming only |

**TProgram Implemented:**
- Event loop (`Execute()`)
- Static members: `Application`, `StatusLine`, `MenuBar`, `DeskTop`
- Palette management (color/blackwhite/monochrome)
- Screen initialization
- Window management (`InsertWindow()`)
- Commands: `cmQuit`, `cmScreenChanged`, `cmCommandSetChanged`, `cmTimerExpired`

**TApplication Implemented:**
- Driver initialization
- `Suspend()`, `Resume()`
- `Cascade()`, `Tile()`

---

### Dialog Controls - 85% Complete

Most dialog controls are implemented.

| Class | File | Status | Missing Features |
|-------|------|--------|------------------|
| TDialog | Application/TDialog.cs | Complete | - |
| TButton | Dialogs/TButton.cs | Complete | Timer animation works |
| TInputLine | Dialogs/TInputLine.cs | 95% | Streaming only |
| TCluster | Dialogs/TCluster.cs | Complete | Abstract base |
| TCheckBoxes | Dialogs/TCheckBoxes.cs | Complete | - |
| TRadioButtons | Dialogs/TRadioButtons.cs | Complete | - |
| TLabel | Dialogs/TLabel.cs | Complete | - |
| TStaticText | Dialogs/TStaticText.cs | Complete | - |
| TListBox | Dialogs/TListBox.cs | Complete | Uses List<string> |
| THistory | Dialogs/THistory.cs | Complete | - |
| THistoryWindow | Dialogs/THistoryWindow.cs | Complete | - |
| THistoryViewer | Dialogs/THistoryViewer.cs | Complete | - |
| THistoryList | Dialogs/THistoryList.cs | Complete | - |
| TSItem | Dialogs/TSItem.cs | Complete | - |
| **TMultiCheckBoxes** | - | **0%** | **ENTIRE CLASS MISSING** |
| **TParamText** | - | **0%** | **ENTIRE CLASS MISSING** |

**TInputLine Features Implemented:**
- Text editing with cursor movement
- Selection highlighting
- Scrolling (left/right arrows indicator)
- Clipboard (cut/copy/paste)
- Validator integration
- State save/restore
- Insert/overwrite modes

---

### Menu System - 85% Complete

Menu system works with some gaps.

| Class | File | Status | Missing Features |
|-------|------|--------|------------------|
| TMenuItem | Menus/TMenuItem.cs | Complete | - |
| TMenu | Menus/TMenu.cs | Complete | - |
| TMenuView | Menus/TMenuView.cs | 90% | `findAltShortcut()` |
| TMenuBar | Menus/TMenuBar.cs | 95% | - |
| TMenuBox | Menus/TMenuBox.cs | 90% | Border coords differ |
| TMenuPopup | Menus/TMenuPopup.cs | 70% | `Execute()`, `HandleEvent()` need work |
| TStatusLine | Menus/TStatusLine.cs | 90% | Hint text display |
| TStatusItem | Menus/TStatusItem.cs | Complete | - |
| TStatusDef | Menus/TStatusDef.cs | Complete | - |
| TSubMenu | Menus/TSubMenu.cs | Complete | - |

**Missing Utilities:**
- `popupMenu()` factory function
- `autoPlacePopup()` positioning helper

---

### Message Box System - 100% Complete ✓

| Function | File | Status |
|----------|------|--------|
| `MessageBox()` | Dialogs/MsgBox.cs | Complete (4 overloads) |
| `MessageBoxRect()` | Dialogs/MsgBox.cs | Complete (4 overloads) |
| `InputBox()` | Dialogs/MsgBox.cs | Complete (2 overloads) |
| `InputBoxRect()` | Dialogs/MsgBox.cs | Complete (2 overloads) |
| `MsgBoxText` | Dialogs/MsgBox.cs | Complete (localizable strings) |
| `MessageBoxFlags` | Dialogs/MsgBox.cs | Complete (all flags) |

**Features:**
- Printf-style format strings via `string.Format()`
- Configurable button combinations (Yes/No/OK/Cancel)
- Message types (Error, Warning, Information, Confirmation)
- Auto-sizing and rect-based variants
- Unicode width calculation

---

### Validator System - 95% Complete ✓

| Class | File | Status | Features |
|-------|------|--------|----------|
| TValidator | Dialogs/TValidator.cs | Complete | Base class with error display |
| TFilterValidator | Dialogs/TFilterValidator.cs | Complete | Character set filtering |
| TRangeValidator | Dialogs/TRangeValidator.cs | Complete | Numeric range validation |
| TLookupValidator | Dialogs/TLookupValidator.cs | Complete | Abstract lookup validation |
| TStringLookupValidator | Dialogs/TStringLookupValidator.cs | Complete | String set validation |
| TPXPictureValidator | Dialogs/TPXPictureValidator.cs | Complete | Picture format validation |

**TPXPictureValidator Pattern Syntax Implemented:**
- `#` digit, `?` letter, `&` uppercase letter
- `!` any to uppercase, `@` any character
- `*` repetition, `{}` groups, `[]` optional
- `;` escape character

---

## Completely Missing Modules (0% Complete)

### Editor Module (0%)

**Required upstream files:** teditor1.cpp, teditor2.cpp, tmemo.cpp, tfiledtr.cpp, teditwnd.cpp, tindictr.cpp, editstat.cpp, edits.cpp, textview.cpp

**Classes to implement:**
| Class | Description | Complexity |
|-------|-------------|------------|
| TIndicator | Line:column display | Low |
| TEditor | Core text editor with gap buffer | High |
| TMemo | In-memory editor | Medium |
| TFileEditor | File-based editor | Medium |
| TEditWindow | Editor window container | Low |
| TTextDevice | Terminal emulation | Medium |
| TTerminal | Terminal view | Medium |

**TEditor Features Required:**
- Gap buffer algorithm for efficient text manipulation
- Selection and clipboard operations
- Undo/redo support
- Search and replace dialogs
- Auto-indent
- EOL type detection (CRLF, LF, CR)
- Encoding support
- Line/column tracking

**Estimated effort:** ~3000+ lines of C# code

---

### File/Directory Dialogs Module (0%)

**Required upstream files:** tfildlg.cpp, tfilecol.cpp, tfillist.cpp, stddlg.cpp, tchdrdlg.cpp, tdircoll.cpp, tdirlist.cpp, sfildlg.cpp

**Classes to implement:**
| Class | Description |
|-------|-------------|
| TFileInputLine | File name input with auto-completion |
| TFileCollection | Sorted collection of TSearchRec |
| TSortedListBox | List box with keyboard search |
| TFileList | File listing view |
| TFileInfoPane | File information display |
| TSearchRec | File search record structure |
| TFileDialog | Main file open/save dialog |
| TDirEntry | Directory path entry |
| TDirCollection | Collection of directory entries |
| TDirListBox | Directory tree view |
| TChDirDialog | Change directory dialog |

**Helper Functions Required:**
- `driveValid()`, `isDir()`, `pathValid()`, `validFileName()`
- `getCurDir()`, `isWild()`, `getHomeDir()`
- Path utilities (split, merge, squeeze)

**Estimated effort:** ~1500+ lines of C# code

---

### Collections Framework (0%)

**Required upstream files:** tcollect.cpp, tsortcol.cpp, tstrcoll.cpp, tstrlist.cpp, trescoll.cpp, tresfile.cpp

**Classes to implement:**
| Class | Description |
|-------|-------------|
| TNSCollection | Non-streamable dynamic array |
| TNSSortedCollection | Non-streamable sorted array with binary search |
| TCollection | Streamable dynamic array |
| TSortedCollection | Streamable sorted collection |
| TStringCollection | Sorted string collection |
| TStrIndexRec | String list index entry |
| TStringList | Read-only string list from stream |
| TStrListMaker | String list builder |
| TResourceItem | Resource index entry |
| TResourceCollection | Resource catalog |
| TResourceFile | Persistent resource manager |

**Features Required:**
- Dynamic arrays with configurable growth
- Sorted collections with binary search
- Callback-based iteration (`forEach`, `firstThat`, `lastThat`)
- String collections with proper memory management

**Note:** C# has `List<T>`, `SortedList<T>`, etc. but these classes provide specific Turbo Vision semantics and streaming support.

**Estimated effort:** ~1000+ lines of C# code

---

### Outline Views Module (0%)

**Required upstream files:** toutline.cpp, soutline.cpp, nmoutlin.cpp

**Classes to implement:**
| Class | Description |
|-------|-------------|
| TNode | Tree node with children and state |
| TOutlineViewer | Abstract base outline view |
| TOutline | Concrete outline implementation |

**Features Required:**
- Tree traversal with visitor pattern (`firstThat()`, `forEach()`)
- Expansion/collapse state
- Graphics generation (tree lines, +/- indicators)
- Keyboard navigation (arrows, +/-, Enter)
- Mouse support (click to expand/collapse)

**Estimated effort:** ~500+ lines of C# code

---

### Color Selector Module (0%)

**Required upstream files:** colorsel.cpp, sclrsel.cpp, nmclrsel.cpp

**Classes to implement:**
| Class | Description |
|-------|-------------|
| TColorItem | Single color palette item |
| TColorGroup | Group of color items |
| TColorIndex | Color index mapping |
| TColorSelector | 16-color grid picker |
| TMonoSelector | Monochrome attribute selector |
| TColorDisplay | Color preview |
| TColorGroupList | Scrollable list of color groups |
| TColorItemList | Scrollable list of color items |
| TColorDialog | Complete color selection dialog |

**Estimated effort:** ~800+ lines of C# code

---

### Help System Module (0%)

**Required upstream files:** help.cpp, helpbase.cpp, shlpbase.cpp

**Classes to implement:**
| Class | Description |
|-------|-------------|
| TParagraph | Text paragraph structure |
| TCrossRef | Hyperlink representation |
| THelpTopic | Help content with paragraphs and cross-refs |
| THelpIndex | Topic indexing for fast lookup |
| THelpFile | Help file I/O and topic loading |
| THelpViewer | Interactive help viewer (TScroller derivative) |
| THelpWindow | Window wrapper for help viewer |

**Features Required:**
- Help file format parsing (binary with magic header "FBHF")
- Topic loading and caching
- Cross-reference navigation
- Text wrapping algorithm
- Keyboard navigation (Tab through cross-refs, Enter to follow)
- Mouse support for clicking hyperlinks
- Dynamic highlighting

**Estimated effort:** ~1200+ lines of C# code

---

### Streaming/Serialization System (70%)

**Design Decision:** JSON-native approach using System.Text.Json with `[JsonPolymorphic]`/`[JsonDerivedType]` attributes for type discrimination. This provides human-readable format while maintaining type safety.

**Implemented:**
| Component | Status | Description |
|-----------|--------|-------------|
| IStreamable | Complete | Base serialization interface |
| IStreamSerializer | Complete | Serializer abstraction |
| IStreamReader/Writer | Complete | Stream reader/writer interfaces |
| StreamableTypeRegistry | Complete | Runtime type registration (mirrors TStreamableTypes) |
| JsonStreamSerializer | Complete | JSON serializer implementation |
| ViewHierarchyRebuilder | Complete | Reconstructs Owner/Next/Last pointers after deserialization |
| TView attributes | Complete | `[JsonPolymorphic]`, `[JsonDerivedType]` for 30+ view types |
| State masking | Complete | Runtime flags (sfActive, sfSelected, etc.) excluded from serialization |
| Linked view resolution | Complete | TLabel.Link and THistory.Link restored via LinkIndex |
| Custom converters | Complete | TPoint, TRect, TKey, TMenu, TMenuItem, TStatusItem, TStatusDef |

**View Types with JSON Serialization Support:**
- Base: TView, TGroup, TFrame, TWindow, TDialog
- Controls: TButton, TInputLine, TLabel, TStaticText, TParamText
- Clusters: TCheckBoxes, TRadioButtons, TMultiCheckBoxes
- Lists: TListBox, TSortedListBox, TListViewer
- Scrolling: TScrollBar, TScroller
- History: THistory, THistoryViewer, THistoryWindow
- File dialogs: TFileInputLine, TFileInfoPane, TFileList, TDirListBox, TFileDialog, TChDirDialog
- Menus: TMenuView, TMenuBar, TMenuBox, TMenuPopup, TStatusLine
- Editor: TEditor, TMemo, TFileEditor, TIndicator, TEditWindow
- Misc: TBackground

**Not Implemented:**
| Feature | Notes |
|---------|-------|
| Binary format | Upstream compatibility not required |
| Validator serialization | Validators are code, not data - intentional |
| TCollection serialization | TCollection<T> converter for future use |

**Estimated remaining effort:** ~500 lines (TCollection converter, documentation)

---

## Missing Minor Classes

| Class | Module | Description |
|-------|--------|-------------|
| TMultiCheckBoxes | Dialogs | Multi-checkbox with variable bit widths |
| TParamText | Dialogs | Printf-style parameterized text |
| TMouse | Platform | Hardware mouse integration |
| THWMouse | Platform | Interrupt handler, button count |
| TSystemError | Platform | System error display |

---

## Test Coverage

| Category | Tests | Status |
|----------|-------|--------|
| TKey Normalization | 1 | Pass |
| Endian/Aliasing | 5 | Pass |
| TRect Geometry | 14 | Pass |
| TPoint Arithmetic | 8 | Pass |
| TColorAttr | 10 | Pass |
| TScreenCell | 5 | Pass |
| TAttrPair | 3 | Pass |
| TDrawBuffer | 27 | Pass |
| TStatusLine | 5 | Pass |
| TGroup/ExecView | 10 | Pass |
| JSON Serialization | 21 | Pass |

**Total: 156 tests (all passing)**

---

## Example Applications

| Example | Directory | Status | Features Demonstrated |
|---------|-----------|--------|----------------------|
| Hello | Examples/Hello | Working | Basic app, menu bar, dialog |
| Palette | Examples/Palette | Working | Color palettes, window management |

---

## Build Status

- **Build:** Clean
- **Tests:** 88 tests passing
- **Examples:** Hello and Palette run successfully

---

## Prioritized Next Steps

### Priority 1: Complete Core Infrastructure (COMPLETED)
1. Implement `TView.TextEvent()` - Text accumulation for efficient input
2. Implement `TView.GetEvent(timeout)` - Timeout-based event waiting
3. Complete `TMenuPopup.Execute()` and `HandleEvent()`
4. Implement `TStatusLine` hint text display
5. Integrate `TTimerQueue` with event loop

### Priority 2: Missing Dialog Classes (COMPLETED)
1. Implement `TMultiCheckBoxes` - Variable-width multi-checkbox
2. Implement `TParamText` - Formatted parameterized text

### Priority 3: File Operations (PARTIAL - missing serialization)
1. Implement `TFileDialog` and supporting classes
2. Implement `TChDirDialog` and supporting classes
3. Implement file path utilities

### Priority 4: Editor Module (PARTIAL - missing serialization and complete `TText` integration)
1. Implement `TIndicator`
2. Implement `TEditor` with gap buffer
3. Implement `TMemo` and `TFileEditor`
4. Implement `TEditWindow`

### Priority 5: Collections Framework (PARTIAL - missing serialization)
1. Implement `TCollection`/`TNSCollection`
2. Implement `TSortedCollection`
3. Implement `TStringCollection`
4. Implement resource management classes

### Priority 6: Serialization (PARTIAL - 70%)
1. Design C# serialization approach - JSON-native with System.Text.Json
2. Implement `IStreamable` pattern - Complete with JsonStreamSerializer
3. Add serialization to all view classes - 30+ view types supported
4. Remaining: TCollection<T> converter, additional documentation

### Priority 7: Platform Completeness
1. Add screen capability detection
2. Add damage tracking to display
3. Implement system clipboard integration
4. Add UTF-16 surrogate handling

### Priority 8: Advanced Features
1. Implement `TOutline` and `TOutlineViewer`
2. Implement `TColorDialog` and related classes
3. Implement Help system

### Priority 9: Cross-Platform
1. Linux driver (ncurses-based)
2. ANSI terminal driver

---

## Upstream File Reference

### Source Files Not Yet Ported (by category)

**Editor (9 files, ~2500 lines):**
- teditor1.cpp, teditor2.cpp, tmemo.cpp, tfiledtr.cpp, teditwnd.cpp
- tindictr.cpp, editstat.cpp, edits.cpp, textview.cpp

**File Dialogs (8 files, ~1500 lines):**
- tfildlg.cpp, tfilecol.cpp, tfillist.cpp, stddlg.cpp
- tchdrdlg.cpp, tdircoll.cpp, tdirlist.cpp
- sfildlg.cpp + supporting s*.cpp files

**Collections (6 files, ~800 lines):**
- tcollect.cpp, tsortcol.cpp, tstrcoll.cpp
- tstrlist.cpp, trescoll.cpp, tresfile.cpp

**Outline (3 files, ~400 lines):**
- toutline.cpp, soutline.cpp, nmoutlin.cpp

**Color Selector (3 files, ~600 lines):**
- colorsel.cpp, sclrsel.cpp, nmclrsel.cpp

**Help System (4 files, ~800 lines):**
- help.cpp, helpbase.cpp, shlpbase.cpp, shelp.cpp

**Streaming (88 files, ~3000 lines):**
- tobjstrm.cpp
- All s*.cpp files (streamable implementations)
- All nm*.cpp files (named object factories)

**Miscellaneous (4 files, ~300 lines):**
- misc.cpp, syserr.cpp, fmtstr.cpp, prntcnst.cpp

**Platform (29 files - different architecture):**
- source/platform/* (not directly ported - Win32ConsoleDriver is custom implementation)

---

*Last updated: 2026-01-05*
*Analysis based on comprehensive file-by-file comparison with upstream magiblot/tvision*
