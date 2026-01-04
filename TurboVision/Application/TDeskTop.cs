using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Container for windows (window manager).
/// </summary>
public class TDeskTop : TGroup
{
    private static readonly byte[] DefaultPalette = [0x01];
    private const char DefaultBackground = '░';

    public TBackground? Background { get; set; }
    public bool TileColumnsFirst { get; protected set; }

    // Cascade state variables (used by forEach callbacks)
    private int _cascadeNum;
    private TView? _lastView;

    // Tile state variables (used by forEach callbacks)
    private int _numTileable;
    private int _numCols;
    private int _numRows;
    private int _leftOver;
    private int _tileNum;

    public TDeskTop(TRect bounds) : base(bounds)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;

        // Disable buffering - draw directly to screen
        Options &= unchecked((ushort)~OptionFlags.ofBuffered);

        Background = InitBackground(new TRect(0, 0, bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y));
        if (Background != null)
        {
            Insert(Background);
        }
    }

    public static TBackground? InitBackground(TRect r)
    {
        return new TBackground(r, DefaultBackground);
    }

    /// <summary>
    /// Returns true if view is tileable (ofTileable option set and visible).
    /// </summary>
    private static bool Tileable(TView p)
    {
        return (p.Options & OptionFlags.ofTileable) != 0 && (p.State & StateFlags.sfVisible) != 0;
    }

    /// <summary>
    /// Counts tileable views and tracks the last one found.
    /// </summary>
    private void DoCount(TView p, object? _)
    {
        if (Tileable(p))
        {
            _cascadeNum++;
            _lastView = p;
        }
    }

    /// <summary>
    /// Cascades a single view within the given rectangle.
    /// </summary>
    private void DoCascade(TView p, object? args)
    {
        if (Tileable(p) && _cascadeNum >= 0)
        {
            var r = (TRect)args!;
            var nr = new TRect(r.A.X + _cascadeNum, r.A.Y + _cascadeNum, r.B.X, r.B.Y);
            p.Locate(ref nr);
            _cascadeNum--;
        }
    }

    /// <summary>
    /// Cascades all tileable windows within the given rectangle.
    /// </summary>
    public void Cascade(TRect r)
    {
        _cascadeNum = 0;
        _lastView = null;
        ForEach(DoCount, null);

        if (_cascadeNum > 0)
        {
            _lastView!.SizeLimits(out var min, out _);
            if (min.X > r.B.X - r.A.X - _cascadeNum ||
                min.Y > r.B.Y - r.A.Y - _cascadeNum)
            {
                TileError();
            }
            else
            {
                _cascadeNum--;
                Lock();
                ForEach(DoCascade, r);
                Unlock();
            }
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmNext:
                    if (Valid(CommandConstants.cmReleasedFocus))
                    {
                        SelectNext(false);
                    }
                    break;
                case CommandConstants.cmPrev:
                    if (Valid(CommandConstants.cmReleasedFocus))
                    {
                        Current?.PutInFrontOf(Background);
                    }
                    break;
                default:
                    return;
            }
            ClearEvent(ref ev);
        }
    }

    /// <summary>
    /// Integer square root approximation.
    /// </summary>
    private static int ISqr(int i)
    {
        int res1 = 2;
        int res2 = i / res1;
        while (Math.Abs(res1 - res2) > 1)
        {
            res1 = (res1 + res2) / 2;
            res2 = i / res1;
        }
        return res1 < res2 ? res1 : res2;
    }

    /// <summary>
    /// Finds the most equal divisors for tiling.
    /// </summary>
    private static void MostEqualDivisors(int n, out int x, out int y, bool favorY)
    {
        int i = ISqr(n);
        if (n % i != 0)
        {
            if (n % (i + 1) == 0)
            {
                i++;
            }
        }
        if (i < n / i)
        {
            i = n / i;
        }

        if (favorY)
        {
            x = n / i;
            y = i;
        }
        else
        {
            y = n / i;
            x = i;
        }
    }

    /// <summary>
    /// Counts tileable views for tile operation.
    /// </summary>
    private void DoCountTileable(TView p, object? _)
    {
        if (Tileable(p))
        {
            _numTileable++;
        }
    }

    /// <summary>
    /// Calculates divider location for proportional splitting.
    /// </summary>
    private static int DividerLoc(int lo, int hi, int num, int pos)
    {
        return (int)((long)(hi - lo) * pos / num + lo);
    }

    /// <summary>
    /// Calculates the rectangle for a tile at a given position.
    /// </summary>
    private TRect CalcTileRect(int pos, TRect r)
    {
        int x, y;
        int d = (_numCols - _leftOver) * _numRows;

        if (pos < d)
        {
            x = pos / _numRows;
            y = pos % _numRows;
        }
        else
        {
            x = (pos - d) / (_numRows + 1) + (_numCols - _leftOver);
            y = (pos - d) % (_numRows + 1);
        }

        var nRect = new TRect(
            DividerLoc(r.A.X, r.B.X, _numCols, x),
            pos >= d ? DividerLoc(r.A.Y, r.B.Y, _numRows + 1, y) : DividerLoc(r.A.Y, r.B.Y, _numRows, y),
            DividerLoc(r.A.X, r.B.X, _numCols, x + 1),
            pos >= d ? DividerLoc(r.A.Y, r.B.Y, _numRows + 1, y + 1) : DividerLoc(r.A.Y, r.B.Y, _numRows, y + 1)
        );
        return nRect;
    }

    /// <summary>
    /// Tiles a single view.
    /// </summary>
    private void DoTile(TView p, object? args)
    {
        if (Tileable(p))
        {
            var r = CalcTileRect(_tileNum, (TRect)args!);
            p.Locate(ref r);
            _tileNum--;
        }
    }

    /// <summary>
    /// Tiles all tileable windows within the given rectangle.
    /// </summary>
    public void Tile(TRect r)
    {
        _numTileable = 0;
        ForEach(DoCountTileable, null);

        if (_numTileable > 0)
        {
            MostEqualDivisors(_numTileable, out _numCols, out _numRows, !TileColumnsFirst);

            if ((r.B.X - r.A.X) / _numCols == 0 || (r.B.Y - r.A.Y) / _numRows == 0)
            {
                TileError();
            }
            else
            {
                _leftOver = _numTileable % _numCols;
                _tileNum = _numTileable - 1;
                Lock();
                ForEach(DoTile, r);
                Unlock();
            }
        }
    }

    public virtual void TileError()
    {
        // Override to handle tiling errors
    }

    public override void ShutDown()
    {
        Background = null;
        base.ShutDown();
    }
}
