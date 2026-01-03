# TurboVision C# Port - Stubbed Structure

  Phase 1: Core Primitives (TurboVision/Core/)

  - TPoint.cs - 2D coordinate struct
  - TRect.cs - Rectangle struct with intersection/union
  - TColorAttr.cs - Foreground/background color attribute
  - TAttrPair.cs - Normal/highlight color pair
  - TPalette.cs - Color palette array
  - TScreenCell.cs - Single screen cell (char + attr)
  - TDrawBuffer.cs - Drawing buffer for views
  - TCommandSet.cs - Bitset for enabled commands

  Phase 2: Event System (TurboVision/Core/)

  - EventConstants.cs - Event type/mask constants
  - KeyConstants.cs - Key codes and TKey struct
  - CommandConstants.cs - Standard command values
  - MouseEvent.cs - Mouse event data
  - KeyDownEvent.cs - Keyboard event data
  - MessageEvent.cs - Command/broadcast message
  - TEvent.cs - Union event struct

  Phase 3: Platform (TurboVision/Platform/)

  - IScreenDriver.cs - Screen output interface
  - IEventSource.cs - Input event source interface
  - TDisplay.cs - Display mode constants
  - TScreen.cs - Static screen management
  - TEventQueue.cs - Event queue polling
  - THardwareInfo.cs - Platform detection

  Phase 4: View Hierarchy (TurboVision/Views/)

  - ViewConstants.cs - State/option/grow flags
  - TObject.cs - Base disposable object
  - TView.cs - Foundation view class
  - TGroup.cs - Container for child views
  - TFrame.cs - Window frame/title
  - TScrollBar.cs - Scrollbar widget
  - TScroller.cs - Scrollable content area
  - TBackground.cs - Desktop background pattern
  - TListViewer.cs - Abstract list display

  Phase 5: Application Framework (TurboVision/Application/)

  - TWindow.cs - Movable/resizable window
  - TDialog.cs - Modal dialog window
  - TDeskTop.cs - Window manager container
  - TProgram.cs - Application skeleton
  - TApplication.cs - Full application class

  Phase 6: Basic Controls (TurboVision/Dialogs/)

  - TStaticText.cs - Read-only label
  - TLabel.cs - Linked label control
  - TButton.cs - Push button
  - TInputLine.cs - Single-line text input
  - TSItem.cs - String item for clusters
  - TCluster.cs - Base for grouped controls
  - TRadioButtons.cs - Radio button group
  - TCheckBoxes.cs - Checkbox group
  - TListBox.cs - List box with strings
  - THistory.cs - Input history dropdown

  Phase 7: Menus (TurboVision/Menus/)

  - TMenuItem.cs - Menu item data
  - TSubMenu.cs - Submenu builder
  - TMenu.cs - Menu container
  - TMenuView.cs - Base menu view
  - TMenuBar.cs - Horizontal menu bar
  - TMenuBox.cs - Dropdown menu box
  - TMenuPopup.cs - Context popup menu
  - TStatusItem.cs - Status line item
  - TStatusDef.cs - Status definition
  - TStatusLine.cs - Status bar
