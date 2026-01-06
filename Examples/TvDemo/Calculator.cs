using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// Command constants for the calculator.
/// </summary>
public static class CalcCommands
{
    public const ushort cmCalcButton = 200;
}

/// <summary>
/// Calculator state enumeration.
/// </summary>
public enum TCalcState
{
    csFirst = 1,
    csValid,
    csError
}

/// <summary>
/// A view that displays the calculator display and handles calculations.
/// </summary>
public class TCalcDisplay : TView
{
    private const int DisplayLen = 25;
    private static readonly byte[] CalcPalette = [0x13];

    private TCalcState _status;
    private string _number = "0";
    private char _sign = ' ';
    private char _operate = '=';
    private double _operand;

    public TCalcDisplay(TRect bounds) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable;
        EventMask = EventConstants.evKeyboard | EventConstants.evBroadcast;
        Clear();
    }

    public override TPalette GetPalette()
    {
        return new TPalette(CalcPalette);
    }

    public override void Draw()
    {
        var color = GetColor(1).Normal;
        var buf = new TDrawBuffer();

        int i = Size.X - _number.Length - 2;
        buf.MoveChar(0, ' ', color, Size.X);
        buf.MoveChar((short)i, _sign, color, 1);
        buf.MoveStr((short)(i + 1), _number, color);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evKeyboard:
                CalcKey((char)ev.KeyDown.CharCode);
                ClearEvent(ref ev);
                break;
            case EventConstants.evBroadcast:
                if (ev.Message.Command == CalcCommands.cmCalcButton)
                {
                    if (ev.Message.InfoPtr is TButton button && button.Title?.Length > 0)
                    {
                        CalcKey(button.Title[0]);
                        ClearEvent(ref ev);
                    }
                }
                break;
        }
    }

    private void Error()
    {
        _status = TCalcState.csError;
        _number = "Error";
        _sign = ' ';
    }

    private void Clear()
    {
        _status = TCalcState.csFirst;
        _number = "0";
        _sign = ' ';
        _operate = '=';
        _operand = 0;
    }

    private double GetDisplay()
    {
        return double.TryParse(_number, out double result) ? result : 0;
    }

    private void SetDisplay(double r)
    {
        if (r < 0.0)
        {
            _sign = '-';
            _number = (-r).ToString();
        }
        else
        {
            _sign = ' ';
            _number = r.ToString();
        }

        if (_number.Length > DisplayLen)
            Error();
    }

    private void CheckFirst()
    {
        if (_status == TCalcState.csFirst)
        {
            _status = TCalcState.csValid;
            _number = "0";
            _sign = ' ';
        }
    }

    private void CalcKey(char key)
    {
        key = char.ToUpper(key);
        if (_status == TCalcState.csError && key != 'C')
            key = ' ';

        switch (key)
        {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                CheckFirst();
                if (_number.Length < 15) // 15 is max visible display length
                {
                    if (_number == "0")
                        _number = "";
                    _number += key;
                }
                break;

            case '.':
                CheckFirst();
                if (!_number.Contains('.'))
                    _number += '.';
                break;

            case (char)8: // Backspace
            case (char)27: // Escape
                CheckFirst();
                if (_number.Length == 1)
                    _number = "0";
                else
                    _number = _number[..^1];
                break;

            case '_': // Underscore (keyboard version of +/-)
            case '\xF1': // +/- extended character
                _sign = _sign == ' ' ? '-' : ' ';
                break;

            case '+':
            case '-':
            case '*':
            case '/':
            case '=':
            case '%':
            case (char)13: // Enter
                if (_status == TCalcState.csValid)
                {
                    _status = TCalcState.csFirst;
                    double r = GetDisplay() * (_sign == '-' ? -1.0 : 1.0);
                    if (key == '%')
                    {
                        if (_operate == '+' || _operate == '-')
                            r = (_operand * r) / 100;
                        else
                            r /= 100;
                    }

                    switch (_operate)
                    {
                        case '+':
                            SetDisplay(_operand + r);
                            break;
                        case '-':
                            SetDisplay(_operand - r);
                            break;
                        case '*':
                            SetDisplay(_operand * r);
                            break;
                        case '/':
                            if (r == 0)
                                Error();
                            else
                                SetDisplay(_operand / r);
                            break;
                    }
                }
                _operate = key;
                _operand = GetDisplay() * (_sign == '-' ? -1.0 : 1.0);
                break;

            case 'C':
                Clear();
                break;
        }
        DrawView();
    }
}

/// <summary>
/// A dialog containing a calculator with buttons and display.
/// </summary>
public class TCalculator : TDialog
{
    private static readonly string[] KeyChar =
    [
        "C", "\x1B", "%", "\xF1", // 0x1B is escape, 0xF1 is +/- char
        "7", "8", "9", "/",
        "4", "5", "6", "*",
        "1", "2", "3", "-",
        "0", ".", "=", "+"
    ];

    public TCalculator()
        : base(new TRect(5, 3, 29, 18), "Calculator")
    {
        Options |= OptionFlags.ofFirstClick;

        for (int i = 0; i <= 19; i++)
        {
            int x = (i % 4) * 5 + 2;
            int y = (i / 4) * 2 + 4;
            var r = new TRect(x, y, x + 5, y + 2);

            var button = new TButton(r, KeyChar[i], CalcCommands.cmCalcButton,
                CommandConstants.bfNormal | CommandConstants.bfBroadcast);
            button.Options &= unchecked((ushort)~OptionFlags.ofSelectable);
            Insert(button);
        }

        var displayRect = new TRect(3, 2, 21, 3);
        Insert(new TCalcDisplay(displayRect));
    }
}
