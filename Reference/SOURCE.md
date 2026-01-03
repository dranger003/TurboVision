# Turbo Vision Upstream Source Reference

Quick reference for porting [magiblot/tvision](https://github.com/magiblot/tvision) to C#.

## Directory Structure

```
tvision/
├── include/tvision/     # Public headers
│   ├── tv.h             # Main include (entry point)
│   ├── ttypes.h         # Basic types
│   ├── objects.h        # TPoint, TRect, TCollection
│   ├── views.h          # TView, TGroup, TWindow, TScrollBar
│   ├── app.h            # TProgram, TApplication, TDeskTop
│   ├── dialogs.h        # TDialog, TButton, TInputLine, TCluster
│   ├── menus.h          # TMenuItem, TMenu, TMenuBar, TStatusLine
│   ├── editors.h        # TEditor, TMemo, TFileEditor
│   ├── drawbuf.h        # TDrawBuffer
│   ├── scrncell.h       # TScreenCell, TColorAttr
│   ├── system.h         # TEvent, TScreen, TEventQueue
│   └── internal/        # Platform-specific internals
├── source/
│   ├── tvision/         # Main implementation (~130 files)
│   └── platform/        # Platform drivers (~30 files)
└── examples/            # Demo applications
```

## Class Hierarchy

### Core Object Model
```
TObject (virtual base)
├── TStreamable (serialization)
├── TView (visual components)
└── TCollection (containers)
```

### View Hierarchy
```
TView
├── TFrame              # Window border/title
├── TScrollBar          # Scrollbar widget
├── TScroller           # Scrollable content area
├── TListViewer         # List display
├── TStaticText         # Read-only text
├── TButton             # Push button
├── TCluster            # Grouped controls
│   ├── TRadioButtons
│   ├── TCheckBoxes
│   └── TMultiCheckBoxes
├── TInputLine          # Text input field
├── TLabel              # Text label
├── TEditor             # Text editing engine
│   ├── TMemo           # In-memory editor
│   └── TFileEditor     # File-based editor
├── TMenuView           # Menu base
│   ├── TMenuBar        # Top menu bar
│   └── TMenuBox        # Dropdown menu
└── TGroup              # Container for child views
    ├── TDeskTop        # Window manager/workspace
    └── TWindow         # Top-level window
        └── TDialog     # Modal dialog
```

### Application Framework
```
TProgram                # Base application loop
└── TApplication        # Full app with menus/status
    ├── owns TDeskTop   # Window management
    ├── owns TMenuBar   # Menu system
    └── owns TStatusLine
```

## Key Abstractions

### Geometry (`objects.h`)
- **TPoint** - `{x, y}` coordinate pair
- **TRect** - `{a: TPoint, b: TPoint}` rectangle (top-left, bottom-right)

### Drawing (`drawbuf.h`, `scrncell.h`)
- **TScreenCell** - Character + color attribute pair (single cell)
- **TColorAttr** - Foreground + background color encoding
- **TDrawBuffer** - Line buffer for rendering
- **TPalette** - Color palette (indices map to actual colors)

### Events (`system.h`)
- **TEvent** - Event structure with type, timestamp, and union data
- **TEventQueue** - Pending event queue

Event types:
- `evMouseDown`, `evMouseUp`, `evMouseMove`, `evMouseWheel`
- `evKeyDown`
- `evCommand` - UI commands
- `evBroadcast` - System-wide notifications

## TView Core Methods

### Construction & Bounds
```cpp
TView(const TRect& bounds)
setBounds(const TRect&)
getBounds() / getExtent()
sizeLimits(TPoint& min, TPoint& max)
```

### Rendering
```cpp
draw()                  // Virtual - override to render content
drawView()              // Internal draw dispatcher
writeBuf() / writeLine() / writeStr() / writeChar()
```

### State Management
```cpp
setState(ushort state, Boolean enable)
getState(ushort state)
show() / hide()
select() / focus()
```

### Event Handling
```cpp
handleEvent(TEvent&)    // Virtual - override to process events
getEvent(TEvent&)       // Get next event
putEvent(TEvent&)       // Post event
clearEvent(TEvent&)     // Mark event as handled
```

### Modal Execution
```cpp
execute()               // Run modal loop
endModal(ushort command)
valid(ushort command)   // Validate before close
```

### Data Exchange
```cpp
dataSize()              // Size of view's data
getData(void* rec)      // Export state to struct
setData(void* rec)      // Import state from struct
```

## State Flags (`views.h`)

| Flag | Meaning |
|------|---------|
| `sfVisible` | View is visible |
| `sfCursorVis` | Cursor is visible |
| `sfCursorIns` | Insert mode cursor |
| `sfActive` | View is active |
| `sfSelected` | View is selected |
| `sfFocused` | View has keyboard focus |
| `sfDragging` | View is being dragged |
| `sfDisabled` | View is disabled |
| `sfModal` | View is in modal mode |
| `sfExposed` | View is exposed (not covered) |

## Option Flags (`views.h`)

| Flag | Meaning |
|------|---------|
| `ofSelectable` | Can receive focus |
| `ofTopSelect` | Select brings to top |
| `ofFirstClick` | Accept first mouse click |
| `ofFramed` | Has a frame |
| `ofPreProcess` | Pre-process events |
| `ofPostProcess` | Post-process events |
| `ofBuffered` | Double-buffered |
| `ofCentered` | Centered in parent |
| `ofValidate` | Validate on close |

## Window Flags (`views.h`)

| Flag | Meaning |
|------|---------|
| `wfMove` | Can be moved |
| `wfGrow` | Can be resized |
| `wfClose` | Has close button |
| `wfZoom` | Has zoom button |

## Command Constants

Standard commands (`views.h`):
- `cmValid`, `cmQuit`, `cmError`
- `cmMenu`, `cmClose`, `cmZoom`, `cmResize`, `cmNext`, `cmPrev`
- `cmOK`, `cmCancel`, `cmYes`, `cmNo`
- `cmCut`, `cmCopy`, `cmPaste`, `cmUndo`, `cmClear`
- `cmHelp`, `cmSave`, `cmSaveAs`, `cmOpen`, `cmNew`

## Menu System

### Structures
```cpp
TMenuItem {
    TStringView name;   // Display text
    ushort command;     // Command to send
    ushort keyCode;     // Shortcut key
    Boolean disabled;
    union { void* param; TMenu* subMenu; };
}

TMenu {
    TMenuItem* items;   // Item list
}
```

### Key Classes
- **TMenuBar** - Horizontal menu at top
- **TMenuBox** - Dropdown/popup menu
- **TStatusLine** - Status bar at bottom
- **TStatusDef** - Status item definitions

## Input Validation

```cpp
TValidator (base)
├── TFilterValidator    # Filter invalid characters
├── TRangeValidator     # Numeric range check
├── TLookupValidator    # Valid values lookup
└── TPXPictureValidator # Format/picture mask
```

## TGroup Methods

```cpp
insert(TView*)          // Add child view
remove(TView*)          // Remove child view
selectNext(Boolean forward)
forEach(callback, void* args)
firstThat(testFunc, void* args)
```

## Linked List Navigation

Views in a TGroup form a circular linked list:
- `next` - Next sibling
- `owner` - Parent TGroup
- `nextView()` / `prevView()` - Navigate siblings
- `makeFirst()` - Bring to front
- `putInFrontOf(TView*)` - Z-order positioning

## Color/Palette System

Views inherit colors from parents via palette indices:
1. View calls `getColor(index)`
2. Index maps through view's palette
3. Cascades up to parent palettes
4. Final color written to screen

Supports: monochrome, 16-color, 256-color modes.

## Key Source Files for Porting

| File | Contains |
|------|----------|
| `tview.cpp` | TView implementation |
| `tgroup.cpp` | TGroup container logic |
| `twindow.cpp` | TWindow implementation |
| `tdialog.cpp` | TDialog implementation |
| `tprogram.cpp` | TProgram event loop |
| `tapplica.cpp` | TApplication init/shutdown |
| `tdeskto.cpp` | TDeskTop window management |
| `tmenu.cpp` | Menu rendering/handling |
| `tevent.cpp` | Event dispatch |
| `tbutton.cpp` | Button widget |
| `tinputln.cpp` | Input line widget |
| `tscrollb.cpp` | Scrollbar widget |
| `teditor.cpp` | Text editor engine |
| `drawbuf.cpp` | Drawing buffer ops |

## Platform Layer (`source/platform/`)

Platform abstraction for terminal I/O:
- `ncurdisp.cpp` / `ncursinp.cpp` - Linux ncurses
- `win32con.cpp` - Windows Console API
- `linuxcon.cpp` - Linux console direct
- `events.cpp` - Event loop management
- `colors.cpp` - Color quantization
- `utf8.cpp` - UTF-8 handling
- `clipboard.cpp` - System clipboard

## Porting Considerations

### C++ to C# Mappings

| C++ | C# Equivalent |
|-----|---------------|
| `ushort` | `ushort` |
| `uchar` | `byte` |
| `Boolean` | `bool` |
| `char*` | `string` or `ReadOnlySpan<char>` |
| `TStringView` | `ReadOnlySpan<char>` or `string` |
| Virtual methods | `virtual`/`override` |
| Multiple inheritance | Interfaces |
| Circular linked lists | Custom or `LinkedList<T>` |
| `union` in structs | Explicit layout or separate types |
| Pointer arithmetic | `Span<T>` or arrays with indices |

### Key Patterns to Preserve
1. **Event-driven architecture** - Keep command-based dispatch
2. **View hierarchy** - Maintain parent/child relationships
3. **Palette cascade** - Color inheritance system
4. **Modal execution** - `Execute()` blocks until `EndModal()`
5. **State flags** - Bitfield state management
6. **Data exchange** - `GetData()`/`SetData()` for serialization

### Platform Abstraction
Consider abstracting:
- Console I/O (Windows Console API vs. ANSI sequences)
- Clipboard access
- Mouse input
- Unicode/encoding handling
