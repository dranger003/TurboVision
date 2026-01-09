/*------------------------------------------------------------*/
/* filename -       TVExposed.cs                              */
/*                                                            */
/* function(s)                                                */
/*                  TVExposed helper structure                */
/*------------------------------------------------------------*/
/*
 *      Turbo Vision - C# Port
 *      Port of tvexposd.cpp from upstream Turbo Vision
 *
 *      Upstream: Reference/tvision/source/tvision/tvexposd.cpp
 */

namespace TurboVision.Views;

/// <summary>
/// Helper structure for calculating view exposure (visibility).
/// Port of TVExposd from upstream tvexposd.cpp.
///
/// This structure implements a sophisticated algorithm to determine if any part
/// of a view is actually visible on screen by checking occlusion from siblings
/// and clipping from parent containers.
/// </summary>
internal ref struct TVExposed
{
    private int _y;        // Current Y coordinate (eax in C++)
    private int _left;     // Left X bound (ebx in C++)
    private int _right;    // Right X bound (ecx in C++)
    private int _temp;     // Temporary bound (esi in C++)
    private TView? _target; // Current target view
    private readonly TView _view;

    public TVExposed(TView view)
    {
        _view = view;
        _y = 0;
        _left = 0;
        _right = 0;
        _temp = 0;
        _target = null;
    }

    public bool Check()
    {
        // L0: Check initial conditions
        if ((_view.State & StateFlags.sfExposed) == 0)
            return false;
        if (_view.Size.X <= 0 || _view.Size.Y <= 0)
            return false;

        // L1: For each row, check if any part is exposed
        for (int row = 0; row < _view.Size.Y; row++)
        {
            _y = row;
            _left = 0;
            _right = _view.Size.X;
            if (!CheckRow(_view))
                return true;
        }
        return false;
    }

    // L11: Transform coordinates to owner's space and check against clip rect
    private bool CheckRow(TView dest)
    {
        _target = dest;
        _y += dest.Origin.Y;
        _left += dest.Origin.X;
        _right += dest.Origin.X;

        var owner = dest.Owner;
        if (owner == null)
            return false;

        // Check if Y is within owner's clip rect
        if (_y < owner.Clip.A.Y)
            return true;
        if (_y >= owner.Clip.B.Y)
            return true;

        // Clamp X range to owner's clip rect
        if (_left < owner.Clip.A.X)
            _left = owner.Clip.A.X;
        if (_right > owner.Clip.B.X)
            _right = owner.Clip.B.X;

        // L13: Check if range is valid
        if (_left >= _right)
            return true;

        // L20: Start checking siblings from owner's last view
        return CheckSiblings(owner.Last);
    }

    // L10: Check if we can recurse up to owner's owner
    private bool CheckOwner(TView dest)
    {
        var owner = dest.Owner;
        if (owner == null)
            return false;
        if (owner.Buffer != null || owner.LockFlag != 0)
            return false;
        return CheckRow(owner);
    }

    // L20: Walk through siblings checking for occlusion
    private bool CheckSiblings(TView? last)
    {
        if (last == null)
            return true;

        var next = last.Next;
        if (next == null)
            return true;
        if (next == _target)
            return CheckOwner(next);
        return CheckSiblingVisibility(next);
    }

    // L21: Check if a sibling occludes the current range
    private bool CheckSiblingVisibility(TView? next)
    {
        if (next == null)
            return true;

        // Skip invisible siblings
        if ((next.State & StateFlags.sfVisible) == 0)
            return CheckSiblings(next);

        // Check Y overlap
        _temp = next.Origin.Y;
        if (_y < _temp)
            return CheckSiblings(next);
        _temp += next.Size.Y;
        if (_y >= _temp)
            return CheckSiblings(next);

        // Check X overlap - sibling's left edge
        _temp = next.Origin.X;
        if (_left < _temp)
            return CheckPartialOcclusion(next);

        // Full left side covered, check right edge
        _temp += next.Size.X;
        if (_left >= _temp)
            return CheckSiblings(next);

        // Left side is covered by sibling, move left bound to sibling's right
        _left = _temp;
        if (_left < _right)
            return CheckSiblings(next);
        return true;
    }

    // L22: Handle partial occlusion (sibling covers middle of range)
    private bool CheckPartialOcclusion(TView? next)
    {
        if (next == null)
            return true;

        // If right edge is beyond sibling's left, need to check further
        if (_right <= _temp)
            return CheckSiblings(next);

        // Check if sibling's right edge is within our range
        _temp += next.Size.X;
        if (_right > _temp)
            return CheckSplitRange(next);

        // Sibling covers right portion, shrink our range to the left
        _right = next.Origin.X;
        return CheckSiblings(next);
    }

    // L23: Handle split range (sibling in middle creates two regions to check)
    private bool CheckSplitRange(TView? next)
    {
        if (next == null)
            return true;

        // Save state for right portion
        var savedTarget = _target;
        var savedTemp = _temp;
        var savedRight = _right;
        var savedY = _y;

        // Check left portion (from _left to sibling's left edge)
        _right = next.Origin.X;
        bool result = CheckSiblings(next);

        // Restore state and check right portion
        _y = savedY;
        _right = savedRight;
        _left = savedTemp;
        _target = savedTarget;

        if (result)
            return CheckSiblings(next);
        return false;
    }
}
