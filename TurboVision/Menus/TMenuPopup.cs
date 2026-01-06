using System.Text.Json.Serialization;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Context/popup menu.
/// </summary>
public class TMenuPopup : TMenuBox
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TMenuPopup";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    // Control character mapping (Ctrl+A through Ctrl+Z to A-Z)
    private static readonly string CtrlCodes = "\0ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public TMenuPopup(TRect bounds, TMenu? menu, TMenuView? parent = null)
        : base(bounds, menu, parent)
    {
        // Popup menus should not re-queue click events on exit
        PutClickEventOnExit = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TMenuPopup owns its menu - set to null so GC can collect
            Menu = null;
        }
        base.Dispose(disposing);
    }

    public override ushort Execute()
    {
        // Do not highlight the default entry (would look ugly in popup)
        if (Menu != null)
        {
            Menu.Default = null;
        }
        return base.Execute();
    }

    public override void HandleEvent(ref TEvent ev)
    {
        // Popup-specific keyboard handling comes BEFORE base handling
        if (ev.What == EventConstants.evKeyDown)
        {
            // Try to find menu item by control character (Ctrl+A through Ctrl+Z)
            char ctrlChar = GetCtrlChar(ev.KeyDown.KeyCode);
            TMenuItem? p = null;

            if (ctrlChar != '\0')
            {
                p = FindItem(ctrlChar);
            }

            // Fall back to hotkey lookup
            if (p == null)
            {
                p = HotKey(ev.KeyDown.ToKey());
            }

            if (p != null && CommandEnabled(p.Command))
            {
                // Generate evCommand event with the selected command
                ev.What = EventConstants.evCommand;
                ev.Message.Command = p.Command;
                ev.Message.InfoPtr = 0;
                PutEvent(ev);
                ClearEvent(ref ev);
            }
            else
            {
                // Clear Alt+character events to prevent them from bubbling
                // (popup menus don't use Alt+letter shortcuts like menu bars)
                char altChar = GetAltChar(ev.KeyDown.KeyCode);
                if (altChar != '\0')
                {
                    ClearEvent(ref ev);
                }
            }
        }

        base.HandleEvent(ref ev);
    }

    /// <summary>
    /// Gets the character for a Ctrl+key press.
    /// Port of getCtrlChar from tvtext2.cpp.
    /// </summary>
    private static char GetCtrlChar(ushort keyCode)
    {
        byte charCode = (byte)(keyCode & 0xFF);

        // Map control characters (ASCII 1-26) to their corresponding letters (A-Z)
        if (charCode > 0 && charCode <= 26)
        {
            return CtrlCodes[charCode];
        }

        return '\0';
    }

    // Alt key scan code to character mapping
    private static readonly string AltCodes1 = "QWERTYUIOP\0\0\0\0ASDFGHJKL\0\0\0\0\0ZXCVBNM";
    private static readonly string AltCodes2 = "1234567890-=";

    /// <summary>
    /// Gets the character for an Alt+key combination from the key code.
    /// </summary>
    private static char GetAltChar(ushort keyCode)
    {
        byte scanCode = (byte)(keyCode >> 8);
        byte charCode = (byte)(keyCode & 0xFF);

        // Alt keys have charCode == 0 and specific scan codes
        if (charCode != 0)
        {
            return '\0';
        }

        // Alt+Q through Alt+M (scan codes 0x10-0x32)
        if (scanCode >= 0x10 && scanCode < 0x10 + AltCodes1.Length)
        {
            return AltCodes1[scanCode - 0x10];
        }

        // Alt+1 through Alt+= (scan codes 0x78-0x83)
        if (scanCode >= 0x78 && scanCode < 0x78 + AltCodes2.Length)
        {
            return AltCodes2[scanCode - 0x78];
        }

        return '\0';
    }

    /// <summary>
    /// Spawns and executes a TMenuPopup on the application desktop.
    /// </summary>
    /// <param name="where">Reference position in absolute coordinates. The top left corner
    /// of the popup will be placed at (where.X, where.Y + 1).</param>
    /// <param name="menuItems">Chain of menu items. This function takes ownership
    /// of the items.</param>
    /// <param name="receiver">If not null, an evCommand event is generated with
    /// the selected command and put into it with PutEvent.</param>
    /// <returns>The selected command, or 0 if cancelled.</returns>
    public static ushort PopupMenu(TPoint where, TMenuItem menuItems, TGroup? receiver = null)
    {
        ushort result = 0;
        var app = TProgram.Application;

        if (app != null)
        {
            var p = app.MakeLocal(where);
            var bounds = new TRect(p, p);
            var menu = new TMenu(menuItems);
            var menuPopup = new TMenuPopup(bounds, menu);

            AutoPlacePopup(menuPopup, p);

            // Execute and dispose the menu
            result = app.ExecView(menuPopup);
            menuPopup.Dispose();

            // Generate an event if requested
            if (result != 0 && receiver != null)
            {
                var ev = new TEvent
                {
                    What = EventConstants.evCommand
                };
                ev.Message.Command = result;
                receiver.PutEvent(ev);
            }
        }

        return result;
    }

    /// <summary>
    /// Automatically places a popup menu to ensure it's fully visible.
    /// Pre: TMenuPopup was constructed with bounds = TRect(p, p).
    /// </summary>
    private static void AutoPlacePopup(TMenuPopup m, TPoint p)
    {
        var app = TProgram.Application;
        if (app == null)
            return;

        // Initially, the menu is placed above 'p'. So we need to move it.
        var r = m.GetBounds();

        // Ensure the popup does not go beyond the desktop's bottom-right corner
        var d = app.Size - p;
        int moveX = Math.Min(m.Size.X, d.X);
        int moveY = Math.Min(m.Size.Y + 1, d.Y);
        r.Move(moveX, moveY);

        // If the popup then contains 'p', try to move it to a better place
        if (r.Contains(p) && (r.B.Y - r.A.Y) < p.Y)
        {
            r.Move(0, -(r.B.Y - p.Y));
        }

        m.SetBounds(r);
    }
}
