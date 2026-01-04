namespace TurboVision.Tests;

using TurboVision.Core;
using TurboVision.Menus;
using TurboVision.Views;

/// <summary>
/// Tests for TStatusLine keyboard event handling.
/// </summary>
[TestClass]
public class TStatusLineTests
{
    /// <summary>
    /// Tests that kbAltX keyboard events are properly matched against TStatusItem.
    /// This simulates what happens when:
    /// 1. TStatusItem is created with KeyConstants.kbAltX
    /// 2. Win32ConsoleDriver generates a keyboard event for Alt+X
    /// </summary>
    [TestMethod]
    public void TStatusItem_ShouldMatchKeyboardEvent_AltX()
    {
        // Simulate how TStatusItem stores the key code
        // When constructed with kbAltX, it gets normalized via TKey constructor
        var statusItem = new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit);

        // Simulate what Win32ConsoleDriver produces for Alt+X:
        // - KeyCode = 0x2D00 (kbAltX) after AltCvt lookup
        // - ControlKeyState = 0x0008 (kbAltShift)
        var keyEvent = new KeyDownEvent();
        keyEvent.KeyCode = KeyConstants.kbAltX;  // 0x2D00
        keyEvent.ControlKeyState = KeyConstants.kbAltShift;  // 0x0008

        // Convert to TKey (this is what TStatusLine.HandleEvent does)
        var eventKey = keyEvent.ToKey();

        // These should be equal
        Assert.AreEqual(statusItem.KeyCode.KeyCode, eventKey.KeyCode,
            $"KeyCode mismatch: StatusItem={statusItem.KeyCode.KeyCode:X4}, Event={eventKey.KeyCode:X4}");
        Assert.AreEqual(statusItem.KeyCode.ControlKeyState, eventKey.ControlKeyState,
            $"ControlKeyState mismatch: StatusItem={statusItem.KeyCode.ControlKeyState:X4}, Event={eventKey.ControlKeyState:X4}");
        Assert.AreEqual(statusItem.KeyCode, eventKey,
            $"TKey equality failed: StatusItem={statusItem.KeyCode}, Event={eventKey}");
    }

    /// <summary>
    /// Tests TKey equality with and without modifiers.
    /// Both TKey(kbAltX, 0) and TKey(kbAltX, kbAltShift) should normalize to the same value.
    /// </summary>
    [TestMethod]
    public void TKey_AltX_ShouldNormalizeIdentically_WithAndWithoutModifiers()
    {
        // Without explicit modifier (as stored in TStatusItem)
        var keyWithoutMod = new TKey(KeyConstants.kbAltX, 0);

        // With explicit modifier (as might come from keyboard event)
        var keyWithMod = new TKey(KeyConstants.kbAltX, KeyConstants.kbAltShift);

        Assert.AreEqual(keyWithoutMod.KeyCode, keyWithMod.KeyCode,
            $"KeyCode should match: without={keyWithoutMod.KeyCode:X4}, with={keyWithMod.KeyCode:X4}");
        Assert.AreEqual(keyWithoutMod.ControlKeyState, keyWithMod.ControlKeyState,
            $"ControlKeyState should match: without={keyWithoutMod.ControlKeyState:X4}, with={keyWithMod.ControlKeyState:X4}");
        Assert.AreEqual(keyWithoutMod, keyWithMod, "TKey instances should be equal");
    }

    /// <summary>
    /// Tests the expected normalized values for Alt+X.
    /// </summary>
    [TestMethod]
    public void TKey_AltX_ShouldNormalizeTo_X_WithAltShift()
    {
        var key = new TKey(KeyConstants.kbAltX, 0);

        Assert.AreEqual((ushort)'X', key.KeyCode, $"KeyCode should be 'X' (0x58), got 0x{key.KeyCode:X4}");
        Assert.AreEqual(KeyConstants.kbAltShift, key.ControlKeyState,
            $"ControlKeyState should be kbAltShift (0x0008), got 0x{key.ControlKeyState:X4}");
    }

    /// <summary>
    /// Tests that TStatusLine.HandleEvent correctly transforms evKeyDown to evCommand for matching keys.
    /// </summary>
    [TestMethod]
    public void TStatusLine_HandleEvent_ShouldTransformKeyDownToCommand()
    {
        // Create a TStatusLine with Alt-X bound to cmQuit
        var bounds = new TRect(0, 0, 80, 1);
        var statusDef = new TStatusDef(0, 0xFFFF,
            new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit));
        var statusLine = new TStatusLine(bounds, statusDef);

        // Verify the status item was set up correctly
        Assert.IsNotNull(statusLine, "TStatusLine should be created");

        // Create a keyboard event for Alt+X (as produced by Win32ConsoleDriver)
        var ev = new TEvent
        {
            What = EventConstants.evKeyDown
        };
        ev.KeyDown.KeyCode = KeyConstants.kbAltX;  // 0x2D00
        ev.KeyDown.ControlKeyState = KeyConstants.kbAltShift;  // 0x0008

        // Call HandleEvent
        statusLine.HandleEvent(ref ev);

        // After handling, the event should be transformed to evCommand with cmQuit
        Assert.AreEqual(EventConstants.evCommand, ev.What,
            $"Event should be transformed to evCommand, but was 0x{ev.What:X4}");
        Assert.AreEqual(CommandConstants.cmQuit, ev.Message.Command,
            $"Command should be cmQuit ({CommandConstants.cmQuit}), but was {ev.Message.Command}");
    }

    /// <summary>
    /// Tests that cmQuit command is enabled by default.
    /// </summary>
    [TestMethod]
    public void CommandEnabled_cmQuit_ShouldBeEnabledByDefault()
    {
        // cmQuit = 1, which should be enabled by default
        bool enabled = TView.CommandEnabled(CommandConstants.cmQuit);
        Assert.IsTrue(enabled, "cmQuit should be enabled by default");
    }
}
