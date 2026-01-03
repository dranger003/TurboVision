namespace TurboVision.Dialogs;

/// <summary>
/// String item for building linked lists (used by TCluster).
/// </summary>
public class TSItem
{
    public string? Value { get; set; }
    public TSItem? Next { get; set; }

    public TSItem(string? value, TSItem? next = null)
    {
        Value = value;
        Next = next;
    }
}
