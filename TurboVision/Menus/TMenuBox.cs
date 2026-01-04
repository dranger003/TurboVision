using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Dropdown/popup menu box.
/// </summary>
public class TMenuBox : TMenuView
{
    // Frame characters: " ┌─┐  └─┘  │ │  ├─┤ "
    // Index 0-4: top frame, 5-9: bottom frame, 10-14: normal line, 15-19: separator
    public static string FrameChars { get; set; } = " ┌─┐  └─┘  │ │  ├─┤ ";

    public TMenuBox(TRect bounds, TMenu? menu, TMenuView? parent) : base(bounds, menu, parent)
    {
        State |= StateFlags.sfShadow;
        Options |= OptionFlags.ofPreProcess;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var cNormal = GetColor(0x0301);
        var cDisabled = GetColor(0x0202);
        var cSelect = GetColor(0x0604);
        var cSelectDisabled = GetColor(0x0505);

        // Draw top frame
        FrameLine(b, 0, cNormal);
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
                    FrameLine(b, 15, cNormal);
                }
                else
                {
                    var color = p.Disabled ? cDisabled : cNormal;
                    if (p == Current)
                    {
                        color = p.Disabled ? cSelectDisabled : cSelect;
                    }

                    // Normal item uses frame index 10
                    FrameLine(b, 10, color);
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
                        int paramX = Size.X - 3 - p.Param.Length;
                        b.MoveCStr(paramX, p.Param, color);
                    }
                }

                WriteBuf(0, y, Size.X, 1, b);
                y++;
                p = p.Next;
            }
        }

        // Draw bottom frame
        FrameLine(b, 5, cNormal);
        WriteBuf(0, Size.Y - 1, Size.X, 1, b);
    }

    private void FrameLine(TDrawBuffer buf, int n, TAttrPair color)
    {
        buf.MoveChar(0, FrameChars[n], color.Normal, 2);
        buf.MoveChar(2, FrameChars[n + 2], color.Normal, Size.X - 4);
        buf.MoveChar(Size.X - 2, FrameChars[n + 3], color.Normal, 2);
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
