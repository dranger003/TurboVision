namespace TurboVision.Tests;

using TurboVision.Core;
using TurboVision.Views;

/// <summary>
/// Tests for event dispatching through the view hierarchy.
/// </summary>
[TestClass]
public class TEventDispatchTests
{
    /// <summary>
    /// Helper view that tracks received events.
    /// </summary>
    private class EventTrackingView : TView
    {
        public List<(ushort What, ushort Command)> ReceivedEvents { get; } = new();
        public bool ClearOnReceive { get; set; }
        public ushort ClearOnCommand { get; set; }

        public EventTrackingView(TRect bounds) : base(bounds)
        {
        }

        public override void HandleEvent(ref TEvent ev)
        {
            base.HandleEvent(ref ev);

            if (ev.What == EventConstants.evCommand)
            {
                ReceivedEvents.Add((ev.What, ev.Message.Command));

                if (ClearOnReceive || ev.Message.Command == ClearOnCommand)
                {
                    ClearEvent(ref ev);
                }
            }
        }
    }

    /// <summary>
    /// Helper group that tracks events and passes them to children.
    /// </summary>
    private class EventTrackingGroup : TGroup
    {
        public List<(ushort What, ushort Command)> ReceivedEvents { get; } = new();
        public bool ClearOnReceive { get; set; }
        public ushort ClearOnCommand { get; set; }

        public EventTrackingGroup(TRect bounds) : base(bounds)
        {
        }

        public override void HandleEvent(ref TEvent ev)
        {
            base.HandleEvent(ref ev);

            if (ev.What == EventConstants.evCommand)
            {
                ReceivedEvents.Add((ev.What, ev.Message.Command));

                if (ClearOnReceive || ev.Message.Command == ClearOnCommand)
                {
                    ClearEvent(ref ev);
                }
            }
        }
    }

    /// <summary>
    /// Tests that command events reach parent handlers after child handlers.
    /// </summary>
    [TestMethod]
    public void CommandEvent_ShouldReachParentAfterChild()
    {
        var parent = new EventTrackingGroup(new TRect(0, 0, 80, 25));
        var child = new EventTrackingView(new TRect(10, 10, 30, 20));
        child.Options |= OptionFlags.ofSelectable; // Make it current/focused

        parent.Insert(child);

        // Create a command event
        var ev = TEvent.Command(100);

        // Dispatch through parent
        parent.HandleEvent(ref ev);

        // Both parent and child should have received the event
        // Child receives during focused phase, parent receives after base.HandleEvent
        Assert.HasCount(1, child.ReceivedEvents, "Child should receive the command");
        Assert.AreEqual(100, child.ReceivedEvents[0].Command, "Child should receive command 100");

        Assert.HasCount(1, parent.ReceivedEvents, "Parent should receive the command");
        Assert.AreEqual(100, parent.ReceivedEvents[0].Command, "Parent should receive command 100");

        // Event should not be cleared (neither handler cleared it)
        Assert.AreEqual(EventConstants.evCommand, ev.What, "Event should still be evCommand");
    }

    /// <summary>
    /// Tests that if a child clears a command event, parent doesn't receive it.
    /// </summary>
    [TestMethod]
    public void CommandEvent_ClearedByChild_ShouldNotReachParent()
    {
        var parent = new EventTrackingGroup(new TRect(0, 0, 80, 25));
        var child = new EventTrackingView(new TRect(10, 10, 30, 20));
        child.Options |= OptionFlags.ofSelectable;
        child.ClearOnCommand = 100; // Clear when receiving command 100

        parent.Insert(child);

        var ev = TEvent.Command(100);
        parent.HandleEvent(ref ev);

        // Child should have received and cleared the event
        Assert.HasCount(1, child.ReceivedEvents, "Child should receive the command");

        // Parent should NOT have received it (event was cleared)
        Assert.IsEmpty(parent.ReceivedEvents, "Parent should NOT receive cleared command");

        // Event should be cleared
        Assert.AreEqual(EventConstants.evNothing, ev.What, "Event should be cleared");
    }

    /// <summary>
    /// Tests that command events dispatched via focusedEvents go to the current (focused) view.
    /// </summary>
    [TestMethod]
    public void CommandEvent_ShouldGoToFocusedView()
    {
        var parent = new EventTrackingGroup(new TRect(0, 0, 80, 25));

        var view1 = new EventTrackingView(new TRect(10, 10, 30, 20));
        view1.Options |= OptionFlags.ofSelectable;

        var view2 = new EventTrackingView(new TRect(40, 10, 60, 20));
        view2.Options |= OptionFlags.ofSelectable;

        parent.Insert(view1);
        parent.Insert(view2); // view2 becomes current (last selectable inserted)

        var ev = TEvent.Command(100);
        parent.HandleEvent(ref ev);

        // Only view2 (current) should receive the command during focused phase
        // view1 would only receive during preprocess/postprocess if it had those flags
        Assert.IsEmpty(view1.ReceivedEvents, "Non-focused view should not receive command");
        Assert.HasCount(1, view2.ReceivedEvents, "Focused view should receive command");
    }

    /// <summary>
    /// Tests that preprocess views receive command events before the focused view.
    /// </summary>
    [TestMethod]
    public void CommandEvent_PreprocessView_ShouldReceiveBeforeFocused()
    {
        var order = new List<string>();

        var parent = new OrderTrackingGroup(new TRect(0, 0, 80, 25), "parent", order);

        var preprocessView = new OrderTrackingView(new TRect(0, 0, 10, 1), "preprocess", order);
        preprocessView.Options |= OptionFlags.ofPreProcess;

        var focusedView = new OrderTrackingView(new TRect(10, 10, 30, 20), "focused", order);
        focusedView.Options |= OptionFlags.ofSelectable;

        parent.Insert(preprocessView);
        parent.Insert(focusedView);

        var ev = TEvent.Command(100);
        parent.HandleEvent(ref ev);

        // Order should be: preprocess, then focused, then parent
        Assert.HasCount(3, order, "All three should receive");
        Assert.AreEqual("preprocess", order[0], "Preprocess should be first");
        Assert.AreEqual("focused", order[1], "Focused should be second");
        Assert.AreEqual("parent", order[2], "Parent should be last");
    }

    private class OrderTrackingView : TView
    {
        private readonly string _name;
        private readonly List<string> _order;

        public OrderTrackingView(TRect bounds, string name, List<string> order) : base(bounds)
        {
            _name = name;
            _order = order;
        }

        public override void HandleEvent(ref TEvent ev)
        {
            base.HandleEvent(ref ev);
            if (ev.What == EventConstants.evCommand)
            {
                _order.Add(_name);
            }
        }
    }

    private class OrderTrackingGroup : TGroup
    {
        private readonly string _name;
        private readonly List<string> _order;

        public OrderTrackingGroup(TRect bounds, string name, List<string> order) : base(bounds)
        {
            _name = name;
            _order = order;
        }

        public override void HandleEvent(ref TEvent ev)
        {
            base.HandleEvent(ref ev);
            if (ev.What == EventConstants.evCommand)
            {
                _order.Add(_name);
            }
        }
    }

    /// <summary>
    /// Tests that custom commands (> 255) are always enabled.
    /// </summary>
    [TestMethod]
    public void CommandEnabled_CustomCommand_ShouldAlwaysBeEnabled()
    {
        // Custom commands > 255 should always be enabled
        Assert.IsTrue(TView.CommandEnabled(256), "Command 256 should be enabled");
        Assert.IsTrue(TView.CommandEnabled(1000), "Command 1000 should be enabled");
        Assert.IsTrue(TView.CommandEnabled(ushort.MaxValue), "Command 65535 should be enabled");
    }

    /// <summary>
    /// Tests that standard commands (0-255) can be enabled and disabled.
    /// </summary>
    [TestMethod]
    public void CommandEnabled_StandardCommand_CanBeToggled()
    {
        // Command 100 should be enabled by default
        Assert.IsTrue(TView.CommandEnabled(100), "Command 100 should be enabled by default");

        // Disable it
        TView.DisableCommand(100);
        Assert.IsFalse(TView.CommandEnabled(100), "Command 100 should be disabled");

        // Re-enable it
        TView.EnableCommand(100);
        Assert.IsTrue(TView.CommandEnabled(100), "Command 100 should be enabled again");
    }
}
