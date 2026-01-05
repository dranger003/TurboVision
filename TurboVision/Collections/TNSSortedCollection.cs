namespace TurboVision.Collections;

/// <summary>
/// Non-streamable sorted collection with binary search.
/// Maintains items in sorted order using a key-based comparison.
/// </summary>
/// <remarks>
/// Matches upstream TNSSortedCollection from tvobjs.h/tsortcol.cpp.
/// Uses generic type parameters for type safety.
/// </remarks>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <typeparam name="TKey">The type of key used for comparison.</typeparam>
public abstract class TNSSortedCollection<T, TKey> : TNSCollection<T> where T : class
{
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
    public TNSSortedCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
        Duplicates = false;
    }

    /// <summary>
    /// Creates an empty sorted collection.
    /// </summary>
    protected TNSSortedCollection() : base()
    {
        Duplicates = false;
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
        // This works when T is the key type
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
/// Non-streamable sorted collection where items are their own keys.
/// Convenience class when T and TKey are the same type.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public abstract class TNSSortedCollection<T> : TNSSortedCollection<T, T> where T : class
{
    /// <summary>
    /// Creates a new sorted collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TNSSortedCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
    }

    /// <summary>
    /// Creates an empty sorted collection.
    /// </summary>
    protected TNSSortedCollection() : base()
    {
    }
}
