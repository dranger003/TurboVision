using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Standard file open/save dialog.
/// </summary>
public class TFileDialog : TDialog
{
    // Localizable text strings
    private const string FilesText = "~F~iles";
    private const string OpenText = "~O~pen";
    private const string OkText = "O~K~";
    private const string ReplaceText = "~R~eplace";
    private const string ClearText = "~C~lear";
    private const string CancelText = "Cancel";
    private const string HelpText = "~H~elp";
    private const string InvalidDriveText = "Invalid drive or directory";
    private const string InvalidFileText = "Invalid file name";

    public TFileInputLine? FileName { get; private set; }
    public TFileList? FileList { get; private set; }
    public string WildCard { get; private set; } = "*.*";
    public string Directory { get; private set; } = "";

    public TFileDialog(
        string wildCard,
        string title,
        string inputName,
        FileDialogFlags options,
        byte histId)
        : base(new TRect(15, 1, 64, 20), title)
    {
        Options |= OptionFlags.ofCentered;
        Flags |= WindowFlags.wfGrow;

        WildCard = wildCard;

        // File name input line
        FileName = new TFileInputLine(new TRect(3, 3, 31, 4), PathUtils.MAXPATH);
        FileName.Data = wildCard;
        Insert(FileName);
        First()!.GrowMode = GrowFlags.gfGrowHiX;

        // Label for input line
        Insert(new TLabel(new TRect(2, 2, 3 + TStringUtils.CstrLen(inputName), 3), inputName, FileName));
        First()!.GrowMode = 0;

        // History button
        Insert(new THistory(new TRect(31, 3, 34, 4), FileName, histId));
        First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;

        // Scroll bar for file list
        var sb = new TScrollBar(new TRect(3, 14, 34, 15));
        Insert(sb);

        // File list
        FileList = new TFileList(new TRect(3, 6, 34, 14), sb);
        Insert(FileList);
        First()!.GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;

        // Files label
        Insert(new TLabel(new TRect(2, 5, 8, 6), FilesText, FileList));
        First()!.GrowMode = 0;

        // Buttons
        ushort opt = CommandConstants.bfDefault;
        var r = new TRect(35, 3, 46, 5);

        if ((options & FileDialogFlags.fdOpenButton) != 0)
        {
            Insert(new TButton(r, OpenText, FileDialogCommands.cmFileOpen, opt));
            First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
            opt = CommandConstants.bfNormal;
            r = new TRect(r.A.X, r.A.Y + 3, r.B.X, r.B.Y + 3);
        }

        if ((options & FileDialogFlags.fdOKButton) != 0)
        {
            Insert(new TButton(r, OkText, FileDialogCommands.cmFileOpen, opt));
            First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
            opt = CommandConstants.bfNormal;
            r = new TRect(r.A.X, r.A.Y + 3, r.B.X, r.B.Y + 3);
        }

        if ((options & FileDialogFlags.fdReplaceButton) != 0)
        {
            Insert(new TButton(r, ReplaceText, FileDialogCommands.cmFileReplace, opt));
            First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
            opt = CommandConstants.bfNormal;
            r = new TRect(r.A.X, r.A.Y + 3, r.B.X, r.B.Y + 3);
        }

        if ((options & FileDialogFlags.fdClearButton) != 0)
        {
            Insert(new TButton(r, ClearText, FileDialogCommands.cmFileClear, opt));
            First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
            r = new TRect(r.A.X, r.A.Y + 3, r.B.X, r.B.Y + 3);
        }

        // Cancel button
        Insert(new TButton(r, CancelText, CommandConstants.cmCancel, CommandConstants.bfNormal));
        First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        r = new TRect(r.A.X, r.A.Y + 3, r.B.X, r.B.Y + 3);

        if ((options & FileDialogFlags.fdHelpButton) != 0)
        {
            Insert(new TButton(r, HelpText, CommandConstants.cmHelp, CommandConstants.bfNormal));
            First()!.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        }

        // File info pane
        Insert(new TFileInfoPane(new TRect(1, 16, 48, 18)));
        First()!.GrowMode = GrowFlags.gfGrowAll & ~GrowFlags.gfGrowLoX;

        SelectNext(false);

        // Adjust size based on screen size
        AdjustDialogSize();

        // Load directory unless fdNoLoadDir is set
        if ((options & FileDialogFlags.fdNoLoadDir) == 0)
        {
            ReadDirectory();
        }
    }

    private void AdjustDialogSize()
    {
        var bounds = GetBounds();
        var screenSize = TProgram.Application?.Size ?? new TPoint(80, 25);

        if (screenSize.X > 90)
        {
            bounds.Grow(15, 0);
        }
        else if (screenSize.X > 63)
        {
            bounds.A = new TPoint(7, bounds.A.Y);
            bounds.B = new TPoint(screenSize.X - 7, bounds.B.Y);
        }

        if (screenSize.Y > 34)
        {
            bounds.Grow(0, 5);
        }
        else if (screenSize.Y > 25)
        {
            bounds.A = new TPoint(bounds.A.X, 3);
            bounds.B = new TPoint(bounds.B.X, screenSize.Y - 3);
        }

        Locate(ref bounds);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case FileDialogCommands.cmFileOpen:
                case FileDialogCommands.cmFileReplace:
                case FileDialogCommands.cmFileClear:
                    EndModal(ev.Message.Command);
                    ClearEvent(ref ev);
                    break;
            }
        }
        else if (ev.What == EventConstants.evBroadcast &&
                 ev.Message.Command == FileDialogCommands.cmFileDoubleClicked)
        {
            var newEv = new TEvent
            {
                What = EventConstants.evCommand,
                Message = new MessageEvent { Command = CommandConstants.cmOK, InfoPtr = null }
            };
            PutEvent(newEv);
            ClearEvent(ref ev);
        }
    }

    /// <summary>
    /// Gets the full file name based on the current input and directory.
    /// </summary>
    public string GetFileName()
    {
        if (FileName == null)
        {
            return "";
        }

        string buf = FileName.Data.Trim();
        buf = PathUtils.FExpand(buf, Directory);

        PathUtils.FnSplit(buf, out string drive, out string dir, out string name, out string ext);

        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ext))
        {
            // Use wildcard pattern if no name specified
            PathUtils.FnSplit(WildCard, out _, out _, out string tName, out string tExt);
            buf = PathUtils.FnMerge(drive, dir, tName, tExt);
        }

        return buf;
    }

    public override void GetData(Span<byte> rec)
    {
        string fileName = GetFileName();
        var bytes = System.Text.Encoding.UTF8.GetBytes(fileName);
        int len = Math.Min(bytes.Length, rec.Length - 1);
        bytes.AsSpan(0, len).CopyTo(rec);
        if (len < rec.Length)
        {
            rec[len] = 0;
        }
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        base.SetData(rec);

        int len = rec.IndexOf((byte)0);
        if (len < 0)
        {
            len = rec.Length;
        }
        string data = System.Text.Encoding.UTF8.GetString(rec.Slice(0, len));

        if (!string.IsNullOrEmpty(data) && PathUtils.IsWild(data))
        {
            Valid(FileDialogCommands.cmFileInit);
            FileName?.Select();
        }
    }

    public override bool Valid(ushort command)
    {
        if (command == 0)
        {
            return true;
        }

        if (base.Valid(command))
        {
            if (command != CommandConstants.cmCancel && command != FileDialogCommands.cmFileClear)
            {
                string fName = GetFileName();

                if (PathUtils.IsWild(fName))
                {
                    // Wild card - update directory and file list
                    PathUtils.FnSplit(fName, out string drive, out string dir, out string name, out string ext);
                    string path = drive + dir;

                    if (CheckDirectory(path))
                    {
                        Directory = path;
                        WildCard = name + ext;
                        if (command != FileDialogCommands.cmFileInit)
                        {
                            FileList?.Select();
                        }
                        FileList?.ReadDirectory(Directory, WildCard);
                    }
                }
                else if (PathUtils.IsDir(fName))
                {
                    // Directory - navigate into it
                    if (CheckDirectory(fName))
                    {
                        Directory = PathUtils.AddFinalSeparator(fName);
                        if (command != FileDialogCommands.cmFileInit)
                        {
                            FileList?.Select();
                        }
                        FileList?.ReadDirectory(Directory, WildCard);
                    }
                }
                else if (PathUtils.ValidFileName(fName))
                {
                    return true;
                }
                else
                {
                    MsgBox.MessageBox(
                        MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
                        "{0}: '{1}'", InvalidFileText, fName);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckDirectory(string path)
    {
        if (PathUtils.PathValid(path))
        {
            return true;
        }

        MsgBox.MessageBox(
            MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
            "{0}: '{1}'", InvalidDriveText, path);
        FileName?.Select();
        return false;
    }

    private void ReadDirectory()
    {
        string curDir = PathUtils.GetCurDir();
        Directory = curDir;
        FileList?.ReadDirectory(WildCard);
    }

    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min = new TPoint(49, 19);
    }

    public override void ShutDown()
    {
        FileName = null;
        FileList = null;
        base.ShutDown();
    }
}
