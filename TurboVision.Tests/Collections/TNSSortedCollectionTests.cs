namespace TurboVision.Tests.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurboVision.Collections;

[TestClass]
public class TNSSortedCollectionTests
{
    private class TestItem
    {
        public int Key { get; }
        public string Value { get; }
        public TestItem(int key, string value) { Key = key; Value = value; }
    }

    private class TestSortedCollection : TNSSortedCollection<TestItem, int>
    {
        public TestSortedCollection(int limit, int delta) : base(limit, delta) { }

        public override int KeyOf(TestItem? item)
        {
            return item?.Key ?? 0;
        }

        protected override int Compare(int key1, int key2)
        {
            return key1.CompareTo(key2);
        }
    }

    [TestMethod]
    public void Insert_MaintainsSortedOrder()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Insert(new TestItem(3, "three"));
        collection.Insert(new TestItem(1, "one"));
        collection.Insert(new TestItem(2, "two"));

        Assert.AreEqual(3, collection.Count);
        Assert.AreEqual(1, collection.At(0)!.Key);
        Assert.AreEqual(2, collection.At(1)!.Key);
        Assert.AreEqual(3, collection.At(2)!.Key);
    }

    [TestMethod]
    public void Search_FindsExistingKey()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Insert(new TestItem(1, "one"));
        collection.Insert(new TestItem(3, "three"));
        collection.Insert(new TestItem(5, "five"));

        bool found = collection.Search(3, out int index);

        Assert.IsTrue(found);
        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void Search_ReturnsInsertPositionForMissingKey()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Insert(new TestItem(1, "one"));
        collection.Insert(new TestItem(5, "five"));

        bool found = collection.Search(3, out int index);

        Assert.IsFalse(found);
        Assert.AreEqual(1, index); // Should be inserted between 1 and 5
    }

    [TestMethod]
    public void Duplicates_WhenFalse_RejectsDuplicateKeys()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Duplicates = false;
        collection.Insert(new TestItem(1, "first"));
        collection.Insert(new TestItem(1, "second"));

        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual("first", collection.At(0)!.Value);
    }

    [TestMethod]
    public void Duplicates_WhenTrue_AllowsDuplicateKeys()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Duplicates = true;
        collection.Insert(new TestItem(1, "first"));
        collection.Insert(new TestItem(1, "second"));

        Assert.AreEqual(2, collection.Count);
    }

    [TestMethod]
    public void IndexOf_FindsCorrectItem()
    {
        var collection = new TestSortedCollection(10, 5);
        var item1 = new TestItem(1, "one");
        var item2 = new TestItem(2, "two");
        collection.Insert(item1);
        collection.Insert(item2);

        int index = collection.IndexOf(item2);

        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void IndexOf_WithDuplicates_FindsExactItem()
    {
        var collection = new TestSortedCollection(10, 5);
        collection.Duplicates = true;
        var item1 = new TestItem(1, "first");
        var item2 = new TestItem(1, "second");
        collection.Insert(item1);
        collection.Insert(item2);

        int index1 = collection.IndexOf(item1);
        int index2 = collection.IndexOf(item2);

        // Note: With duplicates enabled, new items with the same key are inserted
        // BEFORE existing items (upstream behavior), so item2 ends up at index 0
        Assert.AreEqual(1, index1);
        Assert.AreEqual(0, index2);
    }

    [TestMethod]
    public void BinarySearch_WorksWithLargeCollection()
    {
        var collection = new TestSortedCollection(1000, 100);

        // Insert items in random order
        var random = new Random(42);
        var values = Enumerable.Range(0, 100).OrderBy(_ => random.Next()).ToList();
        foreach (int v in values)
        {
            collection.Insert(new TestItem(v, v.ToString()));
        }

        // Verify sorted order
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, collection.At(i)!.Key);
        }

        // Verify search works
        for (int i = 0; i < 100; i++)
        {
            bool found = collection.Search(i, out int index);
            Assert.IsTrue(found);
            Assert.AreEqual(i, index);
        }
    }
}
