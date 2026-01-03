namespace TurboVision.Core;

/// <summary>
/// Represents a rectangle defined by two points (top-left A and bottom-right B).
/// </summary>
public record struct TRect : IEquatable<TRect>
{
    public TPoint A { get; set; }
    public TPoint B { get; set; }

    public TRect()
    {
        A = new TPoint(0, 0);
        B = new TPoint(0, 0);
    }

    public TRect(int ax, int ay, int bx, int by)
    {
        A = new TPoint(ax, ay);
        B = new TPoint(bx, by);
    }

    public TRect(TPoint a, TPoint b)
    {
        A = a;
        B = b;
    }

    /// <summary>
    /// Moves the rectangle by the specified delta.
    /// </summary>
    public TRect Move(int dx, int dy)
    {
        A = new TPoint(A.X + dx, A.Y + dy);
        B = new TPoint(B.X + dx, B.Y + dy);
        return this;
    }

    /// <summary>
    /// Grows the rectangle by the specified delta (shrinks A, expands B).
    /// </summary>
    public TRect Grow(int dx, int dy)
    {
        A = new TPoint(A.X - dx, A.Y - dy);
        B = new TPoint(B.X + dx, B.Y + dy);
        return this;
    }

    /// <summary>
    /// Intersects this rectangle with another, storing the result in this rectangle.
    /// </summary>
    public TRect Intersect(TRect r)
    {
        A = new TPoint(Math.Max(A.X, r.A.X), Math.Max(A.Y, r.A.Y));
        B = new TPoint(Math.Min(B.X, r.B.X), Math.Min(B.Y, r.B.Y));
        return this;
    }

    /// <summary>
    /// Unions this rectangle with another, storing the result in this rectangle.
    /// </summary>
    public TRect Union(TRect r)
    {
        A = new TPoint(Math.Min(A.X, r.A.X), Math.Min(A.Y, r.A.Y));
        B = new TPoint(Math.Max(B.X, r.B.X), Math.Max(B.Y, r.B.Y));
        return this;
    }

    /// <summary>
    /// Checks if this rectangle contains the specified point.
    /// </summary>
    public readonly bool Contains(TPoint p)
    {
        return p.X >= A.X && p.X < B.X && p.Y >= A.Y && p.Y < B.Y;
    }

    /// <summary>
    /// Checks if this rectangle is empty (has no area).
    /// </summary>
    public readonly bool IsEmpty
    {
        get { return A.X >= B.X || A.Y >= B.Y; }
    }
}
