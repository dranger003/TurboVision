# BCL Mapping Reference

Guide for deciding when to use .NET BCL types vs. porting upstream C++ implementations.

> **Note:** All C# types and patterns below follow `Documentation/CODING_STYLE.md` conventions.

## Implementation Status

| Category | Status | Notes |
|----------|--------|-------|
| Primitives | ✓ Complete | All type mappings implemented |
| Collections | ✓ Complete | Generic collections with TV API |
| String/Text | ✓ Complete | UTF-8 via System.Text.Encoding |
| Platform/System | ◐ Partial | Windows complete, cross-platform pending |
| File Operations | ✓ Complete | All file dialogs implemented |
| Editor Module | ✓ Complete | Gap buffer, clipboard, undo |
| Collections Framework | ✓ Complete | TNSCollection, TSortedCollection |
| Serialization | ✓ Complete | JSON-native with System.Text.Json |
| Advanced Features | ◐ Partial | Outline views not started |
| Cross-Platform | ○ Not Started | ncurses, ANSI drivers pending |

## Decision Criteria

| Use BCL When | Port Upstream When |
|--------------|-------------------|
| BCL has equivalent functionality | BCL semantics differ from TV API |
| Upstream uses unsafe/platform code | Algorithm is TV-specific |
| C# idiom exists | Performance characteristics critical |
| Type safety improves over void* | Binary compatibility required |

## Type Mappings

### Primitives

| C++ | C# | Notes |
|-----|-----|-------|
| `ushort` | `ushort` | Direct mapping |
| `uchar` | `byte` | |
| `Boolean` | `bool` | |
| `char*` | `string` | Immutable, GC-managed |
| `char*` (buffer) | `ReadOnlySpan<char>` | When no allocation needed |
| `void*` | `object?` or generic `T` | Prefer generics |
| `size_t` | `nint` | Native-sized integer |

### Collections

| Upstream | BCL | Files Using |
|----------|-----|-------------|
| Pointer arrays | `List<T>` | TListBox.cs |
| Global pointer maps | `Dictionary<K,V>` | THistoryList.cs |
| Circular linked lists | `List<T>` with modulo | TGroup.cs |
| Fixed-size arrays | `Span<T>`, `stackalloc` | Various |
| Bitfields | `byte[]` with bitwise ops | TCommandSet.cs |

### String/Text

| Upstream | BCL | Files Using |
|----------|-----|-------------|
| Manual char* encoding | `Encoding.UTF8` | TInputLine.cs:318,680 |
| `cstrlen()` | `TStringUtils.CstrLen()` | KeyConstants.cs:24 |
| `strwidth()` | `TStringUtils.StrWidth()` | KeyConstants.cs:41 |
| String formatting | `string.Format`, interpolation | Various |

### Platform/System

| Upstream | BCL | Files Using |
|----------|-----|-------------|
| Platform timers | `Stopwatch` | TTimerQueue.cs:32 |
| File operations | `FileStream`, `Path` | See File Operations section below |
| Error codes | Exceptions | Various |

### File Operations ✓ IMPLEMENTED

Comprehensive mappings for file dialogs, directory dialogs, and path utilities.
**Implementation:** `TurboVision/Dialogs/` - TFileDialog.cs, TChDirDialog.cs, PathUtils.cs, etc.

#### Core File Types

| C++ Type | BCL Mapping | Notes |
|----------|-------------|-------|
| `TSearchRec` | `FileInfo` + custom struct | Holds attr, time, size, name |
| `ffblk` / `find_t` | `FileInfo` | DOS find-first structures |
| `TFileCollection` | `List<FileInfo>` | Sorted with custom comparator |
| `TDirCollection` | `List<DirectoryInfo>` | Directory entry collection |
| `TDirEntry` | `DirectoryInfo` wrapper | Display text + path |

#### File Attribute Constants

| C++ Constant | BCL Mapping |
|--------------|-------------|
| `FA_RDONLY` | `FileAttributes.ReadOnly` |
| `FA_HIDDEN` | `FileAttributes.Hidden` |
| `FA_SYSTEM` | `FileAttributes.System` |
| `FA_DIREC` | `FileAttributes.Directory` |
| `FA_ARCH` | `FileAttributes.Archive` |

#### Path Utility Functions

| C++ Function | BCL Mapping |
|--------------|-------------|
| `fnsplit()` | `Path.GetDirectoryName()`, `Path.GetFileName()`, `Path.GetExtension()` |
| `fnmerge()` | `Path.Combine()` |
| `fexpand()` | `Path.GetFullPath()` |
| `squeeze()` | `Path.GetFullPath()` (implicit normalization) |
| `driveValid()` | `DriveInfo.GetDrives()` + validation |
| `isDir()` | `Directory.Exists()` |
| `pathValid()` | `Directory.Exists()` |
| `validFileName()` | `Path.GetInvalidFileNameChars()` check |
| `getCurDir()` | `Directory.GetCurrentDirectory()` |
| `isWild()` | `path.Contains('*') \|\| path.Contains('?')` |
| `getHomeDir()` | `Environment.GetFolderPath(SpecialFolder.UserProfile)` |

#### File Enumeration

| C++ Pattern | BCL Mapping |
|-------------|-------------|
| `findfirst()` / `findnext()` | `Directory.EnumerateFiles()` / `EnumerateDirectories()` |
| Directory walk with ".." | `DirectoryInfo.Parent` + `EnumerateDirectories()` |
| Attribute filtering | `FileInfo.Attributes.HasFlag()` |

#### Cross-Platform Path Handling

| C++ Function | BCL Mapping | Notes |
|--------------|-------------|-------|
| `path_dos2unix()` | `path.Replace('\\', '/')` | Rarely needed |
| `path_unix2dos()` | `path.Replace('/', '\\')` | Rarely needed |
| `isDriveLetter()` | `char.IsLetter()` | |

### Editor Module ✓ IMPLEMENTED

Mappings for TEditor, TMemo, TFileEditor, and related text editing components.
**Implementation:** `TurboVision/Editors/` - TEditor.cs (1689 lines), TMemo.cs, TFileEditor.cs, etc.

#### Text Buffer Management

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `char *buffer` (gap buffer) | `StringBuilder` or custom `GapBuffer<char>` | StringBuilder uses similar internals |
| `uint bufSize` | `StringBuilder.Capacity` | Auto-managed allocation |
| `uint bufLen` | `StringBuilder.Length` | Current content size |
| `bufPtr(uint P)` | `StringBuilder[int]` indexer | Gap-aware access |

#### Selection & Clipboard

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `uint selStart, selEnd` | `(int, int)` tuple or `Range` | Selection range |
| `TEditor *clipboard` | `System.Windows.Forms.Clipboard` or `TextCopy` | Cross-platform clipboard |
| `clipCopy()` / `clipCut()` / `clipPaste()` | `Clipboard.SetText()` / `GetText()` | System clipboard ops |

#### Undo/Redo

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `delCount`, `insCount` tracking | `Stack<EditAction>` | Command pattern |
| `undo()` function | `UndoManager.Undo()` | Stack-based undo |
| Lock/unlock batching | `IDisposable` pattern | Group edits into single undo |

#### Text Encoding & EOL

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `enum Encoding` | `System.Text.Encoding` | `UTF8`, `GetEncoding(850)` for DOS |
| `enum EolType` | Custom enum | `CRLF`, `LF`, `CR` detection |
| `detectEol()` | `string.Contains("\r\n")` checks | Auto-detect line endings |

#### Line & Column Tracking

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TPoint curPos` | `(int line, int column)` tuple | Cursor position |
| `uint curPtr` | `int` | Linear buffer offset |
| `lineStart()` / `lineEnd()` | `string.LastIndexOf('\n')` / `IndexOf('\n')` | Line boundaries |
| `nextWord()` / `prevWord()` | `char.IsWhiteSpace()` + scanner | Word boundary detection |

#### Search & Replace

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `search()` function | `string.IndexOf()` or `Regex.Match()` | Built-in search |
| `efCaseSensitive` | `StringComparison.Ordinal` vs `OrdinalIgnoreCase` | Case handling |
| `efWholeWordsOnly` | `Regex` with `\b` | Word boundary matching |
| `efReplaceAll` | `string.Replace()` or `Regex.Replace()` | Bulk replacement |

#### File I/O

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `char fileName[MAXPATH]` | `string` | Unlimited length |
| `ifstream` / `ofstream` | `FileStream` or `File.ReadAllBytes()` | Binary I/O |
| `loadFile()` / `saveFile()` | `File.ReadAllBytes()` / `WriteAllBytes()` | Bulk file ops |

#### Terminal Output (TTerminal)

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TTextDevice` | Stream-like interface | Abstract output |
| Circular buffer | Custom `RingBuffer<char>` | Fixed-size wrap |
| `queFront`, `queBack` | `int headIndex, tailIndex` | Ring buffer pointers |

### Collections Framework ✓ IMPLEMENTED

Mappings for TNSCollection, TSortedCollection, TStringCollection, and resource management.
**Implementation:** `TurboVision/Collections/` - TNSCollection.cs, TSortedCollection.cs, TStringCollection.cs, etc.

#### Core Collection Types

| C++ Type | BCL Mapping | Notes |
|----------|-------------|-------|
| `TNSCollection` | `List<T>` | Dynamic array, generic |
| `TNSSortedCollection` | `SortedSet<T>` or `SortedList<K,V>` | Binary search, O(log n) |
| `TCollection` | `List<T>` + `IStreamable` | Serializable wrapper |
| `TSortedCollection` | `SortedSet<T>` + `IStreamable` | Serializable sorted |
| `TStringCollection` | `SortedSet<string>` | String storage |

#### Collection Methods

| C++ Method | BCL Mapping | Notes |
|------------|-------------|-------|
| `at(index)` | `List<T>[index]` | Direct indexing |
| `indexOf(item)` | `List<T>.IndexOf()` | Linear search |
| `insert(item)` | `List<T>.Add()` or `Insert()` | Append/insert |
| `freeAll()` | `List<T>.Clear()` + GC | Dispose pattern |
| `pack()` | `List<T>.RemoveAll(x => x == null)` | Compaction |
| `search(key, index)` | `SortedSet<T>.TryGetValue()` | Binary search |
| `compare()` virtual | `IComparer<T>` interface | Custom ordering |

#### Resource Management

| C++ Type | BCL Mapping | Notes |
|----------|-------------|-------|
| `TResourceItem` | `struct { string key; int pos; int size; }` | Resource metadata |
| `TResourceCollection` | `SortedDictionary<string, ResourceEntry>` | Resource index |
| `TResourceFile` | Custom file-based manager | Resource persistence |
| `TStringList` | `ImmutableDictionary<ushort, string>` | Read-only string table |
| `TStrListMaker` | `Dictionary<ushort, string>` builder | String table builder |

### Platform Completeness ◐ PARTIAL

Mappings for screen capabilities, damage tracking, clipboard, and Unicode handling.
**Implementation:** `TurboVision/Platform/` - Win32ConsoleDriver.cs works; damage tracking and cross-platform pending.

#### Screen Capability Detection

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `GetConsoleMode()` / `SetConsoleMode()` | P/Invoke | Windows API |
| `ENABLE_VIRTUAL_TERMINAL_PROCESSING` | `0x0004` constant | ANSI support detection |
| Wine detection | P/Invoke `wine_get_version` | Runtime detection |
| Color mode capability | Cached bool property | Terminal ANSI check |

#### Damage Tracking

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `std::vector<DamageRange>` | `List<(int min, int max)>` | Per-row dirty rectangles |
| `WINDOW_BUFFER_SIZE_RECORD` | Console event polling | Size change detection |

#### Clipboard Integration

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| Windows clipboard | `System.Windows.Forms.Clipboard` | Add WinForms ref |
| Unix clipboard detection | `Environment.GetEnvironmentVariable()` | Check `WAYLAND_DISPLAY`, `DISPLAY` |
| Process spawning | `System.Diagnostics.Process` | `xclip`, `wl-copy` subprocess |
| Pipe communication | `StreamReader` / `StreamWriter` | Redirected I/O |

#### UTF-16 Surrogate Handling

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| Manual surrogate detection | `System.Char.IsSurrogatePair()` | Built-in check |
| UTF-16 → UTF-8 | `System.Text.Encoding.UTF8.GetBytes()` | Auto handles surrogates |
| Codepoint reconstruction | `System.Text.Rune(char, char)` | `new Rune(lead, trail)` |
| Width calculation | `System.Globalization.StringInfo` | Grapheme clusters |

### Serialization ✓ IMPLEMENTED

Mappings for TStreamable, pstream hierarchy, and object persistence.
**Implementation:** `TurboVision/Streaming/` - JsonStreamSerializer.cs, custom converters, 30+ view types supported.
**Design Decision:** JSON-native approach using System.Text.Json (not binary format compatibility).

#### Stream Class Hierarchy

| C++ Class | BCL Mapping | Notes |
|-----------|-------------|-------|
| `pstream` | Abstract `IStream` interface | Base state machine |
| `ipstream` | `BinaryReader` | Input stream |
| `opstream` | `BinaryWriter` | Output stream |
| `iopstream` | `BinaryReader` + `BinaryWriter` | Bidirectional |
| `ifpstream` | `BinaryReader(FileStream)` | File input |
| `ofpstream` | `BinaryWriter(FileStream)` | File output |
| `fpstream` | Dual reader/writer on `FileStream` | File bidirectional |

#### TStreamable Interface

| C++ Concept | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TStreamable` base | `interface IStreamable` | Serialization contract |
| `streamableName()` | `string TypeId` property | Type identifier |
| `write(opstream&)` | `void Write(IStreamWriter)` | Serialize state |
| `read(ipstream&)` | `void Read(IStreamReader)` | Deserialize state |
| `RegisterType()` macro | `IStreamSerializer.Register<T>()` | Type registration |

#### Type Registry & Object Tracking

| C++ Component | BCL Mapping | Notes |
|---------------|-------------|-------|
| `TStreamableTypes` | `Dictionary<string, Type>` | Type name → factory |
| `TStreamableClass` | `TypeMetadata` record | Name + builder |
| `TPWrittenObjects` | `Dictionary<nint, uint>` | Output object tracking |
| `TPReadObjects` | `List<object>` | Input object tracking |

#### Primitive Serialization

| C++ Type | BCL Method | Bytes |
|----------|------------|-------|
| `uchar` | `BinaryWriter.Write(byte)` | 1 |
| `ushort` / `short` | `BinaryWriter.Write(ushort)` | 2 |
| `int` / `uint` | `BinaryWriter.Write(int)` | 4 |
| `long` / `ulong` | `BinaryWriter.Write(long)` | 8 |
| `char*` string | Length-prefixed UTF-8 | 1 + len |

#### Pointer Serialization

| Pointer Type | Format | Notes |
|--------------|--------|-------|
| Null | `[0]` discriminator | Null pointer |
| Object | `[2][name][data][']'` | Inline new object |
| Indexed | `[1][id as ushort]` | Reference existing |

### Advanced Features ◐ PARTIAL

Mappings for outline views, color selector, and help system.

**Color Selector:** ✓ IMPLEMENTED - `TurboVision/Colors/` (TColorDialog.cs, TColorSelector.cs, etc.)
**Help System:** ✓ IMPLEMENTED - `TurboVision/Help/` (THelpFile.cs, THelpViewer.cs, etc.)
**Outline Views:** ○ NOT STARTED - TNode, TOutline, TOutlineViewer need implementation

#### Outline Views (TOutline)

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TNode` linked list | Generic `TreeNode<T>` class | Next/child/parent pointers |
| Tree traversal callbacks | `Action<TreeNode>` delegates | Visitor pattern |
| Recursive descent | `yield return` | LINQ-friendly iteration |
| Tree drawing flags | `[Flags] enum` | `ovExpanded`, `ovChildren`, `ovLast` |
| Line/level bit vectors | `long` or `BitArray` | Graph drawing state |
| Graph string generation | `StringBuilder` | Tree branch chars (│├└─) |

#### Color Selector (TColorDialog)

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TColorItem` linked list | `List<ColorItem>` | Simpler as list |
| `TColorGroup` grouping | Nested `List<ColorGroup>` | Hierarchical model |
| Color grid (4x4) | 2D array or `byte[16]` | Mouse → color index |
| Palette serialization | `BinaryWriter` / `BinaryReader` | Flatten to binary |

#### Help System (THelpFile)

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `TParagraph` linked list | `List<Paragraph>` | Text wrapping at render |
| `TCrossRef` array | `List<CrossRef>` | Offset + length + topic ID |
| Help file format | `BinaryReader` / `BinaryWriter` | Custom binary (not .NET) |
| Topic indexing | `Dictionary<int, long>` | Context ID → file offset |
| Lazy loading | `Stream.Seek()` | Read topic on demand |
| Topic navigation | `Stack<int>` | History for back button |

### Cross-Platform ○ NOT STARTED

Mappings for Linux/ncurses and ANSI terminal drivers.
**Status:** Windows driver (Win32ConsoleDriver.cs) works; Linux/ncurses and ANSI drivers not yet implemented.

#### ncurses Driver

| ncurses API | BCL Mapping | Notes |
|-------------|-------------|-------|
| `newterm()` | P/Invoke wrapper | Create terminal |
| `SCREEN *` | `nint` | Opaque handle |
| `has_colors()` | P/Invoke `tigetnum("colors")` | Color capability |
| `start_color()` | ANSI sequences fallback | `\x1b[39m` default |
| `resize_term()` | SIGWINCH handler or polling | Size changes |
| `curs_set()` | VT sequences | `\x1b[?25h/l` |

#### ANSI Terminal Driver

| ANSI Sequence | Purpose | Example |
|---------------|---------|---------|
| `\x1b[{r};{c}H` | Cursor position | `\x1b[10;5H` |
| `\x1b[2J` | Clear screen | With `\x1b[H` for home |
| `\x1b[K` | Clear to EOL | Erase rest of line |
| `\x1b[?25h` / `\x1b[?25l` | Show/hide cursor | Visibility control |
| `\x1b[{fg};{bg}m` | Set colors | `\x1b[31;44m` red on blue |
| `\x1b[0m` | Reset attributes | Default colors |

#### Platform Abstraction

| C++ Pattern | BCL Mapping | Notes |
|-------------|-------------|-------|
| `Platform` singleton | `IPlatformDriver` interface | Abstract factory |
| `Win32ConsoleAdapter` | `Win32ConsoleDriver` | P/Invoke kernel32 |
| `NcursesDisplay` | `NcursesDriver` | P/Invoke libncurses |
| `AnsiTerminalDriver` | Direct escape sequences | `System.Console` I/O |
| Platform detection | `RuntimeInformation.IsOSPlatform()` | OS-specific driver |

## Extracted Decisions

Existing C++ → C# decisions found in the codebase (via "upstream" comments):

### Views

| File | Decision | Notes |
|------|----------|-------|
| TGroup.cs:352 | Direct draw without buffer | Matches upstream exactly |
| TVWrite.cs:8 | TVWrite ref struct | Matches upstream tvwrite.cpp structure |
| TVWrite.cs:47 | L0 entry point | Matches upstream label naming |
| TVWrite.cs:75 | L10 coordinate transform | Matches upstream label |
| TVWrite.cs:104-107 | L20 occlusion check | Uses goto to match upstream do-while(0) idiom |
| TVWrite.cs:214 | L30 split at boundary | Matches upstream label |
| TVWrite.cs:245 | L40 buffer write | Matches upstream label |
| TVWrite.cs:265 | L50 shadow application | Matches upstream label |
| TVWrite.cs:311,320 | applyShadow | Matches upstream BIOS color handling |
| TScrollBar.cs:12 | Character sets | Matches upstream tvtext1.cpp |
| TListViewer.cs:12 | Special characters | Matches upstream specialChars |

### Core

| File | Decision | Notes |
|------|----------|-------|
| TColorAttr.cs:22 | 64-bit color storage | Matches upstream TColorAttr struct |
| TColorAttr.cs:223 | reverseAttribute() | Matches upstream function |
| TColorDesired.cs:75 | 32-bit color union | Matches upstream colors.h |
| TColorDesired.cs:202 | ColorConversion | Matches upstream colors.h functions |
| KeyConstants.cs:24 | cstrlen() → CstrLen | Upstream equivalent |
| KeyConstants.cs:41 | strwidth() → StrWidth | Upstream equivalent |

### Dialogs

| File | Decision | Notes |
|------|----------|-------|
| TInputLine.cs:318,680 | UTF8 encoding | Replaces manual byte buffers |
| TListBox.cs:75 | TListBoxRec limitation | Can't restore items pointer |
| THistory.cs:15 | Icon characters | Matches upstream tvtext1.cpp |
| MsgBox.cs:283 | strwidth calculation | Matches upstream |
| TPXPictureValidator.cs:138 | No truncation | Matches upstream behavior |

### Application/Menus

| File | Decision | Notes |
|------|----------|-------|
| TProgram.cs:190 | viewHasMouse() | Matches upstream function |
| TMenuView.cs:336 | Unhandled event passing | Matches upstream behavior |

## Adding New Mappings

When porting new code, add entries to this document:

1. Check if BCL provides equivalent functionality
2. If using BCL, add to appropriate table above
3. If porting upstream, add comment with `// Matches upstream [function/file]`
4. For complex decisions, document rationale inline

---

*Tracking commit: 0707c8f*
