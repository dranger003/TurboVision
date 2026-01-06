using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Views;
using static TurboVision.Dialogs.MsgBox;

namespace TvDemo;

/// <summary>
/// A scrollable file viewer.
/// </summary>
public class TFileViewer : TScroller
{
    private string? _fileName;
    private List<string> _fileLines = [];
    private bool _isValid = true;

    public TFileViewer(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar, string? fileName)
        : base(bounds, hScrollBar, vScrollBar)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        _isValid = true;
        ReadFile(fileName);
    }

    public override void Draw()
    {
        var c = GetColor(1).Normal;
        for (short i = 0; i < Size.Y; i++)
        {
            var b = new TDrawBuffer();
            b.MoveChar(0, ' ', c, Size.X);

            int lineIndex = Delta.Y + i;
            if (lineIndex < _fileLines.Count)
            {
                string line = _fileLines[lineIndex];
                if (Delta.X < line.Length)
                {
                    string visiblePart = line.Substring(Math.Min(Delta.X, line.Length));
                    b.MoveStr(0, visiblePart, c);
                }
            }
            WriteBuf(0, i, Size.X, 1, b);
        }
    }

    public override void ScrollDraw()
    {
        base.ScrollDraw();
        Draw();
    }

    public void ReadFile(string? fName)
    {
        _fileName = fName;
        _fileLines.Clear();

        int maxWidth = 0;

        if (fName != null && File.Exists(fName))
        {
            try
            {
                foreach (string line in File.ReadLines(fName))
                {
                    _fileLines.Add(line);
                    maxWidth = Math.Max(maxWidth, line.Length);
                }
                _isValid = true;
            }
            catch
            {
                MessageBox(MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
                    "Failed to open file '{0}'.", fName);
                _isValid = false;
            }
        }
        else if (fName != null)
        {
            MessageBox(MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
                "Failed to open file '{0}'.", fName);
            _isValid = false;
        }

        Limit = new TPoint(maxWidth, _fileLines.Count);
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if (enable && (aState & StateFlags.sfExposed) != 0)
        {
            SetLimit(Limit.X, Limit.Y);
        }
    }

    public override bool Valid(ushort command)
    {
        return _isValid;
    }

    public string? FileName => _fileName;
}

/// <summary>
/// A window containing a file viewer with scrollbars.
/// </summary>
public class TFileWindow : TWindow
{
    private static short _winNumber = 0;

    public TFileWindow(string fileName)
        : base(TApplication.DeskTop?.GetExtent() ?? new TRect(0, 0, 80, 25), fileName, _winNumber++)
    {
        Options |= OptionFlags.ofTileable;

        var r = GetExtent();
        r.Grow(-1, -1);

        Insert(new TFileViewer(r,
            StandardScrollBar(ScrollBarParts.sbHorizontal | ScrollBarParts.sbHandleKeyboard),
            StandardScrollBar(ScrollBarParts.sbVertical | ScrollBarParts.sbHandleKeyboard),
            fileName));
    }
}
