namespace TurboVision.Dialogs;

/// <summary>
/// Static class for managing history entries for input fields.
/// Each history list is identified by a unique ID.
/// </summary>
public static class THistoryList
{
    private static readonly Dictionary<ushort, List<string>> HistoryLists = new();

    /// <summary>
    /// Adds a string to the history list identified by historyId.
    /// Duplicate entries are moved to the front instead of being added again.
    /// </summary>
    public static void HistoryAdd(ushort historyId, string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        if (!HistoryLists.TryGetValue(historyId, out var list))
        {
            list = new List<string>();
            HistoryLists[historyId] = list;
        }

        // Remove existing entry if present (will be moved to front)
        int existingIndex = list.IndexOf(str);
        if (existingIndex >= 0)
        {
            list.RemoveAt(existingIndex);
        }

        // Add to front of list
        list.Insert(0, str);
    }

    /// <summary>
    /// Returns the number of entries in the history list.
    /// </summary>
    public static int HistoryCount(ushort historyId)
    {
        if (HistoryLists.TryGetValue(historyId, out var list))
        {
            return list.Count;
        }
        return 0;
    }

    /// <summary>
    /// Returns the string at the specified index in the history list.
    /// Returns null if index is out of range.
    /// </summary>
    public static string? HistoryStr(ushort historyId, int index)
    {
        if (HistoryLists.TryGetValue(historyId, out var list))
        {
            if (index >= 0 && index < list.Count)
            {
                return list[index];
            }
        }
        return null;
    }

    /// <summary>
    /// Clears all entries in the specified history list.
    /// </summary>
    public static void ClearHistory(ushort historyId)
    {
        if (HistoryLists.TryGetValue(historyId, out var list))
        {
            list.Clear();
        }
    }

    /// <summary>
    /// Clears all history lists.
    /// </summary>
    public static void ClearAllHistory()
    {
        HistoryLists.Clear();
    }
}
