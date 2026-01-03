namespace TurboVision.Tests;

using TurboVision.Core;

/// <summary>
/// Tests for TDrawBuffer drawing operations.
/// </summary>
[TestClass]
public class TDrawBufferTests
{
    [TestMethod]
    public void TDrawBuffer_DefaultConstructor_ShouldCreateMaxWidthBuffer()
    {
        var buffer = new TDrawBuffer();

        Assert.AreEqual(TDrawBuffer.MaxViewWidth, buffer.Length);
    }

    [TestMethod]
    public void TDrawBuffer_WidthConstructor_ShouldCreateSpecifiedWidthBuffer()
    {
        var buffer = new TDrawBuffer(80);

        Assert.AreEqual(80, buffer.Length);
    }

    [TestMethod]
    public void TDrawBuffer_InitialContent_ShouldBeSpaces()
    {
        var buffer = new TDrawBuffer(10);

        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.AreEqual(' ', buffer[i].Char, $"Cell {i} should be space");
        }
    }

    #region MoveChar Tests

    [TestMethod]
    public void MoveChar_ShouldFillBufferWithChar()
    {
        var buffer = new TDrawBuffer(20);
        var attr = new TColorAttr(0x07, 0x01);

        buffer.MoveChar(5, 'X', attr, 10);

        // Before the fill
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(' ', buffer[i].Char);
        }

        // The filled region
        for (int i = 5; i < 15; i++)
        {
            Assert.AreEqual('X', buffer[i].Char);
            Assert.AreEqual(attr, buffer[i].Attr);
        }

        // After the fill
        for (int i = 15; i < 20; i++)
        {
            Assert.AreEqual(' ', buffer[i].Char);
        }
    }

    [TestMethod]
    public void MoveChar_ShouldClipAtBufferEnd()
    {
        var buffer = new TDrawBuffer(20);
        var attr = new TColorAttr(0x07, 0x01);

        // Start at 15, try to fill 10 chars (would go to 25, but buffer is 20)
        buffer.MoveChar(15, 'Y', attr, 10);

        for (int i = 15; i < 20; i++)
        {
            Assert.AreEqual('Y', buffer[i].Char);
        }
    }

    [TestMethod]
    public void MoveChar_ShouldDoNothingIfIndentPastBuffer()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x07, 0x01);

        buffer.MoveChar(20, 'Z', attr, 5);

        // All cells should still be spaces
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.AreEqual(' ', buffer[i].Char);
        }
    }

    #endregion

    #region MoveStr Tests

    [TestMethod]
    public void MoveStr_ShouldWriteStringToBuffer()
    {
        var buffer = new TDrawBuffer(20);
        var attr = new TColorAttr(0x0E, 0x01);

        int written = buffer.MoveStr(3, "Hello", attr);

        Assert.AreEqual(5, written);
        Assert.AreEqual('H', buffer[3].Char);
        Assert.AreEqual('e', buffer[4].Char);
        Assert.AreEqual('l', buffer[5].Char);
        Assert.AreEqual('l', buffer[6].Char);
        Assert.AreEqual('o', buffer[7].Char);

        for (int i = 3; i < 8; i++)
        {
            Assert.AreEqual(attr, buffer[i].Attr);
        }
    }

    [TestMethod]
    public void MoveStr_ShouldClipLongString()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x07, 0x01);

        int written = buffer.MoveStr(7, "Testing", attr);

        Assert.AreEqual(3, written, "Only 3 chars should fit");
        Assert.AreEqual('T', buffer[7].Char);
        Assert.AreEqual('e', buffer[8].Char);
        Assert.AreEqual('s', buffer[9].Char);
    }

    [TestMethod]
    public void MoveStr_ShouldReturnZeroIfIndentPastBuffer()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x07, 0x01);

        int written = buffer.MoveStr(20, "Test", attr);

        Assert.AreEqual(0, written);
    }

    [TestMethod]
    public void MoveStr_ShouldHandleEmptyString()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x07, 0x01);

        int written = buffer.MoveStr(0, "", attr);

        Assert.AreEqual(0, written);
    }

    #endregion

    #region MoveCStr Tests

    [TestMethod]
    public void MoveCStr_ShouldHandleShortcutHighlighting()
    {
        var buffer = new TDrawBuffer(20);
        var attrs = new TAttrPair((byte)0x17, (byte)0x1E);  // Normal: White on Blue, Highlight: Yellow on Blue

        // "~F~ile" - F should be highlighted
        int written = buffer.MoveCStr(0, "~F~ile", attrs);

        Assert.AreEqual(4, written, "4 visible characters");
        Assert.AreEqual('F', buffer[0].Char);
        Assert.AreEqual(attrs.Highlight, buffer[0].Attr, "F should be highlighted");
        Assert.AreEqual('i', buffer[1].Char);
        Assert.AreEqual(attrs.Normal, buffer[1].Attr);
        Assert.AreEqual('l', buffer[2].Char);
        Assert.AreEqual('e', buffer[3].Char);
    }

    [TestMethod]
    public void MoveCStr_ShouldHandleMultipleHighlightedChars()
    {
        var buffer = new TDrawBuffer(20);
        var attrs = new TAttrPair((byte)0x17, (byte)0x1E);

        // "~AB~CD" - AB should be highlighted
        int written = buffer.MoveCStr(0, "~AB~CD", attrs);

        Assert.AreEqual(4, written);
        Assert.AreEqual('A', buffer[0].Char);
        Assert.AreEqual(attrs.Highlight, buffer[0].Attr);
        Assert.AreEqual('B', buffer[1].Char);
        Assert.AreEqual(attrs.Highlight, buffer[1].Attr);
        Assert.AreEqual('C', buffer[2].Char);
        Assert.AreEqual(attrs.Normal, buffer[2].Attr);
        Assert.AreEqual('D', buffer[3].Char);
        Assert.AreEqual(attrs.Normal, buffer[3].Attr);
    }

    [TestMethod]
    public void MoveCStr_ShouldHandleNoTildes()
    {
        var buffer = new TDrawBuffer(20);
        var attrs = new TAttrPair((byte)0x17, (byte)0x1E);

        int written = buffer.MoveCStr(0, "Plain", attrs);

        Assert.AreEqual(5, written);
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(attrs.Normal, buffer[i].Attr, $"Char {i} should have normal attr");
        }
    }

    [TestMethod]
    public void MoveCStr_ShouldToggleHighlightOnEachTilde()
    {
        var buffer = new TDrawBuffer(20);
        var attrs = new TAttrPair((byte)0x17, (byte)0x1E);

        // "A~B~C~D~E" - B and D should be highlighted
        int written = buffer.MoveCStr(0, "A~B~C~D~E", attrs);

        Assert.AreEqual(5, written);
        Assert.AreEqual(attrs.Normal, buffer[0].Attr);     // A
        Assert.AreEqual(attrs.Highlight, buffer[1].Attr);  // B
        Assert.AreEqual(attrs.Normal, buffer[2].Attr);     // C
        Assert.AreEqual(attrs.Highlight, buffer[3].Attr);  // D
        Assert.AreEqual(attrs.Normal, buffer[4].Attr);     // E
    }

    #endregion

    #region PutAttribute / PutChar Tests

    [TestMethod]
    public void PutAttribute_ShouldSetAttributeAtPosition()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x0A, 0x02);

        buffer.PutAttribute(5, attr);

        Assert.AreEqual(attr, buffer[5].Attr);
        Assert.AreEqual(' ', buffer[5].Char, "Char should be unchanged");
    }

    [TestMethod]
    public void PutAttribute_ShouldDoNothingIfIndentPastBuffer()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x0A, 0x02);

        // Should not throw
        buffer.PutAttribute(20, attr);
    }

    [TestMethod]
    public void PutChar_ShouldSetCharAtPosition()
    {
        var buffer = new TDrawBuffer(10);

        buffer.PutChar(5, 'Q');

        Assert.AreEqual('Q', buffer[5].Char);
    }

    [TestMethod]
    public void PutChar_ShouldDoNothingIfIndentPastBuffer()
    {
        var buffer = new TDrawBuffer(10);

        // Should not throw
        buffer.PutChar(20, 'Q');
    }

    #endregion

    #region Indexer Tests

    [TestMethod]
    public void Indexer_ShouldGetAndSetCells()
    {
        var buffer = new TDrawBuffer(10);
        var attr = new TColorAttr(0x0C, 0x00);
        var cell = new TScreenCell('Z', attr);

        buffer[5] = cell;

        Assert.AreEqual(cell.Char, buffer[5].Char);
        Assert.AreEqual(cell.Attr, buffer[5].Attr);
    }

    #endregion

    #region Data Property Tests

    [TestMethod]
    public void Data_ShouldReturnSpanOfCells()
    {
        var buffer = new TDrawBuffer(10);
        var span = buffer.Data;

        Assert.AreEqual(10, span.Length);
    }

    [TestMethod]
    public void Data_ShouldAllowDirectModification()
    {
        var buffer = new TDrawBuffer(10);
        var span = buffer.Data;

        span[0].Char = 'A';
        span[0].Attr = new TColorAttr(0x0E, 0x01);

        Assert.AreEqual('A', buffer[0].Char);
        Assert.AreEqual(new TColorAttr(0x0E, 0x01), buffer[0].Attr);
    }

    #endregion
}
