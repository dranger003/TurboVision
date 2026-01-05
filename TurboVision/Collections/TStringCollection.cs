namespace TurboVision.Collections;

using System.Text;

/// <summary>
/// Sorted collection of strings.
/// Maintains strings in alphabetical order.
/// </summary>
/// <remarks>
/// Matches upstream TStringCollection from resource.h/tstrcoll.cpp.
/// Uses string type instead of char* for memory safety.
/// </remarks>
public class TStringCollection : TSortedCollection<StringWrapper, string>
{
    /// <summary>
    /// Type name for streaming identification.
    /// Matches upstream name constant.
    /// </summary>
    public new const string TypeName = "TStringCollection";

    /// <summary>
    /// Creates a new string collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TStringCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
    }

    /// <summary>
    /// Creates an empty string collection.
    /// Used for streaming initialization.
    /// </summary>
    protected TStringCollection() : base()
    {
    }

    /// <summary>
    /// Gets the streamable type name.
    /// </summary>
    public override string StreamableName
    {
        get { return TypeName; }
    }

    /// <summary>
    /// Compares two string keys.
    /// Matches upstream compare() using strcmp semantics.
    /// </summary>
    /// <param name="key1">First key.</param>
    /// <param name="key2">Second key.</param>
    /// <returns>Negative if key1 &lt; key2, 0 if equal, positive if key1 &gt; key2.</returns>
    protected override int Compare(string key1, string key2)
    {
        return string.Compare(key1, key2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the key for an item (the string itself).
    /// Matches upstream keyOf().
    /// </summary>
    /// <param name="item">The string wrapper item.</param>
    /// <returns>The string value as key.</returns>
    public override string KeyOf(StringWrapper? item)
    {
        return item?.Value ?? string.Empty;
    }

    /// <summary>
    /// Frees a string item.
    /// In C#, strings are GC-managed so this is a no-op.
    /// Matches upstream freeItem().
    /// </summary>
    /// <param name="item">The item to free.</param>
    protected override void FreeItem(StringWrapper? item)
    {
        // Strings are GC-managed in C#, no explicit cleanup needed
    }

    /// <summary>
    /// Writes a string item to the stream.
    /// Matches upstream writeItem().
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <param name="writer">The binary writer.</param>
    protected override void WriteItem(StringWrapper? item, BinaryWriter writer)
    {
        string value = item?.Value ?? string.Empty;
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>
    /// Reads a string item from the stream.
    /// Matches upstream readItem().
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The string wrapper read.</returns>
    protected override StringWrapper? ReadItem(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        string value = Encoding.UTF8.GetString(bytes);
        return new StringWrapper(value);
    }

    /// <summary>
    /// Inserts a string directly into the collection.
    /// Convenience method.
    /// </summary>
    /// <param name="value">The string to insert.</param>
    /// <returns>The index where the string was inserted.</returns>
    public int Insert(string value)
    {
        // Explicitly call base to avoid ambiguity with implicit conversions
        return base.Insert(new StringWrapper(value));
    }

    /// <summary>
    /// Searches for a string by value.
    /// Convenience method.
    /// </summary>
    /// <param name="value">The string to search for.</param>
    /// <param name="index">Output: the index where found or should be inserted.</param>
    /// <returns>True if found.</returns>
    public new bool Search(string value, out int index)
    {
        return base.Search(value, out index);
    }

    /// <summary>
    /// Gets the string at the specified index.
    /// Convenience method.
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <returns>The string at the index.</returns>
    public string? GetString(int index)
    {
        return At(index)?.Value;
    }
}

/// <summary>
/// Wrapper class for strings to allow use in generic collections.
/// Required because C# generics require reference types for the base class constraint.
/// </summary>
public class StringWrapper
{
    /// <summary>
    /// The wrapped string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new string wrapper.
    /// </summary>
    /// <param name="value">The string to wrap.</param>
    public StringWrapper(string value)
    {
        Value = value ?? string.Empty;
    }

    /// <summary>
    /// Explicit conversion from StringWrapper to string.
    /// Use the Value property for implicit access.
    /// </summary>
    public static explicit operator string(StringWrapper wrapper)
    {
        return wrapper.Value;
    }

    /// <summary>
    /// Explicit conversion from string to StringWrapper.
    /// Use the constructor for explicit creation.
    /// </summary>
    public static explicit operator StringWrapper(string value)
    {
        return new StringWrapper(value);
    }

    /// <summary>
    /// Returns the wrapped string value.
    /// </summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Checks equality based on string value.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is StringWrapper other)
        {
            return Value == other.Value;
        }
        if (obj is string str)
        {
            return Value == str;
        }
        return false;
    }

    /// <summary>
    /// Gets hash code based on string value.
    /// </summary>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
