using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// File-based text editor that extends TEditor with file I/O capabilities.
/// Supports loading, saving, and backup file creation.
/// </summary>
public class TFileEditor : TEditor
{
    private const string BackupExt = ".bak";

    /// <summary>
    /// Full path to the current file being edited.
    /// Empty string indicates an untitled document.
    /// </summary>
    public string FileName { get; set; } = "";

    public TFileEditor(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar,
                       TIndicator? indicator, string? fileName)
        : base(bounds, hScrollBar, vScrollBar, indicator, 0)
    {
        // Re-initialize buffer with dynamic sizing
        DoneBuffer();
        InitBuffer();

        if (string.IsNullOrEmpty(fileName))
        {
            FileName = "";
        }
        else
        {
            FileName = Path.GetFullPath(fileName);
            if (IsValid)
            {
                IsValid = LoadFile();
            }
        }
    }

    protected override void InitBuffer()
    {
        // TFileEditor uses dynamically allocated buffer
        BufSize = 0x1000; // Initial 4KB
        Buffer = new char[BufSize];
    }

    protected override void DoneBuffer()
    {
        Buffer = null;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmSave:
                    Save();
                    ClearEvent(ref ev);
                    break;
                case CommandConstants.cmSaveAs:
                    SaveAs();
                    ClearEvent(ref ev);
                    break;
            }
        }
    }

    /// <summary>
    /// Loads the file specified by FileName into the buffer.
    /// </summary>
    public bool LoadFile()
    {
        try
        {
            if (!File.Exists(FileName))
            {
                SetBufLen(0);
                return true;
            }

            using var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            long fileSize = stream.Length;

            if (fileSize > uint.MaxValue - 0x1F || !SetBufSize((uint)fileSize))
            {
                EditorDialog(EditorDialogs.edOutOfMemory);
                return false;
            }

            // Read file content into buffer (at the end, before gap)
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            if (content.Length > BufSize)
            {
                EditorDialog(EditorDialogs.edReadError, FileName);
                return false;
            }

            for (int i = 0; i < content.Length; i++)
            {
                Buffer![BufSize - content.Length + i] = content[i];
            }

            SetBufLen((uint)content.Length);
            return true;
        }
        catch (Exception)
        {
            EditorDialog(EditorDialogs.edReadError, FileName);
            return false;
        }
    }

    /// <summary>
    /// Saves the file. If no filename is set, prompts for one.
    /// </summary>
    public bool Save()
    {
        if (string.IsNullOrEmpty(FileName))
            return SaveAs();
        return SaveFile();
    }

    /// <summary>
    /// Prompts for a filename and saves.
    /// </summary>
    public bool SaveAs()
    {
        if (EditorDialog(EditorDialogs.edSaveAs, FileName) != CommandConstants.cmCancel)
        {
            // The dialog should update FileName
            FileName = Path.GetFullPath(FileName);

            // Notify owner that title may have changed
            if (Owner != null)
            {
                var ev = new TEvent
                {
                    What = EventConstants.evBroadcast
                };
                ev.Message.Command = EditorCommands.cmUpdateTitle;
                ev.Message.InfoPtr = null;
                Owner.HandleEvent(ref ev);
            }

            bool result = SaveFile();

            if (IsClipboard())
                FileName = "";

            return result;
        }
        return false;
    }

    /// <summary>
    /// Saves the buffer content to the file.
    /// </summary>
    public bool SaveFile()
    {
        try
        {
            // Create backup if enabled
            if ((EditorOptions & EditorFlags.BackupFiles) != 0)
            {
                string backupName = Path.ChangeExtension(FileName, BackupExt);
                try
                {
                    if (File.Exists(backupName))
                        File.Delete(backupName);
                    if (File.Exists(FileName))
                        File.Move(FileName, backupName);
                }
                catch
                {
                    // Ignore backup errors
                }
            }

            using var stream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);

            // Write content before gap
            for (uint i = 0; i < CurPtr; i++)
            {
                writer.Write(Buffer![i]);
            }

            // Write content after gap
            for (uint i = CurPtr + GapLen; i < BufSize; i++)
            {
                writer.Write(Buffer![i]);
            }

            IsModified = false;
            Update(UpdateFlags.ufUpdate);
            return true;
        }
        catch (Exception)
        {
            EditorDialog(EditorDialogs.edWriteError, FileName);
            return false;
        }
    }

    public override bool SetBufSize(uint newSize)
    {
        if (newSize == 0)
            newSize = 0x1000;
        else if (newSize > uint.MaxValue - 0x1000)
            newSize = uint.MaxValue - 0x1F;
        else
            newSize = (newSize + 0x0FFF) & 0xFFFFF000; // Round up to 4KB boundary

        if (newSize != BufSize)
        {
            var temp = Buffer;
            Buffer = new char[newSize];

            if (temp != null)
            {
                // Copy content before gap
                uint copyLen = Math.Min((uint)temp.Length, newSize);
                Array.Copy(temp, 0, Buffer, 0, (int)Math.Min(CurPtr, copyLen));

                // Copy content after gap
                uint tailLen = BufLen - CurPtr + DelCount;
                uint srcStart = BufSize - tailLen;
                uint dstStart = newSize - tailLen;
                if (srcStart < temp.Length && dstStart < newSize)
                {
                    Array.Copy(temp, (int)srcStart, Buffer, (int)dstStart, (int)Math.Min(tailLen, newSize - dstStart));
                }
            }

            BufSize = newSize;
            GapLen = BufSize - BufLen;
        }
        return true;
    }

    public override void ShutDown()
    {
        SetCmdState(CommandConstants.cmSave, false);
        SetCmdState(CommandConstants.cmSaveAs, false);
        base.ShutDown();
    }

    public override void UpdateCommands()
    {
        base.UpdateCommands();
        SetCmdState(CommandConstants.cmSave, true);
        SetCmdState(CommandConstants.cmSaveAs, true);
    }

    public override bool Valid(ushort command)
    {
        if (command == CommandConstants.cmValid)
            return IsValid;

        if (IsModified)
        {
            int dialogId = string.IsNullOrEmpty(FileName)
                ? EditorDialogs.edSaveUntitled
                : EditorDialogs.edSaveModify;

            switch (EditorDialog(dialogId, FileName))
            {
                case CommandConstants.cmYes:
                    return Save();
                case CommandConstants.cmNo:
                    IsModified = false;
                    return true;
                case CommandConstants.cmCancel:
                    return false;
            }
        }
        return true;
    }
}
