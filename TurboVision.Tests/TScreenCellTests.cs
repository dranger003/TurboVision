namespace TurboVision.Tests;

using System.Runtime.InteropServices;
using TurboVision.Core;

/// <summary>
/// Tests for TScreenCell, TColorAttr, and TAttrPair types.
/// Based on Reference/tvision/test/platform/scrncell.test.cpp
/// </summary>
[TestClass]
public class TScreenCellTests
{
    #region TColorAttr Tests

    [TestMethod]
    public void TColorAttr_Constructor_ShouldSetForegroundAndBackground()
    {
        // Standard DOS colors: White (0x07) on Blue (0x01)
        var attr = new TColorAttr(0x07, 0x01);

        Assert.AreEqual(0x07, attr.Foreground);
        Assert.AreEqual(0x01, attr.Background);
    }

    [TestMethod]
    public void TColorAttr_Constructor_ShouldMaskForegroundTo4Bits()
    {
        // Foreground should be masked to 4 bits (0x0F)
        var attr = new TColorAttr(0xFF, 0x00);

        Assert.AreEqual(0x0F, attr.Foreground);
    }

    [TestMethod]
    public void TColorAttr_FromByte_ShouldExtractNibbles()
    {
        // BIOS attribute byte: 0x1F = Blue background (1) + Bright White foreground (F)
        TColorAttr attr = 0x1F;

        Assert.AreEqual(0x0F, attr.Foreground, "Foreground should be low nibble");
        Assert.AreEqual(0x01, attr.Background, "Background should be high nibble");
    }

    [TestMethod]
    public void TColorAttr_ToByte_ShouldPackNibbles()
    {
        var attr = new TColorAttr(0x0E, 0x01);  // Yellow (E) on Blue (1)
        byte value = attr;

        Assert.AreEqual(0x1E, value);
    }

    [TestMethod]
    public void TColorAttr_ByteConversion_ShouldRoundTrip()
    {
        for (byte b = 0; b < 0x80; b++)  // Test first 128 values
        {
            TColorAttr attr = b;
            byte result = attr;
            Assert.AreEqual(b, result, $"Byte 0x{b:X2} should round-trip");
        }
    }

    [TestMethod]
    public void TColorAttr_AllStandardDosColors_ShouldWork()
    {
        // Test all 16 foreground colors with black background
        for (byte fg = 0; fg < 16; fg++)
        {
            var attr = new TColorAttr(fg, 0);
            Assert.AreEqual(fg, attr.Foreground);
            Assert.AreEqual(0, attr.Background);
        }

        // Test all 16 background colors with white foreground
        for (byte bg = 0; bg < 16; bg++)
        {
            var attr = new TColorAttr(0x07, bg);
            Assert.AreEqual(0x07, attr.Foreground);
            Assert.AreEqual(bg, attr.Background);
        }
    }

    [TestMethod]
    public void TColorAttr_Value_ShouldContainPackedValue()
    {
        var attr = new TColorAttr(0x0E, 0x01);  // Yellow on Blue

        // Value should be (background << 4) | foreground = 0x1E
        Assert.AreEqual(0x1Eu, attr.Value);
    }

    [TestMethod]
    public void TColorAttr_Equality_ShouldWorkCorrectly()
    {
        var attr1 = new TColorAttr(0x07, 0x01);
        var attr2 = new TColorAttr(0x07, 0x01);
        var attr3 = new TColorAttr(0x08, 0x01);

        Assert.AreEqual(attr1, attr2);
        Assert.AreNotEqual(attr1, attr3);
    }

    #endregion

    #region TScreenCell Tests

    [TestMethod]
    public void TScreenCell_DefaultConstructor_ShouldCreateSpaceWithDefaultAttr()
    {
        var cell = new TScreenCell();

        Assert.AreEqual(' ', cell.Char);
        Assert.AreEqual(default(TColorAttr), cell.Attr);
        Assert.IsFalse(cell.IsWide);
        Assert.IsFalse(cell.IsWideCharTrail);
    }

    [TestMethod]
    public void TScreenCell_Constructor_ShouldSetCharAndAttr()
    {
        var attr = new TColorAttr(0x0E, 0x01);
        var cell = new TScreenCell('X', attr);

        Assert.AreEqual('X', cell.Char);
        Assert.AreEqual(attr, cell.Attr);
    }

    [TestMethod]
    public void TScreenCell_SetCell_ShouldUpdateCharAndAttr()
    {
        var cell = new TScreenCell();
        var attr = new TColorAttr(0x0A, 0x02);
        cell.SetCell('Y', attr);

        Assert.AreEqual('Y', cell.Char);
        Assert.AreEqual(attr, cell.Attr);
        Assert.IsFalse(cell.IsWide);
        Assert.IsFalse(cell.IsWideCharTrail);
    }

    [TestMethod]
    public void TScreenCell_Properties_ShouldBeSettable()
    {
        var cell = new TScreenCell();

        cell.Char = 'Z';
        Assert.AreEqual('Z', cell.Char);

        var attr = new TColorAttr(0x0C, 0x00);
        cell.Attr = attr;
        Assert.AreEqual(attr, cell.Attr);

        cell.IsWide = true;
        Assert.IsTrue(cell.IsWide);

        cell.IsWideCharTrail = true;
        Assert.IsTrue(cell.IsWideCharTrail);
    }

    [TestMethod]
    public void TScreenCell_SetCell_ShouldResetWideFlags()
    {
        var cell = new TScreenCell();
        cell.IsWide = true;
        cell.IsWideCharTrail = true;

        cell.SetCell('A', default);

        Assert.IsFalse(cell.IsWide, "SetCell should reset IsWide");
        Assert.IsFalse(cell.IsWideCharTrail, "SetCell should reset IsWideCharTrail");
    }

    #endregion

    #region TAttrPair Tests

    [TestMethod]
    public void TAttrPair_Constructor_ShouldSetNormalAndHighlight()
    {
        var normal = new TColorAttr(0x07, 0x01);
        var highlight = new TColorAttr(0x0E, 0x01);
        var pair = new TAttrPair(normal, highlight);

        Assert.AreEqual(normal, pair.Normal);
        Assert.AreEqual(highlight, pair.Highlight);
    }

    [TestMethod]
    public void TAttrPair_ByteConstructor_ShouldConvertToTColorAttr()
    {
        var pair = new TAttrPair((byte)0x17, (byte)0x1E);

        Assert.AreEqual(0x07, pair.Normal.Foreground);
        Assert.AreEqual(0x01, pair.Normal.Background);
        Assert.AreEqual(0x0E, pair.Highlight.Foreground);
        Assert.AreEqual(0x01, pair.Highlight.Background);
    }

    [TestMethod]
    public void TAttrPair_Indexer_ShouldReturnCorrectAttr()
    {
        var normal = new TColorAttr(0x07, 0x01);
        var highlight = new TColorAttr(0x0E, 0x01);
        var pair = new TAttrPair(normal, highlight);

        Assert.AreEqual(normal, pair[0]);
        Assert.AreEqual(highlight, pair[1]);
    }

    #endregion

    #region Struct Size Tests (from scrncell.test.cpp)

    [TestMethod]
    public void TColorAttr_ShouldHaveExpectedSize()
    {
        // C# TColorAttr uses uint internally (4 bytes)
        // Note: C++ uses 8 bytes due to 64-bit alignment
        int size = Marshal.SizeOf<TColorAttr>();
        Assert.AreEqual(4, size, "TColorAttr should be 4 bytes (uint)");
    }

    [TestMethod]
    public void TScreenCell_ShouldHaveReasonableSize()
    {
        // We don't require exact match with C++ due to different struct layouts
        // Just verify it's within reasonable bounds
        int size = Marshal.SizeOf<TScreenCell>();
        Assert.IsTrue(size >= 4 && size <= 32,
            $"TScreenCell size {size} should be reasonable (4-32 bytes)");
    }

    #endregion
}
