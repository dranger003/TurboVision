# BCL Mapping Reference

Guide for deciding when to use .NET BCL types vs. porting upstream C++ implementations.

> **Note:** All C# types and patterns below follow `Documentation/CODING_STYLE.md` conventions.

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
| File operations | `FileStream`, `Path` | (Phase 8) |
| Error codes | Exceptions | Various |

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
