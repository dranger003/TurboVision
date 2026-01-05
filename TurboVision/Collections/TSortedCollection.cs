namespace TurboVision.Collections;

/// <summary>
/// Streamable sorted collection with binary search.
/// Combines TCollection streaming with TNSSortedCollection sorting.
/// </summary>
/// <remarks>
/// Matches upstream TSortedCollection from objects.h/tsortcol.cpp.
/// Uses multiple inheritance simulation via composition.
/// </remarks>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <typeparam name="TKey">The type of key used for comparison.</typeparam>
public abstract class TSortedCollection<T, TKey> : TCollection<T> where T : class
{
    /// <summary>
    /// Type name for streaming identification.
    /// Matches upstream name constant.
    /// </summary>
    public new const string TypeName = "TSortedCollection";

    /// <summary>
    /// Whether duplicate keys are allowed.
    /// Matches upstream duplicates field.
    /// </summary>
    public bool Duplicates { get; set; }

    /// <summary>
    /// Creates a new sorted collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TSortedCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
        Duplicates = false;
    }

    /// <summary>
    /// Creates an empty sorted collection.
    /// Used for streaming initialization.
    /// </summary>
    protected TSortedCollection() : base()
    {
        Duplicates = false;
    }

    /// <summary>
    /// Gets the streamable type name.
    /// </summary>
    public override string StreamableName
    {
        get { return TypeName; }
    }

    /// <summary>
    /// Gets the index of an item in the collection.
    /// Matches upstream indexOf() for sorted collections.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>The index, or NotFound if not found.</returns>
    public override int IndexOf(T? item)
    {
        if (item == null)
        {
            return NotFound;
        }

        if (!Search(KeyOf(item), out int i))
        {
            return NotFound;
        }

        if (Duplicates)
        {
            // When duplicates allowed, scan forward to find the exact item
            while (i < Count && !ReferenceEquals(item, Items[i]))
            {
                i++;
            }
        }

        if (i < Count && ReferenceEquals(item, Items[i]))
        {
            return i;
        }

        return NotFound;
    }

    /// <summary>
    /// Inserts an item in sorted order.
    /// Matches upstream insert() for sorted collections.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <returns>The index where the item was inserted.</returns>
    public override int Insert(T? item)
    {
        if (item == null)
        {
            return 0;
        }

        // Search first to find correct position
        bool found = Search(KeyOf(item), out int i);

        // Insert if not found, or if duplicates are allowed
        if (!found || Duplicates)
        {
            AtInsert(i, item);
        }

        return i;
    }

    /// <summary>
    /// Gets the key for an item.
    /// Matches upstream keyOf().
    /// Override this to extract the key from an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The key for the item.</returns>
    public virtual TKey KeyOf(T? item)
    {
        // Default implementation: item is its own key
        return (TKey)(object)item!;
    }

    /// <summary>
    /// Searches for an item by key using binary search.
    /// Matches upstream search().
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="index">Output: the index where the key was found or should be inserted.</param>
    /// <returns>True if found, false otherwise.</returns>
    public virtual bool Search(TKey key, out int index)
    {
        int l = 0;
        int h = Count - 1;
        bool result = false;

        while (l <= h)
        {
            int i = (l + h) >> 1;
            int c = Compare(KeyOf(Items[i]), key);

            if (c < 0)
            {
                l = i + 1;
            }
            else
            {
                h = i - 1;
                if (c == 0)
                {
                    result = true;
                    if (!Duplicates)
                    {
                        l = i;
                    }
                }
            }
        }

        index = l;
        return result;
    }

    /// <summary>
    /// Writes the collection to a binary stream.
    /// Matches upstream write().
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);
        writer.Write(Duplicates);
    }

    /// <summary>
    /// Reads the collection from a binary stream.
    /// Matches upstream read().
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    public override void Read(BinaryReader reader)
    {
        base.Read(reader);
        Duplicates = reader.ReadBoolean();
    }

    /// <summary>
    /// Compares two keys.
    /// Matches upstream compare().
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="key1">First key.</param>
    /// <param name="key2">Second key.</param>
    /// <returns>Negative if key1 &lt; key2, 0 if equal, positive if key1 &gt; key2.</returns>
    protected abstract int Compare(TKey key1, TKey key2);
}

/// <summary>
/// Streamable sorted collection where items are their own keys.
/// Convenience class when T and TKey are the same type.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public abstract class TSortedCollection<T> : TSortedCollection<T, T> where T : class
{
    /// <summary>
    /// Creates a new sorted collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TSortedCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
    }

    /// <summary>
    /// Creates an empty sorted collection.
    /// </summary>
    protected TSortedCollection() : base()
    {
    }
}
