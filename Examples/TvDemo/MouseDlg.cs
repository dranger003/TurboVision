using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Platform;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A static text view that responds to double-clicks for testing mouse settings.
/// </summary>
public class TClickTester : TStaticText
{
    private static readonly byte[] MousePalette = [0x07, 0x08];

    private bool _clicked;

    public TClickTester(TRect bounds, string text) : base(bounds, text)
    {
        _clicked = false;
    }

    public override TPalette GetPalette()
    {
        return new TPalette(MousePalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            if ((ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0)
            {
                _clicked = !_clicked;
                DrawView();
            }
            ClearEvent(ref ev);
        }
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = _clicked ? GetColor(2).Normal : GetColor(1).Normal;

        buf.MoveChar(0, ' ', c, Size.X);
        if (Text != null)
            buf.MoveStr(0, Text, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }
}

/// <summary>
/// A dialog for configuring mouse options including double-click speed.
/// </summary>
public class TMouseDialog : TDialog
{
    private readonly TScrollBar _mouseScrollBar;
    private readonly ushort _oldDelay;

    public TMouseDialog()
        : base(new TRect(0, 0, 34, 12), "Mouse options")
    {
        Options |= OptionFlags.ofCentered;

        var r = new TRect(3, 4, 30, 5);
        _mouseScrollBar = new TScrollBar(r);
        _mouseScrollBar.SetParams(1, 1, 20, 20, 1);
        _mouseScrollBar.Options |= OptionFlags.ofSelectable;
        _mouseScrollBar.SetValue(TEventQueue.DoubleDelay);
        Insert(_mouseScrollBar);

        r = new TRect(2, 2, 21, 3);
        Insert(new TLabel(r, "~M~ouse double click", _mouseScrollBar));

        r = new TRect(3, 3, 30, 4);
        Insert(new TClickTester(r, "Fast       Medium      Slow"));

        r = new TRect(3, 6, 30, 7);
        Insert(new TCheckBoxes(r,
            new TSItem("~R~everse mouse buttons", null)));

        _oldDelay = TEventQueue.DoubleDelay;

        r = new TRect(9, 9, 19, 11);
        Insert(new TButton(r, "O~K~", CommandConstants.cmOK, CommandConstants.bfDefault));

        r = new TRect(21, 9, 31, 11);
        Insert(new TButton(r, "Cancel", CommandConstants.cmCancel, CommandConstants.bfNormal));

        SelectNext(false);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case EventConstants.evCommand:
                if (ev.Message.Command == CommandConstants.cmCancel)
                    TEventQueue.DoubleDelay = _oldDelay;
                break;

            case EventConstants.evBroadcast:
                if (ev.Message.Command == CommandConstants.cmScrollBarChanged)
                {
                    TEventQueue.DoubleDelay = (ushort)_mouseScrollBar.Value;
                    ClearEvent(ref ev);
                }
                break;
        }
    }
}
