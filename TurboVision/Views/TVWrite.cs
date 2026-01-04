using TurboVision.Core;
using TurboVision.Platform;

namespace TurboVision.Views;

/// <summary>
/// Implements hierarchical view writing with occlusion detection and shadow support.
/// This class matches the upstream TVWrite structure from tvwrite.cpp.
/// </summary>
internal ref struct TVWrite
{
    // Coordinates and dimensions (in current owner's coordinate space)
    private int _x;      // Left edge of write region
    private int _y;      // Y coordinate of write region
    private int _count;  // Right edge of write region (exclusive)
    private int _wOffset; // Buffer offset base (original X in view coordinates)

    // Buffer being written
    private readonly ReadOnlySpan<TScreenCell> _buffer;

    // Target view that initiated the write
    private TView? _target;

    // Shadow depth counter (edx in C++)
    // When > 0, shadow effect is applied to written cells
    private int _shadowDepth;

    // Temporary storage for occlusion checks (esi in C++)
    private int _tempPos;

    /// <summary>
    /// Creates a new TVWrite instance for writing a single line.
    /// </summary>
    public TVWrite(ReadOnlySpan<TScreenCell> buffer)
    {
        _buffer = buffer;
        _x = 0;
        _y = 0;
        _count = 0;
        _wOffset = 0;
        _target = null;
        _shadowDepth = 0;
        _tempPos = 0;
    }

    /// <summary>
    /// Entry point for writing a line from a view. Matches L0 in upstream.
    /// </summary>
    /// <param name="dest">The view writing the data</param>
    /// <param name="x">X position in view coordinates</param>
    /// <param name="y">Y position in view coordinates</param>
    /// <param name="count">Number of cells to write</param>
    public void L0(TView dest, int x, int y, int count)
    {
        _x = x;
        _y = y;
        _count = count;
        _wOffset = x;
        _count += x;  // _count is now the right edge (exclusive)
        _shadowDepth = 0;

        // Clip against view's own bounds
        if (y >= 0 && y < dest.Size.Y)
        {
            if (_x < 0)
                _x = 0;
            if (_count > dest.Size.X)
                _count = dest.Size.X;
            if (_x < _count)
                L10(dest);
        }
    }

    /// <summary>
    /// Propagates write to owner's coordinate space. Matches L10 in upstream.
    /// </summary>
    private void L10(TView dest)
    {
        var owner = dest.Owner;
        if ((dest.State & StateFlags.sfVisible) != 0 && owner != null)
        {
            _target = dest;

            // Convert to owner's coordinate space
            _y += dest.Origin.Y;
            _x += dest.Origin.X;
            _count += dest.Origin.X;
            _wOffset += dest.Origin.X;

            // Clip against owner's clip rectangle
            if (owner.Clip.A.Y <= _y && _y < owner.Clip.B.Y)
            {
                if (_x < owner.Clip.A.X)
                    _x = owner.Clip.A.X;
                if (_count > owner.Clip.B.X)
                    _count = owner.Clip.B.X;
                if (_x < _count)
                    L20(owner.Last);
            }
        }
    }

    /// <summary>
    /// Checks for view occlusion and shadow regions. Matches L20 in upstream.
    /// Walks through sibling views in Z-order checking for occlusion.
    /// </summary>
    private void L20(TView? dest)
    {
        if (dest == null)
            return;

        var next = dest.Next;
        if (next == _target)
        {
            // Reached the target view, perform the actual write
            L40(next);
        }
        else if (next != null)
        {
            // Check if 'next' occludes or shadows the write area
            if ((next.State & StateFlags.sfVisible) != 0 && next.Origin.Y <= _y)
            {
                // Check if we're within this view's vertical extent
                _tempPos = next.Origin.Y + next.Size.Y;
                if (_y < _tempPos)
                {
                    // We're within the view's vertical bounds
                    _tempPos = next.Origin.X;
                    if (_x < _tempPos)
                    {
                        // Left part is not occluded
                        if (_count > _tempPos)
                        {
                            // Partial occlusion - split at left edge
                            L30(next);
                        }
                        // else: entirely to the left, continue
                    }
                    else
                    {
                        // Check right edge occlusion
                        _tempPos += next.Size.X;
                        if (_x < _tempPos)
                        {
                            // Start is within the view
                            if (_count > _tempPos)
                            {
                                // Partial occlusion - skip to right edge
                                _x = _tempPos;
                            }
                            else
                            {
                                // Completely occluded by this view
                                return;
                            }
                        }
                        // Check shadow region
                        if ((next.State & StateFlags.sfShadow) != 0 &&
                            next.Origin.Y + TView.ShadowSize.Y <= _y)
                        {
                            _tempPos += TView.ShadowSize.X;
                            if (_x < _tempPos)
                            {
                                _shadowDepth++;
                                if (_count > _tempPos)
                                {
                                    L30(next);
                                    _shadowDepth--;
                                }
                                // Continue with shadow applied
                            }
                        }
                    }
                }
                else if ((next.State & StateFlags.sfShadow) != 0 &&
                         _y < _tempPos + TView.ShadowSize.Y)
                {
                    // Below the view but in shadow region
                    _tempPos = next.Origin.X + TView.ShadowSize.X;
                    if (_x < _tempPos)
                    {
                        if (_count > _tempPos)
                        {
                            L30(next);
                        }
                        // else: entirely to the left of shadow, continue
                    }
                    else
                    {
                        _tempPos += next.Size.X;
                        if (_x < _tempPos)
                        {
                            _shadowDepth++;
                            if (_count > _tempPos)
                            {
                                L30(next);
                                _shadowDepth--;
                            }
                            // Continue with shadow
                        }
                    }
                }
            }
            L20(next);
        }
    }

    /// <summary>
    /// Recursively splits write region at occlusion boundary. Matches L30 in upstream.
    /// </summary>
    private void L30(TView dest)
    {
        // Save state
        var savedTarget = _target;
        int savedWOffset = _wOffset;
        int savedTempPos = _tempPos;
        int savedShadowDepth = _shadowDepth;
        int savedCount = _count;
        int savedY = _y;

        // Limit count to split point
        _count = _tempPos;

        // Recurse with limited region
        L20(dest);

        // Restore state
        _y = savedY;
        _count = savedCount;
        _shadowDepth = savedShadowDepth;
        _tempPos = savedTempPos;
        _wOffset = savedWOffset;
        _target = savedTarget;

        // Continue from split point
        _x = _tempPos;
    }

    /// <summary>
    /// Writes to owner's buffer and propagates up. Matches L40 in upstream.
    /// </summary>
    private void L40(TView? dest)
    {
        if (dest == null)
            return;

        var owner = dest.Owner;
        if (owner?.Buffer != null)
        {
            L50(owner);
        }
        if (owner != null && owner.LockFlag == 0)
        {
            // Propagate up to parent (eventually to screen)
            L10(owner);
        }
    }

    /// <summary>
    /// Copies cells to owner's buffer with shadow application. Matches L50 in upstream.
    /// </summary>
    private void L50(TGroup owner)
    {
        if (owner.Buffer == null)
            return;

        int dstOffset = _y * owner.Size.X + _x;
        int cellCount = _count - _x;

        // Bounds check
        if (dstOffset < 0 || dstOffset + cellCount > owner.Buffer.Length)
            return;

        int srcStart = _x - _wOffset;
        if (srcStart < 0 || srcStart + cellCount > _buffer.Length)
            return;

        if (_shadowDepth == 0)
        {
            // No shadow - direct copy
            for (int i = 0; i < cellCount; i++)
            {
                owner.Buffer[dstOffset + i] = _buffer[srcStart + i];
            }
        }
        else
        {
            // Apply shadow effect
            for (int i = 0; i < cellCount; i++)
            {
                var cell = _buffer[srcStart + i];
                cell.Attr = ApplyShadow(cell.Attr);
                owner.Buffer[dstOffset + i] = cell;
            }
        }

        // If this is the screen buffer, flush to the display
        if (owner.Buffer == TScreen.ScreenBuffer)
        {
            TScreen.Driver?.WriteBuffer(_x, _y, cellCount, 1,
                new ReadOnlySpan<TScreenCell>(owner.Buffer, dstOffset, cellCount));
        }
    }

    /// <summary>
    /// Applies shadow effect to a color attribute. Matches applyShadow in upstream.
    /// </summary>
    private static TColorAttr ApplyShadow(TColorAttr attr)
    {
        var style = attr.Style;
        if ((style & ColorStyle.slNoShadow) == 0)
        {
            // Check if background is not black
            if (attr.Background != 0)
            {
                // Use shadow attribute (dark gray on black)
                attr = new TColorAttr(
                    (byte)(TView.ShadowAttr & 0x0F),
                    (byte)((TView.ShadowAttr >> 4) & 0x0F),
                    (ushort)(style | ColorStyle.slNoShadow));
            }
            else
            {
                // Reverse shadow on black areas
                var reversed = TColorAttr.ReverseAttribute(new TColorAttr(TView.ShadowAttr));
                attr = new TColorAttr(
                    reversed.Foreground,
                    reversed.Background,
                    (ushort)(style | ColorStyle.slNoShadow));
            }
        }
        return attr;
    }
}
