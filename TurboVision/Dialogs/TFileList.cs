using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// File listing view for file dialogs.
/// Displays files and directories from a TFileCollection.
/// </summary>
public class TFileList : TSortedListBox
{
    /// <summary>
    /// Message displayed when too many files are found during directory enumeration.
    /// Matches upstream static const char * _NEAR tooManyFiles.
    /// </summary>
    public static string TooManyFiles { get; set; } = "Too many files.";

    private TFileCollection? _fileCollection;

    public TFileList(TRect bounds, TScrollBar? scrollBar)
        : base(bounds, 2, scrollBar)
    {
    }

    public override void FocusItem(short item)
    {
        base.FocusItem(item);
        if (_fileCollection != null && item >= 0 && item < _fileCollection.Count)
        {
            Message(Owner, EventConstants.evBroadcast, FileDialogCommands.cmFileFocused, _fileCollection[item]);
        }
    }

    public override void SelectItem(short item)
    {
        if (_fileCollection != null && item >= 0 && item < _fileCollection.Count)
        {
            Message(Owner, EventConstants.evBroadcast, FileDialogCommands.cmFileDoubleClicked, _fileCollection[item]);
        }
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        if (_fileCollection != null && item >= 0 && item < _fileCollection.Count)
        {
            var f = _fileCollection[item];
            string name = f.Name;
            if (f.IsDirectory)
            {
                name += "\\";
            }

            int len = Math.Min(name.Length, Math.Min(maxLen, dest.Length - 1));
            name.AsSpan(0, len).CopyTo(dest);
            if (len < dest.Length)
            {
                dest[len] = '\0';
            }
        }
        else if (dest.Length > 0)
        {
            dest[0] = '\0';
        }
    }

    /// <summary>
    /// Sets a new file collection.
    /// </summary>
    public new void NewList(TFileCollection? list)
    {
        _fileCollection = list;
        if (list != null)
        {
            SetRange((short)list.Count);
        }
        else
        {
            SetRange(0);
        }

        if (Range > 0)
        {
            FocusItem(0);
        }
        DrawView();
    }

    /// <summary>
    /// Gets the underlying file collection.
    /// </summary>
    public new TFileCollection? List()
    {
        return _fileCollection;
    }

    /// <summary>
    /// Reads directory contents using the specified wildcard pattern.
    /// </summary>
    public void ReadDirectory(string wildCard)
    {
        ReadDirectory(null, wildCard);
    }

    /// <summary>
    /// Reads directory contents with directory and wildcard.
    /// </summary>
    public void ReadDirectory(string? dir, string wildCard)
    {
        string path;
        if (string.IsNullOrEmpty(dir))
        {
            path = wildCard;
        }
        else
        {
            path = Path.Combine(dir, wildCard);
        }

        var fileList = new TFileCollection();

        try
        {
            // Expand the path
            string expandedPath = PathUtils.FExpand(path);
            PathUtils.FnSplit(expandedPath, out string drive, out string dirPart, out string file, out string ext);
            string searchDir = drive + dirPart;

            // Search pattern
            string searchPattern = file + ext;
            if (string.IsNullOrEmpty(searchPattern))
            {
                searchPattern = "*";
            }

            // Find matching files
            if (Directory.Exists(searchDir))
            {
                try
                {
                    foreach (var fileInfo in new DirectoryInfo(searchDir).EnumerateFiles(searchPattern))
                    {
                        // Skip hidden/system files unless specifically searching for them
                        if ((fileInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                        {
                            continue;
                        }

                        fileList.Insert(new TSearchRec(fileInfo));
                    }
                }
                catch
                {
                    // Ignore file enumeration errors
                }

                // Find directories (always use "*" pattern for directories)
                try
                {
                    foreach (var dirInfo in new DirectoryInfo(searchDir).EnumerateDirectories("*"))
                    {
                        // Skip hidden directories and "."
                        if (dirInfo.Name.StartsWith('.'))
                        {
                            continue;
                        }

                        fileList.Insert(new TSearchRec(dirInfo));
                    }
                }
                catch
                {
                    // Ignore directory enumeration errors
                }

                // Add ".." entry if not at root
                if (searchDir.Length > 3 || !Path.IsPathRooted(searchDir))
                {
                    string trimmed = PathUtils.TrimEndSeparator(searchDir);
                    if (trimmed.Length > 0)
                    {
                        string? parentDir = Path.GetDirectoryName(trimmed);
                        if (parentDir != null)
                        {
                            var dotDot = new TSearchRec
                            {
                                Name = "..",
                                Attr = PathUtils.FA_DIREC,
                                Size = 0,
                                Time = 0x210000 // Default date: 1980-01-01
                            };
                            fileList.Insert(dotDot);
                        }
                    }
                }
            }
        }
        catch
        {
            // Handle directory reading errors silently
        }

        NewList(fileList);

        if (_fileCollection != null && _fileCollection.Count > 0)
        {
            Message(Owner, EventConstants.evBroadcast, FileDialogCommands.cmFileFocused, _fileCollection[0]);
        }
        else
        {
            Message(Owner, EventConstants.evBroadcast, FileDialogCommands.cmFileFocused, TSearchRec.Empty);
        }
    }

    public override int DataSize()
    {
        return 0;
    }

    public override void GetData(Span<byte> rec)
    {
        // No data transfer
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        // No data transfer
    }

    protected override TSearchRec GetKey(ReadOnlySpan<char> s)
    {
        int len = s.IndexOf('\0');
        if (len < 0)
        {
            len = s.Length;
        }

        byte attr = 0;
        // If shift is held or string starts with '.', search for directories
        if ((ShiftState & KeyConstants.kbShift) != 0 || (len > 0 && s[0] == '.'))
        {
            attr = PathUtils.FA_DIREC;
        }

        return new TSearchRec(attr, 0, 0, new string(s.Slice(0, len)));
    }

    protected override bool SearchCollection(TSearchRec key, out int index)
    {
        index = 0;
        if (_fileCollection != null)
        {
            return _fileCollection.Search(key, out index);
        }
        return false;
    }

    /// <summary>
    /// Sends a message to the owner.
    /// </summary>
    private static void Message(TGroup? owner, ushort what, ushort command, object? infoPtr)
    {
        if (owner == null)
        {
            return;
        }

        var ev = new TEvent
        {
            What = what,
            Message = new MessageEvent { Command = command, InfoPtr = infoPtr }
        };

        owner.HandleEvent(ref ev);
    }

    public override void ShutDown()
    {
        _fileCollection = null;
        base.ShutDown();
    }
}
