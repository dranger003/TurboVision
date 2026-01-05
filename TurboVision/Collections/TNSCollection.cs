namespace TurboVision.Collections;

using TurboVision.Views;

/// <summary>
/// Non-streamable dynamic array collection.
/// Provides basic dynamic array functionality with configurable growth.
/// </summary>
/// <remarks>
/// Matches upstream TNSCollection from tvobjs.h/tcollect.cpp.
/// Uses generic type parameter instead of void* for type safety.
/// </remarks>
public class TNSCollection<T> : TObject where T : class
{
    /// <summary>
    /// Maximum collection size constant.
    /// Matches upstream maxCollectionSize.
    /// </summary>
    public const int MaxCollectionSize = int.MaxValue / 2;

    /// <summary>
    /// Constant returned when item not found.
    /// Matches upstream ccNotFound.
    /// </summary>
    public const int NotFound = -1;

    private T?[] _items;
    private int _count;
    private int _limit;
    private int _delta;

    /// <summary>
    /// Whether to delete items when they are removed.
    /// Matches upstream shouldDelete.
    /// </summary>
    public bool ShouldDelete { get; set; } = true;

    /// <summary>
    /// Creates a new collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TNSCollection(int aLimit, int aDelta)
    {
        _items = [];
        _count = 0;
        _limit = 0;
        _delta = aDelta;
        SetLimit(aLimit);
    }

    /// <summary>
    /// Creates an empty collection.
    /// </summary>
    protected TNSCollection()
    {
        _items = [];
        _count = 0;
        _limit = 0;
        _delta = 0;
    }

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    public int Count
    {
        get { return _count; }
    }

    /// <summary>
    /// Gets the current capacity limit.
    /// </summary>
    public int Limit
    {
        get { return _limit; }
    }

    /// <summary>
    /// Gets the growth delta.
    /// </summary>
    public int Delta
    {
        get { return _delta; }
        protected set { _delta = value; }
    }

    /// <summary>
    /// Gets the internal items array for derived classes.
    /// </summary>
    protected T?[] Items
    {
        get { return _items; }
    }

    /// <summary>
    /// Shuts down the collection, freeing all items if ShouldDelete is true.
    /// Matches upstream shutDown().
    /// </summary>
    public override void ShutDown()
    {
        if (ShouldDelete)
        {
            FreeAll();
        }
        else
        {
            RemoveAll();
        }
        SetLimit(0);
        base.ShutDown();
    }

    /// <summary>
    /// Gets the item at the specified index.
    /// Matches upstream at().
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <returns>The item at the index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If index is out of range.</exception>
    public T? At(int index)
    {
        if (index < 0 || index >= _count)
        {
            Error(1, 0);
        }
        return _items[index];
    }

    /// <summary>
    /// Gets the index of an item in the collection.
    /// Matches upstream indexOf().
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>The index, or NotFound if not found.</returns>
    public virtual int IndexOf(T? item)
    {
        for (int i = 0; i < _count; i++)
        {
            if (ReferenceEquals(item, _items[i]))
            {
                return i;
            }
        }
        return NotFound;
    }

    /// <summary>
    /// Removes and frees the item at the specified index.
    /// Matches upstream atFree().
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    public void AtFree(int index)
    {
        var item = At(index);
        AtRemove(index);
        FreeItem(item);
    }

    /// <summary>
    /// Removes the item at the specified index without freeing it.
    /// Matches upstream atRemove().
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    public void AtRemove(int index)
    {
        if (index < 0 || index >= _count)
        {
            Error(1, 0);
        }

        _count--;
        if (index < _count)
        {
            Array.Copy(_items, index + 1, _items, index, _count - index);
        }
        _items[_count] = null;
    }

    /// <summary>
    /// Removes an item from the collection without freeing it.
    /// Matches upstream remove().
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void Remove(T? item)
    {
        int index = IndexOf(item);
        if (index != NotFound)
        {
            AtRemove(index);
        }
    }

    /// <summary>
    /// Removes all items from the collection without freeing them.
    /// Matches upstream removeAll().
    /// </summary>
    public void RemoveAll()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    /// <summary>
    /// Removes and frees an item from the collection.
    /// Matches upstream free().
    /// </summary>
    /// <param name="item">The item to remove and free.</param>
    public void Free(T? item)
    {
        Remove(item);
        FreeItem(item);
    }

    /// <summary>
    /// Frees all items in the collection.
    /// Matches upstream freeAll().
    /// </summary>
    public void FreeAll()
    {
        for (int i = 0; i < _count; i++)
        {
            FreeItem(_items[i]);
        }
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// Matches upstream atInsert().
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <param name="item">The item to insert.</param>
    public void AtInsert(int index, T? item)
    {
        if (index < 0 || index > _count)
        {
            Error(1, 0);
        }
        if (_count == _limit)
        {
            SetLimit(_count + _delta);
        }

        if (index < _count)
        {
            Array.Copy(_items, index, _items, index + 1, _count - index);
        }
        _count++;
        _items[index] = item;
    }

    /// <summary>
    /// Replaces the item at the specified index.
    /// Matches upstream atPut().
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <param name="item">The new item.</param>
    public void AtPut(int index, T? item)
    {
        if (index < 0 || index >= _count)
        {
            Error(1, 0);
        }
        _items[index] = item;
    }

    /// <summary>
    /// Inserts an item at the end of the collection.
    /// Matches upstream insert().
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <returns>The index where the item was inserted.</returns>
    public virtual int Insert(T? item)
    {
        int loc = _count;
        AtInsert(_count, item);
        return loc;
    }

    /// <summary>
    /// Called when a collection error occurs.
    /// Matches upstream error().
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="info">Additional info.</param>
    public virtual void Error(int code, int info)
    {
        throw new InvalidOperationException($"Collection error: code={code}, info={info}");
    }

    /// <summary>
    /// Finds the first item that satisfies the test.
    /// Matches upstream firstThat().
    /// </summary>
    /// <param name="test">Predicate to test each item.</param>
    /// <returns>The first matching item, or null.</returns>
    public T? FirstThat(Func<T?, bool> test)
    {
        for (int i = 0; i < _count; i++)
        {
            if (test(_items[i]))
            {
                return _items[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the last item that satisfies the test.
    /// Matches upstream lastThat().
    /// </summary>
    /// <param name="test">Predicate to test each item.</param>
    /// <returns>The last matching item, or null.</returns>
    public T? LastThat(Func<T?, bool> test)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            if (test(_items[i]))
            {
                return _items[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Executes an action for each item in the collection.
    /// Matches upstream forEach().
    /// </summary>
    /// <param name="action">Action to execute.</param>
    public void ForEach(Action<T?> action)
    {
        for (int i = 0; i < _count; i++)
        {
            action(_items[i]);
        }
    }

    /// <summary>
    /// Removes null entries from the collection.
    /// Matches upstream pack().
    /// </summary>
    public void Pack()
    {
        int dst = 0;
        for (int src = 0; src < _count; src++)
        {
            if (_items[src] != null)
            {
                _items[dst++] = _items[src];
            }
        }
        for (int i = dst; i < _count; i++)
        {
            _items[i] = null;
        }
        _count = dst;
    }

    /// <summary>
    /// Sets the capacity limit of the collection.
    /// Matches upstream setLimit().
    /// </summary>
    /// <param name="aLimit">New capacity limit.</param>
    public virtual void SetLimit(int aLimit)
    {
        if (aLimit < _count)
        {
            aLimit = _count;
        }
        if (aLimit > MaxCollectionSize)
        {
            aLimit = MaxCollectionSize;
        }
        if (aLimit != _limit)
        {
            if (aLimit > 0)
            {
                var newItems = new T?[aLimit];
                if (_count > 0)
                {
                    Array.Copy(_items, newItems, _count);
                }
                _items = newItems;
                _limit = aLimit;
            }
            else
            {
                _items = [];
                _limit = 0;
            }
        }
    }

    /// <summary>
    /// Frees a single item.
    /// Matches upstream freeItem().
    /// Override this to provide custom cleanup.
    /// </summary>
    /// <param name="item">The item to free.</param>
    protected virtual void FreeItem(T? item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Sets the count. For use by derived classes during deserialization.
    /// </summary>
    protected void SetCount(int count)
    {
        _count = count;
    }
}
