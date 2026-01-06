# Implementation Phases

This document outlines the complete high-level phases for achieving full feature parity with the upstream magiblot/tvision C++ library.

## Current State Summary

**Completed Phases:** 1-14 (Core through Streaming - ~75-80% overall)
**Remaining Phases:** 11, 15 (Outline Views, Cross-Platform - ~20-25% remaining)

---

## Phase Overview

| Phase | Name | Status | Estimated LOC |
|-------|------|--------|---------------|
| 1 | Core Primitives | ✓ Complete | ~2,500 |
| 2 | Event System | ✓ Complete | ~1,500 |
| 3 | Platform Abstraction | ◐ Partial (55%) | ~2,000 |
| 4 | View Hierarchy | ✓ Complete | ~3,000 |
| 5 | Application Framework | ✓ Complete | ~1,500 |
| 6 | Basic Controls | ✓ Complete | ~2,500 |
| 7 | Menus & Status Line | ✓ Complete | ~1,500 |
| 8 | File Dialogs | ✓ Complete | ~1,500 |
| 9 | Collections Framework | ✓ Complete | ~1,000 |
| 10 | Editor Module | ✓ Complete | ~3,000 |
| **11** | **Outline Views** | **○ Not Started** | **~500** |
| 12 | Color Selector | ✓ Complete | ~800 |
| 13 | Help System | ✓ Complete | ~1,200 |
| 14 | Streaming/Serialization | ✓ Complete | ~2,000 |
| 15 | Cross-Platform | ○ Not Started | ~2,500 |

**Legend:** ✓ Complete | ◐ Partial | ○ Not Started

---

## Completed Phases (1-10, 12-14)

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

### Phase 3: Platform Abstraction ◐ (55%)

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

**Remaining in this phase:**
- Complete color mode detection
- Damage tracking for performance
- System clipboard integration
- Linux/ncurses driver (Phase 15)
- ANSI terminal driver (Phase 15)

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
├── TMultiCheckBoxes.cs    // Variable-width multi-checkbox
├── TListBox.cs            // List with string items
├── TSortedListBox.cs      // Sorted list with keyboard search
├── THistory.cs            // Input history dropdown
├── THistoryViewer.cs      // History list display
├── THistoryWindow.cs      // History popup window
├── THistoryList.cs        // History data management
├── TParamText.cs          // Printf-style parameterized text
├── TSItem.cs              // String item for clusters
├── MsgBox.cs              // messageBox() / inputBox() functions
├── TValidator.cs          // Base validator class
├── TFilterValidator.cs    // Character filter validator
├── TRangeValidator.cs     // Numeric range validator
├── TLookupValidator.cs    // Abstract lookup validator
├── TStringLookupValidator.cs // String set validator
└── TPXPictureValidator.cs // Picture format validator
```

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
├── TMenuPopup.cs          // Context menu
├── TSubMenu.cs            // Submenu helper
├── TStatusLine.cs         // Bottom status bar
├── TStatusDef.cs          // Status line definitions
└── TStatusItem.cs         // Individual status items
```

---

### Phase 8: File Dialogs ✓

Standard file and directory selection dialogs.

```
TurboVision/Dialogs/
├── TSearchRec.cs          // File search record structure
├── TFileCollection.cs     // Sorted collection of TSearchRec
├── TFileInputLine.cs      // File name input with completion
├── TFileList.cs           // File listing view
├── TFileInfoPane.cs       // File information display
├── TFileDialog.cs         // Main file open/save dialog
├── TDirEntry.cs           // Directory path entry
├── TDirCollection.cs      // Collection of directory entries
├── TDirListBox.cs         // Directory tree view
├── TChDirDialog.cs        // Change directory dialog
├── PathUtils.cs           // Path utility functions
└── FileDialogCommands.cs  // Command constants
```

---

### Phase 9: Collections Framework ✓

Dynamic collections with Turbo Vision semantics.

```
TurboVision/Collections/
├── TNSCollection.cs       // Non-streamable dynamic array
├── TNSSortedCollection.cs // Non-streamable sorted array
├── TCollection.cs         // Streamable dynamic array
├── TSortedCollection.cs   // Streamable sorted collection
├── TStringCollection.cs   // Sorted string collection
├── TStringList.cs         // Read-only string list
├── TStrListMaker.cs       // String list builder
├── TResourceCollection.cs // Resource catalog
└── TResourceFile.cs       // Persistent resource manager
```

**Features Implemented:**
- Dynamic arrays with configurable growth delta
- Sorted collections with binary search
- Callback iteration: `ForEach()`, `FirstThat()`, `LastThat()`
- Index operations: `At()`, `AtPut()`, `AtInsert()`, `AtRemove()`
- C# generics with Turbo Vision API semantics

---

### Phase 10: Editor Module ✓

Text editing with gap buffer algorithm.

```
TurboVision/Editors/
├── TIndicator.cs          // Line:column display
├── TEditor.cs             // Core text editor (~1689 lines)
├── TMemo.cs               // In-memory editor
├── TFileEditor.cs         // File-based editor
├── TEditWindow.cs         // Editor window container
├── TTextDevice.cs         // Terminal emulation base
├── TTerminal.cs           // Terminal view
└── EditorConstants.cs     // Command constants
```

**TEditor Features Implemented:**
- Gap buffer for efficient text manipulation
- Selection with mouse and keyboard
- Clipboard (cut/copy/paste)
- Undo/redo framework
- Find and replace
- Key mapping tables (WordStar-compatible)
- Line/column tracking with TIndicator
- Multiple encodings support

---

### Phase 12: Color Selector ✓

Color palette customization dialogs.

```
TurboVision/Colors/
├── TColorItem.cs          // Single color palette item
├── TColorGroup.cs         // Group of color items
├── TColorSelector.cs      // 16-color grid picker
├── TMonoSelector.cs       // Monochrome attribute selector
├── TColorDisplay.cs       // Color preview
├── TColorGroupList.cs     // Scrollable list of color groups
├── TColorItemList.cs      // Scrollable list of color items
├── TColorDialog.cs        // Complete color selection dialog
└── ColorConstants.cs      // Standard palette definitions
```

---

### Phase 13: Help System ✓

Context-sensitive help with hyperlinks.

```
TurboVision/Help/
├── TParagraph.cs          // Text paragraph structure
├── TCrossRef.cs           // Hyperlink representation
├── THelpTopic.cs          // Help content with paragraphs
├── THelpIndex.cs          // Topic indexing for fast lookup
├── THelpFile.cs           // Help file I/O
├── THelpViewer.cs         // Interactive help viewer
├── THelpWindow.cs         // Window wrapper
└── HelpConstants.cs       // Magic headers and constants
```

**Features Implemented:**
- Binary help file format (magic header "FBHF")
- JSON format fallback
- Topic loading and caching by context ID
- Cross-reference hyperlinks
- Text wrapping algorithm
- Keyboard: Tab/Shift+Tab through cross-refs, Enter to follow
- Mouse: click hyperlinks
- Scroll bar support for long topics

---

### Phase 14: Streaming/Serialization ✓

Object persistence and resource files.

```
TurboVision/Streaming/
├── IStreamable.cs         // Base interface for serializable objects
├── IStreamSerializer.cs   // Serializer abstraction
├── IStreamReader.cs       // Stream reader interface
├── IStreamWriter.cs       // Stream writer interface
├── StreamableTypeRegistry.cs // Runtime type registration
└── Json/
    ├── JsonStreamSerializer.cs    // JSON implementation
    ├── ViewHierarchyRebuilder.cs  // Pointer fixup
    ├── TPointJsonConverter.cs     // Custom converter
    ├── TRectJsonConverter.cs      // Custom converter
    ├── TKeyJsonConverter.cs       // Custom converter
    ├── TMenuJsonConverter.cs      // Custom converter
    └── TStatusJsonConverters.cs   // Status converters
```

**Design Decision:** JSON-native approach using System.Text.Json with `[JsonPolymorphic]`/`[JsonDerivedType]` attributes. Human-readable format, no legacy binary compatibility required.

**View Types with Serialization Support (30+ types):**
- TView, TGroup, TFrame, TWindow, TDialog
- TButton, TInputLine, TLabel, TStaticText, TParamText
- TCheckBoxes, TRadioButtons, TMultiCheckBoxes
- TListBox, TSortedListBox, TListViewer
- TScrollBar, TScroller
- THistory, THistoryViewer, THistoryWindow
- TFileInputLine, TFileInfoPane, TFileList, TDirListBox, TFileDialog, TChDirDialog
- TMenuView, TMenuBar, TMenuBox, TMenuPopup, TStatusLine
- TEditor, TMemo, TFileEditor, TIndicator, TEditWindow
- TBackground

---

## Remaining Phases (11, 15)

### Phase 11: Outline Views ○ (NOT STARTED - HIGHEST PRIORITY)

Tree/hierarchical data display.

**Upstream files:** `outline.h`, `toutline.cpp`, `soutline.cpp`, `nmoutlin.cpp`

**Classes to implement:**
```
TurboVision/Views/
├── TNode.cs               // Tree node with children and state
├── TOutlineViewer.cs      // Abstract base outline view
└── TOutline.cs            // Concrete outline implementation
```

**Features Required:**
- Tree traversal with visitor pattern (`FirstThat()`, `ForEach()`)
- Expansion/collapse state (`ovExpanded`, `ovChildren`, `ovLast`)
- Graphics generation (tree lines: │├└─, +/- indicators)
- Keyboard: arrows, +/-, Enter, * (expand all), Ctrl+* (expand all recursively)
- Mouse: click to expand/collapse, double-click to select

**Key Structures from outline.h:**
```csharp
// TNode: Tree node with linked list structure
public class TNode
{
    public TNode? Next { get; set; }
    public string Text { get; set; }
    public TNode? ChildList { get; set; }
    public bool Expanded { get; set; }
}

// TOutlineViewer: Abstract base (extends TScroller)
public abstract class TOutlineViewer : TScroller
{
    public abstract void Adjust(TNode node, bool expand);
    public abstract TNode? GetRoot();
    public abstract TNode? GetNext(TNode node);
    public abstract TNode? GetChild(TNode node, int i);
    public abstract int GetNumChildren(TNode node);
    public abstract string GetText(TNode node);
    public abstract bool IsExpanded(TNode node);
    public abstract bool HasChildren(TNode node);
    // ... visitor methods, drawing, event handling
}

// TOutline: Concrete implementation
public class TOutline : TOutlineViewer
{
    public TNode? Root { get; set; }
    // ... implementations of abstract methods
}
```

**Dependencies:** TScroller (complete)

**Estimated effort:** ~500 lines of C# code

---

### Phase 15: Cross-Platform ○

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

**Estimated effort:** ~2000-2500 lines of C# code

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
    ├───→ Phase 8: File Dialogs ✓
    │
    ├───→ Phase 9: Collections ✓
    │         ↑
    ├───→ Phase 10: Editor ✓
    │
    ├───→ Phase 11: Outline ←─────────────── MISSING (extends TScroller)
    │
    ├───→ Phase 12: Color Selector ✓
    │
    ├───→ Phase 13: Help System ✓
    │
    └───→ Phase 14: Streaming ✓
```

---

## Implementation Order Recommendation

**Immediate (Single missing UI module):**
1. **Phase 11: Outline Views** - Only missing major UI component (~500 LOC)

**Short-term (Platform polish):**
2. **Phase 3 Completion:** Platform layer polish
   - Damage tracking
   - Color mode detection
   - System clipboard integration

**Long-term (Cross-platform):**
3. **Phase 15: Cross-Platform** - Linux/macOS support (~2500 LOC)

---

## Milestones

### Milestone 1: Basic Application Framework ✓ (Complete)
- Hello World app with menu and dialog
- Basic event handling
- Window management (cascade, tile)

### Milestone 2: File Operations ✓ (Complete)
- File open/save dialogs working
- Directory navigation
- File information display

### Milestone 3: Text Editor ✓ (Complete)
- Functional TEditor with gap buffer
- Find/replace functionality
- Basic file editing (open, edit, save)

### Milestone 4: Complete UI Toolkit ◐ (95% - Missing Outline Only)
- All dialogs and controls working ✓
- Color customization ✓
- Help system ✓
- **Outline/tree views** - NOT YET

### Milestone 5: Full Parity (Future)
- Streaming/serialization ✓
- Cross-platform support - NOT YET
- All upstream features ported

---

## Effort Estimates (Remaining)

| Phase | Estimated LOC | Complexity | Effort |
|-------|---------------|------------|--------|
| 11: Outline | 500 | Medium | 1-2 days |
| 3: Platform Polish | 500 | Medium | 2-3 days |
| 15: Cross-Platform | 2,500 | High | 2-3 weeks |

**Total Remaining:** ~3,500 LOC

---

## Notes

- All phases should maintain test coverage (unit tests for new code)
- Follow CODING_STYLE.md conventions (C# 14, .NET 10 idioms)
- Use Reference/tvision/ for upstream source reference
- Run Examples/Hello, Examples/Palette, and Examples/TvDemo to verify no regressions

---

*Tracking commit: 0707c8f*
