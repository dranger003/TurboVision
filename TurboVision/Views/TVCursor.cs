/*------------------------------------------------------------*/
/* filename -       TVCursor.cs                               */
/*                                                            */
/* function(s)                                                */
/*                  TVCursor helper structure                 */
/*------------------------------------------------------------*/
/*
 *      Turbo Vision - C# Port
 *      Port of tvcursor.cpp from upstream Turbo Vision
 *
 *      Upstream: Reference/tvision/source/tvision/tvcursor.cpp
 */

using TurboVision.Platform;

namespace TurboVision.Views;

/// <summary>
/// Helper structure for cursor positioning and visibility.
/// Port of TVCursor from upstream tvcursor.cpp.
///
/// This structure implements the algorithm to determine where the cursor should
/// be displayed on screen, taking into account view hierarchy, visibility, and
/// clipping from parent containers.
/// </summary>
internal ref struct TVCursor
{
    private readonly TView _self;
    private int _x;
    private int _y;

    public TVCursor(TView self)
    {
        _self = self;
        _x = self.Cursor.X;
        _y = self.Cursor.Y;
    }

    public void Reset()
    {
        int caretSize = ComputeCaretSize();
        if (caretSize > 0)
        {
            TScreen.Driver?.SetCursorPosition(_x, _y);
        }
        TScreen.Driver?.SetCursorType((ushort)caretSize);
    }

    private int ComputeCaretSize()
    {
        // Check all required flags: sfVisible, sfCursorVis, sfFocused
        // The condition !(~state & (flags)) checks if all flags are set
        ushort requiredFlags = StateFlags.sfVisible | StateFlags.sfCursorVis | StateFlags.sfFocused;
        if ((~_self.State & requiredFlags) != 0)
            return 0;

        var v = _self;
        while (_y >= 0 && _y < v.Size.Y && _x >= 0 && _x < v.Size.X)
        {
            _y += v.Origin.Y;
            _x += v.Origin.X;

            if (v.Owner != null)
            {
                if ((v.Owner.State & StateFlags.sfVisible) != 0)
                {
                    if (CaretCovered(v))
                        break;
                    v = v.Owner;
                }
                else
                {
                    break;
                }
            }
            else
            {
                // Reached top of hierarchy - cursor is visible
                return DecideCaretSize();
            }
        }
        return 0;
    }

    private bool CaretCovered(TView v)
    {
        if (v.Owner?.Last == null)
            return false;

        var u = v.Owner.Last.Next;
        while (u != null && u != v)
        {
            if ((u.State & StateFlags.sfVisible) != 0 &&
                u.Origin.Y <= _y && _y < u.Origin.Y + u.Size.Y &&
                u.Origin.X <= _x && _x < u.Origin.X + u.Size.X)
            {
                return true;
            }
            u = u.Next;
        }
        return false;
    }

    private int DecideCaretSize()
    {
        if ((_self.State & StateFlags.sfCursorIns) != 0)
            return 100; // Block cursor
        return TScreen.CursorLines & 0x0F; // Normal cursor
    }
}
