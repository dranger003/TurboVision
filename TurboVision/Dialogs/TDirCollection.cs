namespace TurboVision.Dialogs;

/// <summary>
/// Collection of TDirEntry objects for directory tree display.
/// </summary>
public class TDirCollection : List<TDirEntry>
{
    /// <summary>
    /// Creates an empty TDirCollection.
    /// </summary>
    public TDirCollection()
    {
    }

    /// <summary>
    /// Creates a TDirCollection with initial capacity.
    /// </summary>
    public TDirCollection(int capacity) : base(capacity)
    {
    }

    /// <summary>
    /// Returns the item at the specified index.
    /// </summary>
    public TDirEntry At(int index)
    {
        if (index >= 0 && index < Count)
        {
            return this[index];
        }
        return new TDirEntry("", "");
    }

    /// <summary>
    /// Gets the count of items in the collection.
    /// </summary>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>
    /// Inserts an item at the end of the collection.
    /// </summary>
    public void Insert(TDirEntry item)
    {
        Add(item);
    }

    /// <summary>
    /// Finds the first item matching a predicate.
    /// </summary>
    public TDirEntry? FirstThat(Func<TDirEntry, bool> test)
    {
        foreach (var item in this)
        {
            if (test(item))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the last item matching a predicate.
    /// </summary>
    public TDirEntry? LastThat(Func<TDirEntry, bool> test)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            if (test(this[i]))
            {
                return this[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Executes an action for each item.
    /// </summary>
    public new void ForEach(Action<TDirEntry> action)
    {
        foreach (var item in this)
        {
            action(item);
        }
    }
}
