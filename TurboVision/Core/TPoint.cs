namespace TurboVision.Core;

/// <summary>
/// Represents a point with X and Y coordinates.
/// </summary>
public readonly record struct TPoint(int X, int Y) : IEquatable<TPoint>
{
    public static TPoint operator +(TPoint left, TPoint right)
    {
        return new TPoint(left.X + right.X, left.Y + right.Y);
    }

    public static TPoint operator -(TPoint left, TPoint right)
    {
        return new TPoint(left.X - right.X, left.Y - right.Y);
    }
}
