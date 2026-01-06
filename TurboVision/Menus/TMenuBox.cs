using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Dropdown/popup menu box.
/// </summary>
public class TMenuBox : TMenuView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TMenuBox";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    // Frame characters: " ┌─┐  └─┘  │ │  ├─┤ "
    // Index 0-4: top frame, 5-9: bottom frame, 10-14: normal line, 15-19: separator
    public static string FrameChars { get; set; } = " ┌─┐  └─┘  │ │  ├─┤ ";

    public TMenuBox(TRect bounds, TMenu? menu, TMenuView? parent) : base(GetRect(bounds, menu), menu, parent)
    {
        State |= StateFlags.sfShadow;
        Options |= OptionFlags.ofPreProcess;
    }

    /// <summary>
    /// Calculate the proper bounds for a menu box based on its menu items.
    /// </summary>
    private static TRect GetRect(TRect bounds, TMenu? menu)
    {
        int w = 10;  // Minimum width
        int h = 2;   // Top and bottom frame lines

        if (menu != null)
        {
            for (var p = menu.Items; p != null; p = p.Next)
            {
                if (p.Name != null)
                {
                    // Base width: name length + 6 (3 padding on each side)
                    int l = CStrLen(p.Name) + 6;

                    if (p.Command == 0)
                    {
                        // Submenu indicator "►" needs 3 extra chars
                        l += 3;
                    }
                    else if (p.Param != null)
                    {
                        // Param string (e.g., "Alt-X") needs length + 2
                        l += CStrLen(p.Param) + 2;
                    }

                    w = Math.Max(l, w);
                }
                h++;
            }
        }

        int ax = bounds.A.X;
        int ay = bounds.A.Y;
        int bx = bounds.B.X;
        int by = bounds.B.Y;

        // Fit horizontally: prefer anchoring from left, otherwise from right
        if (ax + w < bx)
            bx = ax + w;
        else
            ax = bx - w;

        // Fit vertically: prefer anchoring from top, otherwise from bottom
        if (ay + h < by)
            by = ay + h;
        else
            ay = by - h;

        return new TRect(ax, ay, bx, by);
    }

    /// <summary>
    /// Calculate displayed length of a control string, ignoring '~' characters.
    /// </summary>
    private static int CStrLen(string text)
    {
        int len = 0;
        foreach (char c in text)
        {
            if (c != '~')
                len++;
        }
        return len;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var cNormal = GetColor(0x0301);
        var cDisabled = GetColor(0x0202);
        var cSelect = GetColor(0x0604);
        var cSelectDisabled = GetColor(0x0505);

        // Draw top frame
        FrameLine(b, 0, cNormal, cNormal);
        WriteBuf(0, 0, Size.X, 1, b);

        // Draw items
        int y = 1;
        if (Menu != null)
        {
            var p = Menu.Items;
            while (p != null && y < Size.Y - 1)
            {
                if (p.IsSeparator)
                {
                    // Separator line uses frame index 15
                    FrameLine(b, 15, cNormal, cNormal);
                }
                else
                {
                    var color = p.Disabled ? cDisabled : cNormal;
                    if (p == Current)
                    {
                        color = p.Disabled ? cSelectDisabled : cSelect;
                    }

                    // Normal item uses frame index 10
                    // Frame edges use cNormal, content area uses color (may be selection)
                    FrameLine(b, 10, cNormal, color);
                    if (p.Name != null)
                    {
                        b.MoveCStr(3, p.Name, color);
                    }

                    // Show submenu indicator or param string
                    if (p.Command == 0 && p.SubMenu != null)
                    {
                        b.PutChar(Size.X - 4, '►');
                    }
                    else if (p.Param != null)
                    {
                        int paramX = Size.X - 3 - CStrLen(p.Param);
                        b.MoveCStr(paramX, p.Param, color);
                    }
                }

                WriteBuf(0, y, Size.X, 1, b);
                y++;
                p = p.Next;
            }
        }

        // Draw bottom frame
        FrameLine(b, 5, cNormal, cNormal);
        WriteBuf(0, Size.Y - 1, Size.X, 1, b);
    }

    private void FrameLine(TDrawBuffer buf, int n, TAttrPair frameColor, TAttrPair contentColor)
    {
        // Left edge: copy 2 chars from FrameChars[n] (e.g., " ┌" for top frame) - always frame color
        buf.MoveBuf(0, FrameChars.AsSpan(n), frameColor.Normal, 2);
        // Middle: fill with content color (may be selection highlight)
        buf.MoveChar(2, FrameChars[n + 2], contentColor.Normal, Size.X - 4);
        // Right edge: copy 2 chars from FrameChars[n+3] (e.g., "┐ " for top frame) - always frame color
        buf.MoveBuf(Size.X - 2, FrameChars.AsSpan(n + 3), frameColor.Normal, 2);
    }

    public override TRect GetItemRect(TMenuItem? item)
    {
        if (item == null || Menu == null)
        {
            return new TRect();
        }

        int y = 1;
        var p = Menu.Items;

        while (p != null && p != item)
        {
            y++;
            p = p.Next;
        }

        if (p != null)
        {
            return new TRect(1, y, Size.X - 1, y + 1);
        }

        return new TRect();
    }
}
