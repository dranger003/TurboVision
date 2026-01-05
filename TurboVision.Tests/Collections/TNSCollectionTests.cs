namespace TurboVision.Tests.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Collections;

[TestClass]
public class TNSCollectionTests
{
    private class TestItem
    {
        public int Value { get; set; }
        public TestItem(int value) { Value = value; }
    }

    private class TestCollection : TNSCollection<TestItem>
    {
        public TestCollection(int limit, int delta) : base(limit, delta) { }
    }

    [TestMethod]
    public void Constructor_SetsInitialState()
    {
        var collection = new TestCollection(10, 5);

        Assert.AreEqual(0, collection.Count);
        Assert.AreEqual(10, collection.Limit);
        Assert.AreEqual(5, collection.Delta);
    }

    [TestMethod]
    public void Insert_AddsItemsToCollection()
    {
        var collection = new TestCollection(10, 5);
        var item1 = new TestItem(1);
        var item2 = new TestItem(2);

        int index1 = collection.Insert(item1);
        int index2 = collection.Insert(item2);

        Assert.AreEqual(0, index1);
        Assert.AreEqual(1, index2);
        Assert.AreEqual(2, collection.Count);
    }

    [TestMethod]
    public void At_ReturnsCorrectItem()
    {
        var collection = new TestCollection(10, 5);
        var item = new TestItem(42);
        collection.Insert(item);

        var result = collection.At(0);

        Assert.AreSame(item, result);
    }

    [TestMethod]
    public void IndexOf_FindsItem()
    {
        var collection = new TestCollection(10, 5);
        var item1 = new TestItem(1);
        var item2 = new TestItem(2);
        collection.Insert(item1);
        collection.Insert(item2);

        int index = collection.IndexOf(item2);

        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void IndexOf_ReturnsNotFoundForMissingItem()
    {
        var collection = new TestCollection(10, 5);
        var item = new TestItem(1);

        int index = collection.IndexOf(item);

        Assert.AreEqual(TNSCollection<TestItem>.NotFound, index);
    }

    [TestMethod]
    public void AtRemove_RemovesItemByIndex()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(2));
        collection.Insert(new TestItem(3));

        collection.AtRemove(1);

        Assert.AreEqual(2, collection.Count);
        Assert.AreEqual(1, collection.At(0)!.Value);
        Assert.AreEqual(3, collection.At(1)!.Value);
    }

    [TestMethod]
    public void Remove_RemovesItemByReference()
    {
        var collection = new TestCollection(10, 5);
        var item = new TestItem(2);
        collection.Insert(new TestItem(1));
        collection.Insert(item);
        collection.Insert(new TestItem(3));

        collection.Remove(item);

        Assert.AreEqual(2, collection.Count);
        Assert.AreEqual(TNSCollection<TestItem>.NotFound, collection.IndexOf(item));
    }

    [TestMethod]
    public void AtInsert_InsertsAtSpecificPosition()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(3));

        collection.AtInsert(1, new TestItem(2));

        Assert.AreEqual(3, collection.Count);
        Assert.AreEqual(1, collection.At(0)!.Value);
        Assert.AreEqual(2, collection.At(1)!.Value);
        Assert.AreEqual(3, collection.At(2)!.Value);
    }

    [TestMethod]
    public void AtPut_ReplacesItemAtIndex()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        var newItem = new TestItem(99);

        collection.AtPut(0, newItem);

        Assert.AreSame(newItem, collection.At(0));
    }

    [TestMethod]
    public void ForEach_IteratesAllItems()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(2));
        collection.Insert(new TestItem(3));

        int sum = 0;
        collection.ForEach(item => sum += item!.Value);

        Assert.AreEqual(6, sum);
    }

    [TestMethod]
    public void FirstThat_FindsMatchingItem()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(5));
        collection.Insert(new TestItem(3));

        var result = collection.FirstThat(item => item!.Value > 2);

        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Value);
    }

    [TestMethod]
    public void LastThat_FindsLastMatchingItem()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(5));
        collection.Insert(new TestItem(3));

        var result = collection.LastThat(item => item!.Value > 2);

        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Value);
    }

    [TestMethod]
    public void Pack_RemovesNullEntries()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(2));
        collection.Insert(new TestItem(3));
        collection.AtPut(1, null);

        collection.Pack();

        Assert.AreEqual(2, collection.Count);
        Assert.AreEqual(1, collection.At(0)!.Value);
        Assert.AreEqual(3, collection.At(1)!.Value);
    }

    [TestMethod]
    public void SetLimit_GrowsCollection()
    {
        var collection = new TestCollection(2, 2);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(2));

        // Should auto-grow when inserting beyond limit
        collection.Insert(new TestItem(3));

        Assert.AreEqual(3, collection.Count);
        Assert.IsTrue(collection.Limit >= 3);
    }

    [TestMethod]
    public void RemoveAll_ClearsCollection()
    {
        var collection = new TestCollection(10, 5);
        collection.Insert(new TestItem(1));
        collection.Insert(new TestItem(2));

        collection.RemoveAll();

        Assert.AreEqual(0, collection.Count);
    }
}
