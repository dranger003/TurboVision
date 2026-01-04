using TurboVision.Core;

namespace TurboVision.Menus;

/// <summary>
/// Context/popup menu.
/// </summary>
public class TMenuPopup : TMenuBox
{
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
}
