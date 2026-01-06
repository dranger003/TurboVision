using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A view that displays a monthly calendar.
/// </summary>
public class TCalendarView : TView
{
    private static readonly string[] MonthNames =
    [
        "",
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    private static readonly int[] DaysInMonth =
    [
        0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    ];

    private int _month;
    private int _year;
    private int _curDay;
    private int _curMonth;
    private int _curYear;

    public TCalendarView(TRect bounds) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable;
        EventMask |= EventConstants.evMouseAuto;

        var now = DateTime.Now;
        _year = _curYear = now.Year;
        _month = _curMonth = now.Month;
        _curDay = now.Day;

        DrawView();
    }

    private static int DayOfWeek(int day, int month, int year)
    {
        if (month < 3)
        {
            month += 10;
            year--;
        }
        else
        {
            month -= 2;
        }

        int century = year / 100;
        int yr = year % 100;
        int dw = ((26 * month - 2) / 10 + day + yr + (yr / 4) + (century / 4) - (2 * century)) % 7;

        if (dw < 0)
            dw += 7;

        return dw;
    }

    public override void Draw()
    {
        int current = 1 - DayOfWeek(1, _month, _year);
        int days = DaysInMonth[_month] + ((_year % 4 == 0 && _month == 2) ? 1 : 0);
        var color = GetColor(6).Normal;
        var boldColor = GetColor(7).Normal;
        var buf = new TDrawBuffer();

        buf.MoveChar(0, ' ', color, 22);
        string header = $"{MonthNames[_month],9} {_year,4} {(char)30}  {(char)31} ";
        buf.MoveStr(0, header, color);
        WriteLine(0, 0, 22, 1, buf);

        buf.MoveChar(0, ' ', color, 22);
        buf.MoveStr(0, "Su Mo Tu We Th Fr Sa", color);
        WriteLine(0, 1, 22, 1, buf);

        for (int i = 1; i <= 6; i++)
        {
            buf.MoveChar(0, ' ', color, 22);
            for (int j = 0; j <= 6; j++)
            {
                if (current < 1 || current > days)
                {
                    buf.MoveStr((short)(j * 3), "   ", color);
                }
                else
                {
                    string dayStr = current.ToString().PadLeft(2);
                    if (_year == _curYear && _month == _curMonth && current == _curDay)
                        buf.MoveStr((short)(j * 3), dayStr, boldColor);
                    else
                        buf.MoveStr((short)(j * 3), dayStr, color);
                }
                current++;
            }
            WriteLine(0, (short)(i + 1), 22, 1, buf);
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if ((State & StateFlags.sfSelected) != 0)
        {
            if ((ev.What & (EventConstants.evMouseDown | EventConstants.evMouseAuto)) != 0)
            {
                var point = MakeLocal(ev.Mouse.Where);
                if (point.X == 15 && point.Y == 0)
                {
                    _month++;
                    if (_month > 12)
                    {
                        _year++;
                        _month = 1;
                    }
                    DrawView();
                }
                else if (point.X == 18 && point.Y == 0)
                {
                    _month--;
                    if (_month < 1)
                    {
                        _year--;
                        _month = 12;
                    }
                    DrawView();
                }
            }
            else if (ev.What == EventConstants.evKeyDown)
            {
                char ch = (char)(ev.KeyDown.KeyCode & 0xFF);
                if (ch == '+' || ev.KeyDown.KeyCode == KeyConstants.kbDown)
                {
                    _month++;
                    if (_month > 12)
                    {
                        _year++;
                        _month = 1;
                    }
                }
                else if (ch == '-' || ev.KeyDown.KeyCode == KeyConstants.kbUp)
                {
                    _month--;
                    if (_month < 1)
                    {
                        _year--;
                        _month = 12;
                    }
                }
                DrawView();
            }
        }
    }
}

/// <summary>
/// A window containing the calendar view.
/// </summary>
public class TCalendarWindow : TWindow
{
    public TCalendarWindow()
        : base(new TRect(1, 1, 23, 11), "Calendar", WindowConstants.wnNoNumber)
    {
        Flags &= unchecked((byte)~(WindowFlags.wfZoom | WindowFlags.wfGrow));
        GrowMode = 0;
        Palette = WindowPalettes.wpCyanWindow;

        var r = GetExtent();
        r.Grow(-1, -1);
        Insert(new TCalendarView(r));
    }
}
