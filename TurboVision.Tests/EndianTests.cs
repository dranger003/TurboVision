namespace TurboVision.Tests;

using TurboVision.Core;

/// <summary>
/// Tests for event structure aliasing and bitfield behavior.
/// Ported from Reference/tvision/test/platform/endian.test.cpp
/// </summary>
[TestClass]
public class EndianTests
{
    /// <summary>
    /// Tests that KeyDownEvent keyCode/charScan aliasing works correctly.
    /// keyCode is stored as low byte = charCode, high byte = scanCode (little-endian).
    /// </summary>
    [TestMethod]
    public void AliasingInKeyDownEvent_ShouldWorkCorrectly()
    {
        var keyDown = new KeyDownEvent();
        keyDown.KeyCode = 0x1234;

        Assert.AreEqual(0x34, keyDown.CharCode, "CharCode should be low byte of KeyCode");
        Assert.AreEqual(0x12, keyDown.ScanCode, "ScanCode should be high byte of KeyCode");
    }

    /// <summary>
    /// Tests that MessageEvent aliasing between InfoPtr and InfoInt works correctly.
    /// </summary>
    [TestMethod]
    public void AliasingInMessageEvent_ShouldWorkCorrectly()
    {
        var message = new MessageEvent();
        message.InfoPtr = 0x12345678;

        // InfoInt should contain the lower 32 bits / lower portion
        // Note: The exact behavior depends on platform (32-bit vs 64-bit)
        // On 32-bit: InfoInt == InfoPtr
        // On 64-bit: InfoInt == lower 32 bits of InfoPtr
        Assert.AreEqual(0x12345678, message.InfoInt, "InfoInt should equal InfoPtr for 32-bit values");
    }

    /// <summary>
    /// Tests TColorAttr foreground/background extraction.
    /// </summary>
    [TestMethod]
    public void TColorAttr_ShouldExtractForegroundAndBackground()
    {
        // Standard BIOS color attribute: background in high nibble, foreground in low nibble
        var attr = new TColorAttr(0x07, 0x01);  // White on Blue

        Assert.AreEqual(0x07, attr.Foreground, "Foreground should be 0x07 (white)");
        Assert.AreEqual(0x01, attr.Background, "Background should be 0x01 (blue)");
    }

    /// <summary>
    /// Tests TColorAttr constructed from a single byte value.
    /// </summary>
    [TestMethod]
    public void TColorAttr_FromByte_ShouldWorkCorrectly()
    {
        // 0x1F = Blue background (1) + Bright White foreground (F)
        TColorAttr attr = 0x1F;

        Assert.AreEqual(0x0F, attr.Foreground, "Foreground should be 0x0F (bright white)");
        Assert.AreEqual(0x01, attr.Background, "Background should be 0x01 (blue)");
    }

    /// <summary>
    /// Tests TColorAttr round-trip conversion to/from byte.
    /// </summary>
    [TestMethod]
    public void TColorAttr_ByteConversion_ShouldRoundTrip()
    {
        TColorAttr attr = new TColorAttr(0x0E, 0x01);  // Yellow on Blue
        byte value = attr;
        TColorAttr restored = value;

        Assert.AreEqual(attr.Foreground, restored.Foreground, "Foreground should round-trip");
        Assert.AreEqual(attr.Background, restored.Background, "Background should round-trip");
    }
}
