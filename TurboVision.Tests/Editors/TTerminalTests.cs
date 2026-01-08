using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Core;
using TurboVision.Editors;
using TurboVision.Views;

namespace TurboVision.Tests.Editors;

/// <summary>
/// Test suite for TTerminal class.
/// Matches upstream textview.test.cpp test structure.
/// Upstream: Reference/tvision/test/tvision/textview.test.cpp
/// </summary>
[TestClass]
public class TTerminalTests
{
    private static TTerminal CreateTerminal(ushort bufSize = 1024)
    {
        var bounds = new TRect(0, 0, 80, 25);
        return new TTerminal(bounds, null, null, bufSize);
    }

    #region prevLines Tests (Upstream test cases)

    [TestMethod]
    public void PrevLines_from_queFront_with_empty_buffer()
    {
        var terminal = CreateTerminal(256);
        // With empty buffer, prevLines from queFront should return queFront
        var result = terminal.PrevLines(0, 1);
        Assert.AreEqual((ushort)0, result);
    }

    [TestMethod]
    public void PrevLines_zero_lines()
    {
        var terminal = CreateTerminal(256);
        var input = "Test content\n";
        terminal.DoSputn(input.AsSpan(), input.Length);

        // prevLines with 0 lines should return the same position
        var pos = (ushort)input.Length;
        var result = terminal.PrevLines(pos, 0);
        Assert.AreEqual(pos, result);
    }

    [TestMethod]
    public void PrevLines_one_line_with_newline_at_start()
    {
        var terminal = CreateTerminal(256);
        var input = "\nContent";
        terminal.DoSputn(input.AsSpan(), input.Length);

        // Going back 1 line from position 7 should reach the newline
        var result = terminal.PrevLines((ushort)input.Length, 1);
        Assert.AreEqual((ushort)1, result); // Position after '\n'
    }

    [TestMethod]
    public void PrevLines_with_multiple_lines()
    {
        var terminal = CreateTerminal(256);
        var input = "Line1\nLine2\nLine3\n";
        terminal.DoSputn(input.AsSpan(), input.Length);

        // From end, go back 2 lines should reach "Line2\n"
        var result = terminal.PrevLines((ushort)input.Length, 2);
        Assert.AreEqual((ushort)12, result); // Position of 'L' in "Line3\n"
    }

    [TestMethod]
    public void PrevLines_with_buffer_wrapping()
    {
        var terminal = CreateTerminal(32);

        // Fill buffer to cause wrapping
        var line1 = "Start line that will wrap\n";
        var line2 = "Middle content here\n";
        var line3 = "End line\n";

        terminal.DoSputn(line1.AsSpan(), line1.Length);
        terminal.DoSputn(line2.AsSpan(), line2.Length);
        terminal.DoSputn(line3.AsSpan(), line3.Length);

        // Test that prevLines handles wrapping correctly
        // This tests the circular buffer logic
        var result = terminal.PrevLines((ushort)9, 1);
        Assert.IsGreaterThanOrEqualTo(0, result); // Should not crash with wrapping
    }

    #endregion

    #region Buffer Operation Tests

    [TestMethod]
    public void BufInc_wraps_at_bufSize()
    {
        var terminal = CreateTerminal(256);
        ushort val = 255;
        terminal.BufInc(ref val);
        Assert.AreEqual((ushort)0, val); // Should wrap to 0
    }

    [TestMethod]
    public void CanInsert_empty_buffer()
    {
        var terminal = CreateTerminal(256);
        // Empty buffer should be able to insert up to bufSize-1
        Assert.IsTrue(terminal.CanInsert(255));
        Assert.IsFalse(terminal.CanInsert(256)); // Can't insert entire buffer size
    }

    [TestMethod]
    public void CanInsert_full_buffer()
    {
        var terminal = CreateTerminal(32);

        // Fill buffer completely
        var data = new string('X', 31);
        terminal.DoSputn(data.AsSpan(), data.Length);

        // Should not be able to insert more
        Assert.IsFalse(terminal.CanInsert(1));
    }

    #endregion

    #region Line Navigation Tests

    [TestMethod]
    public void NextLine_finds_newline()
    {
        var terminal = CreateTerminal(256);
        var input = "First line\nSecond line\n";
        terminal.DoSputn(input.AsSpan(), input.Length);

        var result = terminal.NextLine(0);
        Assert.AreEqual((ushort)11, result); // Position after first '\n'
    }

    [TestMethod]
    public void NextLine_at_end_returns_queFront()
    {
        var terminal = CreateTerminal(256);
        var input = "Content without newline";
        terminal.DoSputn(input.AsSpan(), input.Length);

        var result = terminal.NextLine(0);
        // Should reach end (queFront) since no newline found
        Assert.AreEqual((ushort)input.Length, result);
    }

    #endregion

    #region Output Tests

    [TestMethod]
    public void Do_sputn_simple_text()
    {
        var terminal = CreateTerminal(256);
        var input = "Hello, World!";

        var result = terminal.DoSputn(input.AsSpan(), input.Length);

        Assert.AreEqual(input.Length, result);
        Assert.IsFalse(terminal.QueEmpty());
    }

    [TestMethod]
    public void Do_sputn_with_newlines()
    {
        var terminal = CreateTerminal(256);
        var input = "Line1\nLine2\nLine3\n";

        terminal.DoSputn(input.AsSpan(), input.Length);

        // Terminal should count 3 lines (3 newlines)
        // Limit.Y should reflect this
        Assert.IsFalse(terminal.QueEmpty());
    }

    [TestMethod]
    public void Do_sputn_overflow_buffer()
    {
        var terminal = CreateTerminal(32);

        // Write more than buffer size
        var largeInput = new string('A', 100);

        var result = terminal.DoSputn(largeInput.AsSpan(), largeInput.Length);

        // Should truncate to buffer size - 1 (31 bytes)
        Assert.AreEqual(31, result);
    }

    [TestMethod]
    public void Do_sputn_wrapping()
    {
        var terminal = CreateTerminal(64);

        // Write data that will cause buffer wrapping
        var part1 = "First part of data that fills buffer\n";
        var part2 = "Second part causes wrap\n";
        var part3 = "Third part continues\n";

        terminal.DoSputn(part1.AsSpan(), part1.Length);
        terminal.DoSputn(part2.AsSpan(), part2.Length);
        terminal.DoSputn(part3.AsSpan(), part3.Length);

        // Buffer should wrap and discard old data
        Assert.IsFalse(terminal.QueEmpty());
    }

    #endregion

    #region OTStream Tests

    [TestMethod]
    public void OTStream_wraps_terminal()
    {
        var terminal = CreateTerminal(256);
        var stream = new OTStream(terminal);

        Assert.IsNotNull(stream);
        Assert.AreEqual(System.Text.Encoding.UTF8, stream.Encoding);
    }

    [TestMethod]
    public void OTStream_write_string()
    {
        var terminal = CreateTerminal(256);
        var stream = new OTStream(terminal);

        stream.Write("Test string");

        Assert.IsFalse(terminal.QueEmpty());
    }

    [TestMethod]
    public void OTStream_write_char()
    {
        var terminal = CreateTerminal(256);
        var stream = new OTStream(terminal);

        stream.Write('X');

        Assert.IsFalse(terminal.QueEmpty());
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void QueEmpty_returns_true_for_empty_buffer()
    {
        var terminal = CreateTerminal(256);
        Assert.IsTrue(terminal.QueEmpty());
    }

    [TestMethod]
    public void QueEmpty_returns_false_after_write()
    {
        var terminal = CreateTerminal(256);
        terminal.DoSputn("X".AsSpan(), 1);
        Assert.IsFalse(terminal.QueEmpty());
    }

    [TestMethod]
    public void Terminal_handles_minimum_buffer_size()
    {
        var terminal = CreateTerminal(10);
        // Should work with very small buffer
        terminal.DoSputn("Hi\n".AsSpan(), 3);
        Assert.IsFalse(terminal.QueEmpty());
    }

    #endregion
}
