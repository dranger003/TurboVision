using System.Runtime.InteropServices;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Directory tree list box for change directory dialogs.
/// Displays hierarchical directory structure with tree graphics.
/// </summary>
public class TDirListBox : TListBox
{
    private TDirCollection? _dirCollection;
    private string _dir = "";
    private short _cur;

    // Tree graphics characters matching upstream CP437 values:
    // pathDir   = "\xC0\xC4\xC2" = └─┬
    // firstDir  = "\xC0\xC2\xC4" = └┬─
    // middleDir = " \xC3\xC4"    = " ├─"
    // lastDir   = " \xC0\xC4"    = " └─"
    private const string PathDir = "└─┬";
    private const string FirstDir = "└┬─";
    private const string MiddleDir = " ├─";
    private const string LastDir = " └─";
    private const string Drives = "Drives";

    // Graphics characters for tree lines
    private static readonly char[] Graphics = ['└', '├', '─'];

    public TDirListBox(TRect bounds, TScrollBar? scrollBar)
        : base(bounds, 1, scrollBar)
    {
        _dir = "";
        _cur = 0;
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        if (_dirCollection != null && item >= 0 && item < _dirCollection.Count)
        {
            string text = _dirCollection[item].Text();
            int len = Math.Min(text.Length, Math.Min(maxLen, dest.Length - 1));
            text.AsSpan(0, len).CopyTo(dest);
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

    public override void SelectItem(short item)
    {
        if (_dirCollection != null && item >= 0 && item < _dirCollection.Count)
        {
            Message(Owner, EventConstants.evCommand, FileDialogCommands.cmChangeDir, _dirCollection[item]);
        }
    }

    public override bool IsSelected(short item)
    {
        return item == _cur;
    }

    /// <summary>
    /// Sets up the directory list for a new directory path.
    /// </summary>
    public void NewDirectory(string path)
    {
        _dir = path;
        var dirs = new TDirCollection();

        // On Windows, add "Drives" entry
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dirs.Insert(new TDirEntry(Drives, Drives));
        }

        if (path == Drives)
        {
            ShowDrives(dirs);
        }
        else
        {
            ShowDirs(dirs);
        }

        NewList(dirs);
        FocusItem(_cur);
    }

    /// <summary>
    /// Sets a new directory collection.
    /// </summary>
    public void NewList(TDirCollection? list)
    {
        _dirCollection = list;
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
    /// Gets the underlying directory collection.
    /// </summary>
    public new TDirCollection? List()
    {
        return _dirCollection;
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfFocused) != 0)
        {
            // Make the ChDir button default when this list has focus
            if (Owner is TChDirDialog chDirDialog && chDirDialog.ChDirButton != null)
            {
                chDirDialog.ChDirButton.MakeDefault(enable);
            }
        }
    }

    private void ShowDrives(TDirCollection dirs)
    {
        bool isFirst = true;
        char oldC = '0';

        for (char c = 'A'; c <= 'Z'; c++)
        {
            if (c < 'C' || PathUtils.DriveValid(c))
            {
                if (oldC != '0')
                {
                    string s;
                    if (isFirst)
                    {
                        s = FirstDir + oldC;
                        isFirst = false;
                    }
                    else
                    {
                        s = MiddleDir + oldC;
                    }
                    dirs.Insert(new TDirEntry(s, oldC + ":\\"));
                }

                if (c == PathUtils.GetDisk() + 'A')
                {
                    _cur = (short)dirs.GetCount();
                }
                oldC = c;
            }
        }

        if (oldC != '0')
        {
            string s = LastDir + oldC;
            dirs.Insert(new TDirEntry(s, oldC + ":\\"));
        }
    }

    private void ShowDirs(TDirCollection dirs)
    {
        const int indentSize = 2;
        int indent = indentSize;

        // Create buffer for building display strings
        Span<char> buf = stackalloc char[PathUtils.MAXPATH + PathUtils.MAXFILE + PathUtils.MAXEXT];
        buf.Fill(' ');

        int nameOffset = buf.Length - (PathUtils.MAXFILE + PathUtils.MAXEXT);

        string curDir = _dir;
        int sepIndex;

        // Show root directory
        sepIndex = curDir.IndexOf(Path.DirectorySeparatorChar);
        if (sepIndex < 0)
        {
            sepIndex = curDir.IndexOf(Path.AltDirectorySeparatorChar);
        }

        if (sepIndex >= 0)
        {
            string rootPart = curDir.Substring(0, sepIndex + 1);

            // Build display string: PathDir + root name
            string displayText = PathDir + rootPart;
            dirs.Insert(new TDirEntry(displayText, _dir.Substring(0, sepIndex + 1)));

            curDir = curDir.Substring(sepIndex + 1);
        }
        else
        {
            return;
        }

        // Show directories up to the current one
        while ((sepIndex = curDir.IndexOf(Path.DirectorySeparatorChar)) >= 0 ||
               (sepIndex = curDir.IndexOf(Path.AltDirectorySeparatorChar)) >= 0)
        {
            string dirName = curDir.Substring(0, sepIndex);

            // Build indented display string
            string indentStr = new string(' ', indent);
            string displayText = indentStr + PathDir.Substring(0, 2) + dirName;

            // Calculate the path up to this point
            int pathEnd = _dir.Length - curDir.Length + sepIndex + 1;
            string partialPath = _dir.Substring(0, pathEnd);

            dirs.Insert(new TDirEntry(displayText, partialPath));

            curDir = curDir.Substring(sepIndex + 1);
            indent += indentSize;
        }

        _cur = (short)(dirs.GetCount() - 1);

        // Show subdirectories of current directory
        string searchDir = PathUtils.TrimEndSeparator(_dir);
        if (!Directory.Exists(searchDir))
        {
            return;
        }

        try
        {
            bool isFirst = true;
            var subDirs = new DirectoryInfo(searchDir).EnumerateDirectories("*").ToList();

            for (int i = 0; i < subDirs.Count; i++)
            {
                var subDir = subDirs[i];

                // Skip hidden directories
                if (subDir.Name.StartsWith('.'))
                {
                    continue;
                }

                string prefix;
                if (isFirst)
                {
                    prefix = FirstDir.Substring(0, 2);
                    isFirst = false;
                }
                else
                {
                    prefix = MiddleDir.Substring(0, 2);
                }

                string indentStr = new string(' ', indent);
                string displayText = indentStr + prefix + subDir.Name;

                string subPath = Path.Combine(searchDir, subDir.Name);
                dirs.Insert(new TDirEntry(displayText, subPath));
            }

            // Fix last entry to use "last" graphics
            if (dirs.Count > 0)
            {
                var lastEntry = dirs[dirs.Count - 1];
                string text = lastEntry.Text();

                // Replace first tree character with last tree character
                int graphIdx = text.IndexOf(Graphics[1]); // '├'
                if (graphIdx < 0)
                {
                    graphIdx = text.IndexOf(Graphics[0]); // '└'
                }

                if (graphIdx >= 0)
                {
                    if (text[graphIdx] == Graphics[1])
                    {
                        // Change ├ to └
                        char[] chars = text.ToCharArray();
                        chars[graphIdx] = Graphics[0];
                        if (graphIdx + 1 < chars.Length)
                        {
                            chars[graphIdx + 1] = Graphics[2];
                        }
                        if (graphIdx + 2 < chars.Length)
                        {
                            chars[graphIdx + 2] = Graphics[2];
                        }
                        dirs[dirs.Count - 1] = new TDirEntry(new string(chars), lastEntry.Dir());
                    }
                }
            }
        }
        catch
        {
            // Ignore directory enumeration errors
        }
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
        _dirCollection = null;
        base.ShutDown();
    }
}
