using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// Displays the current line and column position in the editor.
/// Shows modified indicator when the editor content has changed.
/// </summary>
public class TIndicator : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TIndicator";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x02, 0x03];

    // Frame characters for drag state
    private const char DragFrame = '\u2593';   // Dark shade character
    private const char NormalFrame = '\u2591'; // Light shade character

    /// <summary>
    /// Current cursor location (0-based line and column).
    /// Runtime state, ignored for serialization.
    /// </summary>
    [JsonIgnore]
    public TPoint Location { get; protected set; }

    /// <summary>
    /// Whether the associated editor content has been modified.
    /// Runtime state, ignored for serialization.
    /// </summary>
    [JsonIgnore]
    public bool Modified { get; protected set; }

    public TIndicator(TRect bounds) : base(bounds)
    {
        GrowMode = GrowFlags.gfGrowLoY | GrowFlags.gfGrowHiY;
    }

    public override void Draw()
    {
        TColorAttr color;
        char frame;
        var buf = new TDrawBuffer();

        if (!GetState(StateFlags.sfDragging))
        {
            color = MapColor(1);
            frame = DragFrame;
        }
        else
        {
            color = MapColor(2);
            frame = NormalFrame;
        }

        buf.MoveChar(0, frame, color, Size.X);

        // Show modified indicator (diamond character)
        if (Modified)
        {
            buf.PutChar(0, '\u25C6'); // Diamond character (☆ in original was char 15)
        }

        // Format position string: " line:column "
        string posStr = $" {Location.Y + 1}:{Location.X + 1} ";

        // Center the position string around the colon at position 8
        int colonPos = posStr.IndexOf(':');
        int startPos = 8 - colonPos;
        if (startPos < 0) startPos = 0;

        buf.MoveStr(startPos, posStr, color);
        WriteBuf(0, 0, Size.X, 1, buf);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if (aState == StateFlags.sfDragging)
        {
            DrawView();
        }
    }

    /// <summary>
    /// Updates the indicator with new location and modified state.
    /// </summary>
    public void SetValue(TPoint location, bool modified)
    {
        if (!Location.Equals(location) || Modified != modified)
        {
            Location = location;
            Modified = modified;
            DrawView();
        }
    }
}
