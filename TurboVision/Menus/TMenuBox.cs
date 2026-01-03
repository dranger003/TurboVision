using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Dropdown/popup menu box.
/// </summary>
public class TMenuBox : TMenuView
{
    private static readonly string FrameChars = "┌─┐│ │└─┘";

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

        // Draw frame
        b.MoveChar(0, FrameChars[0], cNormal.Normal, 1);
        b.MoveChar(1, FrameChars[1], cNormal.Normal, Size.X - 2);
        b.MoveChar(Size.X - 1, FrameChars[2], cNormal.Normal, 1);
        WriteBuf(0, 0, Size.X, 1, b);

        // Draw items
        int y = 1;
        if (Menu != null)
        {
            var p = Menu.Items;
            while (p != null && y < Size.Y - 1)
            {
                b.MoveChar(0, FrameChars[3], cNormal.Normal, 1);

                if (p.IsSeparator)
                {
                    b.MoveChar(1, '─', cNormal.Normal, Size.X - 2);
                }
                else
                {
                    var color = p.Disabled ? cDisabled : cNormal;
                    if (p == Current)
                    {
                        color = p.Disabled ? cSelectDisabled : cSelect;
                    }

                    b.MoveChar(1, ' ', color.Normal, Size.X - 2);

                    if (p.Name != null)
                    {
                        b.MoveCStr(2, p.Name, new TAttrPair(color.Normal, color.Highlight));
                    }
                }

                b.MoveChar(Size.X - 1, FrameChars[5], cNormal.Normal, 1);
                WriteBuf(0, y, Size.X, 1, b);
                y++;
                p = p.Next;
            }
        }

        // Draw bottom frame
        b.MoveChar(0, FrameChars[6], cNormal.Normal, 1);
        b.MoveChar(1, FrameChars[7], cNormal.Normal, Size.X - 2);
        b.MoveChar(Size.X - 1, FrameChars[8], cNormal.Normal, 1);
        WriteBuf(0, Size.Y - 1, Size.X, 1, b);
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

    private void FrameLine(TDrawBuffer buf, int n)
    {
        // TODO: Draw frame line
    }

    private void DrawLine(TDrawBuffer buf)
    {
        // TODO: Draw separator line
    }
}
