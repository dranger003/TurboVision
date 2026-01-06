namespace TurboVision.Tests.Streaming;

using TurboVision.Core;
using TurboVision.Streaming;
using TurboVision.Streaming.Json;
using TurboVision.Menus;
using TurboVision.Views;

/// <summary>
/// Tests for JSON serialization of TurboVision objects.
/// </summary>
[TestClass]
public class JsonSerializerTests
{
    private JsonStreamSerializer _serializer = null!;

    [TestInitialize]
    public void Setup()
    {
        _serializer = new JsonStreamSerializer();
    }

    [TestMethod]
    public void TPoint_RoundTrip_ShouldPreserveValues()
    {
        var original = new TPoint(10, 20);
        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TPoint>(json, _serializer.Options);

        Assert.AreEqual(original.X, deserialized.X);
        Assert.AreEqual(original.Y, deserialized.Y);
    }

    [TestMethod]
    public void TRect_RoundTrip_ShouldPreserveValues()
    {
        var original = new TRect(5, 10, 25, 30);
        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TRect>(json, _serializer.Options);

        Assert.AreEqual(original.A.X, deserialized.A.X);
        Assert.AreEqual(original.A.Y, deserialized.A.Y);
        Assert.AreEqual(original.B.X, deserialized.B.X);
        Assert.AreEqual(original.B.Y, deserialized.B.Y);
    }

    [TestMethod]
    public void TKey_RoundTrip_ShouldPreserveValues()
    {
        var original = new TKey(KeyConstants.kbF1, KeyConstants.kbAltShift);
        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TKey>(json, _serializer.Options);

        Assert.AreEqual(original.KeyCode, deserialized.KeyCode);
        Assert.AreEqual(original.ControlKeyState, deserialized.ControlKeyState);
    }

    [TestMethod]
    public void TMenuItem_RoundTrip_ShouldPreserveProperties()
    {
        var original = new TMenuItem("~F~ile", 100, new TKey(KeyConstants.kbAltF, 0), HelpContexts.hcNoContext, "Alt-F");

        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TMenuItem>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Command, deserialized.Command);
        Assert.AreEqual(original.Param, deserialized.Param);
    }

    [TestMethod]
    public void TMenuItem_WithSubMenu_ShouldSerializeRecursively()
    {
        var subItem = new TMenuItem("~S~ub Item", 101, new TKey(0, 0));
        var subMenu = new TMenu(subItem);
        var original = new TMenuItem("~P~arent", new TKey(0, 0), subMenu);

        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TMenuItem>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.IsNotNull(deserialized.SubMenu);
        Assert.IsNotNull(deserialized.SubMenu.Items);
        Assert.AreEqual("~S~ub Item", deserialized.SubMenu.Items.Name);
    }

    [TestMethod]
    public void TMenu_WithMultipleItems_ShouldSerializeAsArray()
    {
        var item1 = new TMenuItem("~N~ew", 100, new TKey(KeyConstants.kbCtrlN, 0));
        var item2 = new TMenuItem("~O~pen", 101, new TKey(KeyConstants.kbCtrlO, 0));
        item1.Next = item2;
        var menu = new TMenu(item1);

        var json = System.Text.Json.JsonSerializer.Serialize(menu, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TMenu>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Items);
        Assert.AreEqual("~N~ew", deserialized.Items.Name);
        Assert.IsNotNull(deserialized.Items.Next);
        Assert.AreEqual("~O~pen", deserialized.Items.Next.Name);
    }

    [TestMethod]
    public void TMenu_DefaultItem_ShouldBePreserved()
    {
        var item1 = new TMenuItem("~N~ew", 100, new TKey(0, 0));
        var item2 = new TMenuItem("~O~pen", 101, new TKey(0, 0));
        item1.Next = item2;
        var menu = new TMenu(item1, item2); // Default is item2

        var json = System.Text.Json.JsonSerializer.Serialize(menu, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TMenu>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Default);
        Assert.AreEqual("~O~pen", deserialized.Default.Name);
    }

    [TestMethod]
    public void TStatusItem_RoundTrip_ShouldPreserveProperties()
    {
        var original = new TStatusItem("~F1~ Help", new TKey(KeyConstants.kbF1, 0), CommandConstants.cmHelp);

        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TStatusItem>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Text, deserialized.Text);
        Assert.AreEqual(original.Command, deserialized.Command);
    }

    [TestMethod]
    public void TStatusDef_WithItems_ShouldSerialize()
    {
        var item1 = new TStatusItem("~F1~ Help", new TKey(KeyConstants.kbF1, 0), CommandConstants.cmHelp);
        var item2 = new TStatusItem("~Alt-X~ Exit", new TKey(KeyConstants.kbAltX, 0), CommandConstants.cmQuit);
        item1.Next = item2;
        var def = new TStatusDef(0, 0xFFFF, item1);

        var json = System.Text.Json.JsonSerializer.Serialize(def, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TStatusDef>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual((ushort)0, deserialized.Min);
        Assert.AreEqual((ushort)0xFFFF, deserialized.Max);
        Assert.IsNotNull(deserialized.Items);
        Assert.AreEqual("~F1~ Help", deserialized.Items.Text);
        Assert.IsNotNull(deserialized.Items.Next);
        Assert.AreEqual("~Alt-X~ Exit", deserialized.Items.Next.Text);
    }

    [TestMethod]
    public void TStatusDef_WithChainedDefs_ShouldSerialize()
    {
        var item1 = new TStatusItem("~F1~ Help", new TKey(KeyConstants.kbF1, 0), CommandConstants.cmHelp);
        var def1 = new TStatusDef(0, 100, item1);

        var item2 = new TStatusItem("~Alt-X~ Exit", new TKey(KeyConstants.kbAltX, 0), CommandConstants.cmQuit);
        var def2 = new TStatusDef(101, 200, item2);
        def1.Next = def2;

        var json = System.Text.Json.JsonSerializer.Serialize(def1, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TStatusDef>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual((ushort)0, deserialized.Min);
        Assert.AreEqual((ushort)100, deserialized.Max);
        Assert.IsNotNull(deserialized.Next);
        Assert.AreEqual((ushort)101, deserialized.Next.Min);
        Assert.AreEqual((ushort)200, deserialized.Next.Max);
    }

    [TestMethod]
    public void TKey_Default_ShouldSerializeCorrectly()
    {
        var original = new TKey(0, 0);
        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TKey>(json, _serializer.Options);

        Assert.AreEqual((ushort)0, deserialized.KeyCode);
        Assert.AreEqual((ushort)0, deserialized.ControlKeyState);
    }

    [TestMethod]
    public void TPoint_Negative_ShouldSerializeCorrectly()
    {
        var original = new TPoint(-10, -20);
        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TPoint>(json, _serializer.Options);

        Assert.AreEqual(-10, deserialized.X);
        Assert.AreEqual(-20, deserialized.Y);
    }

    [TestMethod]
    public void TMenuItem_Separator_ShouldSerializeCorrectly()
    {
        var original = TMenuItem.NewLine();

        var json = System.Text.Json.JsonSerializer.Serialize(original, _serializer.Options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TMenuItem>(json, _serializer.Options);

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(deserialized.IsSeparator);
    }
}
