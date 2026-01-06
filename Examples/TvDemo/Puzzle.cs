using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A view for the sliding tile puzzle game (4x4 grid).
/// </summary>
public class TPuzzleView : TView
{
    private static readonly byte[] PuzzlePalette = [0x06, 0x07];

    private static readonly char[] BoardStart =
    [
        'A', 'B', 'C', 'D',
        'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L',
        'M', 'N', 'O', ' '
    ];

    private static readonly byte[] Map =
    [
        0, 1, 0, 1,
        1, 0, 1, 0,
        0, 1, 0, 1,
        1, 0, 1
    ];

    private const string Solution = "ABCDEFGHIJKLMNO ";

    private readonly char[,] _board = new char[6, 6];
    private int _moves;
    private bool _solved;
    private readonly Random _random = new();

    public TPuzzleView(TRect bounds) : base(bounds)
    {
        Options |= OptionFlags.ofSelectable;

        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 6; j++)
                _board[i, j] = ' ';

        for (int i = 0; i <= 3; i++)
            for (int j = 0; j <= 3; j++)
                _board[i, j] = BoardStart[i * 4 + j];

        Scramble();
    }

    public override TPalette GetPalette()
    {
        return new TPalette(PuzzlePalette);
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var colorBack = GetColor(1).Normal;
        var color0 = colorBack;
        var color1 = _solved ? colorBack : GetColor(2).Normal;

        for (short i = 0; i <= 3; i++)
        {
            buf.MoveChar(0, ' ', colorBack, 18);
            if (i == 1)
                buf.MoveStr(13, "Move", colorBack);
            if (i == 2)
                buf.MoveStr(14, _moves.ToString(), colorBack);

            for (short j = 0; j <= 3; j++)
            {
                char c = _board[i, j];
                string tmp = $" {c} ";
                if (c == ' ')
                    buf.MoveStr((short)(j * 3), tmp, color0);
                else
                    buf.MoveStr((short)(j * 3), tmp, Map[c - 'A'] == 0 ? color0 : color1);
            }
            WriteLine(0, i, 18, 1, buf);
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (_solved && (ev.What & (EventConstants.evKeyboard | EventConstants.evMouse)) != 0)
        {
            Scramble();
            ClearEvent(ref ev);
        }

        if (ev.What == EventConstants.evMouseDown)
        {
            MoveTile(ev.Mouse.Where);
            ClearEvent(ref ev);
            WinCheck();
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            MoveKey(ev.KeyDown.KeyCode);
            ClearEvent(ref ev);
            WinCheck();
        }
    }

    public void MoveKey(ushort key)
    {
        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] == ' ')
                break;

        int x = i % 4;
        int y = i / 4;

        switch (key)
        {
            case KeyConstants.kbDown:
                if (y > 0)
                {
                    _board[y, x] = _board[y - 1, x];
                    _board[y - 1, x] = ' ';
                    if (_moves < 1000) _moves++;
                }
                break;

            case KeyConstants.kbUp:
                if (y < 3)
                {
                    _board[y, x] = _board[y + 1, x];
                    _board[y + 1, x] = ' ';
                    if (_moves < 1000) _moves++;
                }
                break;

            case KeyConstants.kbRight:
                if (x > 0)
                {
                    _board[y, x] = _board[y, x - 1];
                    _board[y, x - 1] = ' ';
                    if (_moves < 1000) _moves++;
                }
                break;

            case KeyConstants.kbLeft:
                if (x < 3)
                {
                    _board[y, x] = _board[y, x + 1];
                    _board[y, x + 1] = ' ';
                    if (_moves < 1000) _moves++;
                }
                break;
        }
        DrawView();
    }

    public void MoveTile(TPoint p)
    {
        p = MakeLocal(p);

        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] == ' ')
                break;

        int x = p.X / 3;
        int y = p.Y;

        switch (y * 4 + x - i)
        {
            case -4: // Piece moves down
                MoveKey(KeyConstants.kbDown);
                break;
            case -1: // Piece moves right
                MoveKey(KeyConstants.kbRight);
                break;
            case 1: // Piece moves left
                MoveKey(KeyConstants.kbLeft);
                break;
            case 4: // Piece moves up
                MoveKey(KeyConstants.kbUp);
                break;
        }
        DrawView();
    }

    public void Scramble()
    {
        _moves = 0;
        _solved = false;
        do
        {
            switch (_random.Next(4))
            {
                case 0: MoveKey(KeyConstants.kbUp); break;
                case 1: MoveKey(KeyConstants.kbDown); break;
                case 2: MoveKey(KeyConstants.kbRight); break;
                case 3: MoveKey(KeyConstants.kbLeft); break;
            }
        } while (_moves++ <= 500);

        _moves = 0;
        DrawView();
    }

    public void WinCheck()
    {
        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] != Solution[i])
                break;

        if (i == 16)
            _solved = true;
        DrawView();
    }
}

/// <summary>
/// A window containing the sliding tile puzzle.
/// </summary>
public class TPuzzleWindow : TWindow
{
    public TPuzzleWindow()
        : base(new TRect(1, 1, 21, 7), "Puzzle", WindowConstants.wnNoNumber)
    {
        Flags &= unchecked((byte)~(WindowFlags.wfZoom | WindowFlags.wfGrow));
        GrowMode = 0;

        var r = GetExtent();
        r.Grow(-1, -1);
        Insert(new TPuzzleView(r));
    }
}
