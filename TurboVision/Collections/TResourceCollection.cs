namespace TurboVision.Collections;

using System.Text;

/// <summary>
/// Resource index entry containing position, size, and key.
/// </summary>
/// <remarks>
/// Matches upstream TResourceItem from resource.h.
/// </remarks>
public class TResourceItem
{
    /// <summary>
    /// Position of the resource data in the file.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Size of the resource data in bytes.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Key (name) identifying the resource.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Creates a new resource item.
    /// </summary>
    public TResourceItem()
    {
        Position = 0;
        Size = 0;
        Key = string.Empty;
    }

    /// <summary>
    /// Creates a new resource item with specified values.
    /// </summary>
    public TResourceItem(string key, int position, int size)
    {
        Key = key ?? string.Empty;
        Position = position;
        Size = size;
    }
}

/// <summary>
/// Collection of resource items sorted by key.
/// Provides the index for a resource file.
/// </summary>
/// <remarks>
/// Matches upstream TResourceCollection from resource.h/trescoll.cpp.
/// </remarks>
public class TResourceCollection : TSortedCollection<TResourceItem, string>
{
    /// <summary>
    /// Type name for streaming identification.
    /// Matches upstream name constant.
    /// </summary>
    public new const string TypeName = "TResourceCollection";

    /// <summary>
    /// Creates a new resource collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TResourceCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
    }

    /// <summary>
    /// Creates an empty resource collection.
    /// Used for streaming initialization.
    /// </summary>
    protected TResourceCollection() : base()
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
    /// Gets the key for a resource item.
    /// Matches upstream keyOf().
    /// </summary>
    /// <param name="item">The resource item.</param>
    /// <returns>The resource key.</returns>
    public override string KeyOf(TResourceItem? item)
    {
        return item?.Key ?? string.Empty;
    }

    /// <summary>
    /// Frees a resource item.
    /// Matches upstream freeItem().
    /// </summary>
    /// <param name="item">The item to free.</param>
    protected override void FreeItem(TResourceItem? item)
    {
        // Items are GC-managed in C#
    }

    /// <summary>
    /// Writes a resource item to the stream.
    /// Matches upstream writeItem().
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <param name="writer">The binary writer.</param>
    protected override void WriteItem(TResourceItem? item, BinaryWriter writer)
    {
        if (item == null)
        {
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            return;
        }

        writer.Write(item.Position);
        writer.Write(item.Size);
        byte[] keyBytes = Encoding.UTF8.GetBytes(item.Key);
        writer.Write(keyBytes.Length);
        writer.Write(keyBytes);
    }

    /// <summary>
    /// Reads a resource item from the stream.
    /// Matches upstream readItem().
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The resource item read.</returns>
    protected override TResourceItem? ReadItem(BinaryReader reader)
    {
        var item = new TResourceItem
        {
            Position = reader.ReadInt32(),
            Size = reader.ReadInt32()
        };
        int keyLength = reader.ReadInt32();
        byte[] keyBytes = reader.ReadBytes(keyLength);
        item.Key = Encoding.UTF8.GetString(keyBytes);
        return item;
    }

    /// <summary>
    /// Searches for a resource by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="index">Output: the index where found or should be inserted.</param>
    /// <returns>True if found.</returns>
    public new bool Search(string key, out int index)
    {
        return base.Search(key, out index);
    }
}
