using TurboVision.Application;
using TurboVision.Colors;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Menus;
using TurboVision.Platform;
using TurboVision.Views;
using static TurboVision.Dialogs.MsgBox;

namespace TvDemo;

/// <summary>
/// TVDemo - The canonical Turbo Vision demonstration application.
/// </summary>
public class TVDemo : TApplication
{
    private THeapView? _heap;
    private TClockView? _clock;

    public TVDemo() : base()
    {
        // Create the clock view at top right of menu bar
        var r = GetExtent();
        r.A = new TPoint(r.B.X - 9, r.A.Y);
        r.B = new TPoint(r.B.X, r.A.Y + 1);
        _clock = new TClockView(r);
        _clock.GrowMode = GrowFlags.gfGrowLoX | GrowFlags.gfGrowHiX;
        Insert(_clock);

        // Create the heap view at bottom right of status line
        r = GetExtent();
        r.A = new TPoint(r.B.X - 13, r.B.Y - 1);
        _heap = new THeapView(r);
        _heap.GrowMode = GrowFlags.gfGrowAll;
        Insert(_heap);
    }

    public override TStatusLine? InitStatusLine(TRect r)
    {
        r.A = new TPoint(r.A.X, r.B.Y - 1);

        return new TStatusLine(r,
            new TStatusDef(0, 50,
                new TStatusItem("~F1~ Help", KeyConstants.kbF1, CommandConstants.cmHelp,
                new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit,
                new TStatusItem(null, KeyConstants.kbShiftDel, CommandConstants.cmCut,
                new TStatusItem(null, KeyConstants.kbCtrlIns, CommandConstants.cmCopy,
                new TStatusItem(null, KeyConstants.kbShiftIns, CommandConstants.cmPaste,
                new TStatusItem(null, KeyConstants.kbAltF3, CommandConstants.cmClose,
                new TStatusItem(null, KeyConstants.kbF10, CommandConstants.cmMenu,
                new TStatusItem(null, KeyConstants.kbF5, CommandConstants.cmZoom,
                new TStatusItem(null, KeyConstants.kbNoKey, CommandConstants.cmResize, null))))))))),
            new TStatusDef(50, 0xFFFF,
                new TStatusItem("Howdy", KeyConstants.kbF1, CommandConstants.cmHelp, null),
            null))
        );
    }

    public override TMenuBar? InitMenuBar(TRect r)
    {
        r.B = new TPoint(r.B.X, r.A.Y + 1);

        var sub1 = new TSubMenu("~\u2261~", 0, DemoHelp.hcSystem)
            .Add(new TMenuItem("~A~bout...", TvCmds.cmAboutCmd, KeyConstants.kbNoKey, DemoHelp.hcSAbout))
            .Add(TMenuItem.NewLine())
            .Add(new TMenuItem("~P~uzzle", TvCmds.cmPuzzleCmd, KeyConstants.kbNoKey, DemoHelp.hcSPuzzle))
            .Add(new TMenuItem("Ca~l~endar", TvCmds.cmCalendarCmd, KeyConstants.kbNoKey, DemoHelp.hcSCalendar))
            .Add(new TMenuItem("Ascii ~T~able", TvCmds.cmAsciiCmd, KeyConstants.kbNoKey, DemoHelp.hcSAsciiTable))
            .Add(new TMenuItem("~C~alculator", TvCmds.cmCalcCmd, KeyConstants.kbNoKey, DemoHelp.hcSCalculator))
            .Add(new TMenuItem("~E~vent Viewer", TvCmds.cmEventViewCmd, KeyConstants.kbNoKey, DemoHelp.hcNoContext, "Alt-0"));

        var sub2 = new TSubMenu("~F~ile", 0, DemoHelp.hcFile)
            .Add(new TMenuItem("~O~pen...", TvCmds.cmOpenCmd, KeyConstants.kbF3, DemoHelp.hcFOpen, "F3"))
            .Add(new TMenuItem("~C~hange Dir...", TvCmds.cmChDirCmd, KeyConstants.kbNoKey, DemoHelp.hcFChangeDir))
            .Add(TMenuItem.NewLine())
            .Add(new TMenuItem("~D~OS Shell", CommandConstants.cmDosShell, KeyConstants.kbNoKey, DemoHelp.hcFDosShell))
            .Add(new TMenuItem("E~x~it", CommandConstants.cmQuit, KeyConstants.kbAltX, DemoHelp.hcFExit, "Alt-X"));

        var sub3 = new TSubMenu("~W~indows", 0, DemoHelp.hcWindows)
            .Add(new TMenuItem("~R~esize/move", CommandConstants.cmResize, KeyConstants.kbCtrlF5, DemoHelp.hcWSizeMove, "Ctrl-F5"))
            .Add(new TMenuItem("~Z~oom", CommandConstants.cmZoom, KeyConstants.kbF5, DemoHelp.hcWZoom, "F5"))
            .Add(new TMenuItem("~N~ext", CommandConstants.cmNext, KeyConstants.kbF6, DemoHelp.hcWNext, "F6"))
            .Add(new TMenuItem("~C~lose", CommandConstants.cmClose, KeyConstants.kbAltF3, DemoHelp.hcWClose, "Alt-F3"))
            .Add(new TMenuItem("~T~ile", CommandConstants.cmTile, KeyConstants.kbNoKey, DemoHelp.hcWTile))
            .Add(new TMenuItem("C~a~scade", CommandConstants.cmCascade, KeyConstants.kbNoKey, DemoHelp.hcWCascade));

        var desktopSub = new TSubMenu("~D~esktop", 0)
            .Add(new TMenuItem("~S~ave desktop", TvCmds.cmSaveCmd, KeyConstants.kbNoKey, DemoHelp.hcOSaveDesktop))
            .Add(new TMenuItem("~R~etrieve desktop", TvCmds.cmRestoreCmd, KeyConstants.kbNoKey, DemoHelp.hcORestoreDesktop));

        var sub4 = new TSubMenu("~O~ptions", 0, DemoHelp.hcOptions)
            .Add(new TMenuItem("~M~ouse...", TvCmds.cmMouseCmd, KeyConstants.kbNoKey, DemoHelp.hcOMouse))
            .Add(new TMenuItem("~C~olors...", TvCmds.cmColorCmd, KeyConstants.kbNoKey, DemoHelp.hcOColors))
            .Add(new TMenuItem("~B~ackground...", TvCmds.cmChBackground, KeyConstants.kbNoKey))
            .Add(desktopSub);

        // Chain the submenus together
        sub1.Next = sub2;
        sub2.Next = sub3;
        sub3.Next = sub4;

        return new TMenuBar(r, new TMenu(sub1));
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case TvCmds.cmAboutCmd:
                    AboutDlgBox();
                    break;
                case TvCmds.cmCalendarCmd:
                    Calendar();
                    break;
                case TvCmds.cmAsciiCmd:
                    AsciiTable();
                    break;
                case TvCmds.cmCalcCmd:
                    Calculator();
                    break;
                case TvCmds.cmPuzzleCmd:
                    Puzzle();
                    break;
                case TvCmds.cmEventViewCmd:
                    EventViewer();
                    break;
                case TvCmds.cmChBackground:
                    ChBackground();
                    break;
                case TvCmds.cmOpenCmd:
                    OpenFile("*.*");
                    break;
                case TvCmds.cmChDirCmd:
                    ChangeDir();
                    break;
                case TvCmds.cmMouseCmd:
                    Mouse();
                    break;
                case TvCmds.cmColorCmd:
                    Colors();
                    break;
                case TvCmds.cmSaveCmd:
                    SaveDesktop();
                    break;
                case TvCmds.cmRestoreCmd:
                    RetrieveDesktop();
                    break;
                default:
                    return;
            }
            ClearEvent(ref ev);
        }
    }

    public override void Idle()
    {
        base.Idle();
        _clock?.Update();
        _heap?.Update();

        // Enable/disable tile and cascade based on tileable windows
        if (DeskTop != null && DeskTop.FirstThat((v, _) => (v.Options & OptionFlags.ofTileable) != 0, null) != null)
        {
            EnableCommand(CommandConstants.cmTile);
            EnableCommand(CommandConstants.cmCascade);
        }
        else
        {
            DisableCommand(CommandConstants.cmTile);
            DisableCommand(CommandConstants.cmCascade);
        }
    }

    private void AboutDlgBox()
    {
        var aboutBox = new TDialog(new TRect(0, 0, 39, 13), "About");

        aboutBox.Insert(new TStaticText(new TRect(9, 2, 30, 9),
            "\u0003Turbo Vision Demo\n\n" +
            "\u0003C# Version\n\n" +
            "\u0003Ported from\n\n" +
            "\u0003Borland International"));

        aboutBox.Insert(new TButton(new TRect(14, 10, 26, 12), " OK",
            CommandConstants.cmOK, CommandConstants.bfDefault));

        aboutBox.Options |= OptionFlags.ofCentered;

        ExecuteDialog(aboutBox);
    }

    private void Puzzle()
    {
        var puzz = (TPuzzleWindow?)ValidView(new TPuzzleWindow());
        if (puzz != null)
        {
            puzz.HelpCtx = DemoHelp.hcPuzzle;
            DeskTop?.Insert(puzz);
        }
    }

    private void Calendar()
    {
        var cal = (TCalendarWindow?)ValidView(new TCalendarWindow());
        if (cal != null)
        {
            cal.HelpCtx = DemoHelp.hcCalendar;
            DeskTop?.Insert(cal);
        }
    }

    private void AsciiTable()
    {
        var chart = (TAsciiChart?)ValidView(new TAsciiChart());
        if (chart != null)
        {
            chart.HelpCtx = DemoHelp.hcAsciiTable;
            DeskTop?.Insert(chart);
        }
    }

    private void Calculator()
    {
        var calc = (TCalculator?)ValidView(new TCalculator());
        if (calc != null)
        {
            calc.HelpCtx = DemoHelp.hcCalculator;
            DeskTop?.Insert(calc);
        }
    }

    private void EventViewer()
    {
        var viewer = (TEventViewer?)Message(DeskTop, EventConstants.evBroadcast,
            EventViewCommands.cmFndEventView, 0);
        if (viewer != null)
            viewer.Toggle();
        else if (DeskTop != null)
            DeskTop.Insert(new TEventViewer(DeskTop.GetExtent(), 0x0F00));
    }

    private void ChBackground()
    {
        var b = (TChBackground?)ValidView(new TChBackground(DeskTop?.Background));
        if (b != null)
        {
            DeskTop?.ExecView(b);
            b.Dispose();
        }
    }

    private void OpenFile(string fileSpec)
    {
        var d = (TFileDialog?)ValidView(new TFileDialog(fileSpec, "Open a File", "~N~ame",
            FileDialogFlags.fdOpenButton, 100));

        if (d != null && DeskTop?.ExecView(d) != CommandConstants.cmCancel)
        {
            string? fileName = d.GetFileName();
            if (fileName != null)
            {
                d.HelpCtx = DemoHelp.hcFOFileOpenDBox;
                var w = ValidView(new TFileWindow(fileName));
                if (w != null)
                    DeskTop?.Insert(w);
            }
        }
        d?.Dispose();
    }

    private void ChangeDir()
    {
        var d = ValidView(new TChDirDialog(0, 0));
        if (d != null)
        {
            d.HelpCtx = DemoHelp.hcFCChDirDBox;
            DeskTop?.ExecView(d);
            d.Dispose();
        }
    }

    private void Mouse()
    {
        var mouseCage = (TMouseDialog?)ValidView(new TMouseDialog());
        if (mouseCage != null)
        {
            ushort mouseReverse = TEventQueue.MouseReverse ? (ushort)1 : (ushort)0;
            mouseCage.HelpCtx = DemoHelp.hcOMMouseDBox;
            Span<byte> data = stackalloc byte[2];
            data[0] = (byte)(mouseReverse & 0xFF);
            data[1] = (byte)(mouseReverse >> 8);
            mouseCage.SetData(data);

            if (DeskTop?.ExecView(mouseCage) != CommandConstants.cmCancel)
            {
                mouseCage.GetData(data);
                mouseReverse = (ushort)(data[0] | (data[1] << 8));
            }
            TEventQueue.MouseReverse = mouseReverse != 0;
        }
        mouseCage?.Dispose();
    }

    private void Colors()
    {
        // Build color groups using the + operator for chaining
        var desktopGroup = new TColorGroup("Desktop") + new TColorItem("Color", 1);

        var menusGroup = new TColorGroup("Menus")
            + new TColorItem("Normal", 2)
            + new TColorItem("Disabled", 3)
            + new TColorItem("Shortcut", 4)
            + new TColorItem("Selected", 5)
            + new TColorItem("Selected disabled", 6)
            + new TColorItem("Shortcut selected", 7);

        var dialogsGroup = new TColorGroup("Dialogs/Calc")
            + new TColorItem("Frame/background", 33)
            + new TColorItem("Frame icons", 34)
            + new TColorItem("Scroll bar page", 35)
            + new TColorItem("Scroll bar icons", 36)
            + new TColorItem("Static text", 37)
            + new TColorItem("Label normal", 38)
            + new TColorItem("Label selected", 39)
            + new TColorItem("Label shortcut", 40)
            + new TColorItem("Button normal", 41)
            + new TColorItem("Button default", 42)
            + new TColorItem("Button selected", 43)
            + new TColorItem("Button disabled", 44)
            + new TColorItem("Button shortcut", 45)
            + new TColorItem("Button shadow", 46)
            + new TColorItem("Cluster normal", 47)
            + new TColorItem("Cluster selected", 48)
            + new TColorItem("Cluster shortcut", 49)
            + new TColorItem("Input normal", 50)
            + new TColorItem("Input selected", 51)
            + new TColorItem("Input arrow", 52)
            + new TColorItem("History button", 53)
            + new TColorItem("History sides", 54)
            + new TColorItem("History bar page", 55)
            + new TColorItem("History bar icons", 56)
            + new TColorItem("List normal", 57)
            + new TColorItem("List focused", 58)
            + new TColorItem("List selected", 59)
            + new TColorItem("List divider", 60)
            + new TColorItem("Information pane", 61);

        var viewerGroup = new TColorGroup("Viewer")
            + new TColorItem("Frame passive", 8)
            + new TColorItem("Frame active", 9)
            + new TColorItem("Frame icons", 10)
            + new TColorItem("Scroll bar page", 11)
            + new TColorItem("Scroll bar icons", 12)
            + new TColorItem("Text", 13);

        var puzzleGroup = new TColorGroup("Puzzle")
            + new TColorItem("Frame passive", 8)
            + new TColorItem("Frame active", 9)
            + new TColorItem("Frame icons", 10)
            + new TColorItem("Scroll bar page", 11)
            + new TColorItem("Scroll bar icons", 12)
            + new TColorItem("Normal text", 13)
            + new TColorItem("Highlighted text", 14);

        var calendarGroup = new TColorGroup("Calendar")
            + new TColorItem("Frame passive", 16)
            + new TColorItem("Frame active", 17)
            + new TColorItem("Frame icons", 18)
            + new TColorItem("Scroll bar page", 19)
            + new TColorItem("Scroll bar icons", 20)
            + new TColorItem("Normal text", 21)
            + new TColorItem("Current day", 22);

        var asciiGroup = new TColorGroup("Ascii table")
            + new TColorItem("Frame passive", 24)
            + new TColorItem("Frame active", 25)
            + new TColorItem("Frame icons", 26)
            + new TColorItem("Scroll bar page", 27)
            + new TColorItem("Scroll bar icons", 28)
            + new TColorItem("Text", 29);

        // Chain all groups together
        var group1 = desktopGroup + menusGroup + dialogsGroup + viewerGroup
            + puzzleGroup + calendarGroup + asciiGroup;

        var c = new TColorDialog(null, group1);
        if (ValidView(c) != null)
        {
            c.HelpCtx = DemoHelp.hcOCColorsDBox;
            var pal = GetPalette();
            if (pal != null)
                c.SetData(pal);
            if (DeskTop?.ExecView(c) != CommandConstants.cmCancel && c.Pal != null)
            {
                // Update application palette
                // SetScreenMode(TScreen.ScreenMode);
            }
            c.Dispose();
        }
    }

    private void SaveDesktop()
    {
        // Desktop persistence using JSON format
        // TODO: Implement JSON serialization for desktop views
        MessageBox(MessageBoxFlags.mfInformation | MessageBoxFlags.mfOKButton,
            "Desktop save not yet implemented.");
    }

    private void RetrieveDesktop()
    {
        // Desktop persistence using JSON format
        // TODO: Implement JSON deserialization for desktop views
        MessageBox(MessageBoxFlags.mfInformation | MessageBoxFlags.mfOKButton,
            "Desktop restore not yet implemented.");
    }

    public override void OutOfMemory()
    {
        MessageBox("Not enough memory available to complete operation.",
            MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton);
    }
}
