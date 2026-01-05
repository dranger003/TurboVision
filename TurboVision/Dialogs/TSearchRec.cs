namespace TurboVision.Dialogs;

/// <summary>
/// File search record structure matching upstream TSearchRec.
/// Holds file attributes, time, size, and name for directory listings.
/// </summary>
public class TSearchRec
{
    /// <summary>
    /// File attributes (FA_* flags).
    /// </summary>
    public byte Attr { get; set; }

    /// <summary>
    /// Packed DOS-style timestamp.
    /// </summary>
    public int Time { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File name (without path).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Creates an empty TSearchRec.
    /// </summary>
    public TSearchRec()
    {
    }

    /// <summary>
    /// Creates a TSearchRec from a FileInfo.
    /// </summary>
    public TSearchRec(FileInfo fileInfo)
    {
        Attr = PathUtils.ToUpstreamAttr(fileInfo.Attributes);
        Time = PathUtils.ToPackedTime(fileInfo.LastWriteTime);
        Size = fileInfo.Length;
        Name = fileInfo.Name;
    }

    /// <summary>
    /// Creates a TSearchRec from a DirectoryInfo.
    /// </summary>
    public TSearchRec(DirectoryInfo dirInfo)
    {
        Attr = (byte)(PathUtils.ToUpstreamAttr(dirInfo.Attributes) | PathUtils.FA_DIREC);
        Time = PathUtils.ToPackedTime(dirInfo.LastWriteTime);
        Size = 0;
        Name = dirInfo.Name;
    }

    /// <summary>
    /// Creates a TSearchRec with specified values.
    /// </summary>
    public TSearchRec(byte attr, int time, long size, string name)
    {
        Attr = attr;
        Time = time;
        Size = size;
        Name = name;
    }

    /// <summary>
    /// Returns true if this is a directory entry.
    /// </summary>
    public bool IsDirectory
    {
        get { return (Attr & PathUtils.FA_DIREC) != 0; }
    }

    /// <summary>
    /// Gets the DateTime from the packed time value.
    /// </summary>
    public DateTime DateTime
    {
        get { return PathUtils.FromPackedTime(Time); }
    }

    /// <summary>
    /// Creates an empty/null search record.
    /// </summary>
    public static TSearchRec Empty
    {
        get { return new TSearchRec(); }
    }
}
