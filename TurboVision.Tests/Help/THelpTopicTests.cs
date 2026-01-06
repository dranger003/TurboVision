namespace TurboVision.Tests.Help;

using TurboVision.Core;
using TurboVision.Help;

/// <summary>
/// Tests for THelpTopic and related help system classes.
/// </summary>
[TestClass]
public class THelpTopicTests
{
    [TestMethod]
    public void THelpTopic_DefaultConstructor_ShouldCreateEmptyTopic()
    {
        var topic = new THelpTopic();

        Assert.IsNull(topic.Paragraphs);
        Assert.IsEmpty(topic.CrossRefs);
        Assert.AreEqual(0, topic.NumLines());
        Assert.AreEqual(0, topic.GetNumCrossRefs());
    }

    [TestMethod]
    public void THelpTopic_AddParagraph_ShouldAddSingleParagraph()
    {
        var topic = new THelpTopic();
        var para = new TParagraph("Hello World");

        topic.AddParagraph(para);

        Assert.IsNotNull(topic.Paragraphs);
        Assert.AreEqual("Hello World", topic.Paragraphs.Text);
        Assert.IsNull(topic.Paragraphs.Next);
    }

    [TestMethod]
    public void THelpTopic_AddParagraph_ShouldChainMultipleParagraphs()
    {
        var topic = new THelpTopic();
        var para1 = new TParagraph("First");
        var para2 = new TParagraph("Second");
        var para3 = new TParagraph("Third");

        topic.AddParagraph(para1);
        topic.AddParagraph(para2);
        topic.AddParagraph(para3);

        Assert.IsNotNull(topic.Paragraphs);
        Assert.AreEqual("First", topic.Paragraphs.Text);
        Assert.IsNotNull(topic.Paragraphs.Next);
        Assert.AreEqual("Second", topic.Paragraphs.Next.Text);
        Assert.IsNotNull(topic.Paragraphs.Next.Next);
        Assert.AreEqual("Third", topic.Paragraphs.Next.Next.Text);
        Assert.IsNull(topic.Paragraphs.Next.Next.Next);
    }

    [TestMethod]
    public void THelpTopic_NumLines_ShouldCountLinesCorrectly()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph("Line one\nLine two\nLine three"));

        Assert.AreEqual(3, topic.NumLines());
    }

    [TestMethod]
    public void THelpTopic_GetLine_ShouldReturnCorrectLines()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph("First line\nSecond line\nThird line"));

        Assert.AreEqual("First line", topic.GetLine(1));
        Assert.AreEqual("Second line", topic.GetLine(2));
        Assert.AreEqual("Third line", topic.GetLine(3));
    }

    [TestMethod]
    public void THelpTopic_GetLine_ShouldReturnEmptyForOutOfRange()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph("Only line"));

        Assert.AreEqual(string.Empty, topic.GetLine(5));
    }

    [TestMethod]
    public void THelpTopic_AddCrossRef_ShouldAddReference()
    {
        var topic = new THelpTopic();
        // TCrossRef(refTopic, offset, length)
        var crossRef = new TCrossRef(100, 10, 5);

        topic.AddCrossRef(crossRef);

        Assert.AreEqual(1, topic.GetNumCrossRefs());
        Assert.HasCount(1, topic.CrossRefs);
        Assert.AreEqual(10, topic.CrossRefs[0].Offset);
        Assert.AreEqual(5, topic.CrossRefs[0].Length);
        Assert.AreEqual(100, topic.CrossRefs[0].Ref);
    }

    [TestMethod]
    public void THelpTopic_LongestLineWidth_ShouldReturnMaxWidth()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph("Short\nThis is a longer line\nMedium line"));

        int maxWidth = topic.LongestLineWidth();

        Assert.AreEqual(21, maxWidth); // "This is a longer line" = 21 chars
    }

    [TestMethod]
    public void THelpTopic_SetWidth_ShouldEnableWordWrap()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph("This is a very long line that should wrap", wrap: true));
        topic.SetWidth(20);

        // With wrap enabled and width set, lines should be wrapped
        int numLines = topic.NumLines();
        Assert.IsGreaterThan(1, numLines, "Long text with wrap should produce multiple lines");
    }
}

[TestClass]
public class TParagraphTests
{
    [TestMethod]
    public void TParagraph_Constructor_ShouldSetTextAndSize()
    {
        var para = new TParagraph("Hello");

        Assert.AreEqual("Hello", para.Text);
        Assert.AreEqual(5, para.Size);
        Assert.IsTrue(para.Wrap); // Default is true
        Assert.IsNull(para.Next);
    }

    [TestMethod]
    public void TParagraph_Constructor_WithWrapFalse_ShouldSetWrapFlag()
    {
        var para = new TParagraph("Text", wrap: false);

        Assert.IsFalse(para.Wrap);
    }

    [TestMethod]
    public void TParagraph_Next_ShouldChainParagraphs()
    {
        var para1 = new TParagraph("First");
        var para2 = new TParagraph("Second");

        para1.Next = para2;

        Assert.AreSame(para2, para1.Next);
        Assert.IsNull(para2.Next);
    }
}

[TestClass]
public class TCrossRefTests
{
    [TestMethod]
    public void TCrossRef_Constructor_ShouldSetAllProperties()
    {
        // TCrossRef(refTopic, offset, length)
        var crossRef = new TCrossRef(50, 100, 10);

        Assert.AreEqual(100, crossRef.Offset);
        Assert.AreEqual(10, crossRef.Length);
        Assert.AreEqual(50, crossRef.Ref);
    }

    [TestMethod]
    public void TCrossRef_DefaultConstructor_ShouldInitializeToZero()
    {
        var crossRef = new TCrossRef();

        Assert.AreEqual(0, crossRef.Offset);
        Assert.AreEqual(0, crossRef.Length);
        Assert.AreEqual(0, crossRef.Ref);
    }
}
