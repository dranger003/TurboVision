namespace TurboVision.Dialogs;

/// <summary>
/// Sorted collection of TSearchRec entries for file listings.
/// Matches upstream TFileCollection with custom sorting:
/// - ".." comes first
/// - Directories come after files
/// - Alphabetical order within each group
/// </summary>
public class TFileCollection : List<TSearchRec>, IComparer<TSearchRec>
{
    /// <summary>
    /// Creates an empty TFileCollection.
    /// </summary>
    public TFileCollection()
    {
    }

    /// <summary>
    /// Creates a TFileCollection with initial capacity.
    /// </summary>
    public TFileCollection(int capacity) : base(capacity)
    {
    }

    /// <summary>
    /// Compares two TSearchRec entries for sorting.
    /// Matching upstream compare() behavior with BCL comparison semantics:
    /// - ".." always comes first
    /// - Directories after files
    /// - Alphabetical within groups
    /// </summary>
    public int Compare(TSearchRec? key1, TSearchRec? key2)
    {
        if (key1 == null || key2 == null)
        {
            return 0;
        }

        // Same name
        if (string.Equals(key1.Name, key2.Name, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // ".." always comes first (BCL: return -1 means key1 < key2, sorts first)
        if (key1.Name == "..")
        {
            return -1;
        }
        if (key2.Name == "..")
        {
            return 1;
        }

        // Directories after files (BCL: return 1 means key1 > key2, sorts after)
        bool isDir1 = (key1.Attr & PathUtils.FA_DIREC) != 0;
        bool isDir2 = (key2.Attr & PathUtils.FA_DIREC) != 0;

        if (isDir1 && !isDir2)
        {
            return 1;
        }
        if (isDir2 && !isDir1)
        {
            return -1;
        }

        // Alphabetical order
        return string.Compare(key1.Name, key2.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Inserts an item in sorted order.
    /// </summary>
    public void Insert(TSearchRec item)
    {
        int index = BinarySearch(item, this);
        if (index < 0)
        {
            index = ~index;
        }
        base.Insert(index, item);
    }

    /// <summary>
    /// Searches for an item by key.
    /// </summary>
    public bool Search(TSearchRec key, out int index)
    {
        index = BinarySearch(key, this);
        if (index >= 0)
        {
            return true;
        }
        index = ~index;
        return false;
    }

    /// <summary>
    /// Returns the item at the specified index.
    /// </summary>
    public TSearchRec At(int index)
    {
        if (index >= 0 && index < Count)
        {
            return this[index];
        }
        return TSearchRec.Empty;
    }

    /// <summary>
    /// Gets the count of items in the collection.
    /// </summary>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>
    /// Finds the first item matching a predicate.
    /// </summary>
    public TSearchRec? FirstThat(Func<TSearchRec, bool> test)
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
    public TSearchRec? LastThat(Func<TSearchRec, bool> test)
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
    public new void ForEach(Action<TSearchRec> action)
    {
        foreach (var item in this)
        {
            action(item);
        }
    }

    /// <summary>
    /// Performs binary search using the collection's comparer.
    /// </summary>
    private new int BinarySearch(TSearchRec item, IComparer<TSearchRec> comparer)
    {
        int lo = 0;
        int hi = Count - 1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = comparer.Compare(this[mid], item);

            if (cmp == 0)
            {
                return mid;
            }
            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return ~lo;
    }
}
