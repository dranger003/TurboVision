# C# Coding Style

The core objective is to maintain a high level of consistency across the entire codebase by using easily recognizable patterns.

## Type Declarations

### Primary Constructors

```csharp
public readonly struct ColorBIOS(byte value) : IEquatable<ColorBIOS>
{
    public byte Value { get; } = (byte)(value & 0x0F);
    // ...
}
```

### Records

The `record` modifier provides built-in functionality for encapsulating data. Use `record class` (or just `record`) for reference types and `record struct` for value types.

```csharp
// Reference type (heap-allocated)
public record class Person(string Name, int Age);
public record Person(string Name, int Age);  // Equivalent shorthand

// Value type (stack-allocated)
public record struct Point(int X, int Y);
public readonly record struct ImmutablePoint(int X, int Y);
```

### Required Members

```csharp
public required string Title { get; init; }
```

### Ref Structs with Ref Fields

```csharp
public ref struct DrawContext(ref ScreenCell[] buffer);
```

### Inline Arrays

```csharp
[InlineArray(16)]
public struct CharBuffer { private char _element; }
```

## Initialization and Typing

### Target-Typed New and Collection Expressions

```csharp
List<View> views = [];
int[] bits = [0, 0, 0, 0, 0, 0, 0, 0];
ReadOnlySpan<char> chars = ['─', '│', '┌', '┐', '└', '┘'];
```

### Implicit Typing

The type should be visible exactly once—either on the left side (target-typed `new`) or the right side (`var`).

```csharp
// Target-typed new: type on left
List<string> items = new();
StringBuilder buffer = new();

// Implicit typing: type obvious from right
var items = new List<string>();
var stream = File.OpenRead(path);
var color = ColorBIOS.FromIndex(4);

// Redundant - avoid
List<string> items = new List<string>();

// Unclear - avoid
var result = GetValue();
```

## Strings and Literals

### Raw String Literals

```csharp
string frameChars = """
    ┌─┐
    │ │
    └─┘
    """;
```

### UTF-8 String Literals

```csharp
ReadOnlySpan<byte> ansiReset = "\x1b[0m"u8;
```

### Interpolated String Handlers

```csharp
public void Log($"Drawing at ({x}, {y})");
```

## Memory and Performance

### Span-Based APIs

```csharp
public void WriteCells(int x, int y, ReadOnlySpan<ScreenCell> cells);
```

### Native-Sized Integers

```csharp
nint inputHandle = GetStdHandle(-10);
```

## Pattern Matching

```csharp
// Null-conditional
owner?.Invalidate();

// List patterns
if (args is [var first, .., var last]) { }
```

## Generics

### Generic Math Interfaces

```csharp
public static T Clamp<T>(T value, T min, T max) where T : INumber<T>;
```

## Code Organization

### File-Scoped Namespaces

```csharp
namespace TurboVision.Core.Types;
```

## Style Rules

### Private Field Naming

Prefer the `field` contextual keyword over explicit backing fields. This keeps property logic self-contained without manual field declarations.

```csharp
// Correct: use 'field' keyword for backing storage
public string Title
{
    get { return field; }
    set { field = value ?? throw new ArgumentNullException(); }
}

public bool Visible
{
    get { return field; }
    set
    {
        if (field != value)
        {
            field = value;
            Invalidate();
        }
    }
}
```

When explicit private fields are truly necessary (cached computations, non-property-backing state, etc.), use `_` prefix to distinguish them from primary constructor parameters.

```csharp
// Correct
public class Window(int width, int height)
{
    private Size? _cachedSize;
    private bool _layoutPending;
}

// Incorrect
public class Window(int width, int height)
{
    private Size? cachedSize;
    private bool layoutPending;
}
```

### Avoid Expression-Bodied Members

Use block bodies for all members. Do not use `=>` syntax for properties, methods, or other members.

```csharp
// Correct
public int Width
{
    get { return width; }
}

public void Draw()
{
    Render();
}

// Incorrect
public int Width => width;
public void Draw() => Render();
```
