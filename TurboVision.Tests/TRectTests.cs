namespace TurboVision.Tests;

using TurboVision.Core;

/// <summary>
/// Tests for TRect geometry operations.
/// </summary>
[TestClass]
public class TRectTests
{
    [TestMethod]
    public void TRect_DefaultConstructor_ShouldCreateEmptyRect()
    {
        var rect = new TRect();

        Assert.AreEqual(0, rect.A.X);
        Assert.AreEqual(0, rect.A.Y);
        Assert.AreEqual(0, rect.B.X);
        Assert.AreEqual(0, rect.B.Y);
        Assert.IsTrue(rect.IsEmpty);
    }

    [TestMethod]
    public void TRect_CoordinateConstructor_ShouldSetPoints()
    {
        var rect = new TRect(10, 20, 30, 40);

        Assert.AreEqual(10, rect.A.X);
        Assert.AreEqual(20, rect.A.Y);
        Assert.AreEqual(30, rect.B.X);
        Assert.AreEqual(40, rect.B.Y);
        Assert.IsFalse(rect.IsEmpty);
    }

    [TestMethod]
    public void TRect_PointConstructor_ShouldSetPoints()
    {
        var a = new TPoint(5, 10);
        var b = new TPoint(15, 25);
        var rect = new TRect(a, b);

        Assert.AreEqual(a, rect.A);
        Assert.AreEqual(b, rect.B);
    }

    [TestMethod]
    public void TRect_IsEmpty_ShouldReturnTrueForInvertedRect()
    {
        var rect1 = new TRect(10, 10, 5, 20);   // A.X > B.X
        var rect2 = new TRect(10, 20, 20, 10);  // A.Y > B.Y
        var rect3 = new TRect(10, 10, 10, 20);  // A.X == B.X (zero width)
        var rect4 = new TRect(10, 10, 20, 10);  // A.Y == B.Y (zero height)

        Assert.IsTrue(rect1.IsEmpty, "Rect with A.X > B.X should be empty");
        Assert.IsTrue(rect2.IsEmpty, "Rect with A.Y > B.Y should be empty");
        Assert.IsTrue(rect3.IsEmpty, "Rect with zero width should be empty");
        Assert.IsTrue(rect4.IsEmpty, "Rect with zero height should be empty");
    }

    [TestMethod]
    public void TRect_Move_ShouldShiftBothPoints()
    {
        var rect = new TRect(10, 20, 30, 40);
        rect = rect.Move(5, -10);

        Assert.AreEqual(15, rect.A.X);
        Assert.AreEqual(10, rect.A.Y);
        Assert.AreEqual(35, rect.B.X);
        Assert.AreEqual(30, rect.B.Y);
    }

    [TestMethod]
    public void TRect_Grow_ShouldExpandRect()
    {
        var rect = new TRect(10, 20, 30, 40);
        rect = rect.Grow(5, 10);

        // A shrinks by delta, B expands by delta
        Assert.AreEqual(5, rect.A.X);
        Assert.AreEqual(10, rect.A.Y);
        Assert.AreEqual(35, rect.B.X);
        Assert.AreEqual(50, rect.B.Y);
    }

    [TestMethod]
    public void TRect_Grow_ShouldShrinkRectWithNegativeValues()
    {
        var rect = new TRect(10, 20, 30, 40);
        rect = rect.Grow(-5, -10);

        // Negative grow should shrink
        Assert.AreEqual(15, rect.A.X);
        Assert.AreEqual(30, rect.A.Y);
        Assert.AreEqual(25, rect.B.X);
        Assert.AreEqual(30, rect.B.Y);
    }

    [TestMethod]
    public void TRect_Contains_ShouldReturnTrueForPointInside()
    {
        var rect = new TRect(10, 20, 30, 40);

        Assert.IsTrue(rect.Contains(new TPoint(10, 20)), "Top-left corner should be inside");
        Assert.IsTrue(rect.Contains(new TPoint(20, 30)), "Center should be inside");
        Assert.IsTrue(rect.Contains(new TPoint(29, 39)), "Near bottom-right should be inside");
    }

    [TestMethod]
    public void TRect_Contains_ShouldReturnFalseForPointOutside()
    {
        var rect = new TRect(10, 20, 30, 40);

        Assert.IsFalse(rect.Contains(new TPoint(9, 20)), "Left of rect should be outside");
        Assert.IsFalse(rect.Contains(new TPoint(10, 19)), "Above rect should be outside");
        Assert.IsFalse(rect.Contains(new TPoint(30, 20)), "Right edge should be outside (exclusive)");
        Assert.IsFalse(rect.Contains(new TPoint(10, 40)), "Bottom edge should be outside (exclusive)");
    }

    [TestMethod]
    public void TRect_Intersect_ShouldComputeOverlap()
    {
        var rect1 = new TRect(10, 10, 30, 30);
        var rect2 = new TRect(20, 20, 40, 40);
        rect1 = rect1.Intersect(rect2);

        Assert.AreEqual(20, rect1.A.X);
        Assert.AreEqual(20, rect1.A.Y);
        Assert.AreEqual(30, rect1.B.X);
        Assert.AreEqual(30, rect1.B.Y);
    }

    [TestMethod]
    public void TRect_Intersect_ShouldReturnEmptyForNoOverlap()
    {
        var rect1 = new TRect(10, 10, 20, 20);
        var rect2 = new TRect(30, 30, 40, 40);
        rect1 = rect1.Intersect(rect2);

        Assert.IsTrue(rect1.IsEmpty, "Non-overlapping rects should intersect to empty");
    }

    [TestMethod]
    public void TRect_Union_ShouldComputeBoundingBox()
    {
        var rect1 = new TRect(10, 10, 30, 30);
        var rect2 = new TRect(20, 20, 40, 40);
        rect1 = rect1.Union(rect2);

        Assert.AreEqual(10, rect1.A.X);
        Assert.AreEqual(10, rect1.A.Y);
        Assert.AreEqual(40, rect1.B.X);
        Assert.AreEqual(40, rect1.B.Y);
    }

    [TestMethod]
    public void TRect_Union_ShouldWorkWithDisjointRects()
    {
        var rect1 = new TRect(0, 0, 10, 10);
        var rect2 = new TRect(20, 20, 30, 30);
        rect1 = rect1.Union(rect2);

        Assert.AreEqual(0, rect1.A.X);
        Assert.AreEqual(0, rect1.A.Y);
        Assert.AreEqual(30, rect1.B.X);
        Assert.AreEqual(30, rect1.B.Y);
    }

    [TestMethod]
    public void TRect_Equality_ShouldWorkCorrectly()
    {
        var rect1 = new TRect(10, 20, 30, 40);
        var rect2 = new TRect(10, 20, 30, 40);
        var rect3 = new TRect(10, 20, 30, 41);

        Assert.AreEqual(rect1, rect2);
        Assert.AreNotEqual(rect1, rect3);
    }
}
