namespace TurboVision.Core;

/// <summary>
/// Standard command constants.
/// </summary>
public static class CommandConstants
{
    // Standard commands
    public const ushort cmValid = 0;
    public const ushort cmQuit = 1;
    public const ushort cmError = 2;
    public const ushort cmMenu = 3;
    public const ushort cmClose = 4;
    public const ushort cmZoom = 5;
    public const ushort cmResize = 6;
    public const ushort cmNext = 7;
    public const ushort cmPrev = 8;
    public const ushort cmHelp = 9;

    // Dialog commands
    public const ushort cmOK = 10;
    public const ushort cmCancel = 11;
    public const ushort cmYes = 12;
    public const ushort cmNo = 13;
    public const ushort cmDefault = 14;

    // Edit commands
    public const ushort cmCut = 20;
    public const ushort cmCopy = 21;
    public const ushort cmPaste = 22;
    public const ushort cmUndo = 23;
    public const ushort cmClear = 24;
    public const ushort cmTile = 25;
    public const ushort cmCascade = 26;
    public const ushort cmRedo = 27;

    // Application commands
    public const ushort cmNew = 30;
    public const ushort cmOpen = 31;
    public const ushort cmSave = 32;
    public const ushort cmSaveAs = 33;
    public const ushort cmSaveAll = 34;
    public const ushort cmChDir = 35;
    public const ushort cmDosShell = 36;
    public const ushort cmCloseAll = 37;

    // Standard messages
    public const ushort cmReceivedFocus = 50;
    public const ushort cmReleasedFocus = 51;
    public const ushort cmCommandSetChanged = 52;
    public const ushort cmScrollBarChanged = 53;
    public const ushort cmScrollBarClicked = 54;
    public const ushort cmSelectWindowNum = 55;
    public const ushort cmListItemSelected = 56;
    public const ushort cmScreenChanged = 57;
    public const ushort cmTimerExpired = 58;
    public const ushort cmRecordHistory = 60;

    // Button type flags
    public const ushort bfNormal = 0x00;
    public const ushort bfDefault = 0x01;
    public const ushort bfLeftJust = 0x02;
    public const ushort bfBroadcast = 0x04;
    public const ushort bfGrabFocus = 0x08;
}
