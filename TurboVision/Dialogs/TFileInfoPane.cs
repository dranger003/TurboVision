using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// File information display pane.
/// Shows the current path/wildcard and details of the focused file.
/// </summary>
public class TFileInfoPane : TView
{
    /// <summary>
    /// Streamable name for serialization.
    /// Matches upstream static const char * const _NEAR name.
    /// </summary>
    public static readonly string StreamableName = "TFileInfoPane";

    private static readonly byte[] InfoPanePalette = [0x1E];

    private TSearchRec _fileBlock = new();

    private static readonly string[] Months =
    [
        "", "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    private const string PmText = "p";
    private const string AmText = "a";

    public TFileInfoPane(TRect bounds) : base(bounds)
    {
        EventMask |= EventConstants.evBroadcast;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(1).Normal;

        // First line: path and wildcard
        b.MoveChar(0, ' ', color, Size.X);

        if (Owner is TFileDialog fileDialog)
        {
            string path = fileDialog.Directory + fileDialog.WildCard;
            try
            {
                path = PathUtils.FExpand(path);
            }
            catch
            {
                // Use path as-is if expansion fails
            }
            b.MoveStr(1, path, color);
        }

        WriteLine(0, 0, Size.X, 1, b);

        // Second line: file name and details
        b.MoveChar(0, ' ', color, Size.X);
        b.MoveStr(1, _fileBlock.Name, color);

        if (!string.IsNullOrEmpty(_fileBlock.Name))
        {
            // File size
            string sizeStr = _fileBlock.Size.ToString();
            if (Size.X > 38)
            {
                b.MoveStr(Size.X - 38, sizeStr, color);
            }

            // Date and time
            DateTime dt = _fileBlock.DateTime;

            // Month
            int month = Math.Max(1, Math.Min(12, dt.Month));
            if (Size.X > 22)
            {
                b.MoveStr(Size.X - 22, Months[month], color);
            }

            // Day
            string dayStr = dt.Day.ToString("D2");
            if (Size.X > 18)
            {
                b.MoveStr(Size.X - 18, dayStr, color);
            }

            // Comma and year
            if (Size.X > 16)
            {
                b.PutChar(Size.X - 16, ',');
            }
            string yearStr = dt.Year.ToString();
            if (Size.X > 15)
            {
                b.MoveStr(Size.X - 15, yearStr, color);
            }

            // Time
            bool pm = dt.Hour >= 12;
            int hour12 = dt.Hour % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            string hourStr = hour12.ToString("D2");
            if (Size.X > 9)
            {
                b.MoveStr(Size.X - 9, hourStr, color);
            }

            if (Size.X > 7)
            {
                b.PutChar(Size.X - 7, ':');
            }

            string minStr = dt.Minute.ToString("D2");
            if (Size.X > 6)
            {
                b.MoveStr(Size.X - 6, minStr, color);
            }

            // AM/PM
            if (Size.X > 4)
            {
                b.MoveStr(Size.X - 4, pm ? PmText : AmText, color);
            }
        }

        WriteLine(0, 1, Size.X, 1, b);

        // Fill remaining lines with spaces
        b.MoveChar(0, ' ', color, Size.X);
        WriteLine(0, 2, Size.X, Size.Y - 2, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(InfoPanePalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == FileDialogCommands.cmFileFocused)
        {
            if (ev.Message.InfoPtr is TSearchRec searchRec)
            {
                _fileBlock = new TSearchRec(
                    searchRec.Attr,
                    searchRec.Time,
                    searchRec.Size,
                    searchRec.Name
                );
            }
            else
            {
                _fileBlock = new TSearchRec();
            }
            DrawView();
        }
    }
}
