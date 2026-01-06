using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Platform;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Single-line text input field with optional validation support.
/// </summary>
public class TInputLine : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TInputLine";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x13, 0x13, 0x14, 0x15];

    // Arrow characters for scroll indication
    private const char LeftArrow = '\u25C4';  // ◄
    private const char RightArrow = '\u25BA'; // ►

    // Limit modes
    public const ushort ilMaxBytes = 0;
    public const ushort ilMaxWidth = 1;
    public const ushort ilMaxChars = 2;

    public string Data { get; set; } = "";
    public int MaxLen { get; set; }
    public int MaxWidth { get; set; }
    public int MaxChars { get; set; }
    public int CurPos { get; set; }
    public int FirstPos { get; set; }
    public int SelStart { get; set; }
    public int SelEnd { get; set; }

    [JsonIgnore]
    private int _anchor;

    // Validator support - not serialized (validators are code, not data)
    [JsonIgnore]
    private TValidator? _validator;

    // Saved state for validation rollback - runtime state
    [JsonIgnore]
    private string _oldData = "";
    [JsonIgnore]
    private int _oldCurPos;
    [JsonIgnore]
    private int _oldFirstPos;
    [JsonIgnore]
    private int _oldSelStart;
    [JsonIgnore]
    private int _oldSelEnd;

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TInputLine() : base()
    {
        MaxLen = 255;
        MaxWidth = int.MaxValue;
        MaxChars = int.MaxValue;
        State |= StateFlags.sfCursorVis;
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick;
    }

    public TInputLine(TRect bounds, int limit, ushort limitMode = ilMaxBytes)
        : this(bounds, limit, null, limitMode)
    {
    }

    public TInputLine(TRect bounds, int limit, TValidator? validator, ushort limitMode = ilMaxBytes)
        : base(bounds)
    {
        MaxLen = limitMode == ilMaxBytes ? Math.Min(Math.Max(limit - 1, 0), int.MaxValue - 1) : 255;
        MaxWidth = limitMode == ilMaxWidth ? limit : int.MaxValue;
        MaxChars = limitMode == ilMaxChars ? limit : int.MaxValue;
        _validator = validator;

        State |= StateFlags.sfCursorVis;
        Options |= OptionFlags.ofSelectable | OptionFlags.ofFirstClick;
    }

    public override int DataSize()
    {
        int dSize = 0;

        if (_validator != null)
        {
            string temp = Data;
            dSize = _validator.Transfer(ref temp, [], TVTransfer.vtDataSize);
        }

        if (dSize == 0)
        {
            dSize = MaxLen + 1;
        }

        return dSize;
    }

    private bool CanScroll(int delta)
    {
        if (delta < 0)
        {
            return FirstPos > 0;
        }
        else if (delta > 0)
        {
            return Data.Length - FirstPos + 2 > Size.X;
        }
        return false;
    }

    private int DisplayedPos(int pos)
    {
        // For simple ASCII text, displayed position equals character position
        return Math.Min(pos, Data.Length);
    }

    private int MouseDelta(TEvent ev)
    {
        var mouse = MakeLocal(ev.Mouse.Where);
        if (mouse.X <= 0)
        {
            return -1;
        }
        else if (mouse.X >= Size.X - 1)
        {
            return 1;
        }
        return 0;
    }

    private int MousePos(TEvent ev)
    {
        var mouse = MakeLocal(ev.Mouse.Where);
        mouse = new TPoint(Math.Max(mouse.X, 1), mouse.Y);
        int pos = mouse.X + FirstPos - 1;
        pos = Math.Max(pos, 0);
        pos = Math.Min(pos, Data.Length);
        return pos;
    }

    private void DeleteSelect()
    {
        if (SelStart < SelEnd)
        {
            Data = Data.Remove(SelStart, SelEnd - SelStart);
            CurPos = SelStart;
            SelStart = 0;
            SelEnd = 0;
        }
    }

    private void DeleteCurrent()
    {
        if (CurPos < Data.Length)
        {
            SelStart = CurPos;
            SelEnd = CurPos + 1;
            DeleteSelect();
        }
    }

    private void AdjustSelectBlock()
    {
        if (CurPos < _anchor)
        {
            SelStart = CurPos;
            SelEnd = _anchor;
        }
        else
        {
            SelStart = _anchor;
            SelEnd = CurPos;
        }
    }

    private static int PrevWord(string s, int pos)
    {
        for (int i = pos - 1; i >= 1; i--)
        {
            if (s[i] != ' ' && s[i - 1] == ' ')
            {
                return i;
            }
        }
        return 0;
    }

    private static int NextWord(string s, int pos)
    {
        for (int i = pos; i < s.Length - 1; i++)
        {
            if (s[i] == ' ' && s[i + 1] != ' ')
            {
                return i + 1;
            }
        }
        return s.Length;
    }

    /// <summary>
    /// Saves the current state for potential rollback during validation.
    /// </summary>
    private void SaveState()
    {
        if (_validator != null)
        {
            _oldData = Data;
            _oldCurPos = CurPos;
            _oldFirstPos = FirstPos;
            _oldSelStart = SelStart;
            _oldSelEnd = SelEnd;
        }
    }

    /// <summary>
    /// Restores the previously saved state (used when validation fails).
    /// </summary>
    private void RestoreState()
    {
        if (_validator != null)
        {
            Data = _oldData;
            CurPos = _oldCurPos;
            FirstPos = _oldFirstPos;
            SelStart = _oldSelStart;
            SelEnd = _oldSelEnd;
        }
    }

    /// <summary>
    /// Validates the current input using the validator.
    /// If validation fails, the state is restored to the previous valid state.
    /// </summary>
    /// <param name="noAutoFill">If true, suppresses auto-fill behavior.</param>
    /// <returns>True if valid or no validator, false if invalid.</returns>
    private bool CheckValid(bool noAutoFill)
    {
        if (_validator == null)
        {
            return true;
        }

        int oldLen = Data.Length;
        string newData = Data;

        if (!_validator.IsValidInput(ref newData, noAutoFill))
        {
            RestoreState();
            return false;
        }

        // Apply any modifications from the validator (e.g., auto-fill)
        int newLen = newData.Length;
        if (newLen > MaxLen)
        {
            newData = newData.Substring(0, MaxLen);
            newLen = MaxLen;
        }

        Data = newData;

        // Adjust cursor position if auto-fill added characters
        if (CurPos >= oldLen && newLen > oldLen)
        {
            CurPos = newLen;
        }

        return true;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(GetState(StateFlags.sfFocused) ? (ushort)2 : (ushort)1).Normal;
        var arrowColor = GetColor(4).Normal;

        b.MoveChar(0, ' ', color, Size.X);

        if (Size.X > 1)
        {
            // Draw the text starting from FirstPos
            int textLen = Math.Min(Data.Length - FirstPos, Size.X - 2);
            if (textLen > 0 && FirstPos < Data.Length)
            {
                b.MoveStr(1, Data.AsSpan(FirstPos, textLen), color);
            }
        }

        // Draw scroll arrows if needed
        if (CanScroll(1))
        {
            b.MoveChar(Size.X - 1, RightArrow, arrowColor, 1);
        }
        if (CanScroll(-1))
        {
            b.MoveChar(0, LeftArrow, arrowColor, 1);
        }

        // Draw selection highlight
        if (GetState(StateFlags.sfSelected))
        {
            var selColor = GetColor(3).Normal;
            int l = DisplayedPos(SelStart) - FirstPos;
            int r = DisplayedPos(SelEnd) - FirstPos;
            l = Math.Max(0, l);
            r = Math.Min(Size.X - 2, r);
            if (l < r)
            {
                for (int i = l + 1; i <= r; i++)
                {
                    if (i < b.Length)
                    {
                        b.PutAttribute(i, selColor);
                    }
                }
            }
        }

        WriteLine(0, 0, Size.X, Size.Y, b);
        SetCursor(DisplayedPos(CurPos) - FirstPos + 1, 0);
    }

    public override void GetData(Span<byte> rec)
    {
        // Try validator transfer first
        if (_validator != null)
        {
            string temp = Data;
            int transferred = _validator.Transfer(ref temp, rec, TVTransfer.vtGetData);
            if (transferred != 0)
            {
                return;
            }
        }

        // Default: copy string data
        var bytes = System.Text.Encoding.UTF8.GetBytes(Data);
        int copyLen = Math.Min(bytes.Length, rec.Length - 1);
        bytes.AsSpan(0, copyLen).CopyTo(rec);
        if (copyLen < rec.Length)
        {
            rec[copyLen] = 0; // Null terminator
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (!GetState(StateFlags.sfSelected))
        {
            return;
        }

        switch (ev.What)
        {
            case var _ when ev.What == EventConstants.evMouseDown:
                HandleMouseDown(ref ev);
                break;

            case var _ when ev.What == EventConstants.evKeyDown:
                HandleKeyDown(ref ev);
                break;

            case var _ when ev.What == EventConstants.evCommand:
                switch (ev.Message.Command)
                {
                    case CommandConstants.cmPaste:
                        {
                            SaveState();
                            string clipText = TClipboard.GetText();
                            if (clipText.Length > 0)
                            {
                                DeleteSelect();
                                if (GetState(StateFlags.sfCursorIns))
                                {
                                    // In overwrite mode, delete characters for each pasted char
                                    for (int i = 0; i < clipText.Length && CurPos < Data.Length; i++)
                                    {
                                        DeleteCurrent();
                                    }
                                }

                                if (CheckValid(true))
                                {
                                    // Insert the clipboard text
                                    int insertLen = Math.Min(clipText.Length, MaxLen - Data.Length);
                                    if (insertLen > 0)
                                    {
                                        if (FirstPos > CurPos)
                                        {
                                            FirstPos = CurPos;
                                        }
                                        Data = Data.Insert(CurPos, clipText.Substring(0, insertLen));
                                        CurPos += insertLen;
                                    }
                                    CheckValid(false);
                                }

                                SelStart = 0;
                                SelEnd = 0;
                                DrawView();
                            }
                            ClearEvent(ref ev);
                        }
                        break;

                    case CommandConstants.cmCut:
                    case CommandConstants.cmCopy:
                        if (SelStart < SelEnd)
                        {
                            TClipboard.SetText(Data.AsSpan(SelStart, SelEnd - SelStart));
                            if (ev.Message.Command == CommandConstants.cmCut)
                            {
                                SaveState();
                                DeleteSelect();
                                CheckValid(true);
                                SelStart = 0;
                                SelEnd = 0;
                                DrawView();
                            }
                        }
                        ClearEvent(ref ev);
                        break;
                }
                break;
        }
    }

    private void HandleMouseDown(ref TEvent ev)
    {
        int delta = MouseDelta(ev);
        if (CanScroll(delta))
        {
            // Scroll while mouse is held down near edges
            do
            {
                if (CanScroll(delta))
                {
                    FirstPos += delta;
                    DrawView();
                }
            } while (MouseEvent(ref ev, EventConstants.evMouseAuto));
        }
        else if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0)
        {
            SelectAll(true);
        }
        else
        {
            _anchor = MousePos(ev);
            do
            {
                if (ev.What == EventConstants.evMouseAuto)
                {
                    delta = MouseDelta(ev);
                    if (CanScroll(delta))
                    {
                        FirstPos += delta;
                    }
                }
                CurPos = MousePos(ev);
                AdjustSelectBlock();
                DrawView();
            } while (MouseEvent(ref ev, EventConstants.evMouseMove | EventConstants.evMouseAuto));
        }
        ClearEvent(ref ev);
    }

    private void HandleKeyDown(ref TEvent ev)
    {
        SaveState();
        bool extendBlock = false;
        ushort keyCode = TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode);

        // Check for shift+navigation keys to extend selection
        byte scanCode = (byte)(keyCode >> 8);
        if ((scanCode == 0x47 || scanCode == 0x4B || scanCode == 0x4D || scanCode == 0x4F ||
             scanCode == 0x73 || scanCode == 0x74) &&
            (ev.KeyDown.ControlKeyState & KeyConstants.kbShift) != 0)
        {
            keyCode = (ushort)(scanCode << 8); // Clear char code
            if (CurPos == SelEnd)
            {
                _anchor = SelStart;
            }
            else if (SelStart == SelEnd)
            {
                _anchor = CurPos;
            }
            else
            {
                _anchor = SelEnd;
            }
            extendBlock = true;
        }

        switch (keyCode)
        {
            case KeyConstants.kbLeft:
                if (CurPos > 0)
                {
                    CurPos--;
                }
                break;

            case KeyConstants.kbRight:
                if (CurPos < Data.Length)
                {
                    CurPos++;
                }
                break;

            case KeyConstants.kbCtrlLeft:
                CurPos = PrevWord(Data, CurPos);
                break;

            case KeyConstants.kbCtrlRight:
                CurPos = NextWord(Data, CurPos);
                break;

            case KeyConstants.kbHome:
                CurPos = 0;
                break;

            case KeyConstants.kbEnd:
                CurPos = Data.Length;
                break;

            case KeyConstants.kbBack:
                if (SelStart == SelEnd)
                {
                    if (CurPos > 0)
                    {
                        SelStart = CurPos - 1;
                        SelEnd = CurPos;
                    }
                }
                DeleteSelect();
                CheckValid(true);
                break;

            case KeyConstants.kbCtrlBack:
            case KeyConstants.kbAltBack:
                if (SelStart == SelEnd)
                {
                    SelStart = PrevWord(Data, CurPos);
                    SelEnd = CurPos;
                }
                DeleteSelect();
                CheckValid(true);
                break;

            case KeyConstants.kbDel:
                if (SelStart == SelEnd)
                {
                    DeleteCurrent();
                }
                else
                {
                    DeleteSelect();
                }
                CheckValid(true);
                break;

            case KeyConstants.kbCtrlDel:
                if (SelStart == SelEnd)
                {
                    SelStart = CurPos;
                    SelEnd = NextWord(Data, CurPos);
                }
                DeleteSelect();
                CheckValid(true);
                break;

            case KeyConstants.kbIns:
                SetState(StateFlags.sfCursorIns, !GetState(StateFlags.sfCursorIns));
                break;

            default:
                // Check for printable character
                char ch = (char)ev.KeyDown.CharCode;
                if (ch >= ' ' && ch != 0x7F)
                {
                    DeleteSelect();
                    if (GetState(StateFlags.sfCursorIns))
                    {
                        DeleteCurrent();
                    }

                    if (CheckValid(true))
                    {
                        // Replace tabs and newlines with spaces
                        if (ch == '\t' || ch == '\r' || ch == '\n')
                        {
                            ch = ' ';
                        }

                        // Check limits
                        if (Data.Length < MaxLen && Data.Length < MaxWidth && Data.Length < MaxChars)
                        {
                            if (FirstPos > CurPos)
                            {
                                FirstPos = CurPos;
                            }
                            Data = Data.Insert(CurPos, ch.ToString());
                            CurPos++;
                        }
                        CheckValid(false);
                    }
                }
                else if (ev.KeyDown.CharCode == 25) // Ctrl+Y - clear line
                {
                    Data = "";
                    CurPos = 0;
                }
                else
                {
                    return; // Don't clear event for unhandled keys
                }
                break;
        }

        if (extendBlock)
        {
            AdjustSelectBlock();
        }
        else
        {
            SelStart = 0;
            SelEnd = 0;
        }

        // Adjust scroll position to keep cursor visible
        int curWidth = DisplayedPos(CurPos);
        if (FirstPos > curWidth)
        {
            FirstPos = curWidth;
        }
        int i = curWidth - Size.X + 2;
        if (FirstPos < i)
        {
            FirstPos = i;
        }

        DrawView();
        ClearEvent(ref ev);
    }

    public void SelectAll(bool enable, bool scroll = true)
    {
        SelStart = 0;
        if (enable)
        {
            CurPos = Data.Length;
            SelEnd = Data.Length;
        }
        else
        {
            CurPos = 0;
            SelEnd = 0;
        }

        if (scroll)
        {
            FirstPos = Math.Max(0, DisplayedPos(CurPos) - Size.X + 2);
        }
        DrawView();
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        // Try validator transfer first
        if (_validator != null)
        {
            string temp = Data;
            // Need to convert ReadOnlySpan to Span for the transfer method
            byte[] buffer = rec.ToArray();
            int transferred = _validator.Transfer(ref temp, buffer, TVTransfer.vtSetData);
            if (transferred != 0)
            {
                Data = temp;
                SelectAll(true);
                return;
            }
        }

        // Default: parse as string
        int len = rec.IndexOf((byte)0);
        if (len < 0)
        {
            len = Math.Min(rec.Length, MaxLen);
        }
        Data = System.Text.Encoding.UTF8.GetString(rec.Slice(0, len));
        SelectAll(true);
    }

    public override void SetState(ushort aState, bool enable)
    {
        bool updateBefore = GetState(StateFlags.sfActive) && GetState(StateFlags.sfSelected);
        base.SetState(aState, enable);
        bool updateAfter = GetState(StateFlags.sfActive) && GetState(StateFlags.sfSelected);

        if (aState == StateFlags.sfSelected || (aState == StateFlags.sfActive && GetState(StateFlags.sfSelected)))
        {
            SelectAll(enable, false);
        }
    }

    /// <summary>
    /// Sets or replaces the validator for this input line.
    /// </summary>
    /// <param name="validator">The new validator, or null to remove validation.</param>
    public void SetValidator(TValidator? validator)
    {
        _validator?.Dispose();
        _validator = validator;
    }

    public override bool Valid(ushort cmd)
    {
        if (_validator != null)
        {
            if (cmd == CommandConstants.cmValid)
            {
                return _validator.Status == ValidatorStatus.vsOk;
            }
            else if (cmd != CommandConstants.cmCancel)
            {
                if (!_validator.Validate(Data))
                {
                    Select();
                    return false;
                }
            }
        }
        return true;
    }

    public override void ShutDown()
    {
        _validator?.Dispose();
        _validator = null;
        base.ShutDown();
    }
}
