namespace TurboVision.Tests.Streaming;

using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
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

    // View hierarchy round-trip tests

    [TestMethod]
    public void TDialog_WithButton_ShouldRoundTrip()
    {
        var dialog = new TDialog(new TRect(0, 0, 40, 10), "Test Dialog");
        var button = new TButton(new TRect(10, 5, 30, 7), "OK", CommandConstants.cmOK, CommandConstants.bfDefault);
        dialog.Insert(button);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual("Test Dialog", deserialized.Title);
        Assert.AreEqual(dialog.Size, deserialized.Size);

        // Verify button was restored
        var restoredButton = deserialized.First() as TButton;
        Assert.IsNotNull(restoredButton);
        Assert.AreEqual("OK", restoredButton.Title);
        Assert.AreEqual(CommandConstants.cmOK, restoredButton.Command);
    }

    [TestMethod]
    public void TDialog_WithInputLine_ShouldRoundTrip()
    {
        var dialog = new TDialog(new TRect(0, 0, 50, 12), "Input Dialog");
        var inputLine = new TInputLine(new TRect(10, 3, 40, 4), 128);
        inputLine.Data = "Test Value";
        dialog.Insert(inputLine);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);

        // Find the input line (skip frame which is at position 0)
        TInputLine? restoredInput = null;
        var view = deserialized.First();
        while (view != null)
        {
            if (view is TInputLine inp)
            {
                restoredInput = inp;
                break;
            }
            view = view.NextView();
        }

        Assert.IsNotNull(restoredInput);
        Assert.AreEqual("Test Value", restoredInput.Data);
        // MaxLen is limit-1 for ilMaxBytes mode, so 128 becomes 127
        Assert.AreEqual(127, restoredInput.MaxLen);
    }

    [TestMethod]
    public void TDialog_WithLabelAndInputLine_ShouldResolveLinkIndex()
    {
        var dialog = new TDialog(new TRect(0, 0, 50, 12), "Labeled Input");

        // TDialog inserts a Frame at position 0
        // InputLine will be at position 1
        var inputLine = new TInputLine(new TRect(15, 3, 40, 4), 64);
        dialog.Insert(inputLine);

        var label = new TLabel(new TRect(2, 3, 14, 4), "~N~ame:", inputLine);
        // Set LinkIndex to 1 (inputLine is after the frame)
        label.LinkIndex = 1;
        dialog.Insert(label);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);

        // Find label and input line
        TLabel? restoredLabel = null;
        TInputLine? restoredInput = null;
        var view = deserialized.First();
        while (view != null)
        {
            if (view is TLabel lbl) restoredLabel = lbl;
            if (view is TInputLine inp) restoredInput = inp;
            view = view.NextView();
        }

        Assert.IsNotNull(restoredLabel);
        Assert.IsNotNull(restoredInput);
        Assert.AreEqual("~N~ame:", restoredLabel.Text);

        // Verify link was resolved
        Assert.IsNotNull(restoredLabel.Link, "Label.Link should be resolved after deserialization");
        Assert.AreSame(restoredInput, restoredLabel.Link, "Label.Link should point to the InputLine");
    }

    [TestMethod]
    public void TDialog_MultipleControls_ShouldPreserveZOrder()
    {
        var dialog = new TDialog(new TRect(0, 0, 60, 15), "Multiple Controls");

        // TDialog inserts a Frame first (which becomes Last)
        // Insert() calls InsertBefore(p, First()), so new views go to the front
        // Order after inserts: cancelButton -> okButton -> label -> inputLine -> Frame (= Last)
        var inputLine = new TInputLine(new TRect(15, 3, 50, 4), 64);
        dialog.Insert(inputLine);

        var label = new TLabel(new TRect(2, 3, 14, 4), "~N~ame:", inputLine);
        dialog.Insert(label);

        var okButton = new TButton(new TRect(10, 10, 25, 12), "~O~K", CommandConstants.cmOK, CommandConstants.bfDefault);
        dialog.Insert(okButton);

        var cancelButton = new TButton(new TRect(30, 10, 50, 12), "~C~ancel", CommandConstants.cmCancel, CommandConstants.bfNormal);
        dialog.Insert(cancelButton);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);

        // Verify all controls are present and in order
        var views = new List<TView>();
        var v = deserialized.First();
        while (v != null)
        {
            views.Add(v);
            v = v.NextView();
        }

        // Should have 5 views in insertion order (newest first):
        // cancelButton, okButton, label, inputLine, Frame
        Assert.AreEqual(5, views.Count, "Should have 5 controls (frame + 4 controls)");
        Assert.IsInstanceOfType(views[0], typeof(TButton));     // cancelButton
        Assert.IsInstanceOfType(views[1], typeof(TButton));     // okButton
        Assert.IsInstanceOfType(views[2], typeof(TLabel));      // label
        Assert.IsInstanceOfType(views[3], typeof(TInputLine));  // inputLine
        Assert.IsInstanceOfType(views[4], typeof(TFrame));      // frame
    }

    [TestMethod]
    public void TDialog_WithCheckBoxes_ShouldRoundTrip()
    {
        var dialog = new TDialog(new TRect(0, 0, 50, 12), "Options");

        var items = new TSItem("Option 1",
                   new TSItem("Option 2",
                   new TSItem("Option 3", null)));
        var checkBoxes = new TCheckBoxes(new TRect(5, 3, 40, 6), items);
        checkBoxes.Value = 5; // Option 1 and Option 3 selected (bits 0 and 2)
        dialog.Insert(checkBoxes);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);

        var restoredCheckBoxes = deserialized.First() as TCheckBoxes;
        Assert.IsNotNull(restoredCheckBoxes);
        Assert.AreEqual(5u, restoredCheckBoxes.Value);
    }

    [TestMethod]
    public void TView_StateMasking_ShouldExcludeRuntimeFlags()
    {
        var view = new TView(new TRect(0, 0, 10, 5));
        // Set both persistent and runtime flags
        view.State = (ushort)(StateFlags.sfVisible | StateFlags.sfActive | StateFlags.sfFocused | StateFlags.sfExposed);

        var json = System.Text.Json.JsonSerializer.Serialize(view, _serializer.Options);

        // Parse JSON to check the state value
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var stateValue = doc.RootElement.GetProperty("state").GetUInt16();

        // Should only have sfVisible (runtime flags masked out)
        Assert.AreEqual(StateFlags.sfVisible, stateValue);
    }

    [TestMethod]
    public void TGroup_CircularList_ShouldBeReconstructed()
    {
        var group = new TGroup(new TRect(0, 0, 80, 25));
        var view1 = new TView(new TRect(0, 0, 10, 5));
        var view2 = new TView(new TRect(10, 0, 20, 5));
        var view3 = new TView(new TRect(20, 0, 30, 5));

        group.Insert(view1);
        group.Insert(view2);
        group.Insert(view3);

        var json = _serializer.Serialize(group);
        var deserialized = _serializer.Deserialize<TGroup>(json);

        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.Last, "Last should be set");
        Assert.IsNotNull(deserialized.First(), "First should be accessible");

        // Verify circular list structure
        var first = deserialized.First();
        Assert.IsNotNull(first);
        Assert.IsNotNull(first.Next);
        Assert.IsNotNull(first.Next.Next);

        // Last.Next should be First (circular)
        Assert.AreSame(deserialized.Last!.Next, deserialized.First());

        // Each view should have Owner set
        var v = deserialized.First();
        while (v != null)
        {
            Assert.AreSame(deserialized, v.Owner, "Each view should have Owner set to the group");
            v = v.NextView();
        }
    }

    [TestMethod]
    public void TDialog_WithRadioButtons_ShouldRoundTrip()
    {
        var dialog = new TDialog(new TRect(0, 0, 50, 12), "Selection");

        var items = new TSItem("Choice A",
                   new TSItem("Choice B",
                   new TSItem("Choice C", null)));
        var radioButtons = new TRadioButtons(new TRect(5, 3, 40, 6), items);
        radioButtons.Value = 1; // Choice B selected
        dialog.Insert(radioButtons);

        var json = _serializer.Serialize(dialog);
        var deserialized = _serializer.Deserialize<TDialog>(json);

        Assert.IsNotNull(deserialized);

        var restoredRadioButtons = deserialized.First() as TRadioButtons;
        Assert.IsNotNull(restoredRadioButtons);
        Assert.AreEqual(1u, restoredRadioButtons.Value);
    }
}
