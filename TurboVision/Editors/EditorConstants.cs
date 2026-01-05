namespace TurboVision.Editors;

/// <summary>
/// Editor update flags for controlling redraw behavior.
/// </summary>
public static class UpdateFlags
{
    public const byte ufUpdate = 0x01;
    public const byte ufLine = 0x02;
    public const byte ufView = 0x04;
}

/// <summary>
/// Selection mode flags for cursor positioning.
/// </summary>
public static class SelectModes
{
    public const byte smExtend = 0x01;
    public const byte smDouble = 0x02;
    public const byte smTriple = 0x04;
}

/// <summary>
/// Search result flags.
/// </summary>
public static class SearchFlags
{
    public const uint sfSearchFailed = unchecked((uint)-1);
}

/// <summary>
/// Editor-specific command constants.
/// </summary>
public static class EditorCommands
{
    // Search commands (82-84)
    public const ushort cmFind = 82;
    public const ushort cmReplace = 83;
    public const ushort cmSearchAgain = 84;

    // Cursor movement and editing commands (500+)
    public const ushort cmCharLeft = 500;
    public const ushort cmCharRight = 501;
    public const ushort cmWordLeft = 502;
    public const ushort cmWordRight = 503;
    public const ushort cmLineStart = 504;
    public const ushort cmLineEnd = 505;
    public const ushort cmLineUp = 506;
    public const ushort cmLineDown = 507;
    public const ushort cmPageUp = 508;
    public const ushort cmPageDown = 509;
    public const ushort cmTextStart = 510;
    public const ushort cmTextEnd = 511;
    public const ushort cmNewLine = 512;
    public const ushort cmBackSpace = 513;
    public const ushort cmDelChar = 514;
    public const ushort cmDelWord = 515;
    public const ushort cmDelStart = 516;
    public const ushort cmDelEnd = 517;
    public const ushort cmDelLine = 518;
    public const ushort cmInsMode = 519;
    public const ushort cmStartSelect = 520;
    public const ushort cmHideSelect = 521;
    public const ushort cmIndentMode = 522;
    public const ushort cmUpdateTitle = 523;
    public const ushort cmSelectAll = 524;
    public const ushort cmDelWordLeft = 525;
    public const ushort cmEncoding = 526;
}

/// <summary>
/// Editor dialog identifiers.
/// </summary>
public static class EditorDialogs
{
    public const int edOutOfMemory = 0;
    public const int edReadError = 1;
    public const int edWriteError = 2;
    public const int edCreateError = 3;
    public const int edSaveModify = 4;
    public const int edSaveUntitled = 5;
    public const int edSaveAs = 6;
    public const int edFind = 7;
    public const int edSearchFailed = 8;
    public const int edReplace = 9;
    public const int edReplacePrompt = 10;
}

/// <summary>
/// Editor option flags.
/// </summary>
[Flags]
public enum EditorFlags : ushort
{
    None = 0,
    CaseSensitive = 0x0001,
    WholeWordsOnly = 0x0002,
    PromptOnReplace = 0x0004,
    ReplaceAll = 0x0008,
    DoReplace = 0x0010,
    BackupFiles = 0x0100
}

/// <summary>
/// End-of-line type enumeration.
/// </summary>
public enum EolType : byte
{
    CrLf = 0,
    Lf = 1,
    Cr = 2
}

/// <summary>
/// Text encoding mode enumeration.
/// </summary>
public enum EncodingMode : byte
{
    Default = 0,
    SingleByte = 1
}

/// <summary>
/// Editor constants.
/// </summary>
public static class EditorLimits
{
    public const int MaxLineLength = 256;
    public const int MaxFindStrLen = 80;
    public const int MaxReplaceStrLen = 80;
}

/// <summary>
/// Find dialog record for search operations.
/// </summary>
public struct TFindDialogRec
{
    public string Find;
    public EditorFlags Options;

    public TFindDialogRec(string str, EditorFlags flags)
    {
        Find = str.Length <= EditorLimits.MaxFindStrLen
            ? str
            : str[..EditorLimits.MaxFindStrLen];
        Options = flags;
    }
}

/// <summary>
/// Replace dialog record for search/replace operations.
/// </summary>
public struct TReplaceDialogRec
{
    public string Find;
    public string Replace;
    public EditorFlags Options;

    public TReplaceDialogRec(string find, string replace, EditorFlags flags)
    {
        Find = find.Length <= EditorLimits.MaxFindStrLen
            ? find
            : find[..EditorLimits.MaxFindStrLen];
        Replace = replace.Length <= EditorLimits.MaxReplaceStrLen
            ? replace
            : replace[..EditorLimits.MaxReplaceStrLen];
        Options = flags;
    }
}

/// <summary>
/// Memo data structure for TMemo.GetData/SetData operations.
/// </summary>
public struct TMemoData
{
    public ushort Length;
    public byte[] Buffer;

    public TMemoData(int bufferSize)
    {
        Length = 0;
        Buffer = new byte[bufferSize];
    }
}
