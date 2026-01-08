namespace TurboVision.Tests;

using TurboVision.Core;

/// <summary>
/// Tests for TKey normalization behavior.
/// Ported from Reference/tvision/test/tvision/tkey.test.cpp
/// </summary>
[TestClass]
public class TKeyTests
{
    // Additional key code constants needed for tests
    // (some may need to be added to KeyConstants.cs)
    private const ushort kbCtrlA = 0x0001;
    private const ushort kbCtrlZ = 0x001A;
    private const ushort kbGrayPlus = 0x4E2B;
    private const ushort kbCtrlPrtSc = 0x7200;
    private const ushort kbCtrlTab = 0x9400;
    private const ushort kbCtrlEnter = 0x1C0A;

    // Modifier key constants (Windows-style, matching upstream tvision __FLAT__ mode)
    // These use the production constants from KeyConstants.cs, which match Windows console API
    private const ushort kbShift = KeyConstants.kbShift;         // 0x0010
    private const ushort kbLeftCtrl = KeyConstants.kbLeftCtrl;   // 0x0008
    private const ushort kbRightCtrl = KeyConstants.kbRightCtrl; // 0x0004
    private const ushort kbLeftAlt = KeyConstants.kbLeftAlt;     // 0x0002
    private const ushort kbRightAlt = KeyConstants.kbRightAlt;   // 0x0001

    // Additional BIOS-style key codes referenced in tests
    private const ushort kb0 = 0x0B30;   // BIOS scan code for '0'
    private const ushort kbA = 0x1E61;   // BIOS scan code for 'a'
    private const ushort kbCtrlKpDiv = 0x3500;
    private const ushort kbBiosCtrlN = 0x310E;

    /// <summary>
    /// Test data structure for key normalization tests.
    /// </summary>
    private readonly record struct KeyTestCase(
        ushort InputCode,
        ushort InputMods,
        ushort ExpectedCode,
        ushort ExpectedMods
    );

    /// <summary>
    /// Tests TKey constructor normalization.
    /// Verifies that key codes and modifiers are normalized correctly.
    /// </summary>
    [TestMethod]
    public void TKey_ShouldConstructProperly()
    {
        var testCases = new KeyTestCase[]
        {
            // kbNoKey
            new(KeyConstants.kbNoKey, 0, KeyConstants.kbNoKey, 0),
            new(KeyConstants.kbNoKey, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), KeyConstants.kbNoKey, 0),

            // kbCtrlA normalization
            new(kbCtrlA, 0, (ushort)'A', KeyConstants.kbCtrlShift),
            new(kbCtrlA, KeyConstants.kbCtrlShift, (ushort)'A', KeyConstants.kbCtrlShift),
            new(kbCtrlA, (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kbCtrlA, KeyConstants.kbAltShift, (ushort)'A', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // kbCtrlZ normalization
            new(kbCtrlZ, 0, (ushort)'Z', KeyConstants.kbCtrlShift),
            new(kbCtrlZ, KeyConstants.kbCtrlShift, (ushort)'Z', KeyConstants.kbCtrlShift),
            new(kbCtrlZ, (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'Z', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kbCtrlZ, KeyConstants.kbAltShift, (ushort)'Z', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // kbCtrlZ + 1 (edge case, should pass through)
            new((ushort)(kbCtrlZ + 1), 0, (ushort)(kbCtrlZ + 1), 0),
            new((ushort)(kbCtrlZ + 1), (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)(kbCtrlZ + 1), (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // kbGrayPlus (should pass through)
            new(kbGrayPlus, 0, kbGrayPlus, 0),
            new(kbGrayPlus, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), kbGrayPlus, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // kbCtrlPrtSc
            new(kbCtrlPrtSc, 0, kbCtrlPrtSc, KeyConstants.kbCtrlShift),
            new(kbCtrlPrtSc, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), kbCtrlPrtSc, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // 'A' character handling
            new((ushort)'A', 0, (ushort)'A', 0),
            new((ushort)'A', kbShift, (ushort)'A', kbShift),
            new((ushort)'A', KeyConstants.kbCtrlShift, (ushort)'A', KeyConstants.kbCtrlShift),
            new((ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new((ushort)'A', KeyConstants.kbAltShift, (ushort)'A', KeyConstants.kbAltShift),
            new((ushort)'A', (ushort)(kbShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbAltShift)),
            new((ushort)'A', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new((ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // 'a' character handling (should normalize to 'A')
            new((ushort)'a', 0, (ushort)'A', 0),
            new((ushort)'a', kbRightAlt, (ushort)'A', KeyConstants.kbAltShift),
            new((ushort)'a', kbShift, (ushort)'A', kbShift),
            new((ushort)'a', KeyConstants.kbCtrlShift, (ushort)'A', KeyConstants.kbCtrlShift),
            new((ushort)'a', (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new((ushort)'a', KeyConstants.kbAltShift, (ushort)'A', KeyConstants.kbAltShift),
            new((ushort)'a', (ushort)(kbShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbAltShift)),
            new((ushort)'a', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new((ushort)'a', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // '0' character handling
            new((ushort)'0', 0, (ushort)'0', 0),
            new((ushort)'0', kbShift, (ushort)'0', kbShift),
            new((ushort)'0', KeyConstants.kbCtrlShift, (ushort)'0', KeyConstants.kbCtrlShift),
            new((ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new((ushort)'0', KeyConstants.kbAltShift, (ushort)'0', KeyConstants.kbAltShift),
            new((ushort)'0', (ushort)(kbShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbAltShift)),
            new((ushort)'0', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new((ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // BIOS-style key codes kbA (0x1E61 = 'a' with scan code 0x1E)
            new(kbA, 0, (ushort)'A', 0),
            new(kbA, kbShift, (ushort)'A', kbShift),
            new(kbA, KeyConstants.kbCtrlShift, (ushort)'A', KeyConstants.kbCtrlShift),
            new(kbA, (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kbA, KeyConstants.kbAltShift, (ushort)'A', KeyConstants.kbAltShift),
            new(kbA, (ushort)(kbShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbAltShift)),
            new(kbA, (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kbA, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'A', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // BIOS-style key codes kb0 (0x0B30 = '0' with scan code 0x0B)
            new(kb0, 0, (ushort)'0', 0),
            new(kb0, kbShift, (ushort)'0', kbShift),
            new(kb0, KeyConstants.kbCtrlShift, (ushort)'0', KeyConstants.kbCtrlShift),
            new(kb0, (ushort)(kbShift | KeyConstants.kbCtrlShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kb0, KeyConstants.kbAltShift, (ushort)'0', KeyConstants.kbAltShift),
            new(kb0, (ushort)(kbShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbAltShift)),
            new(kb0, (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kb0, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), (ushort)'0', (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // Arrow keys with modifiers
            new(KeyConstants.kbRight, kbShift, KeyConstants.kbRight, kbShift),

            // kbAltX normalization
            new(KeyConstants.kbAltX, 0, (ushort)'X', KeyConstants.kbAltShift),

            // Tab key handling
            new(KeyConstants.kbTab, 0, KeyConstants.kbTab, 0),
            new(KeyConstants.kbTab, kbShift, KeyConstants.kbTab, kbShift),
            new(KeyConstants.kbShiftTab, 0, KeyConstants.kbTab, kbShift),
            new(KeyConstants.kbTab, KeyConstants.kbCtrlShift, KeyConstants.kbTab, KeyConstants.kbCtrlShift),
            new(KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift), KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(KeyConstants.kbShiftTab, KeyConstants.kbCtrlShift, KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(KeyConstants.kbShiftTab, (ushort)(kbShift | KeyConstants.kbCtrlShift), KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kbCtrlTab, kbShift, KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(KeyConstants.kbTab, KeyConstants.kbAltShift, KeyConstants.kbTab, KeyConstants.kbAltShift),
            new(KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(KeyConstants.kbShiftTab, KeyConstants.kbAltShift, KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbAltShift)),
            new(KeyConstants.kbShiftTab, (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kbCtrlTab, KeyConstants.kbAltShift, KeyConstants.kbTab, (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kbCtrlTab, (ushort)(kbShift | KeyConstants.kbAltShift), KeyConstants.kbTab, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // Enter key handling
            new(KeyConstants.kbEnter, 0, KeyConstants.kbEnter, 0),
            new(KeyConstants.kbEnter, kbShift, KeyConstants.kbEnter, kbShift),
            new(KeyConstants.kbEnter, KeyConstants.kbCtrlShift, KeyConstants.kbEnter, KeyConstants.kbCtrlShift),
            new(KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift), KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(kbCtrlEnter, kbShift, KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift)),
            new(KeyConstants.kbEnter, KeyConstants.kbAltShift, KeyConstants.kbEnter, KeyConstants.kbAltShift),
            new(KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift), KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kbCtrlEnter, KeyConstants.kbAltShift, KeyConstants.kbEnter, (ushort)(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),
            new(kbCtrlEnter, (ushort)(kbShift | KeyConstants.kbAltShift), KeyConstants.kbEnter, (ushort)(kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)),

            // Left/Right Ctrl normalization
            new(KeyConstants.kbTab, kbLeftCtrl, KeyConstants.kbTab, KeyConstants.kbCtrlShift),
            new(KeyConstants.kbTab, kbRightCtrl, KeyConstants.kbTab, KeyConstants.kbCtrlShift),
            new(kbCtrlTab, kbLeftCtrl, KeyConstants.kbTab, KeyConstants.kbCtrlShift),
            new(kbCtrlTab, kbRightCtrl, KeyConstants.kbTab, KeyConstants.kbCtrlShift),
            new(kbA, kbLeftCtrl, (ushort)'A', KeyConstants.kbCtrlShift),
            new(kbA, kbRightCtrl, (ushort)'A', KeyConstants.kbCtrlShift),

            // Special BIOS key codes
            new(kbCtrlKpDiv, 0, (ushort)'/', KeyConstants.kbCtrlShift),
            new(kbBiosCtrlN, 0, (ushort)'N', KeyConstants.kbCtrlShift),
        };

        var failedCases = new List<string>();

        foreach (var testCase in testCases)
        {
            var key = new TKey(testCase.InputCode, testCase.InputMods);

            if (key.KeyCode != testCase.ExpectedCode || key.ControlKeyState != testCase.ExpectedMods)
            {
                failedCases.Add(
                    $"Input(0x{testCase.InputCode:X4}, 0x{testCase.InputMods:X4}) -> " +
                    $"Expected(0x{testCase.ExpectedCode:X4}, 0x{testCase.ExpectedMods:X4}), " +
                    $"Got(0x{key.KeyCode:X4}, 0x{key.ControlKeyState:X4})"
                );
            }
        }

        if (failedCases.Count > 0)
        {
            Assert.Fail($"Failed {failedCases.Count} of {testCases.Length} test cases:\n" +
                string.Join("\n", failedCases.Take(10)) +
                (failedCases.Count > 10 ? $"\n... and {failedCases.Count - 10} more" : ""));
        }
    }
}
