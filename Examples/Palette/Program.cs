using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Menus;
using TurboVision.Views;

namespace Palette;

// Command constants for this application
public static class Commands
{
    public const ushort cmAbout = 100;
    public const ushort cmPaletteView = 101;
}

/// <summary>
/// TTestView - View that displays text in colors from its palette.
/// Demonstrates how a view's palette maps to its owner's palette.
/// </summary>
public class TTestView(TRect r) : TView(r)
{
    // Six palette entries that reference indices 9-14 in the owner (window) palette
    private static readonly byte[] TestViewPalette = [0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E];

    public override void Draw()
    {
        var buf = new TDrawBuffer();

        // Loop through palette entries (6 entries, indices 1-6)
        for (int i = 1; i <= 6; i++)
        {
            // GetColor returns a pair, but for single color we use MapColor
            var textAttr = MapColor((byte)i);

            // Format the display text showing the palette index and resolved color
            string text = $" This line uses index {i:X2}, color is {(byte)textAttr:X2} ";
            buf.MoveStr(0, text, textAttr);
            WriteLine(0, i - 1, Size.X, 1, buf);
        }

        // The last line bypasses the palette system entirely,
        // using a hardcoded attribute (Purple on Black = 0x05)
        buf.MoveStr(0, "   This line bypasses the palettes!    ", new TColorAttr(0x05));
        WriteLine(0, 6, Size.X, 1, buf);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(TestViewPalette);
    }
}

/// <summary>
/// TTestWindow - Window with an extended palette for the test view.
/// Demonstrates how window palettes chain to the application palette.
/// </summary>
public class TTestWindow : TWindow
{
    public const int TestWidth = 42;
    public const int TestHeight = 11;

    // Extended window palettes that include 6 additional entries
    // The base 8 entries are the standard window palette, followed by 6 custom entries
    // The custom entries (0x88-0x8D) reference indices 136-141 in the app palette
    private static readonly byte[] BluePalette =
        [0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D];
    private static readonly byte[] CyanPalette =
        [0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D];
    private static readonly byte[] GrayPalette =
        [0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D];

    public TTestWindow() : base(new TRect(0, 0, TestWidth, TestHeight), null, WindowConstants.wnNoNumber)
    {
        var r = GetExtent();
        r.Grow(-2, -2);
        Insert(new TTestView(r));
        Options |= OptionFlags.ofCentered;
        Flags = WindowFlags.wfMove | WindowFlags.wfClose;
    }

    public override TPalette? GetPalette()
    {
        // Return the appropriate palette based on the current window palette setting
        // 'Palette' is inherited from TWindow and indicates blue/cyan/gray
        return Palette switch
        {
            WindowPalettes.wpCyanWindow => new TPalette(CyanPalette),
            WindowPalettes.wpGrayWindow => new TPalette(GrayPalette),
            _ => new TPalette(BluePalette)
        };
    }

    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        // Fixed size window - cannot be resized
        min = new TPoint(TestWidth, TestHeight);
        max = new TPoint(TestWidth, TestHeight);
    }
}

/// <summary>
/// TTestApp - Application with extended palette for custom colors.
/// Demonstrates how to extend the system palette with custom entries.
/// </summary>
public class TTestApp : TApplication
{
    // Extended application palettes for each display type
    // These append 6 custom color entries (indices 136-141) to the base 135-entry palette
    // Color: Yellow/Cyan, Magenta/Cyan, White/Cyan, Cyan/Magenta, Cyan/Brown, Yellow/Magenta
    private static readonly byte[] TestAppColor = [0x3E, 0x2D, 0x72, 0x5F, 0x68, 0x4E];
    // Black & White alternates
    private static readonly byte[] TestAppBW = [0x07, 0x07, 0x0F, 0x70, 0x78, 0x7F];
    // Monochrome alternates
    private static readonly byte[] TestAppMono = [0x07, 0x0F, 0x70, 0x09, 0x0F, 0x79];

    // Base application palettes from TProgram
    private static readonly byte[] AppColorPalette =
    [
        0x71, 0x70, 0x78, 0x74, 0x20, 0x28, 0x24, 0x17, 0x1F, 0x1A, 0x31, 0x31, 0x1E, 0x71, 0x1F, // 1-15
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x3E, 0x21, 0x3F, 0x70, 0x7F, 0x7A, 0x13, 0x13, 0x70, 0x7F, 0x7E, // 16-31
        0x70, 0x7F, 0x7A, 0x13, 0x13, 0x70, 0x70, 0x7F, 0x7E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x70, 0x30, // 32-47
        0x3F, 0x3E, 0x1F, 0x2F, 0x1A, 0x20, 0x72, 0x31, 0x31, 0x30, 0x2F, 0x3E, 0x31, 0x13, 0x38, 0x00, // 48-63
        0x17, 0x1F, 0x1A, 0x71, 0x71, 0x1E, 0x17, 0x1F, 0x1E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x10, 0x30, // 64-79
        0x3F, 0x3E, 0x70, 0x2F, 0x7A, 0x20, 0x12, 0x31, 0x31, 0x30, 0x2F, 0x3E, 0x31, 0x13, 0x38, 0x00, // 80-95
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x3E, 0x30, 0x3F, 0x3E, 0x20, 0x2B, 0x2F, 0x78, 0x2E, 0x30, 0x70, // 96-111
        0x7F, 0x7E, 0x1F, 0x2F, 0x1A, 0x20, 0x32, 0x31, 0x71, 0x70, 0x2F, 0x7E, 0x71, 0x13, 0x78, 0x00, // 112-127
        0x37, 0x3F, 0x3A, 0x13, 0x13, 0x30, 0x3E, 0x1E // 128-135 (help colors)
    ];

    private static readonly byte[] AppBlackWhitePalette =
    [
        0x70, 0x70, 0x78, 0x7F, 0x07, 0x07, 0x0F, 0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x0F,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x07, 0x0F,
        0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x70, 0x7F, 0x7F, 0x07, 0x0F, 0x0F, 0x78, 0x0F, 0x78, 0x07,
        0x0F, 0x0F, 0x0F, 0x70, 0x0F, 0x07, 0x70, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x08, 0x00,
        0x07, 0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x0F, 0x0F, 0x70, 0x78, 0x7F, 0x08, 0x7F, 0x08, 0x70,
        0x7F, 0x7F, 0x7F, 0x0F, 0x70, 0x70, 0x07, 0x70, 0x70, 0x70, 0x07, 0x7F, 0x70, 0x07, 0x78, 0x00,
        0x70, 0x7F, 0x7F, 0x70, 0x07, 0x70, 0x70, 0x7F, 0x7F, 0x07, 0x0F, 0x0F, 0x78, 0x0F, 0x78, 0x07,
        0x0F, 0x0F, 0x0F, 0x70, 0x0F, 0x07, 0x70, 0x70, 0x70, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x08, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x0F, 0x70
    ];

    private static readonly byte[] AppMonochromePalette =
    [
        0x70, 0x07, 0x07, 0x0F, 0x70, 0x70, 0x70, 0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x70, 0x00, 0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x07, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x70, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x01, 0x00,
        0x70, 0x70, 0x70, 0x07, 0x07, 0x70, 0x70, 0x70, 0x0F, 0x07, 0x07, 0x0F, 0x70, 0x0F, 0x70, 0x07,
        0x0F, 0x0F, 0x07, 0x70, 0x07, 0x07, 0x70, 0x07, 0x07, 0x07, 0x70, 0x0F, 0x07, 0x07, 0x01, 0x00,
        0x07, 0x0F, 0x07, 0x70, 0x70, 0x07, 0x0F, 0x70
    ];

    // Cached extended palettes
    private TPalette? _colorPalette;
    private TPalette? _blackWhitePalette;
    private TPalette? _monochromePalette;

    public override TStatusLine? InitStatusLine(TRect r)
    {
        var statusRect = new TRect(r.A.X, r.B.Y - 1, r.B.X, r.B.Y);
        return new TStatusLine(statusRect,
            new TStatusDef(0, 0xFFFF,
                new TStatusItem("~Alt-X~ Exit", KeyConstants.kbAltX, CommandConstants.cmQuit,
                new TStatusItem(null, KeyConstants.kbF10, CommandConstants.cmMenu))));
    }

    public override TMenuBar? InitMenuBar(TRect r)
    {
        var menuRect = new TRect(r.A.X, r.A.Y, r.B.X, r.A.Y + 1);

        // Build menu: About -> Palette -> Exit
        var exitItem = new TMenuItem("E~x~it", CommandConstants.cmQuit, KeyConstants.kbAltX,
            HelpContexts.hcNoContext, "Alt-X");
        var paletteItem = new TMenuItem("~P~alette", Commands.cmPaletteView, KeyConstants.kbAltP,
            HelpContexts.hcNoContext, null, exitItem);
        var aboutItem = new TMenuItem("~A~bout...", Commands.cmAbout, KeyConstants.kbAltA,
            HelpContexts.hcNoContext, null, paletteItem);

        return new TMenuBar(menuRect, new TMenu(aboutItem));
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case Commands.cmAbout:
                    AboutDlg();
                    ClearEvent(ref ev);
                    break;
                case Commands.cmPaletteView:
                    PaletteView();
                    ClearEvent(ref ev);
                    break;
            }
        }
    }

    public override TPalette? GetPalette()
    {
        // Return the extended palette based on current display mode
        return AppPalette switch
        {
            apBlackWhite => _blackWhitePalette ??= CreateExtendedPalette(AppBlackWhitePalette, TestAppBW),
            apMonochrome => _monochromePalette ??= CreateExtendedPalette(AppMonochromePalette, TestAppMono),
            _ => _colorPalette ??= CreateExtendedPalette(AppColorPalette, TestAppColor)
        };
    }

    /// <summary>
    /// Creates an extended palette by concatenating the base palette with custom entries.
    /// </summary>
    private static TPalette CreateExtendedPalette(byte[] basePalette, byte[] extension)
    {
        var combined = new byte[basePalette.Length + extension.Length];
        Array.Copy(basePalette, combined, basePalette.Length);
        Array.Copy(extension, 0, combined, basePalette.Length, extension.Length);
        return new TPalette(combined);
    }

    /// <summary>
    /// Creates and shows the About dialog box.
    /// </summary>
    private void AboutDlg()
    {
        var aboutDlgBox = new TDialog(new TRect(0, 0, 47, 13), "About");
        if (ValidView(aboutDlgBox) != null)
        {
            aboutDlgBox.Insert(
                new TStaticText(
                    new TRect(2, 1, 45, 9),
                    "\n\u0003PALETTE EXAMPLE\n \n" +
                    "\u0003A Turbo Vision Demo\n \n" +
                    "\u0003written by\n \n" +
                    "\u0003Borland C++ Tech Support\n"
                ));
            aboutDlgBox.Insert(
                new TButton(new TRect(18, 10, 29, 12), "OK", CommandConstants.cmOK,
                    CommandConstants.bfDefault)
                );
            aboutDlgBox.Options |= OptionFlags.ofCentered;
            DeskTop?.ExecView(aboutDlgBox);
            aboutDlgBox.Dispose();
        }
    }

    /// <summary>
    /// Creates and inserts the palette test window.
    /// </summary>
    private void PaletteView()
    {
        var view = new TTestWindow();
        if (ValidView(view) != null)
        {
            DeskTop?.Insert(view);
        }
    }
}

public static class Program
{
    public static void Main()
    {
        using var app = new TTestApp();
        app.Run();
    }
}
