# Implementation Phases

This document outlines the complete high-level phases for achieving full feature parity with the upstream magiblot/tvision C++ library.

## Current State Summary

**Completed Phases:** 1-7 (Core through Menus - ~40% overall)
**Remaining Phases:** 8-15 (File Dialogs through Cross-Platform - ~60% remaining)

---

## Phase Overview

| Phase | Name | Status | Estimated LOC |
|-------|------|--------|---------------|
| 1 | Core Primitives | ✓ Complete | ~2,500 |
| 2 | Event System | ✓ Complete | ~1,500 |
| 3 | Platform Abstraction | ◐ Partial (50%) | ~2,000 |
| 4 | View Hierarchy | ✓ Complete | ~3,000 |
| 5 | Application Framework | ✓ Complete | ~1,500 |
| 6 | Basic Controls | ✓ Complete | ~2,500 |
| 7 | Menus & Status Line | ✓ Complete | ~1,500 |
| 8 | File Dialogs | ○ Not Started | ~1,500 |
| 9 | Collections Framework | ○ Not Started | ~1,000 |
| 10 | Editor Module | ○ Not Started | ~3,000 |
| 11 | Outline Views | ○ Not Started | ~500 |
| 12 | Color Selector | ○ Not Started | ~800 |
| 13 | Help System | ○ Not Started | ~1,200 |
| 14 | Streaming/Serialization | ○ Not Started | ~2,000 |
| 15 | Cross-Platform | ○ Not Started | ~2,500 |

**Legend:** ✓ Complete | ◐ Partial | ○ Not Started

---

## Completed Phases (1-7)

### Phase 1: Core Primitives ✓

Foundational value types and utilities with no dependencies.

```
TurboVision/Core/
├── TPoint.cs              // readonly record struct { X, Y } with operators
├── TRect.cs               // record struct { A, B : TPoint } with geometry methods
├── TColorAttr.cs          // Color attribute (foreground, background, style)
├── TColorDesired.cs       // Color union (BIOS, RGB, XTerm, Default)
├── TAttrPair.cs           // Pair of TColorAttr (normal, highlight)
├── TPalette.cs            // Color palette array wrapper
├── TDrawBuffer.cs         // Screen line buffer for drawing
├── TScreenCell.cs         // Single cell: character + attributes
├── TCommandSet.cs         // Bitset for 256 enabled/disabled commands
├── EventConstants.cs      // evNothing, evKeyDown, evMouseDown, evCommand...
├── CommandConstants.cs    // cmQuit, cmClose, cmZoom, cmResize, cmMenu...
└── KeyConstants.cs        // kbEnter, kbEsc, kbTab, kbAltX, etc.
```

---

### Phase 2: Event System ✓

Event structures and keyboard/mouse handling.

```
TurboVision/Core/
├── TEvent.cs              // Union-like struct: What, KeyDown, Mouse, Message
├── KeyDownEvent.cs        // KeyCode, ControlKeyState, Text buffer
├── MouseEvent.cs          // Where, Buttons, EventFlags, Wheel
└── MessageEvent.cs        // Command, InfoPtr
```

---

### Phase 3: Platform Abstraction ◐

Driver interfaces and Windows implementation (partial).

```
TurboVision/Platform/
├── IScreenDriver.cs       // Interface for screen operations
├── IEventSource.cs        // Interface for input events
├── Win32ConsoleDriver.cs  // Windows Console API implementation
├── TScreen.cs             // Static screen state, mode, size
├── TDisplay.cs            // Display capabilities
├── THardwareInfo.cs       // Platform detection
├── TEventQueue.cs         // Event polling, waitForEvents
├── TTimerQueue.cs         // Timer management
└── TClipboard.cs          // Clipboard access (minimal)
```

**Missing in this phase:**
- Complete color mode detection
- Damage tracking for performance
- System clipboard integration
- Linux/ncurses driver
- ANSI terminal driver

---

### Phase 4: View Hierarchy ✓

Core view classes and rendering system.

```
TurboVision/Views/
├── TObject.cs             // Base class with Dispose pattern
├── TView.cs               // Foundation view class (~50 methods)
├── TGroup.cs              // Container that owns child views
├── TFrame.cs              // Window border drawing
├── TScrollBar.cs          // Standalone scrollbar
├── TScroller.cs           // Scrollable content area
├── TListViewer.cs         // Abstract list display
├── TBackground.cs         // Desktop background pattern
└── TVWrite.cs             // Hierarchical screen write system
```

---

### Phase 5: Application Framework ✓

Application skeleton and window management.

```
TurboVision/Application/
├── TProgram.cs            // Application skeleton, event loop
├── TApplication.cs        // Adds menu bar, status line, desktop
├── TDeskTop.cs            // Container for windows
├── TWindow.cs             // Movable, resizable window with frame
└── TDialog.cs             // Modal dialog (TWindow + modal behavior)
```

---

### Phase 6: Basic Controls ✓

Dialog controls and input widgets.

```
TurboVision/Dialogs/
├── TStaticText.cs         // Label/static text
├── TButton.cs             // Clickable button with animation
├── TInputLine.cs          // Single-line text input
├── TLabel.cs              // Label linked to another control
├── TCluster.cs            // Base for checkbox/radio groups
├── TCheckBoxes.cs         // Checkbox group
├── TRadioButtons.cs       // Radio button group
├── TListBox.cs            // List with string items
├── THistory.cs            // Input history dropdown
├── THistoryViewer.cs      // History list display
├── THistoryWindow.cs      // History popup window
├── THistoryList.cs        // History data management
├── TSItem.cs              // String item for clusters
├── MsgBox.cs              // messageBox() / inputBox() functions
├── TValidator.cs          // Base validator class
├── TFilterValidator.cs    // Character filter validator
├── TRangeValidator.cs     // Numeric range validator
├── TLookupValidator.cs    // Abstract lookup validator
├── TStringLookupValidator.cs // String set validator
└── TPXPictureValidator.cs // Picture format validator
```

**Missing in this phase:**
- TMultiCheckBoxes
- TParamText

---

### Phase 7: Menus & Status Line ✓

Menu system and status bar.

```
TurboVision/Menus/
├── TMenuItem.cs           // Menu item data structure
├── TMenu.cs               // Menu container
├── TMenuView.cs           // Base menu view
├── TMenuBar.cs            // Top menu bar
├── TMenuBox.cs            // Dropdown menu
├── TMenuPopup.cs          // Context menu (partial)
├── TSubMenu.cs            // Submenu helper
├── TStatusLine.cs         // Bottom status bar
├── TStatusDef.cs          // Status line definitions
└── TStatusItem.cs         // Individual status items
```

---

## Remaining Phases (8-15)

### Phase 8: File Dialogs

Standard file and directory selection dialogs.

**Upstream files:** tfildlg.cpp, tfilecol.cpp, tfillist.cpp, stddlg.cpp, tchdrdlg.cpp, tdircoll.cpp, tdirlist.cpp

**Classes to implement:**
```
TurboVision/Dialogs/
├── TSearchRec.cs          // File search record structure
├── TFileCollection.cs     // Sorted collection of TSearchRec
├── TFileInputLine.cs      // File name input with completion
├── TSortedListBox.cs      // List box with keyboard search
├── TFileList.cs           // File listing view
├── TFileInfoPane.cs       // File information display
├── TFileDialog.cs         // Main file open/save dialog
├── TDirEntry.cs           // Directory path entry
├── TDirCollection.cs      // Collection of directory entries
├── TDirListBox.cs         // Directory tree view
└── TChDirDialog.cs        // Change directory dialog
```

**Helper functions:**
- `driveValid()`, `isDir()`, `pathValid()`, `validFileName()`
- `getCurDir()`, `isWild()`, `getHomeDir()`
- Path utilities (split, merge, squeeze)

**Dependencies:** Phase 9 (Collections) is optional but useful

---

### Phase 9: Collections Framework

Dynamic collections with Turbo Vision semantics.

**Upstream files:** tcollect.cpp, tsortcol.cpp, tstrcoll.cpp, tstrlist.cpp, trescoll.cpp, tresfile.cpp

**Classes to implement:**
```
TurboVision/Collections/
├── TNSCollection.cs       // Non-streamable dynamic array
├── TNSSortedCollection.cs // Non-streamable sorted array
├── TCollection.cs         // Streamable dynamic array
├── TSortedCollection.cs   // Streamable sorted collection
├── TStringCollection.cs   // Sorted string collection
├── TStrIndexRec.cs        // String list index entry
├── TStringList.cs         // Read-only string list
├── TStrListMaker.cs       // String list builder
├── TResourceItem.cs       // Resource index entry
├── TResourceCollection.cs // Resource catalog
└── TResourceFile.cs       // Persistent resource manager
```

**Features:**
- Dynamic arrays with configurable growth delta
- Sorted collections with binary search
- Callback iteration: `forEach()`, `firstThat()`, `lastThat()`
- Index operations: `at()`, `atPut()`, `atInsert()`, `atRemove()`

**Note:** Consider using C# generics while maintaining Turbo Vision API semantics.

**Dependencies:** Useful for Phases 8, 10, 12, 13

---

### Phase 10: Editor Module

Text editing with gap buffer algorithm.

**Upstream files:** teditor1.cpp, teditor2.cpp, tmemo.cpp, tfiledtr.cpp, teditwnd.cpp, tindictr.cpp, editstat.cpp, edits.cpp, textview.cpp

**Classes to implement:**
```
TurboVision/Editor/
├── TIndicator.cs          // Line:column display
├── TEditor.cs             // Core text editor (~1500 lines)
├── TMemo.cs               // In-memory editor
├── TFileEditor.cs         // File-based editor
├── TEditWindow.cs         // Editor window container
├── TTextDevice.cs         // Terminal emulation base
└── TTerminal.cs           // Terminal view
```

**TEditor Features:**
- Gap buffer for efficient text manipulation
- Selection with mouse and keyboard
- Clipboard (cut/copy/paste)
- Undo/redo support
- Find and replace dialogs
- Auto-indent
- EOL detection (CRLF, LF, CR)
- Encoding support (UTF-8, etc.)
- Line/column tracking with TIndicator
- Word wrap option
- Tab handling

**Editor Commands:**
- Navigation: `cmCharLeft`, `cmCharRight`, `cmWordLeft`, `cmWordRight`, `cmLineUp`, `cmLineDown`, `cmPageUp`, `cmPageDown`, `cmHome`, `cmEnd`, `cmTextStart`, `cmTextEnd`
- Editing: `cmBackspace`, `cmDelChar`, `cmDelWord`, `cmDelLine`, `cmNewLine`, `cmInsertLine`
- Selection: `cmStartSelect`, `cmHideSelect`, `cmSelectAll`
- Search: `cmFind`, `cmReplace`, `cmSearchAgain`

**Dependencies:** Phase 9 (Collections) for undo history

---

### Phase 11: Outline Views

Tree/hierarchical data display.

**Upstream files:** toutline.cpp, soutline.cpp, nmoutlin.cpp

**Classes to implement:**
```
TurboVision/Views/
├── TNode.cs               // Tree node with children and state
├── TOutlineViewer.cs      // Abstract base outline view
└── TOutline.cs            // Concrete outline implementation
```

**Features:**
- Tree traversal with visitor pattern
- Expansion/collapse state (`ovExpanded`, `ovChildren`, `ovLast`)
- Graphics generation (tree lines, +/- indicators)
- Keyboard: arrows, +/-, Enter, *, Ctrl+* (expand all)
- Mouse: click to expand/collapse, double-click to select

**Dependencies:** None (builds on Phase 4)

---

### Phase 12: Color Selector

Color palette customization dialogs.

**Upstream files:** colorsel.cpp, sclrsel.cpp, nmclrsel.cpp

**Classes to implement:**
```
TurboVision/Dialogs/
├── TColorItem.cs          // Single color palette item
├── TColorGroup.cs         // Group of color items
├── TColorIndex.cs         // Color index mapping
├── TColorSelector.cs      // 16-color grid picker
├── TMonoSelector.cs       // Monochrome attribute selector
├── TColorDisplay.cs       // Color preview
├── TColorGroupList.cs     // Scrollable list of color groups
├── TColorItemList.cs      // Scrollable list of color items
└── TColorDialog.cs        // Complete color selection dialog
```

**Features:**
- 16-color grid for foreground/background selection
- Monochrome mode: Normal, Highlight, Underline, Inverse
- Color group organization (Window, Desktop, Menu, etc.)
- Live preview of color changes
- Commands: `cmColorForegroundChanged`, `cmColorBackgroundChanged`, `cmColorSet`

**Dependencies:** Phase 9 (Collections) for group lists

---

### Phase 13: Help System

Context-sensitive help with hyperlinks.

**Upstream files:** help.cpp, helpbase.cpp, shlpbase.cpp

**Classes to implement:**
```
TurboVision/Help/
├── TParagraph.cs          // Text paragraph structure
├── TCrossRef.cs           // Hyperlink representation
├── THelpTopic.cs          // Help content with paragraphs
├── THelpIndex.cs          // Topic indexing for fast lookup
├── THelpFile.cs           // Help file I/O
├── THelpViewer.cs         // Interactive help viewer
└── THelpWindow.cs         // Window wrapper
```

**Features:**
- Binary help file format (magic header "FBHF")
- Topic loading and caching by context ID
- Cross-reference hyperlinks
- Text wrapping algorithm
- Keyboard: Tab/Shift+Tab through cross-refs, Enter to follow
- Mouse: click hyperlinks
- Scroll bar support for long topics

**Help File Format:**
```
Magic Header: 0x46484246 ("FBHF")
THelpIndex: Array of topic positions
THelpTopic[]: Serialized topic data
  - TParagraph[]: Text content
  - TCrossRef[]: Hyperlink definitions
```

**Dependencies:** Phase 14 (Streaming) for file format

---

### Phase 14: Streaming/Serialization

Object persistence and resource files.

**Upstream files:** tobjstrm.cpp, all s*.cpp (46 files), all nm*.cpp (42 files)

**Classes to implement:**
```
TurboVision/Streaming/
├── TStreamable.cs         // Base interface for serializable objects
├── TStreamableClass.cs    // Runtime type registration
├── TStreamableTypes.cs    // Type registry database
├── TPWrittenObjects.cs    // Output stream object tracking
├── TPReadObjects.cs       // Input stream object tracking
├── PStream.cs             // Base stream with type management
├── IPStream.cs            // Input stream reader
├── OPStream.cs            // Output stream writer
├── IOPStream.cs           // Bidirectional stream
├── FPBase.cs              // File buffer base
├── IFPStream.cs           // File input stream
├── OFPStream.cs           // File output stream
└── FPStream.cs            // Bidirectional file stream
```

**Design Decision:**

The upstream C++ uses pointer-based object graphs. Options for C#:

1. **Binary Compatible** - Match upstream format exactly (for file interop)
2. **Native .NET** - Use BinaryFormatter, JSON, or custom format
3. **Hybrid** - New format with optional legacy import

**Recommendation:** Option 3 - Design modern format but provide legacy import.

**Integration Required:**
All view classes need `Read()` and `Write()` methods added:
- TView, TGroup, TWindow, TDialog, TFrame, TScrollBar, etc.
- All dialog controls (TButton, TInputLine, TCluster, etc.)
- Menu classes (TMenuBar, TStatusLine, etc.)

**Dependencies:** Required for Phase 13 (Help System)

---

### Phase 15: Cross-Platform

Linux and generic Unix support.

**Components to implement:**
```
TurboVision/Platform/
├── NcursesDriver.cs       // ncurses-based display/input
├── AnsiTerminalDriver.cs  // Generic ANSI escape sequences
├── LinuxConsoleDriver.cs  // Linux console specifics
├── UnixClipboard.cs       // X11/Wayland clipboard
└── SignalHandler.cs       // SIGWINCH, SIGTSTP, etc.
```

**NcursesDriver Features:**
- Terminal capability database (terminfo)
- Mouse support via ncurses
- Color mode detection (8, 16, 256, true color)
- Wide character support
- Unicode/UTF-8 handling

**AnsiTerminalDriver Features:**
- Generic ANSI escape sequence output
- XTerm-compatible mouse tracking
- 256-color and true color support
- Terminal resize detection

**Signal Handling:**
- SIGWINCH - Terminal resize
- SIGTSTP/SIGCONT - Suspend/resume
- SIGINT - Interrupt handling

**Dependencies:** Platform-specific, no internal dependencies

---

## Dependency Graph

```
Phase 1: Core Primitives
    ↓
Phase 2: Event System
    ↓
Phase 3: Platform (Windows) ────────────────────────────→ Phase 15: Cross-Platform
    ↓
Phase 4: TView → TGroup → TFrame, TScrollBar, TBackground
    ↓
Phase 5: TProgram → TApplication → TDeskTop, TWindow, TDialog
    ↓
Phase 6: Controls (TButton, TInputLine, TListBox, etc.)
    ↓
Phase 7: Menus (TMenuBar, TStatusLine)
    │
    ├───→ Phase 8: File Dialogs ←─── Phase 9: Collections
    │                                      ↑
    ├───→ Phase 10: Editor ────────────────┘
    │
    ├───→ Phase 11: Outline
    │
    ├───→ Phase 12: Color Selector ←─ Phase 9: Collections
    │
    └───→ Phase 13: Help System ←──── Phase 14: Streaming
```

---

## Implementation Order Recommendation

**Immediate (Enhances current functionality):**
1. Phase 8: File Dialogs - Enables file-based applications
2. Phase 9: Collections - Foundation for complex data

**Short-term (Common features):**
3. Phase 11: Outline - Tree views for file browsers, etc.
4. Phase 12: Color Selector - Customization support

**Medium-term (Major features):**
5. Phase 10: Editor - Text editing applications
6. Phase 13: Help System - User documentation

**Long-term (Infrastructure):**
7. Phase 14: Streaming - Persistence and resources
8. Phase 15: Cross-Platform - Linux/macOS support
9. Phase 3 completion - Platform layer polish

---

## Milestones

### Milestone 1: Basic Application Framework ✓ (Current)
- Hello World app with menu and dialog
- Basic event handling
- Window management (cascade, tile)

### Milestone 2: File Operations
- File open/save dialogs working
- Directory navigation
- File information display

### Milestone 3: Text Editor
- Functional TEditor with gap buffer
- Find/replace dialogs
- Basic file editing (open, edit, save)

### Milestone 4: Complete UI Toolkit
- All dialogs and controls working
- Color customization
- Outline/tree views
- Help system

### Milestone 5: Full Parity
- Streaming/serialization
- Cross-platform support
- All upstream features ported

---

## Effort Estimates

| Phase | Estimated LOC | Complexity | Effort |
|-------|---------------|------------|--------|
| 8: File Dialogs | 1,500 | Medium | 2-3 weeks |
| 9: Collections | 1,000 | Medium | 1-2 weeks |
| 10: Editor | 3,000 | High | 4-6 weeks |
| 11: Outline | 500 | Low | 1 week |
| 12: Color Selector | 800 | Medium | 1-2 weeks |
| 13: Help System | 1,200 | Medium | 2-3 weeks |
| 14: Streaming | 2,000 | High | 3-4 weeks |
| 15: Cross-Platform | 2,500 | High | 4-6 weeks |

**Total Remaining:** ~12,500 LOC, ~20-30 weeks of effort

---

## Notes

- All phases should maintain test coverage (unit tests for new code)
- Follow CODING_STYLE.md conventions (C# 14, .NET 10 idioms)
- Use Reference/tvision/ for upstream source reference
- Run Examples/Hello and Examples/Palette to verify no regressions

---

*Last updated: 2026-01-04*
