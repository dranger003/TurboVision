using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Views;

namespace TurboVision.Colors;

/// <summary>
/// A dialog for editing application color palettes.
/// </summary>
public class TColorDialog : TDialog
{
    private static TColorIndex? _colorIndexes;

    private readonly TColorDisplay _display;
    private readonly TColorGroupList _groups;
    private readonly TLabel _forLabel;
    private readonly TColorSelector _forSel;
    private readonly TLabel _bakLabel;
    private readonly TColorSelector _bakSel;
    private readonly TLabel _monoLabel;
    private readonly TMonoSelector _monoSel;
    private byte _groupIndex;

    /// <summary>
    /// The palette being edited (copy of application palette).
    /// </summary>
    public TPalette? Pal { get; private set; }

    public TColorDialog(TPalette? palette, TColorGroup groups)
        : base(new TRect(0, 0, 61, 18), ColorStrings.Colors)
    {
        Options |= OptionFlags.ofCentered;

        if (palette != null)
        {
            Pal = new TPalette(palette);
        }

        // Group list with scrollbar
        var sb = new TScrollBar(new TRect(18, 3, 19, 14));
        Insert(sb);

        _groups = new TColorGroupList(new TRect(3, 3, 18, 14), sb, groups);
        Insert(_groups);
        Insert(new TLabel(new TRect(2, 2, 8, 3), ColorStrings.GroupText, _groups));

        // Item list with scrollbar
        sb = new TScrollBar(new TRect(41, 3, 42, 14));
        Insert(sb);

        var itemList = new TColorItemList(new TRect(21, 3, 41, 14), sb, groups.Items);
        Insert(itemList);
        Insert(new TLabel(new TRect(20, 2, 25, 3), ColorStrings.ItemText, itemList));

        // Foreground selector
        _forSel = new TColorSelector(new TRect(45, 3, 57, 7), TColorSelector.ColorSel.csForeground);
        Insert(_forSel);
        _forLabel = new TLabel(new TRect(45, 2, 57, 3), ColorStrings.ForText, _forSel);
        Insert(_forLabel);

        // Background selector
        _bakSel = new TColorSelector(new TRect(45, 9, 57, 11), TColorSelector.ColorSel.csBackground);
        Insert(_bakSel);
        _bakLabel = new TLabel(new TRect(45, 8, 57, 9), ColorStrings.BakText, _bakSel);
        Insert(_bakLabel);

        // Color display preview
        _display = new TColorDisplay(new TRect(44, 12, 58, 14), ColorStrings.TextText);
        Insert(_display);

        // Monochrome selector (hidden by default)
        _monoSel = new TMonoSelector(new TRect(44, 3, 59, 7));
        _monoSel.Hide();
        Insert(_monoSel);
        _monoLabel = new TLabel(new TRect(43, 2, 49, 3), ColorStrings.ColorText, _monoSel);
        _monoLabel.Hide();
        Insert(_monoLabel);

        // OK and Cancel buttons
        Insert(new TButton(new TRect(36, 15, 46, 17), ColorStrings.OkText, CommandConstants.cmOK, CommandConstants.bfDefault));
        Insert(new TButton(new TRect(48, 15, 58, 17), ColorStrings.CancelText, CommandConstants.cmCancel, CommandConstants.bfNormal));

        SelectNext(false);

        if (Pal != null)
        {
            SetData(Pal);
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == ColorCommands.cmNewColorItem)
        {
            _groupIndex = (byte)_groups.Focused;
        }

        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == ColorCommands.cmNewColorIndex)
        {
            int infoInt = ev.Message.InfoInt;
            if (Pal != null && infoInt >= 0 && infoInt < Pal.Data.Length)
            {
                _display.SetColor(Pal.Data[infoInt]);
            }
        }
    }

    public override int DataSize()
    {
        return Pal?.Data.Length ?? 0;
    }

    public override void GetData(Span<byte> rec)
    {
        GetIndexes(ref _colorIndexes);
        if (Pal != null && rec.Length >= Pal.Data.Length)
        {
            var data = Pal.Data;
            for (int i = 0; i < data.Length && i < rec.Length; i++)
            {
                rec[i] = data[i]; // implicit conversion TColorAttr -> byte
            }
        }
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (Pal == null)
        {
            Pal = new TPalette(rec);
        }
        else if (rec.Length > 0)
        {
            // Update existing palette entries
            for (int i = 0; i < rec.Length && i < Pal.Length; i++)
            {
                Pal[i] = rec[i]; // implicit conversion byte -> TColorAttr
            }
        }

        SetIndexes(ref _colorIndexes);

        byte index = _groups.GetGroupIndex(_groupIndex);
        if (Pal != null && index < Pal.Data.Length)
        {
            _display.SetColor(Pal.Data[index]);
        }
        _groups.FocusItem(_groupIndex);

        if (TView.ShowMarkers)
        {
            _forLabel.Hide();
            _forSel.Hide();
            _bakLabel.Hide();
            _bakSel.Hide();
            _monoLabel.Show();
            _monoSel.Show();
        }

        _groups.Select();
    }

    /// <summary>
    /// Sets data from a palette object.
    /// </summary>
    public void SetData(TPalette palette)
    {
        // Skip the length prefix at index 0 since SetData creates a new TPalette that adds its own.
        // Palette.Data[0] is length, Data[1..ColorCount] are the actual color values.
        var data = palette.Data;
        int colorCount = palette.ColorCount;
        Span<byte> bytes = stackalloc byte[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            bytes[i] = data[i + 1]; // Start from index 1 (skip length prefix)
        }
        SetData((ReadOnlySpan<byte>)bytes);
    }

    private void SetIndexes(ref TColorIndex? colorIdx)
    {
        byte numGroups = _groups.GetNumGroups();

        if (colorIdx != null && colorIdx.ColorSize != numGroups)
        {
            colorIdx = null;
        }

        if (colorIdx == null)
        {
            colorIdx = new TColorIndex
            {
                GroupIndex = 0,
                ColorSize = numGroups,
                ColorIndices = new byte[256]
            };
        }

        for (byte index = 0; index < numGroups; index++)
        {
            _groups.SetGroupIndex(index, colorIdx.ColorIndices[index]);
        }

        _groupIndex = colorIdx.GroupIndex;
    }

    private void GetIndexes(ref TColorIndex? colorIdx)
    {
        byte numGroups = _groups.GetNumGroups();

        if (colorIdx == null)
        {
            colorIdx = new TColorIndex
            {
                ColorSize = numGroups,
                ColorIndices = new byte[256]
            };
        }

        colorIdx.GroupIndex = _groupIndex;
        for (byte index = 0; index < numGroups; index++)
        {
            colorIdx.ColorIndices[index] = _groups.GetGroupIndex(index);
        }
    }
}
