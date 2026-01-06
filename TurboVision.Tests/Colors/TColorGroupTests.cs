namespace TurboVision.Tests.Colors;

using TurboVision.Colors;

/// <summary>
/// Tests for TColorGroup and TColorItem.
/// </summary>
[TestClass]
public class TColorGroupTests
{
    [TestMethod]
    public void TColorGroup_Constructor_ShouldSetName()
    {
        var group = new TColorGroup("Desktop");

        Assert.AreEqual("Desktop", group.Name);
        Assert.IsNull(group.Items);
        Assert.IsNull(group.Next);
        Assert.AreEqual(0, group.Index);
    }

    [TestMethod]
    public void TColorGroup_Constructor_WithItems_ShouldSetItems()
    {
        var item = new TColorItem("Color", 1);
        var group = new TColorGroup("Desktop", item);

        Assert.AreEqual("Desktop", group.Name);
        Assert.IsNotNull(group.Items);
        Assert.AreEqual("Color", group.Items.Name);
    }

    [TestMethod]
    public void TColorGroup_PlusOperator_ShouldAddItemToGroup()
    {
        var group = new TColorGroup("Desktop");
        var item = new TColorItem("Color", 1);

        var result = group + item;

        Assert.AreSame(group, result);
        Assert.IsNotNull(group.Items);
        Assert.AreEqual("Color", group.Items.Name);
        Assert.AreEqual(1, group.Items.Index);
    }

    [TestMethod]
    public void TColorGroup_PlusOperator_ShouldChainMultipleItems()
    {
        var group = new TColorGroup("Menus")
            + new TColorItem("Normal", 2)
            + new TColorItem("Disabled", 3)
            + new TColorItem("Selected", 4);

        Assert.IsNotNull(group.Items);
        Assert.AreEqual("Normal", group.Items.Name);
        Assert.AreEqual(2, group.Items.Index);

        Assert.IsNotNull(group.Items.Next);
        Assert.AreEqual("Disabled", group.Items.Next.Name);

        Assert.IsNotNull(group.Items.Next.Next);
        Assert.AreEqual("Selected", group.Items.Next.Next.Name);

        Assert.IsNull(group.Items.Next.Next.Next);
    }

    [TestMethod]
    public void TColorGroup_PlusOperator_ShouldChainGroups()
    {
        var group1 = new TColorGroup("Desktop") + new TColorItem("Color", 1);
        var group2 = new TColorGroup("Menus") + new TColorItem("Normal", 2);
        var group3 = new TColorGroup("Dialogs") + new TColorItem("Frame", 3);

        var result = group1 + group2 + group3;

        Assert.AreSame(group1, result);
        Assert.AreSame(group2, group1.Next);
        Assert.AreSame(group3, group2.Next);
        Assert.IsNull(group3.Next);
    }

    [TestMethod]
    public void TColorGroup_Index_ShouldBeSettable()
    {
        var group = new TColorGroup("Test");
        group.Index = 5;

        Assert.AreEqual(5, group.Index);
    }

    [TestMethod]
    public void TColorGroup_CountItems_ShouldCountCorrectly()
    {
        var group = new TColorGroup("Test")
            + new TColorItem("Item1", 1)
            + new TColorItem("Item2", 2)
            + new TColorItem("Item3", 3);

        int count = 0;
        var item = group.Items;
        while (item != null)
        {
            count++;
            item = item.Next;
        }

        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public void TColorGroup_CountGroups_ShouldCountCorrectly()
    {
        var groups = new TColorGroup("Group1")
            + new TColorGroup("Group2")
            + new TColorGroup("Group3")
            + new TColorGroup("Group4");

        int count = 0;
        TColorGroup? g = groups;
        while (g != null)
        {
            count++;
            g = g.Next;
        }

        Assert.AreEqual(4, count);
    }
}

[TestClass]
public class TColorItemTests
{
    [TestMethod]
    public void TColorItem_Constructor_ShouldSetNameAndIndex()
    {
        var item = new TColorItem("Frame/background", 33);

        Assert.AreEqual("Frame/background", item.Name);
        Assert.AreEqual(33, item.Index);
        Assert.IsNull(item.Next);
    }

    [TestMethod]
    public void TColorItem_Constructor_WithNext_ShouldChain()
    {
        var item2 = new TColorItem("Second", 2);
        var item1 = new TColorItem("First", 1, item2);

        Assert.AreSame(item2, item1.Next);
    }

    [TestMethod]
    public void TColorItem_Next_ShouldBeSettable()
    {
        var item1 = new TColorItem("First", 1);
        var item2 = new TColorItem("Second", 2);

        item1.Next = item2;

        Assert.AreSame(item2, item1.Next);
    }
}

[TestClass]
public class TColorIndexTests
{
    [TestMethod]
    public void TColorIndex_DefaultConstructor_ShouldInitialize()
    {
        var colorIndex = new TColorIndex();

        Assert.AreEqual(0, colorIndex.GroupIndex);
        Assert.AreEqual(0, colorIndex.ColorSize);
        Assert.IsNotNull(colorIndex.ColorIndices);
        Assert.HasCount(256, colorIndex.ColorIndices);
    }

    [TestMethod]
    public void TColorIndex_Properties_ShouldBeSettable()
    {
        var colorIndex = new TColorIndex
        {
            GroupIndex = 5,
            ColorSize = 10
        };
        colorIndex.ColorIndices[0] = 42;

        Assert.AreEqual(5, colorIndex.GroupIndex);
        Assert.AreEqual(10, colorIndex.ColorSize);
        Assert.AreEqual(42, colorIndex.ColorIndices[0]);
    }
}
