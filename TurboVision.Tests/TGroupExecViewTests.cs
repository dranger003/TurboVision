namespace TurboVision.Tests;

using TurboVision.Core;
using TurboVision.Views;

/// <summary>
/// Tests for TGroup.ExecView modal execution.
/// These tests modify static TView.CurCommandSet state and must not run in parallel.
/// </summary>
[TestClass]
[DoNotParallelize]
public class TGroupExecViewTests
{
    /// <summary>
    /// Helper class to track if a view is in the owner's view hierarchy.
    /// </summary>
    private class TestView : TView
    {
        public bool WasInHierarchyDuringExecute { get; private set; }
        public ushort ExecuteReturnValue { get; set; } = CommandConstants.cmOK;

        public TestView(TRect bounds) : base(bounds)
        {
        }

        public override ushort Execute()
        {
            // Check if we're still in the hierarchy during Execute
            WasInHierarchyDuringExecute = Owner != null;
            return ExecuteReturnValue;
        }
    }

    /// <summary>
    /// Helper group class for testing.
    /// </summary>
    private class TestGroup : TGroup
    {
        public TestGroup(TRect bounds) : base(bounds)
        {
        }
    }

    /// <summary>
    /// Tests that ExecView on a view that wasn't owned inserts and then removes it.
    /// </summary>
    [TestMethod]
    public void ExecView_UnownedView_ShouldInsertThenRemove()
    {
        var group = new TestGroup(new TRect(0, 0, 80, 25));
        var view = new TestView(new TRect(10, 10, 30, 20));

        // View starts unowned
        Assert.IsNull(view.Owner, "View should start unowned");

        // Execute the view
        var result = group.ExecView(view);

        // After ExecView, view should be removed (unowned again)
        Assert.IsNull(view.Owner, "View should be unowned after ExecView");
        Assert.IsTrue(view.WasInHierarchyDuringExecute, "View should have been in hierarchy during Execute");
        Assert.AreEqual(CommandConstants.cmOK, result, "Should return the execute result");
    }

    /// <summary>
    /// Tests that ExecView on an already-owned view does NOT remove it from the hierarchy.
    /// This is the critical bug that was fixed - menus were being removed after ExecView.
    /// </summary>
    [TestMethod]
    public void ExecView_AlreadyOwnedView_ShouldNotRemove()
    {
        var group = new TestGroup(new TRect(0, 0, 80, 25));
        var view = new TestView(new TRect(10, 10, 30, 20));

        // Pre-insert the view into the group
        group.Insert(view);

        // View should be owned by group
        Assert.AreEqual(group, view.Owner, "View should be owned by group after Insert");

        // Execute the view
        var result = group.ExecView(view);

        // After ExecView, view should STILL be owned (not removed!)
        Assert.AreEqual(group, view.Owner, "View should STILL be owned after ExecView on already-owned view");
        Assert.IsTrue(view.WasInHierarchyDuringExecute, "View should have been in hierarchy during Execute");
        Assert.AreEqual(CommandConstants.cmOK, result, "Should return the execute result");
    }

    /// <summary>
    /// Tests that command set is properly saved and restored during ExecView.
    /// </summary>
    [TestMethod]
    public void ExecView_ShouldRestoreCommandSet()
    {
        var group = new TestGroup(new TRect(0, 0, 80, 25));

        // Create a view that disables some commands during Execute
        var view = new CommandChangingView(new TRect(10, 10, 30, 20));

        // Enable a test command before ExecView
        TView.EnableCommand(100);
        Assert.IsTrue(TView.CommandEnabled(100), "Command 100 should be enabled before ExecView");

        // Execute the view (which will disable command 100)
        group.ExecView(view);

        // After ExecView, command 100 should be restored to enabled
        Assert.IsTrue(TView.CommandEnabled(100), "Command 100 should be restored after ExecView");
    }

    private class CommandChangingView : TView
    {
        public CommandChangingView(TRect bounds) : base(bounds) { }

        public override ushort Execute()
        {
            // Disable a command during modal execution
            TView.DisableCommand(100);
            return CommandConstants.cmOK;
        }
    }

    /// <summary>
    /// Tests that view options are properly saved and restored during ExecView.
    /// </summary>
    [TestMethod]
    public void ExecView_ShouldRestoreViewOptions()
    {
        var group = new TestGroup(new TRect(0, 0, 80, 25));
        var view = new TestView(new TRect(10, 10, 30, 20));

        // Set specific options
        view.Options = OptionFlags.ofSelectable | OptionFlags.ofFirstClick;
        var originalOptions = view.Options;

        group.ExecView(view);

        // Options should be restored
        Assert.AreEqual(originalOptions, view.Options, "View options should be restored after ExecView");
    }

    /// <summary>
    /// Tests that sfModal state is properly set during ExecView and cleared after.
    /// </summary>
    [TestMethod]
    public void ExecView_ShouldSetAndClearModalState()
    {
        var group = new TestGroup(new TRect(0, 0, 80, 25));
        var view = new ModalStateTrackingView(new TRect(10, 10, 30, 20));

        // Not modal before
        Assert.IsFalse(view.GetState(StateFlags.sfModal), "View should not be modal before ExecView");

        group.ExecView(view);

        // Should have been modal during Execute
        Assert.IsTrue(view.WasModalDuringExecute, "View should be modal during Execute");

        // Not modal after
        Assert.IsFalse(view.GetState(StateFlags.sfModal), "View should not be modal after ExecView");
    }

    private class ModalStateTrackingView : TView
    {
        public bool WasModalDuringExecute { get; private set; }

        public ModalStateTrackingView(TRect bounds) : base(bounds) { }

        public override ushort Execute()
        {
            WasModalDuringExecute = GetState(StateFlags.sfModal);
            return CommandConstants.cmOK;
        }
    }
}
