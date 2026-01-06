using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Help;

/// <summary>
/// A scrollable viewer for help topics with hyperlink support.
/// </summary>
public class THelpViewer : TScroller
{
    /// <summary>
    /// The help file being displayed.
    /// </summary>
    public THelpFile? HelpFile { get; private set; }

    /// <summary>
    /// The current topic being displayed.
    /// </summary>
    public THelpTopic? Topic { get; private set; }

    /// <summary>
    /// The currently selected cross-reference (1-based).
    /// </summary>
    public int Selected { get; private set; }

    public THelpViewer(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar,
                       THelpFile helpFile, int context)
        : base(bounds, hScrollBar, vScrollBar)
    {
        Options |= OptionFlags.ofSelectable;
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        HelpFile = helpFile;
        Topic = helpFile.GetTopic(context);
        Topic.SetWidth(Size.X);
        SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
        Selected = 1;
    }

    public override void ChangeBounds(TRect bounds)
    {
        base.ChangeBounds(bounds);
        if (Topic != null)
        {
            Topic.SetWidth(Size.X);
            SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
        }
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        int keyCount = 0;
        TPoint keyPoint = new(0, 0);
        byte keyLength = 0;
        int keyRef = 0;

        var normal = GetColor(1).Normal;
        var keyword = GetColor(2).Normal;
        var selKeyword = GetColor(3).Normal;

        if (Topic == null)
        {
            b.MoveChar(0, ' ', normal, Size.X);
            for (int row = 0; row < Size.Y; row++)
            {
                WriteLine(0, row, Size.X, 1, b);
            }
            return;
        }

        Topic.SetWidth(Size.X);

        // Find first visible cross-ref
        if (Topic.GetNumCrossRefs() > 0)
        {
            do
            {
                Topic.GetCrossRef(keyCount, out keyPoint, out keyLength, out keyRef);
                keyCount++;
            } while (keyCount < Topic.GetNumCrossRefs() && keyPoint.Y <= Delta.Y);
        }

        for (int i = 1; i <= Size.Y; i++)
        {
            b.MoveChar(0, ' ', normal, Size.X);
            string line = Topic.GetLine(i + Delta.Y);

            if (StringWidth(line) > Delta.X)
            {
                // Skip first Delta.X characters
                string visible = line.Length > Delta.X ? line[Delta.X..] : string.Empty;
                b.MoveStr(0, visible, normal, Size.X);
            }

            // Highlight cross-references on this line
            while (i + Delta.Y == keyPoint.Y)
            {
                int len = keyLength;
                int x = keyPoint.X;

                if (x < Delta.X)
                {
                    len -= (Delta.X - x);
                    x = Delta.X;
                }

                var color = (keyCount == Selected) ? selKeyword : keyword;

                for (int j = 0; j < len && (x - Delta.X + j) < Size.X; j++)
                {
                    b.PutAttribute(x - Delta.X + j, color);
                }

                if (keyCount < Topic.GetNumCrossRefs())
                {
                    Topic.GetCrossRef(keyCount, out keyPoint, out keyLength, out keyRef);
                    keyCount++;
                }
                else
                {
                    keyPoint = new TPoint(0, 0);
                }
            }

            WriteLine(0, i - 1, Size.X, 1, b);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(HelpConstants.HelpViewerPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (Topic == null) return;

        TPoint keyPoint;
        byte keyLength;
        int keyRef;

        switch (ev.What)
        {
            case EventConstants.evKeyDown:
                switch (ev.KeyDown.KeyCode)
                {
                    case KeyConstants.kbTab:
                        Selected++;
                        if (Selected > Topic.GetNumCrossRefs())
                        {
                            Selected = 1;
                        }
                        if (Topic.GetNumCrossRefs() != 0)
                        {
                            MakeSelectVisible(Selected - 1, out keyPoint, out keyLength, out keyRef);
                        }
                        DrawView();
                        ClearEvent(ref ev);
                        break;

                    case KeyConstants.kbShiftTab:
                        Selected--;
                        if (Selected == 0)
                        {
                            Selected = Topic.GetNumCrossRefs();
                        }
                        if (Topic.GetNumCrossRefs() != 0)
                        {
                            MakeSelectVisible(Selected - 1, out keyPoint, out keyLength, out keyRef);
                        }
                        DrawView();
                        ClearEvent(ref ev);
                        break;

                    case KeyConstants.kbEnter:
                        if (Selected <= Topic.GetNumCrossRefs())
                        {
                            Topic.GetCrossRef(Selected - 1, out keyPoint, out keyLength, out keyRef);
                            SwitchToTopic(keyRef);
                        }
                        ClearEvent(ref ev);
                        break;

                    case KeyConstants.kbEsc:
                        ev.What = EventConstants.evCommand;
                        ev.Message.Command = CommandConstants.cmClose;
                        PutEvent(ev);
                        ClearEvent(ref ev);
                        break;
                }
                break;

            case EventConstants.evMouseDown:
                var mouse = MakeLocal(ev.Mouse.Where);
                mouse = new TPoint(mouse.X + Delta.X, mouse.Y + Delta.Y);
                int keyCount = 0;

                while (keyCount < Topic.GetNumCrossRefs())
                {
                    Topic.GetCrossRef(keyCount, out keyPoint, out keyLength, out keyRef);
                    keyCount++;

                    if (keyPoint.Y == mouse.Y + 1 &&
                        mouse.X >= keyPoint.X &&
                        mouse.X < keyPoint.X + keyLength)
                    {
                        Selected = keyCount;
                        DrawView();
                        SwitchToTopic(keyRef);
                        ClearEvent(ref ev);
                        return;
                    }
                }
                break;

            case EventConstants.evCommand:
                if (ev.Message.Command == CommandConstants.cmClose &&
                    Owner != null && (Owner.State & StateFlags.sfModal) != 0)
                {
                    EndModal(CommandConstants.cmClose);
                    ClearEvent(ref ev);
                }
                break;
        }
    }

    /// <summary>
    /// Ensures the selected cross-reference is visible.
    /// </summary>
    private void MakeSelectVisible(int index, out TPoint keyPoint, out byte keyLength, out int keyRef)
    {
        Topic!.GetCrossRef(index, out keyPoint, out keyLength, out keyRef);
        var d = Delta;

        if (keyPoint.X < d.X)
        {
            d = new TPoint(keyPoint.X, d.Y);
        }
        if (keyPoint.X > d.X + Size.X)
        {
            d = new TPoint(keyPoint.X - Size.X, d.Y);
        }
        if (keyPoint.Y <= d.Y)
        {
            d = new TPoint(d.X, keyPoint.Y - 1);
        }
        if (keyPoint.Y > d.Y + Size.Y)
        {
            d = new TPoint(d.X, keyPoint.Y - Size.Y);
        }

        if (d.X != Delta.X || d.Y != Delta.Y)
        {
            ScrollTo(d.X, d.Y);
        }
    }

    /// <summary>
    /// Switches to a different topic.
    /// </summary>
    public void SwitchToTopic(int context)
    {
        if (HelpFile == null) return;

        Topic = HelpFile.GetTopic(context);
        Topic.SetWidth(Size.X);
        ScrollTo(0, 0);
        SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
        Selected = 1;
        DrawView();
    }

    private static int StringWidth(string text)
    {
        int width = 0;
        foreach (char c in text)
        {
            if (c != '\n' && c != '\r')
            {
                width++;
            }
        }
        return width;
    }

    public override void ShutDown()
    {
        HelpFile?.Dispose();
        HelpFile = null;
        Topic = null;
        base.ShutDown();
    }
}
