using System.Text;
using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Menus;
using TurboVision.Platform;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// Delegate for editor dialog callbacks.
/// </summary>
public delegate ushort TEditorDialog(int dialog, params object[] args);

/// <summary>
/// Core text editor with gap buffer implementation.
/// Provides efficient text manipulation, selection, clipboard, undo, and search/replace.
/// </summary>
public class TEditor : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TEditor";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;
    private static readonly byte[] DefaultPalette = [0x06, 0x07];

    // Static members shared across all editor instances
    public static TEditorDialog EditorDialog { get; set; } = DefaultEditorDialog;
    public static EditorFlags EditorOptions { get; set; } = EditorFlags.BackupFiles | EditorFlags.PromptOnReplace;
    public static string FindStr { get; set; } = "";
    public static string ReplaceStr { get; set; } = "";
    public static TEditor? Clipboard { get; set; }

    // Key mapping tables
    private static readonly (ushort key, ushort cmd)[] FirstKeys =
    [
        (KeyConstants.kbCtrlA, EditorCommands.cmSelectAll),
        (KeyConstants.kbCtrlC, EditorCommands.cmPageDown),
        (KeyConstants.kbCtrlD, EditorCommands.cmCharRight),
        (KeyConstants.kbCtrlE, EditorCommands.cmLineUp),
        (KeyConstants.kbCtrlF, EditorCommands.cmWordRight),
        (KeyConstants.kbCtrlG, EditorCommands.cmDelChar),
        (KeyConstants.kbCtrlH, EditorCommands.cmBackSpace),
        (KeyConstants.kbCtrlK, 0xFF02),
        (KeyConstants.kbCtrlL, EditorCommands.cmSearchAgain),
        (KeyConstants.kbCtrlM, EditorCommands.cmNewLine),
        (KeyConstants.kbCtrlO, EditorCommands.cmIndentMode),
        (KeyConstants.kbCtrlP, EditorCommands.cmEncoding),
        (KeyConstants.kbCtrlQ, 0xFF01),
        (KeyConstants.kbCtrlR, EditorCommands.cmPageUp),
        (KeyConstants.kbCtrlS, EditorCommands.cmCharLeft),
        (KeyConstants.kbCtrlT, EditorCommands.cmDelWord),
        (KeyConstants.kbCtrlU, CommandConstants.cmUndo),
        (KeyConstants.kbCtrlV, EditorCommands.cmInsMode),
        (KeyConstants.kbCtrlX, EditorCommands.cmLineDown),
        (KeyConstants.kbCtrlY, EditorCommands.cmDelLine),
        (KeyConstants.kbLeft, EditorCommands.cmCharLeft),
        (KeyConstants.kbRight, EditorCommands.cmCharRight),
        (KeyConstants.kbAltBack, EditorCommands.cmDelWordLeft),
        (KeyConstants.kbCtrlBack, EditorCommands.cmDelWordLeft),
        (KeyConstants.kbCtrlDel, EditorCommands.cmDelWord),
        (KeyConstants.kbCtrlLeft, EditorCommands.cmWordLeft),
        (KeyConstants.kbCtrlRight, EditorCommands.cmWordRight),
        (KeyConstants.kbHome, EditorCommands.cmLineStart),
        (KeyConstants.kbEnd, EditorCommands.cmLineEnd),
        (KeyConstants.kbUp, EditorCommands.cmLineUp),
        (KeyConstants.kbDown, EditorCommands.cmLineDown),
        (KeyConstants.kbPgUp, EditorCommands.cmPageUp),
        (KeyConstants.kbPgDn, EditorCommands.cmPageDown),
        (KeyConstants.kbCtrlHome, EditorCommands.cmTextStart),
        (KeyConstants.kbCtrlEnd, EditorCommands.cmTextEnd),
        (KeyConstants.kbIns, EditorCommands.cmInsMode),
        (KeyConstants.kbDel, EditorCommands.cmDelChar),
        (KeyConstants.kbShiftIns, CommandConstants.cmPaste),
        (KeyConstants.kbShiftDel, CommandConstants.cmCut),
        (KeyConstants.kbCtrlIns, CommandConstants.cmCopy),
        ((ushort)(KeyConstants.kbCtrlDel & 0xFF00), CommandConstants.cmClear),
    ];

    private static readonly (char key, ushort cmd)[] QuickKeys =
    [
        ('A', EditorCommands.cmReplace),
        ('C', EditorCommands.cmTextEnd),
        ('D', EditorCommands.cmLineEnd),
        ('F', EditorCommands.cmFind),
        ('H', EditorCommands.cmDelStart),
        ('R', EditorCommands.cmTextStart),
        ('S', EditorCommands.cmLineStart),
        ('Y', EditorCommands.cmDelEnd),
    ];

    private static readonly (char key, ushort cmd)[] BlockKeys =
    [
        ('B', EditorCommands.cmStartSelect),
        ('C', CommandConstants.cmPaste),
        ('H', EditorCommands.cmHideSelect),
        ('K', CommandConstants.cmCopy),
        ('Y', CommandConstants.cmCut),
    ];

    // Instance members - view references (ignored for serialization, rebuilt from hierarchy)
    [JsonIgnore]
    public TScrollBar? HScrollBar { get; set; }
    [JsonIgnore]
    public TScrollBar? VScrollBar { get; set; }
    [JsonIgnore]
    public TIndicator? Indicator { get; set; }

    // Buffer and internal state - ignored for serialization (content serialized via GetContent/SetContent)
    [JsonIgnore]
    protected char[]? Buffer { get; set; }
    [JsonIgnore]
    protected uint BufSize { get; set; }
    [JsonIgnore]
    protected uint BufLen { get; set; }
    [JsonIgnore]
    protected uint GapLen { get; set; }

    // Selection state - serialized
    [JsonPropertyName("selStart")]
    protected uint SelStart { get; set; }
    [JsonPropertyName("selEnd")]
    protected uint SelEnd { get; set; }
    [JsonPropertyName("curPtr")]
    protected uint CurPtr { get; set; }

    // Cursor position - serialized
    [JsonPropertyName("curPos")]
    protected TPoint CurPos { get; set; }

    // Scroll position - serialized
    [JsonPropertyName("delta")]
    protected TPoint Delta { get; set; }

    // Runtime state - ignored
    [JsonIgnore]
    protected TPoint Limit { get; set; }
    [JsonIgnore]
    protected int DrawLine { get; set; }
    [JsonIgnore]
    protected uint DrawPtr { get; set; }
    [JsonIgnore]
    protected uint DelCount { get; set; }
    [JsonIgnore]
    protected uint InsCount { get; set; }
    [JsonIgnore]
    protected bool IsValid { get; set; }
    [JsonIgnore]
    protected bool CanUndo { get; set; } = true;

    // Editor state - serialized
    [JsonPropertyName("isModified")]
    protected bool IsModified { get; set; }

    // Runtime state - ignored
    [JsonIgnore]
    protected bool Selecting { get; set; }

    // Editor options - serialized
    [JsonPropertyName("overwrite")]
    protected bool Overwrite { get; set; }
    [JsonPropertyName("autoIndent")]
    protected bool AutoIndent { get; set; } = true;
    [JsonPropertyName("eolType")]
    protected EolType EolType { get; set; }
    [JsonPropertyName("encoding")]
    protected EncodingMode Encoding { get; set; }

    // Runtime state - ignored
    [JsonIgnore]
    protected byte LockCount { get; set; }
    [JsonIgnore]
    protected byte UpdateFlags_ { get; set; }
    [JsonIgnore]
    protected int KeyState { get; set; }

    public TEditor(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar,
                   TIndicator? indicator, uint bufSize) : base(bounds)
    {
        HScrollBar = hScrollBar;
        VScrollBar = vScrollBar;
        Indicator = indicator;
        BufSize = bufSize;
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        Options |= OptionFlags.ofSelectable;
        EventMask = EventConstants.evMouseDown | EventConstants.evKeyDown |
                    EventConstants.evCommand | EventConstants.evBroadcast;
        ShowCursor();
        InitBuffer();

        if (Buffer != null)
        {
            IsValid = true;
        }
        else
        {
            EditorDialog(EditorDialogs.edOutOfMemory);
            BufSize = 0;
            IsValid = false;
        }
        SetBufLen(0);
    }

    public override void ShutDown()
    {
        DoneBuffer();
        base.ShutDown();
    }

    /// <summary>
    /// Default editor dialog handler - returns cancel for all dialogs.
    /// Applications should replace this with their own dialog implementation.
    /// </summary>
    private static ushort DefaultEditorDialog(int dialog, params object[] args)
    {
        return CommandConstants.cmCancel;
    }

    // Buffer management

    protected virtual void InitBuffer()
    {
        Buffer = new char[BufSize];
    }

    protected virtual void DoneBuffer()
    {
        Buffer = null;
    }

    /// <summary>
    /// Gets the character at logical position P in the buffer,
    /// accounting for the gap.
    /// </summary>
    public char BufChar(uint p)
    {
        return Buffer![BufPtr(p)];
    }

    /// <summary>
    /// Converts a logical position to a physical buffer position,
    /// accounting for the gap.
    /// </summary>
    public uint BufPtr(uint p)
    {
        return p < CurPtr ? p : p + GapLen;
    }

    /// <summary>
    /// Gets a span of characters starting at position P.
    /// </summary>
    protected ReadOnlySpan<char> BufChars(uint p)
    {
        if (Encoding == EncodingMode.SingleByte)
        {
            return Buffer.AsSpan((int)BufPtr(p), 1);
        }
        else
        {
            int len = Math.Min((int)(Math.Max(Math.Max(CurPtr, BufLen) - p, 1u)), 4);
            Span<char> result = stackalloc char[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = BufChar(p + (uint)i);
            }
            return result.ToArray();
        }
    }

    public override void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        Delta = new TPoint(
            Math.Max(0, Math.Min(Delta.X, Limit.X - Size.X)),
            Math.Max(0, Math.Min(Delta.Y, Limit.Y - Size.Y))
        );
        Update(UpdateFlags.ufView);
    }

    // Navigation methods

    /// <summary>
    /// Returns the character column position for the given buffer position.
    /// </summary>
    public int CharPos(uint p, uint target)
    {
        int pos = 0;
        while (p < target)
        {
            var chars = BufChars(p);
            if (chars[0] == '\t')
                pos |= 7;
            NextChar(chars, ref p, ref pos);
        }
        return pos;
    }

    /// <summary>
    /// Returns the buffer position for a given column position on a line.
    /// </summary>
    public uint CharPtr(uint p, int target)
    {
        int pos = 0;
        uint lastP = p;
        while (pos < target && p < BufLen)
        {
            var chars = BufChars(p);
            char c = chars[0];
            if (c == '\r' || c == '\n')
                break;
            lastP = p;
            if (c == '\t')
                pos |= 7;
            NextChar(chars, ref p, ref pos);
        }
        if (pos > target)
            p = lastP;
        return p;
    }

    /// <summary>
    /// Advances position P by one character, updating width.
    /// </summary>
    private void NextChar(ReadOnlySpan<char> s, ref uint p, ref int width)
    {
        if (Encoding == EncodingMode.SingleByte || s.Length == 0)
        {
            p++;
            width++;
        }
        else
        {
            // Handle multi-byte characters
            p++;
            width++;
        }
    }

    /// <summary>
    /// Returns the position of the next character after P.
    /// </summary>
    public uint NextChar(uint p)
    {
        if (p + 1 < BufLen)
        {
            if (BufChar(p) == '\r' && BufChar(p + 1) == '\n')
                return p + 2;
            if (Encoding == EncodingMode.SingleByte)
                return p + 1;
            return p + 1; // Simplified for now
        }
        return BufLen;
    }

    /// <summary>
    /// Returns the position of the previous character before P.
    /// </summary>
    public uint PrevChar(uint p)
    {
        if (p > 1)
        {
            if (BufChar(p - 2) == '\r' && BufChar(p - 1) == '\n')
                return p - 2;
            if (Encoding == EncodingMode.SingleByte)
                return p - 1;
            return p - 1; // Simplified for now
        }
        return 0;
    }

    /// <summary>
    /// Returns the position of the start of the line containing P.
    /// </summary>
    public uint LineStart(uint p)
    {
        uint i = p;
        while (i-- > 0)
        {
            char c = BufChar(i);
            if (c == '\r')
            {
                if (i + 1 != CurPtr && i + 1 != BufLen && BufChar(i + 1) == '\n')
                    return i + 2;
                return i + 1;
            }
            else if (c == '\n')
            {
                return i + 1;
            }
        }
        return 0;
    }

    /// <summary>
    /// Returns the position of the end of the line containing P.
    /// </summary>
    public uint LineEnd(uint p)
    {
        for (uint i = p; i < BufLen; i++)
        {
            char c = BufChar(i);
            if (c == '\r' || c == '\n')
                return i;
        }
        return BufLen;
    }

    /// <summary>
    /// Returns the position of the start of the next line after P.
    /// </summary>
    public uint NextLine(uint p)
    {
        return NextChar(LineEnd(p));
    }

    /// <summary>
    /// Returns the position of the start of the previous line before P.
    /// </summary>
    public uint PrevLine(uint p)
    {
        return LineStart(PrevChar(p));
    }

    /// <summary>
    /// Returns the position after moving count lines from P.
    /// </summary>
    public uint LineMove(uint p, int count)
    {
        uint i = p;
        p = LineStart(p);
        int pos = CharPos(p, i);
        while (count != 0)
        {
            i = p;
            if (count < 0)
            {
                p = PrevLine(p);
                count++;
            }
            else
            {
                p = NextLine(p);
                count--;
            }
        }
        if (p != i)
            p = CharPtr(p, pos);
        return p;
    }

    /// <summary>
    /// Returns the position of the start of the next word after P.
    /// </summary>
    public uint NextWord(uint p)
    {
        if (p < BufLen)
        {
            char a = BufChar(p);
            char b;
            do
            {
                b = a;
                p = NextChar(p);
            } while (p < BufLen && !IsWordBoundary((a = BufChar(p)), b));
        }
        return p;
    }

    /// <summary>
    /// Returns the position of the start of the previous word before P.
    /// </summary>
    public uint PrevWord(uint p)
    {
        if (p > 0)
        {
            p = PrevChar(p);
            if (p > 0)
            {
                char a = BufChar(p);
                char b;
                do
                {
                    b = a;
                    p = PrevChar(p);
                    a = BufChar(p);
                } while (p > 0 && !IsWordBoundary(a, b));
                if (IsWordBoundary(a, b))
                    p = NextChar(p);
            }
        }
        return p;
    }

    /// <summary>
    /// Returns the indented line start position (skipping leading whitespace).
    /// </summary>
    public uint IndentedLineStart(uint p)
    {
        uint startPtr = LineStart(p);
        uint destPtr = startPtr;
        while (destPtr < BufLen)
        {
            char c = BufChar(destPtr);
            if (c != ' ' && c != '\t')
                break;
            destPtr++;
        }
        return destPtr == p ? startPtr : destPtr;
    }

    private static int GetCharType(char ch)
    {
        if (ch == '\t' || ch == ' ' || ch == '\0') return 0;
        if (ch == '\n' || ch == '\r') return 1;
        if ("!\"#$%&'()*+,-./:;<=>?@[\\]^`{|}~".Contains(ch)) return 2;
        return 3;
    }

    private static bool IsWordBoundary(char a, char b)
    {
        return GetCharType(a) != GetCharType(b);
    }

    // Selection and editing

    public bool HasSelection()
    {
        return SelStart != SelEnd;
    }

    public void HideSelect()
    {
        Selecting = false;
        SetSelect(CurPtr, CurPtr, false);
    }

    public void StartSelect()
    {
        HideSelect();
        Selecting = true;
    }

    public void SetSelect(uint newStart, uint newEnd, bool curStart)
    {
        uint p = curStart ? newStart : newEnd;
        byte flags = UpdateFlags.ufUpdate;

        if (newStart != SelStart || newEnd != SelEnd)
        {
            if (newStart != newEnd || SelStart != SelEnd)
                flags = UpdateFlags.ufView;
        }

        if (p != CurPtr)
        {
            if (p > CurPtr)
            {
                uint l = p - CurPtr;
                Array.Copy(Buffer!, (int)(CurPtr + GapLen), Buffer!, (int)CurPtr, (int)l);
                int lines = CountLines(Buffer!, (int)CurPtr, (int)l);
                CurPos = new TPoint(CurPos.X, CurPos.Y + lines);
                CurPtr = p;
            }
            else
            {
                uint l = CurPtr - p;
                CurPtr = p;
                int lines = CountLines(Buffer!, (int)CurPtr, (int)l);
                CurPos = new TPoint(CurPos.X, CurPos.Y - lines);
                Array.Copy(Buffer!, (int)CurPtr, Buffer!, (int)(CurPtr + GapLen), (int)l);
            }
            DelCount = 0;
            InsCount = 0;
            SetBufSize(BufLen);
        }

        DrawLine = CurPos.Y;
        DrawPtr = LineStart(p);
        CurPos = new TPoint(CharPos(DrawPtr, p), CurPos.Y);
        SelStart = newStart;
        SelEnd = newEnd;
        Update(flags);
    }

    public void SetCurPtr(uint p, byte selectMode)
    {
        uint anchor;
        if ((selectMode & SelectModes.smExtend) == 0)
            anchor = p;
        else if (CurPtr == SelStart)
            anchor = SelEnd;
        else
            anchor = SelStart;

        if (p < anchor)
        {
            if ((selectMode & SelectModes.smDouble) != 0)
            {
                p = PrevWord(NextWord(p));
                anchor = NextWord(PrevWord(anchor));
            }
            else if ((selectMode & SelectModes.smTriple) != 0)
            {
                p = PrevLine(NextLine(p));
                anchor = NextLine(PrevLine(anchor));
            }
            SetSelect(p, anchor, true);
        }
        else
        {
            if ((selectMode & SelectModes.smDouble) != 0)
            {
                p = NextWord(p);
                anchor = PrevWord(NextWord(anchor));
            }
            else if ((selectMode & SelectModes.smTriple) != 0)
            {
                p = NextLine(p);
                anchor = PrevLine(NextLine(anchor));
            }
            SetSelect(anchor, p, false);
        }
    }

    public void DeleteSelect()
    {
        InsertText(ReadOnlySpan<char>.Empty, false);
    }

    public void DeleteRange(uint startPtr, uint endPtr, bool delSelect)
    {
        if (HasSelection() && delSelect)
        {
            DeleteSelect();
        }
        else
        {
            SetSelect(CurPtr, endPtr, true);
            DeleteSelect();
            SetSelect(startPtr, CurPtr, false);
            DeleteSelect();
        }
    }

    // Text insertion

    public bool InsertText(ReadOnlySpan<char> text, bool selectText)
    {
        return InsertBuffer(text, 0, (uint)text.Length, CanUndo, selectText);
    }

    public bool InsertEOL(bool selectText)
    {
        string eol = EolType switch
        {
            EolType.Lf => "\n",
            EolType.Cr => "\r",
            _ => "\r\n"
        };
        return InsertText(eol.AsSpan(), selectText);
    }

    protected bool InsertBuffer(ReadOnlySpan<char> p, uint offset, uint length, bool allowUndo, bool selectText)
    {
        Selecting = false;
        uint selLen = SelEnd - SelStart;
        if (selLen == 0 && length == 0)
            return true;

        uint delLen = 0;
        if (allowUndo)
        {
            if (CurPtr == SelStart)
                delLen = selLen;
            else if (selLen > InsCount)
                delLen = selLen - InsCount;
        }

        ulong newSize = BufLen + DelCount - selLen + delLen + length;

        if (newSize > BufLen + DelCount)
        {
            if (newSize > uint.MaxValue - 0x1Fu || !SetBufSize((uint)newSize))
            {
                EditorDialog(EditorDialogs.edOutOfMemory);
                SelEnd = SelStart;
                return false;
            }
        }

        int selLines = CountLines(Buffer!, (int)BufPtr(SelStart), (int)selLen);
        if (CurPtr == SelEnd)
        {
            if (allowUndo && delLen > 0)
            {
                Array.Copy(Buffer!, (int)SelStart, Buffer!,
                    (int)(CurPtr + GapLen - DelCount - delLen), (int)delLen);
            }
            if (allowUndo)
                InsCount -= selLen - delLen;
            CurPtr = SelStart;
            CurPos = new TPoint(CurPos.X, CurPos.Y - selLines);
        }

        if (Delta.Y > CurPos.Y)
        {
            int newDeltaY = Delta.Y - selLines;
            if (newDeltaY < CurPos.Y)
                newDeltaY = CurPos.Y;
            Delta = new TPoint(Delta.X, newDeltaY);
        }

        if (length > 0)
        {
            p.Slice((int)offset, (int)length).CopyTo(Buffer.AsSpan((int)CurPtr));
        }

        int lines = CountLines(Buffer!, (int)CurPtr, (int)length);
        CurPtr += length;
        CurPos = new TPoint(CurPos.X, CurPos.Y + lines);
        DrawLine = CurPos.Y;
        DrawPtr = LineStart(CurPtr);
        CurPos = new TPoint(CharPos(DrawPtr, CurPtr), CurPos.Y);

        if (!selectText)
            SelStart = CurPtr;
        SelEnd = CurPtr;
        BufLen += length - selLen;
        GapLen -= length - selLen;

        if (allowUndo)
        {
            DelCount += delLen;
            InsCount += length;
        }

        Limit = new TPoint(Limit.X, Limit.Y + lines - selLines);
        Delta = new TPoint(Delta.X, Math.Max(0, Math.Min(Delta.Y, Limit.Y - Size.Y)));

        if (!IsClipboard())
            IsModified = true;

        SetBufSize(BufLen + DelCount);

        if (selLines == 0 && lines == 0)
            Update(UpdateFlags.ufLine);
        else
            Update(UpdateFlags.ufView);

        return true;
    }

    public uint InsertMultilineText(ReadOnlySpan<char> text)
    {
        int i = 0, j = 0;
        while (i < text.Length)
        {
            if (text[i] == '\n' || text[i] == '\r')
            {
                if (!InsertText(text.Slice(j, i - j), false)) return (uint)j;
                if (!InsertEOL(false)) return (uint)i;
                if (i + 1 < text.Length && text[i] == '\r' && text[i + 1] == '\n')
                    i++;
                j = i + 1;
            }
            i++;
        }
        if (!InsertText(text.Slice(j, i - j), false)) return (uint)j;
        return (uint)i;
    }

    public virtual bool InsertFrom(TEditor editor)
    {
        var text = new char[editor.SelEnd - editor.SelStart];
        for (uint i = editor.SelStart; i < editor.SelEnd; i++)
        {
            text[i - editor.SelStart] = editor.BufChar(i);
        }
        return InsertBuffer(text, 0, (uint)text.Length, CanUndo, IsClipboard());
    }

    // Buffer size management

    public virtual bool SetBufSize(uint newSize)
    {
        return newSize <= BufSize;
    }

    protected void SetBufLen(uint length)
    {
        BufLen = length;
        GapLen = BufSize - length;
        SelStart = 0;
        SelEnd = 0;
        CurPtr = 0;
        Delta = new TPoint(0, 0);
        CurPos = Delta;
        Limit = new TPoint(EditorLimits.MaxLineLength, CountLines(Buffer!, (int)GapLen, (int)BufLen) + 1);
        DrawLine = 0;
        DrawPtr = 0;
        DelCount = 0;
        InsCount = 0;
        IsModified = false;
        DetectEol();
        Update(UpdateFlags.ufView);
    }

    protected void DetectEol()
    {
        for (uint p = 0; p < BufLen; p++)
        {
            char c = BufChar(p);
            if (c == '\r')
            {
                EolType = (p + 1 < BufLen && BufChar(p + 1) == '\n')
                    ? EolType.CrLf
                    : EolType.Cr;
                return;
            }
            else if (c == '\n')
            {
                EolType = EolType.Lf;
                return;
            }
        }
        EolType = EolType.CrLf; // Default
    }

    // Clipboard operations

    public bool ClipCopy()
    {
        bool result = false;
        if (Clipboard != this)
        {
            if (Clipboard != null)
            {
                result = Clipboard.InsertFrom(this);
            }
            else
            {
                // Use internal clipboard
                var text = new StringBuilder();
                for (uint i = SelStart; i < SelEnd; i++)
                {
                    text.Append(BufChar(i));
                }
                TClipboard.SetText(text.ToString());
                result = true;
            }
            Selecting = false;
            Update(UpdateFlags.ufUpdate);
        }
        return result;
    }

    public void ClipCut()
    {
        if (ClipCopy())
            DeleteSelect();
    }

    public void ClipPaste()
    {
        if (Clipboard != this)
        {
            if (Clipboard != null)
            {
                InsertFrom(Clipboard);
            }
            else
            {
                var text = TClipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    InsertMultilineText(text.AsSpan());
                }
            }
        }
    }

    public bool IsClipboard()
    {
        return Clipboard == this;
    }

    // Undo

    public void Undo()
    {
        if (DelCount != 0 || InsCount != 0)
        {
            SelStart = CurPtr - InsCount;
            SelEnd = CurPtr;
            uint length = DelCount;
            DelCount = 0;
            InsCount = 0;

            var undoText = new char[length];
            Array.Copy(Buffer!, (int)(CurPtr + GapLen - length), undoText, 0, (int)length);
            InsertBuffer(undoText, 0, length, false, true);
        }
    }

    // New line with auto-indent

    public void NewLine()
    {
        uint p = LineStart(CurPtr);
        uint i = p;
        while (i < CurPtr && (Buffer![i] == ' ' || Buffer[i] == '\t'))
            i++;
        InsertEOL(false);
        if (AutoIndent)
        {
            var indent = new char[i - p];
            for (uint j = p; j < i; j++)
                indent[j - p] = Buffer![j];
            InsertText(indent, false);
        }
    }

    // Search and replace

    public bool Search(string findStr, EditorFlags opts)
    {
        uint pos = CurPtr;
        uint i;
        do
        {
            i = (opts & EditorFlags.CaseSensitive) != 0
                ? Scan(Buffer!, BufPtr(pos), BufLen - pos, findStr)
                : IScan(Buffer!, BufPtr(pos), BufLen - pos, findStr);

            if (i != SearchFlags.sfSearchFailed)
            {
                i += pos;
                if ((opts & EditorFlags.WholeWordsOnly) == 0 ||
                    !((i != 0 && IsWordChar(BufChar(i - 1))) ||
                      (i + (uint)findStr.Length != BufLen && IsWordChar(BufChar(i + (uint)findStr.Length)))))
                {
                    Lock();
                    SetSelect(i, i + (uint)findStr.Length, false);
                    TrackCursor(!CursorVisible());
                    Unlock();
                    return true;
                }
                else
                {
                    pos = i + 1;
                }
            }
        } while (i != SearchFlags.sfSearchFailed);
        return false;
    }

    private static bool IsWordChar(char ch)
    {
        return !" !\"#$%&'()*+,-./:;<=>?@[\\]^`{|}~\0".Contains(ch);
    }

    private static uint Scan(char[] block, uint start, uint size, string str)
    {
        if (string.IsNullOrEmpty(str)) return SearchFlags.sfSearchFailed;
        uint len = (uint)str.Length;
        for (uint i = 0; i < size; i++)
        {
            bool match = true;
            for (uint k = 0; k < len && i + k < size; k++)
            {
                if (block[start + i + k] != str[(int)k])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return SearchFlags.sfSearchFailed;
    }

    private static uint IScan(char[] block, uint start, uint size, string str)
    {
        if (string.IsNullOrEmpty(str)) return SearchFlags.sfSearchFailed;
        uint len = (uint)str.Length;
        for (uint i = 0; i < size; i++)
        {
            bool match = true;
            for (uint k = 0; k < len && i + k < size; k++)
            {
                if (char.ToUpperInvariant(block[start + i + k]) != char.ToUpperInvariant(str[(int)k]))
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return SearchFlags.sfSearchFailed;
    }

    public void Find()
    {
        var findRec = new TFindDialogRec(FindStr, EditorOptions);
        if (EditorDialog(EditorDialogs.edFind, findRec) != CommandConstants.cmCancel)
        {
            FindStr = findRec.Find;
            EditorOptions = findRec.Options & ~EditorFlags.DoReplace;
            DoSearchReplace();
        }
    }

    public void Replace()
    {
        var replaceRec = new TReplaceDialogRec(FindStr, ReplaceStr, EditorOptions);
        if (EditorDialog(EditorDialogs.edReplace, replaceRec) != CommandConstants.cmCancel)
        {
            FindStr = replaceRec.Find;
            ReplaceStr = replaceRec.Replace;
            EditorOptions = replaceRec.Options | EditorFlags.DoReplace;
            DoSearchReplace();
        }
    }

    public void DoSearchReplace()
    {
        ushort i;
        do
        {
            i = CommandConstants.cmCancel;
            if (!Search(FindStr, EditorOptions))
            {
                if ((EditorOptions & (EditorFlags.ReplaceAll | EditorFlags.DoReplace)) !=
                    (EditorFlags.ReplaceAll | EditorFlags.DoReplace))
                {
                    EditorDialog(EditorDialogs.edSearchFailed);
                }
            }
            else if ((EditorOptions & EditorFlags.DoReplace) != 0)
            {
                i = CommandConstants.cmYes;
                if ((EditorOptions & EditorFlags.PromptOnReplace) != 0)
                {
                    var c = MakeGlobal(Cursor);
                    i = EditorDialog(EditorDialogs.edReplacePrompt, c);
                }
                if (i == CommandConstants.cmYes)
                {
                    Lock();
                    InsertText(ReplaceStr.AsSpan(), false);
                    TrackCursor(false);
                    Unlock();
                }
            }
        } while (i != CommandConstants.cmCancel && (EditorOptions & EditorFlags.ReplaceAll) != 0);
    }

    // Scrolling and display

    public void ScrollTo(int x, int y)
    {
        x = Math.Max(0, Math.Min(x, Limit.X - Size.X));
        y = Math.Max(0, Math.Min(y, Limit.Y - Size.Y));
        if (x != Delta.X || y != Delta.Y)
        {
            Delta = new TPoint(x, y);
            Update(UpdateFlags.ufView);
        }
    }

    public void TrackCursor(bool center)
    {
        if (center)
        {
            ScrollTo(CurPos.X - Size.X + 1, CurPos.Y - Size.Y / 2);
        }
        else
        {
            ScrollTo(
                Math.Max(CurPos.X - Size.X + 1, Math.Min(Delta.X, CurPos.X)),
                Math.Max(CurPos.Y - Size.Y + 1, Math.Min(Delta.Y, CurPos.Y))
            );
        }
    }

    public bool CursorVisible()
    {
        return CurPos.Y >= Delta.Y && CurPos.Y < Delta.Y + Size.Y;
    }

    // Update and drawing

    public void Lock()
    {
        LockCount++;
    }

    public void Unlock()
    {
        if (LockCount > 0)
        {
            LockCount--;
            if (LockCount == 0)
                DoUpdate();
        }
    }

    public void Update(byte flags)
    {
        UpdateFlags_ |= flags;
        if (LockCount == 0)
            DoUpdate();
    }

    public void DoUpdate()
    {
        if (UpdateFlags_ != 0)
        {
            SetCursor(CurPos.X - Delta.X, CurPos.Y - Delta.Y);
            if ((UpdateFlags_ & UpdateFlags.ufView) != 0)
            {
                DrawView();
            }
            else if ((UpdateFlags_ & UpdateFlags.ufLine) != 0)
            {
                DrawLines(CurPos.Y - Delta.Y, 1, LineStart(CurPtr));
            }

            HScrollBar?.SetParams(Delta.X, 0, Limit.X - Size.X, Size.X / 2, 1);
            VScrollBar?.SetParams(Delta.Y, 0, Limit.Y - Size.Y, Size.Y - 1, 1);
            Indicator?.SetValue(CurPos, IsModified);

            if (GetState(StateFlags.sfActive))
                UpdateCommands();

            UpdateFlags_ = 0;
        }
    }

    public override void Draw()
    {
        if (DrawLine != Delta.Y)
        {
            DrawPtr = LineMove(DrawPtr, Delta.Y - DrawLine);
            DrawLine = Delta.Y;
        }
        DrawLines(0, Size.Y, DrawPtr);
    }

    protected void DrawLines(int y, int count, uint linePtr)
    {
        var color = GetColor(0x0201);
        var buf = new TDrawBuffer();

        while (count-- > 0)
        {
            FormatLine(buf, linePtr, Delta.X + Size.X, color);
            WriteBuf(0, y, Size.X, 1, buf.Data.Slice(Delta.X, Size.X));
            linePtr = NextLine(linePtr);
            y++;
        }
    }

    protected void FormatLine(TDrawBuffer buf, uint p, int width, TAttrPair colors)
    {
        var normalColor = colors.Normal;
        var selectColor = colors.Highlight;

        var ranges = new (TColorAttr color, uint end)[]
        {
            (normalColor, SelStart),
            (selectColor, SelEnd),
            (normalColor, BufLen)
        };

        buf.Data.Clear();
        int x = 0;

        foreach (var (color, end) in ranges)
        {
            while (p < end && x < width)
            {
                char c = BufChar(p);
                if (c == '\r' || c == '\n')
                    goto fill;
                if (c == '\t')
                {
                    do
                    {
                        buf.Data[x++] = new TScreenCell(' ', color);
                    } while (x % 8 != 0 && x < width);
                    p++;
                }
                else
                {
                    buf.Data[x++] = new TScreenCell(c, color);
                    p++;
                }
            }
        }

    fill:
        var lastColor = x < width ? ranges[^1].color : normalColor;
        while (x < width)
        {
            buf.Data[x++] = new TScreenCell(' ', lastColor);
        }
    }

    // Mouse and event handling

    public uint GetMousePtr(TPoint m)
    {
        var mouse = MakeLocal(m);
        mouse = new TPoint(
            Math.Max(0, Math.Min(mouse.X, Size.X - 1)),
            Math.Max(0, Math.Min(mouse.Y, Size.Y - 1))
        );
        return CharPtr(LineMove(DrawPtr, mouse.Y + Delta.Y - DrawLine), mouse.X + Delta.X);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    protected void ConvertEvent(ref TEvent ev)
    {
        if (ev.What == EventConstants.evKeyDown)
        {
            // Handle shift+numeric keypad
            if ((ev.KeyDown.ControlKeyState & KeyConstants.kbShift) != 0 &&
                ev.KeyDown.ScanCode >= 0x47 && ev.KeyDown.ScanCode <= 0x51)
            {
                // Zero out the character code part by keeping only the scan code
                ev.KeyDown.KeyCode = (ushort)(ev.KeyDown.KeyCode & 0xFF00);
            }

            ushort key = ev.KeyDown.KeyCode;
            if (KeyState != 0)
            {
                // Convert control characters to uppercase letters
                if ((key & 0xFF) >= 0x01 && (key & 0xFF) <= 0x1A)
                    key += 0x40;
                if ((key & 0xFF) >= 0x61 && (key & 0xFF) <= 0x7A)
                    key -= 0x20;
            }

            ushort cmd = ScanKeyMap(key);
            KeyState = 0;

            if (cmd != 0)
            {
                if ((cmd & 0xFF00) == 0xFF00)
                {
                    KeyState = cmd & 0xFF;
                    ClearEvent(ref ev);
                }
                else
                {
                    ev.What = EventConstants.evCommand;
                    ev.Message.Command = cmd;
                }
            }
        }
    }

    private ushort ScanKeyMap(ushort keyCode)
    {
        byte codeLow = (byte)(keyCode & 0xFF);
        byte codeHi = (byte)(keyCode >> 8);

        // Select key table based on state
        if (KeyState == 1) // Quick keys (Ctrl+Q prefix)
        {
            char ch = char.ToUpperInvariant((char)codeLow);
            foreach (var (key, cmd) in QuickKeys)
            {
                if (key == ch) return cmd;
            }
        }
        else if (KeyState == 2) // Block keys (Ctrl+K prefix)
        {
            char ch = char.ToUpperInvariant((char)codeLow);
            foreach (var (key, cmd) in BlockKeys)
            {
                if (key == ch) return cmd;
            }
        }
        else // First keys
        {
            foreach (var (key, cmd) in FirstKeys)
            {
                byte mapLow = (byte)(key & 0xFF);
                byte mapHi = (byte)(key >> 8);
                if (mapLow == codeLow && (mapHi == 0 || mapHi == codeHi))
                    return cmd;
            }
        }
        return 0;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        bool centerCursor = !CursorVisible();
        byte selectMode = 0;

        if (Selecting ||
            (ev.What == EventConstants.evMouse && (ev.Mouse.ControlKeyState & KeyConstants.kbShift) != 0) ||
            (ev.What == EventConstants.evKeyDown && (ev.KeyDown.ControlKeyState & KeyConstants.kbShift) != 0))
        {
            selectMode = SelectModes.smExtend;
        }

        ConvertEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evMouseDown:
                HandleMouseDown(ref ev, selectMode, centerCursor);
                break;

            case EventConstants.evKeyDown:
                HandleKeyDown(ref ev, centerCursor);
                break;

            case EventConstants.evCommand:
                HandleCommand(ref ev, selectMode, centerCursor);
                break;

            case EventConstants.evBroadcast:
                HandleBroadcast(ref ev);
                break;

            default:
                return;
        }

        ClearEvent(ref ev);
    }

    private void HandleMouseDown(ref TEvent ev, byte selectMode, bool centerCursor)
    {
        if ((ev.Mouse.Buttons & EventConstants.mbRightButton) != 0)
        {
            var menu = InitContextMenu(ev.Mouse.Where);
            TMenuPopup.PopupMenu(ev.Mouse.Where, menu, Owner);
            return;
        }

        if ((ev.Mouse.Buttons & EventConstants.mbMiddleButton) != 0)
        {
            var lastMouse = MakeLocal(ev.Mouse.Where);
            while (MouseEvent(ref ev, EventConstants.evMouse))
            {
                var mouse = MakeLocal(ev.Mouse.Where);
                var d = Delta + (lastMouse - mouse);
                ScrollTo(d.X, d.Y);
                lastMouse = mouse;
            }
            return;
        }

        if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0)
            selectMode |= SelectModes.smDouble;
        else if ((ev.Mouse.EventFlags & EventConstants.meTripleClick) != 0)
            selectMode |= SelectModes.smTriple;

        do
        {
            Lock();
            if (ev.What == EventConstants.evMouseAuto)
            {
                var mouse = MakeLocal(ev.Mouse.Where);
                var d = Delta;
                if (mouse.X < 0) d = new TPoint(d.X - 1, d.Y);
                if (mouse.X >= Size.X) d = new TPoint(d.X + 1, d.Y);
                if (mouse.Y < 0) d = new TPoint(d.X, d.Y - 1);
                if (mouse.Y >= Size.Y) d = new TPoint(d.X, d.Y + 1);
                ScrollTo(d.X, d.Y);
            }
            else if (ev.What == EventConstants.evMouseWheel)
            {
                var scrollEv = ev;
                VScrollBar?.HandleEvent(ref scrollEv);
                HScrollBar?.HandleEvent(ref scrollEv);
            }
            SetCurPtr(GetMousePtr(ev.Mouse.Where), selectMode);
            selectMode |= SelectModes.smExtend;
            Unlock();
        } while (MouseEvent(ref ev, EventConstants.evMouseMove | EventConstants.evMouseAuto | EventConstants.evMouseWheel));
    }

    private void HandleKeyDown(ref TEvent ev, bool centerCursor)
    {
        if ((Encoding != EncodingMode.SingleByte && ev.KeyDown.TextLength > 0) ||
            ev.KeyDown.CharCode == 9 ||
            (ev.KeyDown.CharCode >= 32 && ev.KeyDown.CharCode < 255))
        {
            Lock();
            if ((ev.KeyDown.ControlKeyState & KeyConstants.kbPaste) != 0)
            {
                Span<char> buf = stackalloc char[512];
                while (TextEvent(ref ev, buf, out int length))
                {
                    InsertMultilineText(buf.Slice(0, length));
                }
            }
            else
            {
                if (Overwrite && !HasSelection())
                {
                    if (CurPtr != LineEnd(CurPtr))
                        SelEnd = NextChar(CurPtr);
                }

                if (Encoding != EncodingMode.SingleByte && ev.KeyDown.TextLength > 0)
                {
                    InsertText(ev.KeyDown.GetText(), false);
                }
                else
                {
                    Span<char> ch = stackalloc char[1];
                    ch[0] = (char)ev.KeyDown.CharCode;
                    InsertText(ch, false);
                }
            }
            TrackCursor(centerCursor);
            Unlock();
        }
    }

    private void HandleCommand(ref TEvent ev, byte selectMode, bool centerCursor)
    {
        switch (ev.Message.Command)
        {
            case EditorCommands.cmFind:
                Find();
                return;
            case EditorCommands.cmReplace:
                Replace();
                return;
            case EditorCommands.cmSearchAgain:
                DoSearchReplace();
                return;
            case EditorCommands.cmEncoding:
                ToggleEncoding();
                return;
        }

        Lock();
        switch (ev.Message.Command)
        {
            case CommandConstants.cmCut:
                ClipCut();
                break;
            case CommandConstants.cmCopy:
                ClipCopy();
                break;
            case CommandConstants.cmPaste:
                ClipPaste();
                break;
            case CommandConstants.cmUndo:
                Undo();
                break;
            case CommandConstants.cmClear:
                DeleteSelect();
                break;
            case EditorCommands.cmCharLeft:
                SetCurPtr(PrevChar(CurPtr), selectMode);
                break;
            case EditorCommands.cmCharRight:
                SetCurPtr(NextChar(CurPtr), selectMode);
                break;
            case EditorCommands.cmWordLeft:
                SetCurPtr(PrevWord(CurPtr), selectMode);
                break;
            case EditorCommands.cmWordRight:
                SetCurPtr(NextWord(CurPtr), selectMode);
                break;
            case EditorCommands.cmLineStart:
                SetCurPtr(AutoIndent ? IndentedLineStart(CurPtr) : LineStart(CurPtr), selectMode);
                break;
            case EditorCommands.cmLineEnd:
                SetCurPtr(LineEnd(CurPtr), selectMode);
                break;
            case EditorCommands.cmLineUp:
                SetCurPtr(LineMove(CurPtr, -1), selectMode);
                break;
            case EditorCommands.cmLineDown:
                SetCurPtr(LineMove(CurPtr, 1), selectMode);
                break;
            case EditorCommands.cmPageUp:
                SetCurPtr(LineMove(CurPtr, -(Size.Y - 1)), selectMode);
                break;
            case EditorCommands.cmPageDown:
                SetCurPtr(LineMove(CurPtr, Size.Y - 1), selectMode);
                break;
            case EditorCommands.cmTextStart:
                SetCurPtr(0, selectMode);
                break;
            case EditorCommands.cmTextEnd:
                SetCurPtr(BufLen, selectMode);
                break;
            case EditorCommands.cmNewLine:
                NewLine();
                break;
            case EditorCommands.cmBackSpace:
                DeleteRange(PrevChar(CurPtr), CurPtr, true);
                break;
            case EditorCommands.cmDelChar:
                DeleteRange(CurPtr, NextChar(CurPtr), true);
                break;
            case EditorCommands.cmDelWord:
                DeleteRange(CurPtr, NextWord(CurPtr), false);
                break;
            case EditorCommands.cmDelWordLeft:
                DeleteRange(PrevWord(CurPtr), CurPtr, false);
                break;
            case EditorCommands.cmDelStart:
                DeleteRange(LineStart(CurPtr), CurPtr, false);
                break;
            case EditorCommands.cmDelEnd:
                DeleteRange(CurPtr, LineEnd(CurPtr), false);
                break;
            case EditorCommands.cmDelLine:
                DeleteRange(LineStart(CurPtr), NextLine(CurPtr), false);
                break;
            case EditorCommands.cmInsMode:
                ToggleInsMode();
                break;
            case EditorCommands.cmStartSelect:
                StartSelect();
                break;
            case EditorCommands.cmHideSelect:
                HideSelect();
                break;
            case EditorCommands.cmIndentMode:
                AutoIndent = !AutoIndent;
                break;
            case EditorCommands.cmSelectAll:
                SetCurPtr(0, selectMode);
                selectMode |= SelectModes.smExtend;
                SetCurPtr(BufLen, selectMode);
                break;
            default:
                Unlock();
                return;
        }
        TrackCursor(centerCursor);
        Unlock();
    }

    private void HandleBroadcast(ref TEvent ev)
    {
        if (ev.Message.Command == CommandConstants.cmScrollBarChanged)
        {
            if (ev.Message.InfoPtr == HScrollBar || ev.Message.InfoPtr == VScrollBar)
            {
                CheckScrollBar(ev, HScrollBar, ref _deltaXTemp);
                CheckScrollBar(ev, VScrollBar, ref _deltaYTemp);
                if (_deltaXTemp != Delta.X || _deltaYTemp != Delta.Y)
                {
                    Delta = new TPoint(_deltaXTemp, _deltaYTemp);
                }
            }
        }
    }

    private int _deltaXTemp, _deltaYTemp;

    private void CheckScrollBar(TEvent ev, TScrollBar? sb, ref int d)
    {
        if (ev.Message.InfoPtr == sb && sb != null && sb.Value != d)
        {
            d = sb.Value;
            Update(UpdateFlags.ufView);
        }
    }

    // Mode toggles

    public void ToggleInsMode()
    {
        Overwrite = !Overwrite;
        SetState(StateFlags.sfCursorIns, !GetState(StateFlags.sfCursorIns));
    }

    public void ToggleEncoding()
    {
        Encoding = Encoding == EncodingMode.Default
            ? EncodingMode.SingleByte
            : EncodingMode.Default;
        UpdateFlags_ |= UpdateFlags.ufView;
        SetSelect(SelStart, SelEnd, CurPtr < SelEnd);
    }

    // State management

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        switch (aState)
        {
            case StateFlags.sfActive:
                HScrollBar?.SetState(StateFlags.sfVisible, enable);
                VScrollBar?.SetState(StateFlags.sfVisible, enable);
                Indicator?.SetState(StateFlags.sfVisible, enable);
                UpdateCommands();
                break;
            case StateFlags.sfExposed:
                if (enable)
                    Unlock();
                break;
        }
    }

    // Commands

    protected void SetCmdState(ushort command, bool enable)
    {
        var s = new TCommandSet();
        s.EnableCmd(command);
        if (enable && GetState(StateFlags.sfActive))
            EnableCommands(s);
        else
            DisableCommands(s);
    }

    public virtual void UpdateCommands()
    {
        SetCmdState(CommandConstants.cmUndo, DelCount != 0 || InsCount != 0);
        if (!IsClipboard())
        {
            SetCmdState(CommandConstants.cmCut, HasSelection());
            SetCmdState(CommandConstants.cmCopy, HasSelection());
            SetCmdState(CommandConstants.cmPaste, Clipboard == null || Clipboard.HasSelection());
        }
        SetCmdState(CommandConstants.cmClear, HasSelection());
        SetCmdState(EditorCommands.cmFind, true);
        SetCmdState(EditorCommands.cmReplace, true);
        SetCmdState(EditorCommands.cmSearchAgain, true);
    }

    // Context menu

    public virtual TMenuItem InitContextMenu(TPoint where)
    {
        var undo = new TMenuItem("~U~ndo", CommandConstants.cmUndo, KeyConstants.kbCtrlU, HelpContexts.hcNoContext, "Ctrl-U");
        var paste = new TMenuItem("~P~aste", CommandConstants.cmPaste, KeyConstants.kbShiftIns, HelpContexts.hcNoContext, "Shift-Ins", undo);
        var copy = new TMenuItem("~C~opy", CommandConstants.cmCopy, KeyConstants.kbCtrlIns, HelpContexts.hcNoContext, "Ctrl-Ins", paste);
        return new TMenuItem("Cu~t~", CommandConstants.cmCut, KeyConstants.kbShiftDel, HelpContexts.hcNoContext, "Shift-Del", copy);
    }

    // Validation

    public override bool Valid(ushort command)
    {
        return IsValid;
    }

    // Helper methods

    private static int CountLines(char[] buf, int start, int count)
    {
        int lines = 0;
        for (int i = 0; i < count; i++)
        {
            if (buf[start + i] == '\r')
            {
                lines++;
                if (i + 1 < count && buf[start + i + 1] == '\n')
                    i++;
            }
            else if (buf[start + i] == '\n')
            {
                lines++;
            }
        }
        return lines;
    }
}
