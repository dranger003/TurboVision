using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Menus;

/// <summary>
/// Horizontal menu bar at top of screen.
/// </summary>
public class TMenuBar : TMenuView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TMenuBar";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    public TMenuBar(TRect bounds, TMenu? menu) : base(bounds, menu)
    {
        GrowMode = GrowFlags.gfGrowHiX;
        Options |= OptionFlags.ofPreProcess;
    }

    public TMenuBar(TRect bounds, TSubMenu menu) : base(bounds, new TMenu(menu))
    {
        GrowMode = GrowFlags.gfGrowHiX;
        Options |= OptionFlags.ofPreProcess;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var cNormal = GetColor(0x0301);
        var cDisabled = GetColor(0x0202);
        var cSelect = GetColor(0x0604);
        var cSelectDisabled = GetColor(0x0505);

        b.MoveChar(0, ' ', cNormal.Normal, Size.X);

        if (Menu != null)
        {
            int x = 1;
            var p = Menu.Items;

            while (p != null && x < Size.X)
            {
                if (p.Name != null)
                {
                    var color = p.Disabled ? cDisabled : cNormal;
                    if (p == Current)
                    {
                        color = p.Disabled ? cSelectDisabled : cSelect;
                    }

                    b.MoveChar(x, ' ', color.Normal, 1);
                    x++;
                    x += b.MoveCStr(x, p.Name, new TAttrPair(color.Normal, color.Highlight));
                    b.MoveChar(x, ' ', color.Normal, 1);
                    x++;
                }
                p = p.Next;
            }
        }

        WriteBuf(0, 0, Size.X, Size.Y, b);
    }

    public override TRect GetItemRect(TMenuItem? item)
    {
        if (item == null || Menu == null)
        {
            return new TRect();
        }

        int x = 1;
        var p = Menu.Items;

        while (p != null && p != item)
        {
            if (p.Name != null)
            {
                x += p.Name.Length - p.Name.Count(c => c == '~') + 2;
            }
            p = p.Next;
        }

        if (p != null && p.Name != null)
        {
            int width = p.Name.Length - p.Name.Count(c => c == '~') + 2;
            return new TRect(x, 0, x + width, 1);
        }

        return new TRect();
    }
}
