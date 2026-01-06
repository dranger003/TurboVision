using System.Text.Json.Serialization;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// Window container for the file editor.
/// Provides a complete editing interface with scrollbars, indicator, and frame.
/// </summary>
public class TEditWindow : TWindow
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TEditWindow";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly TPoint MinEditWinSize = new(24, 6);

    private const string ClipboardTitle = "Clipboard";
    private const string UntitledText = "Untitled";

    /// <summary>
    /// The file editor contained within this window.
    /// Runtime reference to child view, ignored for serialization.
    /// </summary>
    [JsonIgnore]
    public TFileEditor? Editor { get; private set; }

    public TEditWindow(TRect bounds, string? fileName, int number)
        : base(bounds, null, (short)number)
    {
        Options |= OptionFlags.ofTileable;

        // Create horizontal scrollbar
        var hScrollBar = new TScrollBar(new TRect(18, Size.Y - 1, Size.X - 2, Size.Y));
        hScrollBar.Hide();
        Insert(hScrollBar);

        // Create vertical scrollbar
        var vScrollBar = new TScrollBar(new TRect(Size.X - 1, 1, Size.X, Size.Y - 1));
        vScrollBar.Hide();
        Insert(vScrollBar);

        // Create indicator
        var indicator = new TIndicator(new TRect(2, Size.Y - 1, 16, Size.Y));
        indicator.Hide();
        Insert(indicator);

        // Create editor
        var r = GetExtent();
        r.Grow(-1, -1);
        Editor = new TFileEditor(r, hScrollBar, vScrollBar, indicator, fileName);
        Insert(Editor);
    }

    public override void Close()
    {
        if (Editor?.IsClipboard() == true)
        {
            Hide();
        }
        else
        {
            base.Close();
        }
    }

    public override string? GetTitle(short maxSize)
    {
        if (Editor == null)
            return null;

        if (Editor.IsClipboard())
            return ClipboardTitle;

        if (string.IsNullOrEmpty(Editor.FileName))
            return UntitledText;

        return Editor.FileName;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == EditorCommands.cmUpdateTitle)
        {
            Frame?.DrawView();
            ClearEvent(ref ev);
        }
    }

    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out _, out max);
        min = MinEditWinSize;
    }
}
