namespace TurboVision.Tests.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Collections;

[TestClass]
public class TStringListTests
{
    [TestMethod]
    public void TStrListMaker_Put_StoresStrings()
    {
        var maker = new TStrListMaker(1000, 100);
        maker.Put(0, "Hello");
        maker.Put(1, "World");

        Assert.IsTrue(maker.StringDataSize > 0);
        Assert.AreEqual(1, maker.IndexCount);
    }

    [TestMethod]
    public void TStrListMaker_NonConsecutiveKeys_CreatesMultipleIndexBlocks()
    {
        var maker = new TStrListMaker(1000, 100);
        maker.Put(0, "Zero");
        maker.Put(100, "Hundred");

        Assert.AreEqual(2, maker.IndexCount);
    }

    [TestMethod]
    public void TStringList_ReadWrite_RoundTrips()
    {
        // Create a string list
        var maker = new TStrListMaker(1000, 100);
        maker.Put(0, "Zero");
        maker.Put(1, "One");
        maker.Put(2, "Two");
        maker.Put(10, "Ten");
        maker.Put(11, "Eleven");

        // Write to stream
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        maker.Write(writer);

        // Read back
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var list = TStringList.Build(reader);

        // Verify
        Assert.AreEqual("Zero", list.Get(0));
        Assert.AreEqual("One", list.Get(1));
        Assert.AreEqual("Two", list.Get(2));
        Assert.AreEqual("Ten", list.Get(10));
        Assert.AreEqual("Eleven", list.Get(11));
    }

    [TestMethod]
    public void TStringList_Get_ReturnsEmptyForMissingKey()
    {
        var maker = new TStrListMaker(1000, 100);
        maker.Put(0, "Zero");
        maker.Put(2, "Two");

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        maker.Write(writer);

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var list = TStringList.Build(reader);

        Assert.AreEqual(string.Empty, list.Get(1));
        Assert.AreEqual(string.Empty, list.Get(100));
    }

    [TestMethod]
    public void TStrIndexRec_WriteRead_RoundTrips()
    {
        var original = new TStrIndexRec(42, 10, 100);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        original.Write(writer);

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var loaded = TStrIndexRec.Read(reader);

        Assert.AreEqual(42, loaded.Key);
        Assert.AreEqual(10, loaded.Count);
        Assert.AreEqual(100, loaded.Offset);
    }

    [TestMethod]
    public void TStrListMaker_MaxKeys_SplitsIntoMultipleBlocks()
    {
        var maker = new TStrListMaker(10000, 100);

        // Put more than MAXKEYS (16) consecutive strings
        for (ushort i = 0; i < 20; i++)
        {
            maker.Put(i, $"String{i}");
        }

        Assert.AreEqual(2, maker.IndexCount);
    }
}
