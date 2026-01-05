using System.Runtime.InteropServices;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Change directory dialog.
/// Allows navigation and selection of a new working directory.
/// </summary>
public class TChDirDialog : TDialog
{
    // Localizable text strings
    private const string ChangeDirTitle = "Change Directory";
    private const string DirNameText = "Directory ~n~ame";
    private const string DirTreeText = "Directory ~t~ree";
    private const string OkText = "O~K~";
    private const string ChdirText = "~C~hdir";
    private const string RevertText = "~R~evert";
    private const string HelpText = "Help";
    private const string DrivesText = "Drives";
    private const string InvalidText = "Invalid directory";

    public TInputLine? DirInput { get; private set; }
    public TDirListBox? DirList { get; private set; }
    public TButton? OkButton { get; private set; }
    public TButton? ChDirButton { get; private set; }

    public TChDirDialog(ChDirDialogFlags options, byte histId)
        : base(new TRect(16, 2, 64, 20), ChangeDirTitle)
    {
        Options |= OptionFlags.ofCentered;
        Flags |= WindowFlags.wfGrow;

        // Directory name input
        DirInput = new TInputLine(new TRect(3, 3, 42, 4), PathUtils.MAXPATH - 1);
        DirInput.GrowMode = GrowFlags.gfGrowHiX;
        Insert(DirInput);

        // Label for input
        Insert(new TLabel(new TRect(2, 2, 17, 3), DirNameText, DirInput));

        // History button
        var history = new THistory(new TRect(42, 3, 45, 4), DirInput, histId);
        history.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        Insert(history);

        // Scroll bar for directory list
        var sb = new TScrollBar(new TRect(32, 6, 33, 16));
        Insert(sb);

        // Directory list
        DirList = new TDirListBox(new TRect(3, 6, 32, 16), sb);
        DirList.GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        Insert(DirList);

        // Label for directory tree
        Insert(new TLabel(new TRect(2, 5, 17, 6), DirTreeText, DirList));

        // OK button
        OkButton = new TButton(new TRect(35, 6, 45, 8), OkText, CommandConstants.cmOK, CommandConstants.bfDefault);
        OkButton.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        Insert(OkButton);

        // Chdir button
        ChDirButton = new TButton(new TRect(35, 9, 45, 11), ChdirText, FileDialogCommands.cmChangeDir, CommandConstants.bfNormal);
        ChDirButton.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        Insert(ChDirButton);

        // Revert button
        var revertButton = new TButton(new TRect(35, 12, 45, 14), RevertText, FileDialogCommands.cmRevert, CommandConstants.bfNormal);
        revertButton.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        Insert(revertButton);

        // Help button (optional)
        if ((options & ChDirDialogFlags.cdHelpButton) != 0)
        {
            var helpButton = new TButton(new TRect(35, 15, 45, 17), HelpText, CommandConstants.cmHelp, CommandConstants.bfNormal);
            helpButton.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
            Insert(helpButton);
        }

        // Initialize dialog unless cdNoLoadDir is set
        if ((options & ChDirDialogFlags.cdNoLoadDir) == 0)
        {
            SetUpDialog();
        }

        SelectNext(false);
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

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            string curDir;

            switch (ev.Message.Command)
            {
                case FileDialogCommands.cmRevert:
                    curDir = GetCurrentDir();
                    break;

                case FileDialogCommands.cmChangeDir:
                    if (DirList?.List() == null || DirList.Focused < 0 || DirList.Focused >= DirList.List()!.Count)
                    {
                        return;
                    }

                    var entry = DirList.List()![DirList.Focused];
                    curDir = entry.Dir();

                    if (curDir == DrivesText)
                    {
                        break;
                    }

                    if (PathUtils.IsSeparator(curDir[0]) || PathUtils.DriveValid(curDir[0]))
                    {
                        if (!curDir.EndsWith(Path.DirectorySeparatorChar) && !curDir.EndsWith(Path.AltDirectorySeparatorChar))
                        {
                            curDir += Path.DirectorySeparatorChar;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;

                default:
                    return;
            }

            DirList?.NewDirectory(curDir);
            curDir = PathUtils.TrimEndSeparator(curDir);

            if (DirInput != null)
            {
                DirInput.Data = curDir;
                DirInput.DrawView();
            }

            DirList?.Select();
            ClearEvent(ref ev);
        }
    }

    public override bool Valid(ushort command)
    {
        if (command != CommandConstants.cmOK)
        {
            return true;
        }

        if (DirInput == null)
        {
            return true;
        }

        string path = PathUtils.FExpand(DirInput.Data);
        path = PathUtils.TrimEndSeparator(path);

        if (!ChangeDir(path))
        {
            MsgBox.MessageBox(
                MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
                "{0}: '{1}'.", InvalidText, path);
            return false;
        }

        return true;
    }

    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min = new TPoint(48, 18);
    }

    private void SetUpDialog()
    {
        if (DirList != null)
        {
            string curDir = GetCurrentDir();
            DirList.NewDirectory(curDir);

            if (DirInput != null)
            {
                curDir = PathUtils.TrimEndSeparator(curDir);
                DirInput.Data = curDir;
                DirInput.DrawView();
            }
        }
    }

    private static string GetCurrentDir()
    {
        string dir = PathUtils.GetCurDir();

        // On Unix, remove drive letter prefix if present
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (dir.Length >= 2 && dir[1] == ':')
            {
                dir = dir.Substring(2);
            }
        }

        return dir;
    }

    private static bool ChangeDir(string path)
    {
        try
        {
            // Set drive if path contains drive letter
            if (path.Length >= 2 && path[1] == ':')
            {
                PathUtils.SetDisk(path[0]);
            }

            Directory.SetCurrentDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override void ShutDown()
    {
        DirList = null;
        DirInput = null;
        OkButton = null;
        ChDirButton = null;
        base.ShutDown();
    }
}
