using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Checkbox group (multiple selection).
/// Each checkbox can be toggled independently.
/// </summary>
public class TCheckBoxes : TCluster
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TCheckBoxes";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private const string Button = " [ ] ";

    public TCheckBoxes(TRect bounds, TSItem? strings) : base(bounds, strings)
    {
    }

    public override void Draw()
    {
        DrawBox(Button, 'X');
    }

    public override bool Mark(int item)
    {
        return (Value & (1u << item)) != 0;
    }

    public override void Press(int item)
    {
        Value ^= (1u << item);
    }
}
