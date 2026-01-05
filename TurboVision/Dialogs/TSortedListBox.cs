using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// List box with incremental keyboard search in a sorted collection.
/// Matches upstream TSortedListBox behavior.
/// </summary>
public class TSortedListBox : TListBox
{
    protected ushort ShiftState { get; set; }
    private short _searchPos = -1;

    public TSortedListBox(TRect bounds, ushort numCols, TScrollBar? scrollBar)
        : base(bounds, numCols, scrollBar)
    {
        ShowCursor();
        SetCursor(1, 0);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        short oldValue = Focused;
        base.HandleEvent(ref ev);

        // Reset search position on focus change or release
        if (oldValue != Focused ||
            (ev.What == EventConstants.evBroadcast &&
             ev.Message.Command == CommandConstants.cmReleasedFocus))
        {
            _searchPos = -1;
        }

        if (ev.What == EventConstants.evKeyDown)
        {
            char charCode = (char)ev.KeyDown.CharCode;
            if (charCode != 0)
            {
                int value = Focused;
                Span<char> curString = stackalloc char[256];
                curString.Clear();

                if (value < Range)
                {
                    GetText(curString, (short)value, 255);
                }

                short oldPos = _searchPos;

                if (ev.KeyDown.KeyCode == KeyConstants.kbBack)
                {
                    // Backspace - reduce search string
                    if (_searchPos == -1)
                    {
                        return;
                    }
                    _searchPos--;
                    if (_searchPos == -1)
                    {
                        ShiftState = ev.KeyDown.ControlKeyState;
                    }
                    if (_searchPos >= 0 && _searchPos < 255)
                    {
                        curString[_searchPos + 1] = '\0';
                    }
                }
                else if (charCode == '.')
                {
                    // Jump to dot position in current string
                    int len = curString.IndexOf('\0');
                    if (len < 0)
                    {
                        len = curString.Length;
                    }
                    int dotPos = curString.Slice(0, len).IndexOf('.');
                    if (dotPos < 0)
                    {
                        _searchPos = -1;
                    }
                    else
                    {
                        _searchPos = (short)dotPos;
                    }
                }
                else
                {
                    // Add character to search string
                    _searchPos++;
                    if (_searchPos == 0)
                    {
                        ShiftState = ev.KeyDown.ControlKeyState;
                    }
                    if (_searchPos >= 0 && _searchPos < 255)
                    {
                        curString[_searchPos] = charCode;
                        curString[_searchPos + 1] = '\0';
                    }
                }

                // Search for matching item
                var key = GetKey(curString);
                if (SearchCollection(key, out int foundIndex))
                {
                    value = foundIndex;
                }
                else if (foundIndex >= 0 && foundIndex < Range)
                {
                    value = foundIndex;
                }

                if (value < Range)
                {
                    Span<char> newString = stackalloc char[256];
                    newString.Clear();
                    GetText(newString, (short)value, 255);

                    if (StringsEqualPrefix(curString, newString, _searchPos + 1))
                    {
                        if (value != oldValue)
                        {
                            FocusItem((short)value);
                            SetCursor(Cursor.X + _searchPos + 1, Cursor.Y);
                        }
                        else
                        {
                            SetCursor(Cursor.X + (_searchPos - oldPos), Cursor.Y);
                        }
                    }
                    else
                    {
                        _searchPos = oldPos;
                    }
                }
                else
                {
                    _searchPos = oldPos;
                }

                // Clear event if search position changed or it was an alpha character
                if (_searchPos != oldPos || char.IsLetter(charCode))
                {
                    ClearEvent(ref ev);
                }
            }
        }
    }

    /// <summary>
    /// Gets a search key from a string. Override in derived classes.
    /// </summary>
    protected virtual TSearchRec GetKey(ReadOnlySpan<char> s)
    {
        int len = s.IndexOf('\0');
        if (len < 0)
        {
            len = s.Length;
        }
        return new TSearchRec(0, 0, 0, new string(s.Slice(0, len)));
    }

    /// <summary>
    /// Sets a new sorted list.
    /// </summary>
    public virtual void NewList(TFileCollection? list)
    {
        if (Items != null)
        {
            Items.Clear();
        }

        if (list != null)
        {
            // Convert TSearchRec items to strings for the base class
            Items = [];
            foreach (var item in list)
            {
                string text = item.Name;
                if (item.IsDirectory)
                {
                    text += "\\";
                }
                Items.Add(text);
            }
            SetRange((short)Items.Count);
        }
        else
        {
            Items = null;
            SetRange(0);
        }

        _searchPos = -1;

        if (Range > 0)
        {
            FocusItem(0);
        }
        DrawView();
    }

    /// <summary>
    /// Gets the underlying collection. Override to provide typed access.
    /// </summary>
    public new virtual TFileCollection? List()
    {
        // TSortedListBox uses a TFileCollection internally
        return null;
    }

    /// <summary>
    /// Searches the collection for a key.
    /// </summary>
    protected virtual bool SearchCollection(TSearchRec key, out int index)
    {
        index = 0;
        var list = List();
        if (list != null)
        {
            return list.Search(key, out index);
        }

        // Fallback to string search in Items
        if (Items != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].StartsWith(key.Name, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Compares two strings for equality up to a given length (case-insensitive).
    /// </summary>
    private static bool StringsEqualPrefix(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int count)
    {
        if (count <= 0)
        {
            return true;
        }

        int len1 = s1.IndexOf('\0');
        if (len1 < 0)
        {
            len1 = s1.Length;
        }

        int len2 = s2.IndexOf('\0');
        if (len2 < 0)
        {
            len2 = s2.Length;
        }

        if (len1 < count || len2 < count)
        {
            return false;
        }

        return s1.Slice(0, count).Equals(s2.Slice(0, count), StringComparison.OrdinalIgnoreCase);
    }
}
