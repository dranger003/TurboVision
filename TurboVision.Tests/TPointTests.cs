namespace TurboVision.Tests;

using TurboVision.Core;

/// <summary>
/// Tests for TPoint arithmetic and equality operations.
/// </summary>
[TestClass]
public class TPointTests
{
    [TestMethod]
    public void TPoint_Constructor_ShouldSetCoordinates()
    {
        var point = new TPoint(10, 20);

        Assert.AreEqual(10, point.X);
        Assert.AreEqual(20, point.Y);
    }

    [TestMethod]
    public void TPoint_Addition_ShouldAddCoordinates()
    {
        var p1 = new TPoint(10, 20);
        var p2 = new TPoint(5, 15);
        var result = p1 + p2;

        Assert.AreEqual(15, result.X);
        Assert.AreEqual(35, result.Y);
    }

    [TestMethod]
    public void TPoint_Addition_ShouldHandleNegativeValues()
    {
        var p1 = new TPoint(10, 20);
        var p2 = new TPoint(-5, -10);
        var result = p1 + p2;

        Assert.AreEqual(5, result.X);
        Assert.AreEqual(10, result.Y);
    }

    [TestMethod]
    public void TPoint_Subtraction_ShouldSubtractCoordinates()
    {
        var p1 = new TPoint(10, 20);
        var p2 = new TPoint(5, 15);
        var result = p1 - p2;

        Assert.AreEqual(5, result.X);
        Assert.AreEqual(5, result.Y);
    }

    [TestMethod]
    public void TPoint_Subtraction_ShouldHandleNegativeResults()
    {
        var p1 = new TPoint(10, 20);
        var p2 = new TPoint(15, 25);
        var result = p1 - p2;

        Assert.AreEqual(-5, result.X);
        Assert.AreEqual(-5, result.Y);
    }

    [TestMethod]
    public void TPoint_Equality_ShouldCompareBothCoordinates()
    {
        var p1 = new TPoint(10, 20);
        var p2 = new TPoint(10, 20);
        var p3 = new TPoint(10, 21);
        var p4 = new TPoint(11, 20);

        Assert.AreEqual(p1, p2);
        Assert.AreNotEqual(p1, p3);
        Assert.AreNotEqual(p1, p4);
    }

    [TestMethod]
    public void TPoint_Zero_ShouldBeIdentityForAddition()
    {
        var p = new TPoint(10, 20);
        var zero = new TPoint(0, 0);

        Assert.AreEqual(p, p + zero);
        Assert.AreEqual(p, zero + p);
    }

    [TestMethod]
    public void TPoint_AdditionAndSubtraction_ShouldBeInverse()
    {
        var p1 = new TPoint(10, 20);
        var delta = new TPoint(5, 15);

        var result = (p1 + delta) - delta;
        Assert.AreEqual(p1, result);
    }
}
