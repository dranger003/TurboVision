namespace TurboVision.Tests.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Collections;

[TestClass]
public class TStringCollectionTests
{
    [TestMethod]
    public void Insert_MaintainsAlphabeticalOrder()
    {
        var collection = new TStringCollection(10, 5);
        collection.Insert("cherry");
        collection.Insert("apple");
        collection.Insert("banana");

        Assert.AreEqual(3, collection.Count);
        Assert.AreEqual("apple", collection.GetString(0));
        Assert.AreEqual("banana", collection.GetString(1));
        Assert.AreEqual("cherry", collection.GetString(2));
    }

    [TestMethod]
    public void Search_FindsExistingString()
    {
        var collection = new TStringCollection(10, 5);
        collection.Insert("apple");
        collection.Insert("banana");
        collection.Insert("cherry");

        bool found = collection.Search("banana", out int index);

        Assert.IsTrue(found);
        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void Search_ReturnsInsertPositionForMissing()
    {
        var collection = new TStringCollection(10, 5);
        collection.Insert("apple");
        collection.Insert("cherry");

        bool found = collection.Search("banana", out int index);

        Assert.IsFalse(found);
        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void WriteRead_RoundTrips()
    {
        var collection = new TStringCollection(10, 5);
        collection.Insert("first");
        collection.Insert("second");
        collection.Insert("third");

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        collection.Write(writer);

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var loaded = new TStringCollection(10, 5);
        loaded.Read(reader);

        Assert.AreEqual(3, loaded.Count);
        Assert.AreEqual("first", loaded.GetString(0));
        Assert.AreEqual("second", loaded.GetString(1));
        Assert.AreEqual("third", loaded.GetString(2));
    }

    [TestMethod]
    public void StringWrapper_ExplicitConversions()
    {
        var wrapper = new StringWrapper("test");
        string str = (string)wrapper;

        Assert.AreEqual("test", str);
        Assert.AreEqual("test", wrapper.Value);
    }

    [TestMethod]
    public void StringWrapper_Equality()
    {
        var a = new StringWrapper("test");
        var b = new StringWrapper("test");
        var c = new StringWrapper("other");

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.IsTrue(a.Equals("test"));
    }
}
