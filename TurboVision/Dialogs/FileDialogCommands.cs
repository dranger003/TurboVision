namespace TurboVision.Dialogs;

/// <summary>
/// Command constants for file and directory dialogs.
/// </summary>
public static class FileDialogCommands
{
    // Commands
    public const ushort cmFileOpen = 1001;
    public const ushort cmFileReplace = 1002;
    public const ushort cmFileClear = 1003;
    public const ushort cmFileInit = 1004;
    public const ushort cmChangeDir = 1005;
    public const ushort cmRevert = 1006;

    // Messages
    public const ushort cmFileFocused = 102;
    public const ushort cmFileDoubleClicked = 103;
}

/// <summary>
/// Option flags for TFileDialog.
/// </summary>
[Flags]
public enum FileDialogFlags : ushort
{
    fdOKButton = 0x0001,
    fdOpenButton = 0x0002,
    fdReplaceButton = 0x0004,
    fdClearButton = 0x0008,
    fdHelpButton = 0x0010,
    fdNoLoadDir = 0x0100
}

/// <summary>
/// Option flags for TChDirDialog.
/// </summary>
[Flags]
public enum ChDirDialogFlags : ushort
{
    cdNormal = 0x0000,
    cdNoLoadDir = 0x0001,
    cdHelpButton = 0x0002
}
