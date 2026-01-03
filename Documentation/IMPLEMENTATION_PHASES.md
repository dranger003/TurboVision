## Implementation Phases

### Phase 1: Core Primitives (Foundation)

These are value types and utilities with no dependencies—pure C# translations.

```
TurboVision/Core/
    TPoint.cs           // struct { X, Y } with operators
    TRect.cs            // struct { A, B : TPoint } with Move, Grow, Intersect, Union, Contains
    TColorAttr.cs       // Color attribute (foreground, background, style flags)
    TAttrPair.cs        // Pair of TColorAttr
    TPalette.cs         // Color palette array wrapper
    TDrawBuffer.cs      // Screen line buffer for drawing operations
    TScreenCell.cs      // Single cell: character + attributes
    TCommandSet.cs      // Bitset for enabled/disabled commands
    TStringView.cs      // ReadOnlySpan<char> equivalent (or just use it directly)
```

**Why first:** Everything else depends on these. They're self-contained and easy to validate.

---

### Phase 2: Event System

```
TurboVision/Core/
    TEvent.cs           // Union-like struct: What, KeyDown, Mouse, Message
    KeyDownEvent.cs     // KeyCode, ControlKeyState, Text
    MouseEvent.cs       // Where, Buttons, EventFlags, Wheel
    MessageEvent.cs     // Command, InfoPtr
    EventConstants.cs   // evNothing, evKeyDown, evMouseDown, evCommand, evBroadcast...
    KeyConstants.cs     // kbEnter, kbEsc, kbTab, kbAltX, etc.
    CommandConstants.cs // cmQuit, cmClose, cmZoom, cmResize, cmMenu...
```

**Why second:** Views need events to function. The event structure is fundamental to the message-passing architecture.

---

### Phase 3: Platform Abstraction (Windows-only initially)

```
TurboVision/Platform/
    IScreenDriver.cs        // Interface for screen operations
    IEventSource.cs         // Interface for input events
    Win32ConsoleDriver.cs   // Windows Console API implementation
    TScreen.cs              // Static screen state, mode, size
    TDisplay.cs             // Display capabilities
    THardwareInfo.cs        // Platform detection
    TEventQueue.cs          // Event polling, waitForEvents
```

**Why third:** You need a way to draw and receive input before you can test views. This is where `kernel32.dll` P/Invoke lives for Windows console access.

Key Win32 APIs you'll wrap:
- `WriteConsoleOutput` / `WriteConsoleOutputW`
- `ReadConsoleInput`
- `SetConsoleCursorPosition`
- `GetConsoleScreenBufferInfo`
- `SetConsoleMode`

---

### Phase 4: View Hierarchy Core

```
TurboVision/Views/
    TObject.cs          // Base class (or skip if not using streaming)
    TView.cs            // THE foundation view class
    TGroup.cs           // Container that owns child views
    TFrame.cs           // Window border drawing
    TScrollBar.cs       // Standalone scrollbar
    TScroller.cs        // Scrollable content area
    TBackground.cs      // Desktop background pattern
```

**TView is the heart.** Port it carefully. Key members:
- `Origin`, `Size`, `Cursor`, `GrowMode`, `DragMode`, `Options`, `EventMask`, `State`
- `Owner` (parent TGroup)
- `Draw()`, `HandleEvent()`, `SetState()`, `GetPalette()`, `MapColor()`
- `WriteBuf()`, `WriteChar()`, `WriteStr()`, `WriteLine()`

**TGroup** adds:
- `Last`, `Current`, `Phase` (focus chain as circular linked list)
- `Insert()`, `Delete()`, `ForEach()`, `Execute()`, `ExecView()`
- Event routing to subviews

---

### Phase 5: Application Framework

```
TurboVision/Application/
    TProgram.cs         // Application skeleton, event loop, initXxx pattern
    TApplication.cs     // Adds menu bar, status line, desktop
    TDeskTop.cs         // Container for windows
    TWindow.cs          // Movable, resizable window with frame
    TDialog.cs          // Modal dialog (TWindow + modal behavior)
```

At this point you can run a minimal app:

```csharp
var app = new TApplication();
app.Run();
```

---

### Phase 6: Basic Controls

```
TurboVision/Dialogs/
    TStaticText.cs      // Label
    TButton.cs          // Clickable button
    TInputLine.cs       // Single-line text input
    TLabel.cs           // Label linked to another control
    THistory.cs         // Input history dropdown
    TCluster.cs         // Base for checkbox/radio groups
    TCheckBoxes.cs
    TRadioButtons.cs
    TListViewer.cs      // Abstract list base
    TListBox.cs         // Concrete list with TCollection
```

---

### Phase 7: Menus

```
TurboVision/Menus/
    TMenuItem.cs        // Menu item data structure
    TMenu.cs            // Menu container
    TMenuView.cs        // Base menu view
    TMenuBar.cs         // Top menu bar
    TMenuBox.cs         // Dropdown menu
    TMenuPopup.cs       // Context menu
    TStatusLine.cs      // Bottom status bar with hints
    TStatusDef.cs       // Status line definitions
    TStatusItem.cs      // Individual status items
```

---

### Phase 8: Editor (Later)

```
TurboVision/Editor/
    TIndicator.cs
    TEditor.cs
    TMemo.cs
    TFileEditor.cs
```

This is complex and can be deferred. The core framework works without it.

---

## Dependency Graph (Simplified)

```
Phase 1: Core Primitives
    ↓
Phase 2: Event System
    ↓
Phase 3: Platform (Windows Console)
    ↓
Phase 4: TView → TGroup → TFrame, TScrollBar, TBackground
    ↓
Phase 5: TProgram → TApplication → TDeskTop, TWindow, TDialog
    ↓
Phase 6: Controls (TButton, TInputLine, TListBox, etc.)
    ↓
Phase 7: Menus (TMenuBar, TStatusLine)
    ↓
Phase 8: TEditor
```

---

## First Milestone Target

Get this running:

```csharp
public class HelloApp : TApplication
{
    public override TMenuBar? InitMenuBar(TRect r)
    {
        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r,
            new TSubMenu("~F~ile", KeyCode.AltF,
                new TMenuItem("~Q~uit", Command.Quit, KeyCode.AltX)));
    }
}

var app = new HelloApp();
app.Run();
```

That requires: Phases 1–5 plus the menu classes from Phase 7.

---

## Practical Suggestion

Start by stubbing the interfaces and class shells, then implement them bottom-up. Port the `hello.cpp` example as your test harness—it exercises the minimal path through the framework.

The upstream `source/tvision/` directory maps almost 1:1 to what `/TurboVision` folder will contain. Keep your `Reference/tvision` submodule handy for side-by-side comparison during porting.
