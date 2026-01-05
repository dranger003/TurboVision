namespace TurboVision.Tests.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Collections;

[TestClass]
public class TResourceFileTests
{
    [TestMethod]
    public void TResourceItem_Constructor_InitializesProperties()
    {
        var item = new TResourceItem("test", 100, 50);

        Assert.AreEqual("test", item.Key);
        Assert.AreEqual(100, item.Position);
        Assert.AreEqual(50, item.Size);
    }

    [TestMethod]
    public void TResourceCollection_Insert_MaintainsSortedOrder()
    {
        var collection = new TResourceCollection(10, 5);
        collection.Insert(new TResourceItem("charlie", 0, 10));
        collection.Insert(new TResourceItem("alpha", 10, 20));
        collection.Insert(new TResourceItem("bravo", 30, 15));

        Assert.AreEqual(3, collection.Count);
        Assert.AreEqual("alpha", collection.At(0)!.Key);
        Assert.AreEqual("bravo", collection.At(1)!.Key);
        Assert.AreEqual("charlie", collection.At(2)!.Key);
    }

    [TestMethod]
    public void TResourceCollection_Search_FindsKey()
    {
        var collection = new TResourceCollection(10, 5);
        collection.Insert(new TResourceItem("alpha", 0, 10));
        collection.Insert(new TResourceItem("bravo", 10, 20));

        bool found = collection.Search("bravo", out int index);

        Assert.IsTrue(found);
        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void TResourceFile_PutGet_StoresAndRetrievesData()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        byte[] data = [1, 2, 3, 4, 5];
        resFile.Put("test", data);
        resFile.Flush();

        stream.Position = 0;
        using var resFile2 = new TResourceFile(stream, ownsStream: false);
        var retrieved = resFile2.GetBytes("test");

        Assert.IsNotNull(retrieved);
        CollectionAssert.AreEqual(data, retrieved);
    }

    [TestMethod]
    public void TResourceFile_Count_ReturnsResourceCount()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        resFile.Put("first", [1, 2, 3]);
        resFile.Put("second", [4, 5, 6]);
        resFile.Put("third", [7, 8, 9]);

        Assert.AreEqual(3, resFile.Count);
    }

    [TestMethod]
    public void TResourceFile_KeyAt_ReturnsResourceKey()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        resFile.Put("alpha", [1]);
        resFile.Put("bravo", [2]);

        // Keys are sorted
        Assert.AreEqual("alpha", resFile.KeyAt(0));
        Assert.AreEqual("bravo", resFile.KeyAt(1));
    }

    [TestMethod]
    public void TResourceFile_Remove_DeletesResource()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        resFile.Put("test", [1, 2, 3]);
        Assert.AreEqual(1, resFile.Count);

        resFile.Remove("test");
        Assert.AreEqual(0, resFile.Count);
    }

    [TestMethod]
    public void TResourceFile_GetBytes_ReturnsNullForMissing()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        var result = resFile.GetBytes("nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TResourceFile_Flush_PersistsData()
    {
        using var stream = new MemoryStream();

        // Write data
        using (var resFile = new TResourceFile(stream, ownsStream: false))
        {
            resFile.Put("test", [1, 2, 3, 4, 5]);
        }

        // Read back from same stream
        stream.Position = 0;
        using var resFile2 = new TResourceFile(stream, ownsStream: false);

        Assert.AreEqual(1, resFile2.Count);
        var data = resFile2.GetBytes("test");
        Assert.IsNotNull(data);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, data);
    }

    [TestMethod]
    public void TResourceFile_MultipleResources_AllPersist()
    {
        using var stream = new MemoryStream();

        using (var resFile = new TResourceFile(stream, ownsStream: false))
        {
            resFile.Put("alpha", System.Text.Encoding.UTF8.GetBytes("Alpha data"));
            resFile.Put("beta", System.Text.Encoding.UTF8.GetBytes("Beta data"));
            resFile.Put("gamma", System.Text.Encoding.UTF8.GetBytes("Gamma data"));
        }

        stream.Position = 0;
        using var resFile2 = new TResourceFile(stream, ownsStream: false);

        Assert.AreEqual(3, resFile2.Count);

        var alpha = resFile2.GetBytes("alpha");
        var beta = resFile2.GetBytes("beta");
        var gamma = resFile2.GetBytes("gamma");

        Assert.AreEqual("Alpha data", System.Text.Encoding.UTF8.GetString(alpha!));
        Assert.AreEqual("Beta data", System.Text.Encoding.UTF8.GetString(beta!));
        Assert.AreEqual("Gamma data", System.Text.Encoding.UTF8.GetString(gamma!));
    }

    [TestMethod]
    public void TResourceFile_UpdateExisting_ReplacesData()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        resFile.Put("test", [1, 2, 3]);
        resFile.Put("test", [4, 5, 6, 7, 8]);

        Assert.AreEqual(1, resFile.Count);
        var data = resFile.GetBytes("test");
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6, 7, 8 }, data);
    }

    [TestMethod]
    public void TResourceFile_PutWithWriter_StreamsLargeData()
    {
        using var stream = new MemoryStream();
        using var resFile = new TResourceFile(stream, ownsStream: false);

        resFile.Put("large", writer =>
        {
            for (int i = 0; i < 1000; i++)
            {
                writer.Write(i);
            }
        });

        var reader = resFile.GetReader("large", out int size);
        Assert.IsNotNull(reader);
        Assert.AreEqual(4000, size); // 1000 ints * 4 bytes

        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(i, reader.ReadInt32());
        }
    }
}
